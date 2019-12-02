namespace RemoteCommunication.Internal
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
