namespace RemoteCommunication
{
    using Internal;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class CommunicatorBase : IDisposable
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
