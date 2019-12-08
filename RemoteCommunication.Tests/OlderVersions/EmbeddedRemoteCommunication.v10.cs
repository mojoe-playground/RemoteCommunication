/*
Embedded Remote Communication

https://github.com/mojoe-playground/RemoteCommunication
https://www.nuget.org/packages/RemoteCommunication.Embedded/

Copyright (c) 2019 József Molnár, all rights reserved
Licensed under the MIT license
https://github.com/mojoe-playground/RemoteCommunication/blob/master/LICENSE

For proxy support
- Add a reference to System.Reflection.DispatchProxy nuget package
    <PackageReference Include="System.Reflection.DispatchProxy" Version="4.*" />
- Define RemoteCommunication_ProxySupport conditional compilation symbol
    <DefineConstants>$(DefineConstants);RemoteCommunication_ProxySupport</DefineConstants>

If proxy support is enabled internal types will be visible for assemblies named ProxyBuilder 
*/

#if RemoteCommunication_ProxySupport
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ProxyBuilder")]
#endif
namespace EmbeddedRemoteCommunication.v10
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Runtime.InteropServices;

    internal class BuiltInTypesSerializer : ISerializer
    {
        const byte _boolean = 1;
        const byte _byte = 2;
        const byte _char = 3;
        const byte _decimal = 4;
        const byte _double = 5;
        const byte _int16 = 6;
        const byte _int32 = 7;
        const byte _int64 = 8;
        const byte _sbyte = 9;
        const byte _single = 10;
        const byte _string = 11;
        const byte _uint16 = 12;
        const byte _uint32 = 13;
        const byte _uint64 = 14;

        const byte _null = 64;
        const byte _array = 128;       // array of bytes: _array + _byte; array of nullable bytes: _array + _null + _byte with an extra byte for every value indicating null (1) or not (0)

        public string Id => "sv";

        public bool CanSerialize(object data, SerializationManager manager) =>
            data == null
            || data is bool || data is bool[] || data is bool?[]
            || data is byte || data is byte[] || data is byte?[]
            || data is char || data is char[] || data is char?[]
            || data is decimal || data is decimal[] || data is decimal?[]
            || data is double || data is double[] || data is double?[]
            || data is short || data is short[] || data is short?[]
            || data is int || data is int[] || data is int?[]
            || data is long || data is long[] || data is long?[]
            || data is sbyte || data is sbyte[] || data is sbyte?[]
            || data is float || data is float[] || data is float?[]
            || data is string || data is string[]
            || data is ushort || data is ushort[] || data is ushort?[]
            || data is uint || data is uint[] || data is uint?[]
            || data is ulong || data is ulong[] || data is ulong?[]
            ;

        public object Deserialize(BinaryReader reader, SerializationManager manager)
        {
            var type = reader.ReadByte();
            switch (type)
            {
                case _null: return null;
                case _boolean: return reader.ReadBoolean();
                case _boolean + _array: return ReadArray<bool>(reader);
                case _boolean + _null + _array: return ReadNullableArray<bool>(reader);
                case _byte: return reader.ReadByte();
                case _byte + _array: return ReadArray<byte>(reader);
                case _byte + _null + _array: return ReadNullableArray<byte>(reader);
                case _char: return reader.ReadChar();
                case _char + _array: return ReadArrayFallback(reader, () => reader.ReadChar());
                case _char + _null + _array: return ReadNullableArrayFallback(reader, () => reader.ReadChar());
                case _decimal: return reader.ReadDecimal();
                case _decimal + _array: return ReadArrayFallback(reader, () => reader.ReadDecimal());
                case _decimal + _null + _array: return ReadNullableArrayFallback(reader, () => reader.ReadDecimal());
                case _double: return reader.ReadDouble();
                case _double + _array: return ReadArray<double>(reader);
                case _double + _null + _array: return ReadNullableArray<double>(reader);
                case _int16: return reader.ReadInt16();
                case _int16 + _array: return ReadArray<short>(reader);
                case _int16 + _null + _array: return ReadNullableArray<short>(reader);
                case _int32: return reader.ReadInt32();
                case _int32 + _array: return ReadArray<int>(reader);
                case _int32 + _null + _array: return ReadNullableArray<int>(reader);
                case _int64: return reader.ReadInt64();
                case _int64 + _array: return ReadArray<long>(reader);
                case _int64 + _null + _array: return ReadNullableArray<long>(reader);
                case _sbyte: return reader.ReadSByte();
                case _sbyte + _array: return ReadArray<sbyte>(reader);
                case _sbyte + _null + _array: return ReadNullableArray<sbyte>(reader);
                case _single: return reader.ReadSingle();
                case _single + _array: return ReadArray<float>(reader);
                case _single + _null + _array: return ReadNullableArray<float>(reader);
                case _string: return reader.ReadString();
                case _string + _null + _array: return ReadStringArray(reader);
                case _uint16: return reader.ReadUInt16();
                case _uint16 + _array: return ReadArray<ushort>(reader);
                case _uint16 + _null + _array: return ReadNullableArray<ushort>(reader);
                case _uint32: return reader.ReadUInt32();
                case _uint32 + _array: return ReadArray<uint>(reader);
                case _uint32 + _null + _array: return ReadNullableArray<uint>(reader);
                case _uint64: return reader.ReadUInt64();
                case _uint64 + _array: return ReadArray<ulong>(reader);
                case _uint64 + _null + _array: return ReadNullableArray<ulong>(reader);

                default:
                    throw new InvalidOperationException("Unknown data type: " + type);
            }
        }

        private string[] ReadStringArray(BinaryReader br)
        {
            var count = br.ReadInt32();

            var nullArray = br.ReadBytes(count / 8 + 1);
            var nulls = new BitArray(nullArray);

            var res = new string[count];
            for (var i = 0; i < count; i++)
                res[i] = nulls.Get(i) ? br.ReadString() : null;

            return res;
        }

        private T[] ReadArrayFallback<T>(BinaryReader br, Func<T> reader)
        {
            var count = br.ReadInt32();
            var res = new T[count];
            for (var i = 0; i < count; i++)
                res[i] = reader();

            return res;
        }

        private T[] ReadArray<T>(BinaryReader br)
        {
            var count = br.ReadInt32();
            var size = br.ReadInt32();
            var res = new T[count];
            var bytes = br.ReadBytes(size);
            Buffer.BlockCopy(bytes, 0, res, 0, size);

            return res;
        }

        private T?[] ReadNullableArrayFallback<T>(BinaryReader br, Func<T> reader) where T : struct
        {
            var count = br.ReadInt32();

            var nullArray = br.ReadBytes(count / 8 + 1);
            var nulls = new BitArray(nullArray);

            var res = new T?[count];
            for (var i = 0; i < count; i++)
                res[i] = nulls.Get(i) ? (T?)reader() : null;

            return res;
        }

        private T?[] ReadNullableArray<T>(BinaryReader br) where T : struct
        {
            var count = br.ReadInt32();
            var size = br.ReadInt32();

            var nullArray = br.ReadBytes(count / 8 + 1);
            var nulls = new BitArray(nullArray);

            var valueArray = new T[count];
            var bytes = br.ReadBytes(size);
            Buffer.BlockCopy(bytes, 0, valueArray, 0, size);

            var res = new T?[count];
            for (var i = 0; i < count; i++)
                res[i] = nulls.Get(i) ? (T?)valueArray[i] : null;

            return res;
        }

        public void Serialize(object data, BinaryWriter writer, SerializationManager manager)
        {
            if (data == null)
            {
                writer.Write(_null);
                return;
            }

            if (Write<bool>(_boolean, writer, data, v => writer.Write(v)) || WriteArray<bool>(_boolean, writer, data) || WriteArrayNull<bool>(_boolean, writer, data))
                return;

            if (Write<byte>(_byte, writer, data, v => writer.Write(v)) || WriteArray<byte>(_byte, writer, data) || WriteArrayNull<byte>(_byte, writer, data))
                return;

            if (Write<char>(_char, writer, data, v => writer.Write(v)) || WriteArrayFallback<char>(_char, writer, data, ch => writer.Write(ch)) || WriteArrayNullFallback<char>(_char, writer, data, ch => writer.Write(ch)))
                return;

            if (Write<decimal>(_decimal, writer, data, v => writer.Write(v)) || WriteArrayFallback<decimal>(_decimal, writer, data, v => writer.Write(v)) || WriteArrayNullFallback<decimal>(_decimal, writer, data, v => writer.Write(v)))
                return;

            if (Write<double>(_double, writer, data, v => writer.Write(v)) || WriteArray<double>(_double, writer, data) || WriteArrayNull<double>(_double, writer, data))
                return;

            if (Write<short>(_int16, writer, data, v => writer.Write(v)) || WriteArray<short>(_int16, writer, data) || WriteArrayNull<short>(_int16, writer, data))
                return;

            if (Write<int>(_int32, writer, data, v => writer.Write(v)) || WriteArray<int>(_int32, writer, data) || WriteArrayNull<int>(_int32, writer, data))
                return;

            if (Write<long>(_int64, writer, data, v => writer.Write(v)) || WriteArray<long>(_int64, writer, data) || WriteArrayNull<long>(_int64, writer, data))
                return;

            if (Write<sbyte>(_sbyte, writer, data, v => writer.Write(v)) || WriteArray<sbyte>(_sbyte, writer, data) || WriteArrayNull<sbyte>(_sbyte, writer, data))
                return;

            if (Write<float>(_single, writer, data, v => writer.Write(v)) || WriteArray<float>(_single, writer, data) || WriteArrayNull<float>(_single, writer, data))
                return;

            if (Write<string>(_string, writer, data, v => writer.Write(v)) || WriteStringArray(_string, writer, data))
                return;

            if (Write<ushort>(_uint16, writer, data, v => writer.Write(v)) || WriteArray<ushort>(_uint16, writer, data) || WriteArrayNull<ushort>(_uint16, writer, data))
                return;

            if (Write<uint>(_uint32, writer, data, v => writer.Write(v)) || WriteArray<uint>(_uint32, writer, data) || WriteArrayNull<uint>(_uint32, writer, data))
                return;

            if (Write<ulong>(_uint64, writer, data, v => writer.Write(v)) || WriteArray<ulong>(_uint64, writer, data) || WriteArrayNull<ulong>(_uint64, writer, data))
                return;

            throw new InvalidOperationException($"Unsupported data: {data} ({data.GetType()})");
        }

        private bool Write<T>(byte type, BinaryWriter bw, object value, Action<T> writer)
        {
            if (value.GetType() != typeof(T))
                return false;

            bw.Write(type);
            writer((T)value);
            return true;
        }

        private bool WriteStringArray(byte baseType, BinaryWriter bw, object value)
        {
            if (value.GetType() != typeof(string[]))
                return false;

            var array = (string[])value;

            bw.Write((byte)(baseType + _null + _array));
            bw.Write(array.Length);

            var bitArray = new BitArray(array.Length, true);
            for (var i = 0; i < array.Length; i++)
                if (array[i] == null)
                    bitArray.Set(i, false);
            var nullArray = new byte[array.Length / 8 + 1];
            bitArray.CopyTo(nullArray, 0);

            bw.Write(nullArray);

            foreach (var s in array)
                if (s != null)
                    bw.Write(s);

            return true;
        }

        private bool WriteArrayFallback<T>(byte baseType, BinaryWriter bw, object value, Action<T> writer)
        {
            if (value.GetType() != typeof(T[]))
                return false;

            var array = (T[])value;

            bw.Write((byte)(baseType + _array));
            bw.Write(array.Length);

            foreach (var i in array)
                writer(i);

            return true;
        }

        private bool WriteArray<T>(byte baseType, BinaryWriter bw, object value)
        {
            if (value.GetType() != typeof(T[]))
                return false;

            var array = (T[])value;

            bw.Write((byte)(baseType + _array));
            var size = GetElementSize(typeof(T)) * array.Length;
            bw.Write(array.Length);
            bw.Write(size);

            var bytes = new byte[size];
            Buffer.BlockCopy(array, 0, bytes, 0, size);
            bw.Write(bytes);

            return true;
        }

        private int GetElementSize(Type t)
        {
            if (t == typeof(bool))
                return 1;
            return Marshal.SizeOf(t);
        }

        private bool WriteArrayNullFallback<T>(byte baseType, BinaryWriter bw, object value, Action<T> writer) where T : struct
        {
            if (value.GetType() != typeof(T?[]))
                return false;

            var nullableArray = (T?[])value;
            var bitArray = new BitArray(nullableArray.Length, true);
            for (var i = 0; i < nullableArray.Length; i++)
                if (!nullableArray[i].HasValue)
                    bitArray.Set(i, false);
            var nullArray = new byte[nullableArray.Length / 8 + 1];
            bitArray.CopyTo(nullArray, 0);

            bw.Write((byte)(baseType + _null + _array));
            bw.Write(nullableArray.Length);

            bw.Write(nullArray);

            foreach (var i in nullableArray)
                if (i.HasValue)
                    writer(i.Value);

            return true;
        }

        private bool WriteArrayNull<T>(byte baseType, BinaryWriter bw, object value) where T : struct
        {
            if (value.GetType() != typeof(T?[]))
                return false;

            var nullableArray = (T?[])value;
            var bitArray = new BitArray(nullableArray.Length, true);
            var array = new T[nullableArray.Length];
            for (var i = 0; i < nullableArray.Length; i++)
                if (nullableArray[i].HasValue)
                    array[i] = nullableArray[i].Value;
                else
                    bitArray.Set(i, false);
            var nullArray = new byte[nullableArray.Length / 8 + 1];
            bitArray.CopyTo(nullArray, 0);

            bw.Write((byte)(baseType + _null + _array));
            bw.Write(nullableArray.Length);
            var size = GetElementSize(typeof(T)) * nullableArray.Length;
            bw.Write(size);

            bw.Write(nullArray);

            var bytes = new byte[size];
            Buffer.BlockCopy(array, 0, bytes, 0, size);
            bw.Write(bytes);

            return true;
        }
    }
}
namespace EmbeddedRemoteCommunication.v10
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;

    internal class CollectionSerializer<T> : ISerializer
    {
        public CollectionSerializer() : this(null)
        { }

        public CollectionSerializer(string id)
        {
            if (string.IsNullOrEmpty(id))
                id = typeof(T).Name;

            Id = "col:" + id;
        }

        public string Id { get; }

        public bool CanSerialize(object data, SerializationManager manager)
        {
            var l = ToList(data);
            if (l == null)
                return false;
            return l.All(i => manager.CanSerialize(i));
        }

        public object Deserialize(BinaryReader reader, SerializationManager manager)
        {
            var listType = reader.ReadInt32();
            var count = reader.ReadInt32();
            ICollection<T> res;
            if (listType == 2)
                res = new ObservableCollection<T>();
            else if (listType == 3)
                res = new Collection<T>();
            else
                res = new List<T>();

            for (var i = 0; i < count; i++)
                res.Add((T)manager.Deserialize(reader));

            if (listType == 1)
                return res.ToArray();

            return res;
        }

        public void Serialize(object data, BinaryWriter writer, SerializationManager manager)
        {
            var l = ToList(data);
            writer.Write(ListType(data));
            writer.Write(l.Count);
            foreach (var i in l)
                manager.Serialize(i, writer);
        }

        private List<T> ToList(object data)
        {
            if (data is List<T> list)
                return list;
            if (data is T[] array)
                return array.ToList();
            if (data is IEnumerable<T> col)
                return col.ToList();
            return null;
        }

        private int ListType(object data)
        {
            if (data is T[])
                return 1;
            if (data is ObservableCollection<T>)
                return 2;
            if (data is Collection<T>)
                return 3;
            return 0;
        }
    }
}
namespace EmbeddedRemoteCommunication.v10
{
    using Internal;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    internal class Communicator : CommunicatorBase
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
            if ((inShutdown || _shutdownSource != null) && _activeResponses.Count == 0 && _activeRequests.Count == 0)
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
namespace EmbeddedRemoteCommunication.v10
{
    using Internal;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal abstract class CommunicatorBase : IDisposable
    {
        private IChannel _channel;
        private SerializationManager _serializationManager = new SerializationManager();

        public CommunicatorBase(IChannel channel, string address)
        {
            Address = address;
            _channel = channel;
            _channel.Receive = Receive;
        }

        public string Address { get; private set; }

        protected bool IsOpened { get; private set; }

        public async Task Open()
        {
            await _channel.Open(Address);
            IsOpened = true;
        }

        private const int MagicHeader = 0x52434556;
        private const short EnvelopeVersionV10 = 0x0100;

        private void Receive(Stream stream)
        {
            using (var br = new BinaryReader(new WrapperStream(stream), Encoding.UTF8))
            {
                var header = br.ReadInt32();
                if (MagicHeader != header)
                    throw new InvalidOperationException("Unknown header");
                var version = br.ReadUInt16();
                if (version == EnvelopeVersionV10)
                    ProcessEnvelope((Envelope)_serializationManager.Deserialize(br));
                else
                    throw new InvalidOperationException("Unknown envelope version");
            }
        }

        private protected abstract void ProcessEnvelope(Envelope envelope);

        protected void SetSerializers(IEnumerable<ISerializer> serializers)
        {
            if (IsOpened)
                throw new InvalidOperationException("Can not set serializers after channel opened");
            var l = new List<ISerializer>(serializers);
            l.Insert(0, new EnvelopeSerializer());
            _serializationManager = new SerializationManager(l);
        }

        protected async Task Send(object data, string target, CancellationToken token)
        {
            var msg = new Envelope { From = Address, Data = data };

            if (!_serializationManager.CanSerialize(msg))
                throw new InvalidOperationException("Cannot serialize data");

            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(new WrapperStream(ms), Encoding.UTF8))
                {
                    bw.Write(MagicHeader);
                    bw.Write(EnvelopeVersionV10);

                    _serializationManager.Serialize(msg, bw);
                    ms.Position = 0;
                }

                await _channel.Send(ms, target, token).ConfigureAwait(false);
            }
        }

