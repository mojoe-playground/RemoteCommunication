namespace RemoteCommunication
{
    using System;
    using System.IO;

    public class VersionSerializer : ISerializer
    {
        public string Id => "ver";

        public bool CanSerialize(object data, SerializationManager manager) => data is Version;

        public object Deserialize(BinaryReader reader, SerializationManager manager) => new Version(reader.ReadString());

        public void Serialize(object data, BinaryWriter writer, SerializationManager manager) => writer.Write(((Version)data).ToString());
    }
}
