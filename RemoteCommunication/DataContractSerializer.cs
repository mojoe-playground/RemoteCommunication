namespace RemoteCommunication
{
    using System.IO;

    public class DataContractSerializer<T> : ISerializer
    {
        public string Id => "dc:" + typeof(T).Name;

        public DataContractSerializer() : this(null)
        { }

        public DataContractSerializer(System.Runtime.Serialization.DataContractSerializer serializer)
        {
            if (serializer == null)
                serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(T));

            Serializer = serializer;

            using (var ms = new MemoryStream())
                serializer.WriteObject(ms, null);
        }

        private System.Runtime.Serialization.DataContractSerializer Serializer { get; }

        public bool CanSerialize(object data, SerializationManager manager) => data is T;
        public object Deserialize(BinaryReader reader, SerializationManager manager) => (T)Serializer.ReadObject(reader.BaseStream);
        public void Serialize(object data, BinaryWriter writer, SerializationManager manager) => Serializer.WriteObject(writer.BaseStream, data);
    }
}
