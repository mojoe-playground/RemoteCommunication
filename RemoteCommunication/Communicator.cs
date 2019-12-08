namespace RemoteCommunication
{
    using Internal;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class Communicator : CommunicatorBase
    {
        private readonly Dictionary<string, (Func<object[], Task> handler, int paramsCount)> _messageHandlers = new Dictionary<string, (Func<object[], Task> handler, int paramsCount)>();
        private readonly Dictionary<string, (Func<object[], CancellationToken, Task<object>> handler, int paramsCount)> _requestHandlers = new Dictionary<string, (Func<object[], CancellationToken, Task<object>> handler, int paramsCount)>();
        private readonly Dictionary<Guid, RequestData> _activeRequests = new Dictionary<Guid, RequestData>();
        private readonly Dictionary<Guid, RequestData> _activeResponses = new Dictionary<Guid, RequestData>();
        private TaskCompletionSource<bool> _shutdownSource;
        private readonly object _activeRequestsResponsesLock = new object();

        public Communicator(IChannel channel, string address, params ISerializer[] serializers) : base(channel, address)
        {
            var sr = new List<ISerializer> { new RequestSerializer(), new ResponseSerializer(), new MessageSerializer(), new FaultSerializer() };
            sr.AddRange(serializers);
            SetSerializers(sr);
        }

        public TimeSpan AliveCheckInterval { get; set; } = TimeSpan.FromSeconds(10);

        public void AddMessageHandler(string verb, Action<object[]> handler, int paramsCount) => AddMessageHandler(verb, p => { handler.Invoke(p); return Task.CompletedTask; }, paramsCount);

        public void AddMessageHandler(string verb, Func<object[], Task> handler, int paramsCount)
        {
            lock (_messageHandlers)
                _messageHandlers[verb] = (handler, paramsCount);
        }

        public void AddRequestHandler(string verb, Func<object[], CancellationToken, object> handler, int paramsCount) => AddRequestHandler(verb, (p, c) => Task.FromResult(handler.Invoke(p, c)), paramsCount);

        public void AddRequestHandler(string verb, Func<object[], CancellationToken, Task<object>> handler, int paramsCount)
        {
            lock (_requestHandlers)
                _requestHandlers[verb] = (handler, paramsCount);
        }

        public Task SendMessage(string address, string verb, params object[] parameters) => SendMessage(address, verb, CancellationToken.None, parameters);
        public Task SendMessage(string address, string verb, CancellationToken token, params object[] parameters) => Send(new Message { Verb = verb, Parameters = parameters }, address, token);

        public Task SendRequest(string address, string verb, params object[] parameters) => SendRequest(address, verb, CancellationToken.None, parameters);
        public Task SendRequest(string address, string verb, CancellationToken token, params object[] parameters) => SendRequest<object>(address, verb, token, parameters);
        public Task<T> SendRequest<T>(string address, string verb, params object[] parameters) => SendRequest<T>(address, verb, CancellationToken.None, parameters);
        public async Task<T> SendRequest<T>(string address, string verb, CancellationToken token, params object[] parameters)
        {
            if (!IsOpened)
                throw new InvalidOperationException("Communication channel is not open");

            token.ThrowIfCancellationRequested();
            /*
             Cancellation: Send __Cancel__ to address, return as cancelled if got response with Cancelled or timed out
             Timeout: Send __IsAlive__ to address, if not got KeepAlive in a time period throw exception
             Exception: If receive Faulted, throw exception
             Return with result if receive Result 
             */

            var id = Guid.NewGuid();
            var req = new Request { RequestId = id, Verb = verb, Parameters = parameters };
            var data = new RequestData { Result = new TaskCompletionSource<object>(), Alive = true };

            lock (_activeRequestsResponsesLock)
                _activeRequests[id] = data;

            try
            {
                using (var ctr = token.Register(() =>
                {
                    _ = Send(new Request { RequestId = id, Verb = "__Cancel__", Parameters = Array.Empty<object>() }, address, CancellationToken.None);
                }))
                {
                    await Send(req, address, token).ConfigureAwait(false);

                    var result = data.Result.Task;
                    while (true)
                    {
                        if (await Task.WhenAny(result, Task.Delay((int)(AliveCheckInterval.TotalMilliseconds / 2))).ConfigureAwait(false) == result)
                        {
                            if (result.IsCanceled)
                                token.ThrowIfCancellationRequested();

                            if (result.IsFaulted)
                                throw data.Fault;

                            if (result.IsCompleted)
                                return (T)result.Result;
                        }
                        else
                        {
                            if (!data.Alive)
                                throw new TimeoutException();

                            data.Alive = false;
                            _ = Send(new Request { RequestId = id, Verb = "__KeepAlive__", Parameters = Array.Empty<object>() }, address, CancellationToken.None);
                        }
                    }
                }
            }
            finally
            {
                lock (_activeRequestsResponsesLock)
                {
                    _activeRequests.Remove(id);
                    CheckShutdown();
                }
            }
        }

        private bool CheckShutdown(bool inShutdown = false)
        {
            if ((inShutdown || _shutdownSource!=null) && _activeResponses.Count == 0 && _activeRequests.Count == 0)
            {
                _shutdownSource?.TrySetResult(true);
                Dispose();
                return true;
            }

            return false;
        }

        public async Task Shutdown()
        {
            TaskCompletionSource<bool> tcl;
            lock (_activeRequestsResponsesLock)
            {
                if (CheckShutdown(true))
                    return;

                if (_shutdownSource == null)
                    _shutdownSource = new TaskCompletionSource<bool>();

                tcl = _shutdownSource;
            }

            await tcl.Task.ConfigureAwait(false);
            Dispose();
        }

        private protected override void ProcessEnvelope(Envelope envelope)
        {
            if (envelope.Data is Message msg)
                Task.Run(async () => await ProcessMessage(msg).ConfigureAwait(false));
            else if (envelope.Data is Request req)
                Task.Run(async () => await ProcessRequest(req, envelope.From).ConfigureAwait(false));
            else if (envelope.Data is Response res)
                Task.Run(() => ProcessRespose(res));
            else
                System.Diagnostics.Debug.WriteLine($"Unknown envelope data {envelope.Data} ({envelope.Data?.GetType()})");
        }

        private async Task ProcessMessage(Message msg)
        {
            Func<object[], Task> handler = null;
            int paramsCount = 0;
            lock (_messageHandlers)
                if (!_messageHandlers.TryGetValue(msg.Verb, out var h))
                    throw new InvalidOperationException("Unknown verb: " + msg.Verb);
                else
                {
                    handler = h.handler;
                    paramsCount = h.paramsCount;
                }

            if (msg.Parameters.Length != paramsCount)
                throw new InvalidOperationException($"Invalid parameter count for verb {msg.Verb} - expected {paramsCount}, actual: {msg.Parameters.Length}");

            await handler(msg.Parameters).ConfigureAwait(false);
        }

        private async Task ProcessRequest(Request req, RequestData data, string from)
        {
            switch (req.Verb)
            {
                case "__Cancel__":
                    data.Cancellation.Cancel();
                    break;

                case "__KeepAlive__":
                    await Send(new Response { RequestId = req.RequestId, ResultType = ResultType.KeepAlive }, from, CancellationToken.None).ConfigureAwait(false);
                    break;

                default:
                    try
                    {
                        Func<object[], CancellationToken, Task<object>> handler = null;
                        int paramsCount = 0;
                        lock (_requestHandlers)
                            if (!_requestHandlers.TryGetValue(req.Verb, out var h))
                                throw new InvalidOperationException("Unknown verb: " + req.Verb);
                            else
                            {
                                handler = h.handler;
                                paramsCount = h.paramsCount;
                            }

                        if (req.Parameters.Length != paramsCount)
                            throw new InvalidOperationException($"Invalid parameter count for verb {req.Verb} - expected {paramsCount}, actual: {req.Parameters.Length}");

                        var res = await handler(req.Parameters, data.Cancellation.Token).ConfigureAwait(false);
                        await Send(new Response { RequestId = req.RequestId, ResultType = ResultType.Result, Result = res }, from, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        await Send(new Response { RequestId = req.RequestId, ResultType = ResultType.Cancelled }, from, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        await Send(new Response { RequestId = req.RequestId, ResultType = ResultType.Faulted, Result = FaultException.WrapException(e) }, from, CancellationToken.None).ConfigureAwait(false);
                    }

                    break;
            }
        }

        private void ProcessRespose(Response res)
        {
            RequestData data;
            lock (_activeRequestsResponsesLock)
                if (!_activeRequests.TryGetValue(res.RequestId, out data))
                    return;

            switch (res.ResultType)
            {
                case ResultType.KeepAlive:
                    data.Alive = true;
                    return;

                case ResultType.Cancelled:
                    data.Result.SetCanceled();
                    return;

                case ResultType.Faulted:
                    data.Fault = (FaultException)res.Result;
                    data.Result.SetException(data.Fault);
                    return;

                case ResultType.Result:
                    data.Result.SetResult(res.Result);
                    return;
            }
        }

        private async Task ProcessRequest(Request req, string from)
        {
            RequestData data;
            lock (_activeRequestsResponsesLock)
                if (!_activeResponses.TryGetValue(req.RequestId, out data))
                {
                    data = new RequestData() { Cancellation = new CancellationTokenSource(), Counter = 1 };
                    _activeResponses.Add(req.RequestId, data);
                }
                else
                    data.Counter++;

            try
            {
                await ProcessRequest(req, data, from).ConfigureAwait(false);
            }
            finally
            {
                lock (_activeRequestsResponsesLock)
                {
                    data.Counter--;
                    if (data.Counter == 0)
                    {
                        data.Cancellation.Dispose();
                        _activeResponses.Remove(req.RequestId);
                    }

                    CheckShutdown();
                }
            }
        }

        private class RequestData
        {
            public bool Alive { get; set; }
            public int Counter { get; set; }
            public CancellationTokenSource Cancellation { get; set; }
            public TaskCompletionSource<object> Result { get; set; }
            public Exception Fault { get; set; }
        }
    }
}
