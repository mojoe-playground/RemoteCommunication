namespace RemoteCommunication.Internal
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