        #region IDisposable Support
        private bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _channel.Dispose();
                    _channel = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose() => Dispose(true);
        #endregion

        /// <summary>
        /// Wraps a stream preventing from closing in user code
        /// </summary>
        private class WrapperStream : Stream
        {
            public WrapperStream(Stream s) => Stream = s;

            private Stream Stream { get; }

            public override bool CanRead => Stream.CanRead;

            public override bool CanSeek => Stream.CanSeek;

            public override bool CanWrite => Stream.CanWrite;

            public override long Length => Stream.Length;

            public override long Position { get => Stream.Position; set => Stream.Position = value; }

            public override void Flush() => Stream.Flush();
            public override int Read(byte[] buffer, int offset, int count) => Stream.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => Stream.Seek(offset, origin);
            public override void SetLength(long value) => Stream.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => Stream.Write(buffer, offset, count);
        }
    }
}
namespace EmbeddedRemoteCommunication.v10
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class CommunicatorExtensions
    {
        public static void AddMessageHandler(this Communicator communicator, string verb, Action handler) => communicator.AddMessageHandler(verb, p => handler(), 0);
        public static void AddMessageHandler<T>(this Communicator communicator, string verb, Action<T> handler) => communicator.AddMessageHandler(verb, p => handler((T)p[0]), 1);
        public static void AddMessageHandler<T1, T2>(this Communicator communicator, string verb, Action<T1, T2> handler) => communicator.AddMessageHandler(verb, p => handler((T1)p[0], (T2)p[1]), 2);
        public static void AddMessageHandler<T1, T2, T3>(this Communicator communicator, string verb, Action<T1, T2, T3> handler) => communicator.AddMessageHandler(verb, p => handler((T1)p[0], (T2)p[1], (T3)p[2]), 3);

        public static void AddMessageHandler(this Communicator communicator, string verb, Func<Task> handler) => communicator.AddMessageHandler(verb, async p => await handler().ConfigureAwait(false), 0);
        public static void AddMessageHandler<T>(this Communicator communicator, string verb, Func<T, Task> handler) => communicator.AddMessageHandler(verb, async p => await handler((T)p[0]).ConfigureAwait(false), 1);
        public static void AddMessageHandler<T1, T2>(this Communicator communicator, string verb, Func<T1, T2, Task> handler) => communicator.AddMessageHandler(verb, async p => await handler((T1)p[0], (T2)p[1]).ConfigureAwait(false), 2);
        public static void AddMessageHandler<T1, T2, T3>(this Communicator communicator, string verb, Func<T1, T2, T3, Task> handler) => communicator.AddMessageHandler(verb, async p => await handler((T1)p[0], (T2)p[1], (T3)p[2]).ConfigureAwait(false), 3);

        public static void AddRequestHandler(this Communicator communicator, string verb, Action<CancellationToken> handler) => communicator.AddRequestHandler(verb, (p, c) => { handler(c); return Task.FromResult<object>(null); }, 0);
        public static void AddRequestHandler<TRes>(this Communicator communicator, string verb, Func<CancellationToken, TRes> handler) => communicator.AddRequestHandler(verb, (p, c) => handler(c), 0);
        public static void AddRequestHandler<T>(this Communicator communicator, string verb, Action<T, CancellationToken> handler) => communicator.AddRequestHandler(verb, (p, c) => { handler((T)p[0], c); return Task.FromResult<object>(null); }, 1);
        public static void AddRequestHandler<T, TRes>(this Communicator communicator, string verb, Func<T, CancellationToken, TRes> handler) => communicator.AddRequestHandler(verb, (p, c) => handler((T)p[0], c), 1);
        public static void AddRequestHandler<T1, T2>(this Communicator communicator, string verb, Action<T1, T2, CancellationToken> handler) => communicator.AddRequestHandler(verb, (p, c) => { handler((T1)p[0], (T2)p[1], c); return Task.FromResult<object>(null); }, 2);
        public static void AddRequestHandler<T1, T2, TRes>(this Communicator communicator, string verb, Func<T1, T2, CancellationToken, TRes> handler) => communicator.AddRequestHandler(verb, (p, c) => handler((T1)p[0], (T2)p[1], c), 2);
        public static void AddRequestHandler<T1, T2, T3>(this Communicator communicator, string verb, Action<T1, T2, T3, CancellationToken> handler) => communicator.AddRequestHandler(verb, (p, c) => { handler((T1)p[0], (T2)p[1], (T3)p[2], c); return Task.FromResult<object>(null); }, 3);
        public static void AddRequestHandler<T1, T2, T3, TRes>(this Communicator communicator, string verb, Func<T1, T2, T3, CancellationToken, TRes> handler) => communicator.AddRequestHandler(verb, (p, c) => handler((T1)p[0], (T2)p[1], (T3)p[2], c), 3);

        public static void AddRequestHandler(this Communicator communicator, string verb, Func<CancellationToken, Task> handler) => communicator.AddRequestHandler(verb, async (p, c) => { await handler(c).ConfigureAwait(false); return null; }, 0);
        public static void AddRequestHandler<TRes>(this Communicator communicator, string verb, Func<CancellationToken, Task<TRes>> handler) => communicator.AddRequestHandler(verb, async (p, c) => await handler(c).ConfigureAwait(false), 0);
        public static void AddRequestHandler<T>(this Communicator communicator, string verb, Func<T, CancellationToken, Task> handler) => communicator.AddRequestHandler(verb, async (p, c) => { await handler((T)p[0], c).ConfigureAwait(false); return null; }, 1);
        public static void AddRequestHandler<T, TRes>(this Communicator communicator, string verb, Func<T, CancellationToken, Task<TRes>> handler) => communicator.AddRequestHandler(verb, async (p, c) => await handler((T)p[0], c).ConfigureAwait(false), 1);
        public static void AddRequestHandler<T1, T2>(this Communicator communicator, string verb, Func<T1, T2, CancellationToken, Task> handler) => communicator.AddRequestHandler(verb, async (p, c) => { await handler((T1)p[0], (T2)p[1], c).ConfigureAwait(false); return null; }, 2);
        public static void AddRequestHandler<T1, T2, TRes>(this Communicator communicator, string verb, Func<T1, T2, CancellationToken, Task<TRes>> handler) => communicator.AddRequestHandler(verb, async (p, c) => await handler((T1)p[0], (T2)p[1], c).ConfigureAwait(false), 2);
        public static void AddRequestHandler<T1, T2, T3>(this Communicator communicator, string verb, Func<T1, T2, T3, CancellationToken, Task> handler) => communicator.AddRequestHandler(verb, async (p, c) => { await handler((T1)p[0], (T2)p[1], (T3)p[2], c).ConfigureAwait(false); return null; }, 3);
        public static void AddRequestHandler<T1, T2, T3, TRes>(this Communicator communicator, string verb, Func<T1, T2, T3, CancellationToken, Task<TRes>> handler) => communicator.AddRequestHandler(verb, async (p, c) => await handler((T1)p[0], (T2)p[1], (T3)p[2], c).ConfigureAwait(false), 3);

    }
}
namespace EmbeddedRemoteCommunication.v10
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class CommunicatorServiceHandlingExtensions
    {
        private static readonly Dictionary<Type, PropertyInfo> _taskResults = new Dictionary<Type, PropertyInfo>();

#if RemoteCommunication_ProxySupport
        internal class CommunicatorProxy : DispatchProxy
        {
            private static readonly Dictionary<Type, MethodInfo> _sendRequests = new Dictionary<Type, MethodInfo>();

            internal Communicator Communicator { get; set; }
            internal string TargetAddress { get; set; }
            internal string VerbPrefix { get; set; }

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                var cancellationTokenIndex = Array.FindIndex(args, p => p != null && p.GetType() == typeof(CancellationToken));
                var token = default(CancellationToken);
                if (cancellationTokenIndex >= 0)
                {
                    token = (CancellationToken)args[cancellationTokenIndex];
                    var l = args.ToList();
                    l.RemoveAt(cancellationTokenIndex);
                    args = l.ToArray();
                }

                if (targetMethod.ReturnType == typeof(Task))
                    return Communicator.SendRequest(TargetAddress, VerbPrefix + targetMethod.Name, token, args);
                if (targetMethod.ReturnType == typeof(void))
                {
                    Communicator.SendRequest(TargetAddress, VerbPrefix + targetMethod.Name, token, args).Wait();
                    return null;
                }

                var task = false;
                var returnType = targetMethod.ReturnType;

                if (targetMethod.ReturnType.IsGenericType && targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    task = true;
                    returnType = targetMethod.ReturnType.GetGenericArguments().Single();
                }

                MethodInfo sendRequest;
                lock (_sendRequests)
                    if (!_sendRequests.TryGetValue(returnType, out sendRequest))
                    {
                        var openMethod = typeof(Communicator).GetMethods().Where(m => m.Name == "SendRequest" && m.IsGenericMethod && m.GetParameters().Any(p => p.ParameterType == typeof(CancellationToken))).Single();
                        sendRequest = openMethod.MakeGenericMethod(returnType);
                        _sendRequests[returnType] = sendRequest;
                    }

                var res = sendRequest.Invoke(Communicator, new object[] { TargetAddress, VerbPrefix + targetMethod.Name, token, args });

                if (task)
                    return res;

                PropertyInfo resultProperty;
                lock (_taskResults)
                    if (!_taskResults.TryGetValue(returnType, out resultProperty))
                    {
                        var taskType = typeof(Task<>).MakeGenericType(returnType);

                        resultProperty = taskType.GetProperty("Result");
                        _taskResults[returnType] = resultProperty;
                    }

                return resultProperty.GetValue(res);
            }
        }

        public static T CreateProxy<T>(this Communicator communicator, string targetAddress, string verbPrefix = null)
        {
            var res = DispatchProxy.Create<T, CommunicatorProxy>();
            var proxy = (CommunicatorProxy)((object)res);
            proxy.Communicator = communicator;
            proxy.TargetAddress = targetAddress;
            proxy.VerbPrefix = verbPrefix;

            return res;
        }
