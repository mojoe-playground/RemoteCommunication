namespace RemoteCommunication
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Pipes;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class NetPipeChannel : IChannel
    {
        private readonly List<NamedPipeServerStream> _servers = new List<NamedPipeServerStream>();

        public NetPipeChannel(int numberOfServers = 1)
        {
            NumberOfServers = numberOfServers;
        }

        public int NumberOfServers { get; }
        public Action<Stream> Receive { get; set; }

        private bool _disposed;
        public void Dispose()
        {
            lock (_servers)
                if (!_disposed)
                {
                    _disposed = true;
                    foreach (var s in _servers)
                        s.Dispose();
                    _servers.Clear();
                }
        }
        public Task Open(string address)
        {
            lock (_servers)
                for (var i = 0; i < NumberOfServers; i++)
                {
                    var s = new NamedPipeServerStream(address, PipeDirection.In, NumberOfServers, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    s.BeginWaitForConnection(WaitForConnectionCallBack, i);
                    _servers.Add(s);
                }

            return Task.CompletedTask;
        }

        private void WaitForConnectionCallBack(IAsyncResult result)
        {
            NamedPipeServerStream s;
            int idx;

            lock (_servers)
            {
                if (_disposed)
                    return;

                idx = (int)result.AsyncState;
                s = _servers[idx];
            }

            s.EndWaitForConnection(result);

            try
            { Receive?.Invoke(s); }
            catch { }

            try
            {
                s.Disconnect();
                s.BeginWaitForConnection(WaitForConnectionCallBack, idx);
            }
            catch (ObjectDisposedException) { }
        }

        public async Task Send(Stream message, string address, CancellationToken token)
        {
            using (var client = new NamedPipeClientStream(".", address, PipeDirection.Out, PipeOptions.None))
            {
                await client.ConnectAsync(1000, token).ConfigureAwait(false);
                await message.CopyToAsync(client, 81920, token).ConfigureAwait(false);
            }
        }
    }
}
