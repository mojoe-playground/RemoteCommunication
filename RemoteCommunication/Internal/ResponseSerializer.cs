namespace RemoteCommunication.Internal
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