#endif

        public static void AddSingletonService<T>(this Communicator communicator, T service, string verbPrefix = null)
        {
            foreach (var m in typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                var prs = m.GetParameters();
                var paramsCount = prs.Length;
                var cancellationTokenIndex = Array.FindIndex(prs, p => p.ParameterType == typeof(CancellationToken));
                if (cancellationTokenIndex >= 0)
                    paramsCount--;

                if (m.ReturnType == typeof(Task))
                    communicator.AddRequestHandler(verbPrefix + m.Name, async (p, ct) =>
                    {
                        await ((Task)m.Invoke(service, AddCancellationToken(p, cancellationTokenIndex, ct))).ConfigureAwait(false);
                        return null;
                    }, paramsCount);
                else if (m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    PropertyInfo resultProperty;
                    lock (_taskResults)
                        if (!_taskResults.TryGetValue(m.ReturnType, out resultProperty))
                        {
                            resultProperty = m.ReturnType.GetProperty("Result");
                            _taskResults[m.ReturnType] = resultProperty;
                        }

                    communicator.AddRequestHandler(verbPrefix + m.Name, async (p, ct) =>
                    {
                        var result = m.Invoke(service, AddCancellationToken(p, cancellationTokenIndex, ct));
                        var task = (Task)result;
                        await task.ConfigureAwait(false);
                        return resultProperty.GetValue(result);
                    }, paramsCount);
                }
                else
                    communicator.AddRequestHandler(verbPrefix + m.Name, (p, ct) => m.Invoke(service, AddCancellationToken(p, cancellationTokenIndex, ct)), paramsCount);
            }
        }

        private static object[] AddCancellationToken(object[] parameters, int cancellationTokenIndex, CancellationToken token)
        {
            if (cancellationTokenIndex < 0)
                return parameters;
            var l = parameters.ToList();
            l.Insert(cancellationTokenIndex, token);
            return l.ToArray();
        }
    }
}
namespace EmbeddedRemoteCommunication.v10
{
    using System.IO;

