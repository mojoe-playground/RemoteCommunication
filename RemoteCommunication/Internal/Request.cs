namespace RemoteCommunication.Internal
{
    using System;

    internal class Request
    {
        public Guid RequestId { get; set; }
        public string Verb { get; set; }
        public object[] Parameters { get; set; }
    }
}
