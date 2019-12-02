namespace RemoteCommunication.Internal
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