    internal class DataContractSerializer<T> : ISerializer
    {
        public DataContractSerializer() : this(null, null)
        { }

        public DataContractSerializer(System.Runtime.Serialization.DataContractSerializer serializer) : this(serializer, null)
        { }

        public DataContractSerializer(string id) : this(null, id)
        { }

        public DataContractSerializer(System.Runtime.Serialization.DataContractSerializer serializer, string id)
        {
            if (serializer == null)
                serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(T));

            if (string.IsNullOrEmpty(id))
                id = typeof(T).Name;
            Id = "dc:" + id;

            Serializer = serializer;

            using (var ms = new MemoryStream())
                serializer.WriteObject(ms, null);
        }

        public string Id { get; }

        private System.Runtime.Serialization.DataContractSerializer Serializer { get; }

        public bool CanSerialize(object data, SerializationManager manager) => data is T;
        public object Deserialize(BinaryReader reader, SerializationManager manager) => (T)Serializer.ReadObject(reader.BaseStream);
        public void Serialize(object data, BinaryWriter writer, SerializationManager manager) => Serializer.WriteObject(writer.BaseStream, data);
    }
}
namespace EmbeddedRemoteCommunication.v10
{
    using System;
    using System.Runtime.Serialization;

    internal class FaultException : Exception
    {
        public FaultException()
        {
        }

