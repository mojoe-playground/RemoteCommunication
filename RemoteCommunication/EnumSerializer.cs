namespace RemoteCommunication
{
    using System;
    using System.Globalization;
    using System.IO;

    public class EnumSerializer<T> : ISerializer where T : struct, IConvertible
    {
        public EnumSerializer() : this(null)
        { }

        public EnumSerializer(string id)
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");

            if (string.IsNullOrEmpty(id))
                id = typeof(T).Name;
            Id = "E:" + id;
        }

        public string Id { get; }

        public bool CanSerialize(object data, SerializationManager manager) => data is T;
        public object Deserialize(BinaryReader reader, SerializationManager manager) => (T)Enum.ToObject(typeof(T), reader.ReadInt32());
        public void Serialize(object data, BinaryWriter writer, SerializationManager manager) => writer.Write(((T)data).ToInt32(CultureInfo.InvariantCulture));
    }
}
