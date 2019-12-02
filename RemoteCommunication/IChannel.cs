namespace RemoteCommunication
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IChannel : IDisposable
    {
        Action<Stream> Receive { get; set; }
        Task Open(string address);
        Task Send(Stream message, string address, CancellationToken token);
    }
}