        public FaultException(string message) : base(message)
        {
        }

        public FaultException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FaultException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public static FaultException WrapException(Exception exception) => exception == null ? null : new FaultException(exception.GetType().FullName, exception.Message, exception.StackTrace, WrapException(exception.InnerException));

        public FaultException(string type, string message, string callStack, Exception innerException) : base(message, innerException)
        {
            Type = type;
            CallStack = callStack;
        }

        public string Type { get; }
        public string CallStack { get; }
    }
}
namespace EmbeddedRemoteCommunication.v10
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    internal interface IChannel : IDisposable
    {
        Action<Stream> Receive { get; set; }
        Task Open(string address);
        Task Send(Stream message, string address, CancellationToken token);
    }
}
namespace EmbeddedRemoteCommunication.v10.Internal
{
    internal class Envelope
    {
        public string From { get; set; }
        public object Data { get; set; }
    }
}
namespace EmbeddedRemoteCommunication.v10.Internal
{
    using System.IO;

    internal class EnvelopeSerializer : ISerializer
    {
        public string Id => "env";

        public bool CanSerialize(object data, SerializationManager manager) => data is Envelope env && manager.CanSerialize(env.Data);

        public object Deserialize(BinaryReader reader, SerializationManager manager) =>
            new Envelope
            {
                From = reader.ReadString(),
                Data = manager.Deserialize(reader)
            };

