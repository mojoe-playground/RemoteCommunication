namespace RemoteCommunication
{
    using System.IO;

    public class DataContractSerializer<T> : ISerializer
    {
        public DataContractSerializer() : this(null, null)
        { }

        public DataContractSerializer(System.Runtime.Serialization.DataContractSerializer serializer) : this(serializer, null)
        { }

        public DataContractSerializer(string id) : this(null, id)
        { }

        public DataContractSerializer(System.Runtime.Serialization.DataContractSerializer serializer, string id)
        {
            if (serializer == null)
                serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(T));

            if (string.IsNullOrEmpty(id))
                id = typeof(T).Name;
            Id = "dc:" + id;

            Serializer = serializer;

            using (var ms = new MemoryStream())
                serializer.WriteObject(ms, null);
        }

        public string Id { get; }

        private System.Runtime.Serialization.DataContractSerializer Serializer { get; }

        public bool CanSerialize(object data, SerializationManager manager) => data is T;
        public object Deserialize(BinaryReader reader, SerializationManager manager) => (T)Serializer.ReadObject(reader.BaseStream);
        public void Serialize(object data, BinaryWriter writer, SerializationManager manager) => Serializer.WriteObject(writer.BaseStream, data);
    }
}
