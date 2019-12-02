namespace RemoteCommunication.Internal
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