        public void Serialize(object data, BinaryWriter writer, SerializationManager manager)
        {
            var env = (Envelope)data;

            writer.Write(env.From);
            manager.Serialize(env.Data, writer);
        }
    }
}
namespace EmbeddedRemoteCommunication.v10.Internal
{
    using System;
    using System.IO;

    internal class FaultSerializer : ISerializer
    {
        public string Id => "f";

        public bool CanSerialize(object data, SerializationManager manager) => data is FaultException f && manager.CanSerialize(f.InnerException);

        public object Deserialize(BinaryReader reader, SerializationManager manager) => new FaultException(reader.ReadString(), reader.ReadString(), reader.ReadString(), (Exception)manager.Deserialize(reader));

        public void Serialize(object data, BinaryWriter writer, SerializationManager manager)
        {
            var fault = (FaultException)data;
            writer.Write(fault.Type);
            writer.Write(fault.Message);
            writer.Write(fault.CallStack);
            manager.Serialize(fault.InnerException, writer);
        }
    }
}
namespace EmbeddedRemoteCommunication.v10.Internal
{
    internal class Message
    {
        public string Verb { get; set; }
        public object[] Parameters { get; set; }
    }
}
namespace EmbeddedRemoteCommunication.v10.Internal
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal class MessageSerializer : ISerializer
    {
        public string Id => "msg";

        public bool CanSerialize(object data, SerializationManager manager) => data is Message msg && msg.Parameters.All(p => manager.CanSerialize(p));

        public object Deserialize(BinaryReader reader, SerializationManager manager)
        {
            var r = new Message { Verb = reader.ReadString() };

            var parameters = new List<object>();
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
                parameters.Add(manager.Deserialize(reader));

            r.Parameters = parameters.ToArray();

            return r;
        }

        public void Serialize(object data, BinaryWriter writer, SerializationManager manager)
        {
            var msg = (Message)data;

            writer.Write(msg.Verb);
            writer.Write(msg.Parameters.Length);

            foreach (var p in msg.Parameters)
                manager.Serialize(p, writer);
        }
    }
}
namespace EmbeddedRemoteCommunication.v10.Internal
{
    using System;

