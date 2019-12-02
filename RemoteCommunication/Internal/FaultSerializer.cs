namespace RemoteCommunication.Internal
{
    using System;
    using System.IO;

    internal class FaultSerializer : ISerializer
    {
        public string Id => "f";

        public bool CanSerialize(object data, SerializationManager manager) => data is FaultException f && manager.CanSerialize(f.InnerException);

        public object Deserialize(BinaryReader reader, SerializationManager manager) => new FaultException(reader.ReadString(), reader.ReadString(), reader.ReadString(), (Exception)manager.Deserialize(reader));

        public void Serialize(object data, BinaryWriter writer, SerializationManager manaager)
        {
            var fault = (FaultException)data;
            writer.Write(fault.Type);
            writer.Write(fault.Message);
            writer.Write(fault.CallStack);
            manaager.Serialize(fault.InnerException, writer);
        }
    }
}
