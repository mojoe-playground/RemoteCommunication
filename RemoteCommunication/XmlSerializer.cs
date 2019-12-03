namespace RemoteCommunication
{
    using System.IO;

    public class XmlSerializer<T> : ISerializer
    {
        public string Id => "xml:" + typeof(T).Name;

        public XmlSerializer() : this(null)
        { }

        public XmlSerializer(System.Xml.Serialization.XmlSerializer serializer)
        {
            if (serializer == null)
                serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

            Serializer = serializer;
        }

        private System.Xml.Serialization.XmlSerializer Serializer { get; }

        public bool CanSerialize(object data, SerializationManager manager) => data is T;
        public object Deserialize(BinaryReader reader, SerializationManager manager) => (T)Serializer.Deserialize(reader.BaseStream);
        public void Serialize(object data, BinaryWriter writer, SerializationManager manager) => Serializer.Serialize(writer.BaseStream, data);
    }
}