    internal class Request
    {
        public Guid RequestId { get; set; }
        public string Verb { get; set; }
        public object[] Parameters { get; set; }
    }
}
namespace EmbeddedRemoteCommunication.v10.Internal
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal class RequestSerializer : ISerializer
    {
        public string Id => "req";

        public bool CanSerialize(object data, SerializationManager manager) => data is Request r && r.Parameters.All(p => manager.CanSerialize(p));

        public object Deserialize(BinaryReader reader, SerializationManager manager)
        {
            var r = new Request
            {
                RequestId = new Guid(reader.ReadBytes(16)),
                Verb = reader.ReadString()
            };

            var parameters = new List<object>();
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
                parameters.Add(manager.Deserialize(reader));

            r.Parameters = parameters.ToArray();

            return r;
        }

        public void Serialize(object data, BinaryWriter writer, SerializationManager manager)
        {
            var r = (Request)data;

            writer.Write(r.RequestId.ToByteArray());
            writer.Write(r.Verb);
            writer.Write(r.Parameters.Length);

            foreach (var p in r.Parameters)
                manager.Serialize(p, writer);
        }
    }
}
namespace EmbeddedRemoteCommunication.v10.Internal
{
    using System;

    internal enum ResultType { None, Result, Cancelled, Faulted, KeepAlive }

    internal class Response
    {
        public Guid RequestId { get; set; }
        public ResultType ResultType { get; set; }
        public object Result { get; set; }
    }
}
namespace EmbeddedRemoteCommunication.v10.Internal
{
    using System;
    using System.IO;
    using System.Text;

    internal class ResponseSerializer : ISerializer
    {
        public string Id => "res";

        public bool CanSerialize(object data, SerializationManager manager) => data is Response r && manager.CanSerialize(r.Result);

        public object Deserialize(BinaryReader reader, SerializationManager manager) =>
            new Response
            {
                RequestId = new Guid(reader.ReadBytes(16)),
                ResultType = (ResultType)reader.ReadInt32(),
                Result = manager.Deserialize(reader)
            };

        public void Serialize(object data, BinaryWriter writer, SerializationManager manager)
        {
            var res = (Response)data;
            writer.Write(res.RequestId.ToByteArray());
            writer.Write((int)res.ResultType);

            manager.Serialize(res.Result, writer);
        }
    }
}
namespace EmbeddedRemoteCommunication.v10
{
    using System.IO;

