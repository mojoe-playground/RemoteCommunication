namespace RemoteCommunication
{
    using System.IO;

    public class XmlSerializer<T> : ISerializer
    {
        public XmlSerializer() : this(null, null)
        { }

        public XmlSerializer(System.Xml.Serialization.XmlSerializer serializer) : this(serializer, null)
        { }

        public XmlSerializer(string id) : this(null, id)
        { }

        public XmlSerializer(System.Xml.Serialization.XmlSerializer serializer, string id)
        {
            if (serializer == null)
                serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

            if (string.IsNullOrEmpty(id))
                id = typeof(T).Name;
            Id = "xml:" + id;

            Serializer = serializer;
        }

        public string Id { get; }

        private System.Xml.Serialization.XmlSerializer Serializer { get; }

        public bool CanSerialize(object data, SerializationManager manager) => data is T;
        public object Deserialize(BinaryReader reader, SerializationManager manager) => (T)Serializer.Deserialize(reader.BaseStream);
        public void Serialize(object data, BinaryWriter writer, SerializationManager manager) => Serializer.Serialize(writer.BaseStream, data);
    }
}
