namespace RemoteCommunication
{
    using System;
    using System.Runtime.Serialization;

    public class FaultException : Exception
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