    internal interface ISerializer
    {
        string Id { get; }
        bool CanSerialize(object data, SerializationManager manager);
        void Serialize(object data, BinaryWriter writer, SerializationManager manager);
        object Deserialize(BinaryReader reader, SerializationManager manager);
    }
}
namespace EmbeddedRemoteCommunication.v10
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Pipes;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class NetPipeChannel : IChannel
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
namespace EmbeddedRemoteCommunication.v10
{
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    internal class ObjectSerializer<TInformation, TConcrete> : ISerializer where TConcrete : TInformation, new()
    {
        public ObjectSerializer() : this(null)
        { }

        public ObjectSerializer(string id)
        {
            if (string.IsNullOrEmpty(id))
                id = typeof(TInformation).Name;
            Id = "obj:" + id;
        }

        public string Id { get; }

        private static Dictionary<string, PropertyInfo> Properties { get; } = typeof(TInformation).GetProperties().Where(p => p.CanRead).ToDictionary(p => p.Name);
        private static Dictionary<string, PropertyInfo> TargetProperties { get; } = typeof(TConcrete).GetProperties().Where(p => p.CanWrite).ToDictionary(p => p.Name);
        private static Dictionary<string, PropertyInfo> TargetCollectionProperties { get; } = typeof(TConcrete).GetProperties().Where(CollectionInfo).ToDictionary(p => p.Name);

        private static bool CollectionInfo(PropertyInfo property)
        {
            if (property.PropertyType == typeof(string))
                return false;

            return typeof(IEnumerable).IsAssignableFrom(property.PropertyType);
        }

        private IEnumerable<(string name, object value)> GetProperties(TInformation obj)
        {
            foreach (var p in Properties)
                if (SerializeAllProperties || TargetProperties.ContainsKey(p.Key) || TargetCollectionProperties.ContainsKey(p.Key))
                    yield return (p.Key, p.Value.GetValue(obj));
        }

        public bool CanSerialize(object data, SerializationManager manager) => data is TInformation t && GetProperties(t).All(p => manager.CanSerialize(p.value));

        public bool SerializeAllProperties { get; set; }

        public object Deserialize(BinaryReader reader, SerializationManager manager)
        {
            var res = new TConcrete();

            while (true)
            {
                var name = reader.ReadString();
                if (string.IsNullOrEmpty(name))
                    break;

                var value = manager.Deserialize(reader);

                if (TargetProperties.TryGetValue(name, out var pi))
                    pi.SetValue(res, value);
                else if (value is IEnumerable enumerable && TargetCollectionProperties.TryGetValue(name, out pi))
                {
                    var collection = pi.GetValue(res);

                    if (collection == null)
                        continue;

                    foreach (var i in value.GetType().GetInterfaces())
                        if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        {
                            var add = collection.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).Where(m => m.Name == "Add" && m.GetParameters() is ParameterInfo[] parameters && parameters.Length == 1 && parameters[0].ParameterType == i.GenericTypeArguments[0]).FirstOrDefault();
                            if (add == null)
                                continue;

                            var enumeratorMethod = i.GetMethod("GetEnumerator");
                            var enumerator = (IEnumerator)enumeratorMethod.Invoke(value, null);
                            while (enumerator.MoveNext())
                                add.Invoke(collection, new object[] { enumerator.Current });

                            break;
                        }
                }
            }

            return res;
        }

        public void Serialize(object data, BinaryWriter writer, SerializationManager manager)
        {
            foreach (var (name, value) in GetProperties((TInformation)data))
            {
                writer.Write(name);
                manager.Serialize(value, writer);
            }

            writer.Write(string.Empty);
        }
    }

    internal class ObjectSerializer<T> : ObjectSerializer<T, T> where T : new()
    { }
}
namespace EmbeddedRemoteCommunication.v10
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal class SerializationManager
    {
        public SerializationManager(params ISerializer[] serializers)
        {
            Serializers = serializers;
        }

        public SerializationManager(IEnumerable<ISerializer> serializers)
        {
            Serializers = new List<ISerializer>(serializers);
        }

        public IReadOnlyList<ISerializer> Serializers { get; }

        public object Deserialize(BinaryReader reader)
        {
            var sid = reader.ReadString();

            var serializer = Serializers.FirstOrDefault(s => s.Id == sid);
            if (serializer == null)
                throw new InvalidOperationException($"Unknown serializer {sid}");

            return serializer.Deserialize(reader, this);
        }

        public void Serialize(object data, BinaryWriter writer)
        {
            var targetSerializer = Serializers.FirstOrDefault(s => s.CanSerialize(data, this));
            if (targetSerializer == null)
                throw new InvalidOperationException($"Serializer not found for object {data} ({data?.GetType()})");

            writer.Write(targetSerializer.Id);
            targetSerializer.Serialize(data, writer, this);
        }

        public bool CanSerialize(object data) => Serializers.FirstOrDefault(s => s.CanSerialize(data, this)) != null;
    }
}
namespace EmbeddedRemoteCommunication.v10
{
    using System.IO;

    internal class XmlSerializer<T> : ISerializer
    {
        public XmlSerializer() : this(null, null)
        { }

        public XmlSerializer(System.Xml.Serialization.XmlSerializer serializer) : this(serializer, null)
        { }

        public XmlSerializer(string id) : this(null, id)
        { }

        public XmlSerializer(System.Xml.Serialization.XmlSerializer serializer, string id)
        {
            if (serializer == null)
                serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

            if (string.IsNullOrEmpty(id))
                id = typeof(T).Name;
            Id = "xml:" + id;

            Serializer = serializer;
        }

        public string Id { get; }

        private System.Xml.Serialization.XmlSerializer Serializer { get; }

        public bool CanSerialize(object data, SerializationManager manager) => data is T;
        public object Deserialize(BinaryReader reader, SerializationManager manager) => (T)Serializer.Deserialize(reader.BaseStream);
        public void Serialize(object data, BinaryWriter writer, SerializationManager manager) => Serializer.Serialize(writer.BaseStream, data);
    }
}
