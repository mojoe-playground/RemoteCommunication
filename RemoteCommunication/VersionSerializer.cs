namespace RemoteCommunication
{
    using System;
    using System.IO;

    public class VersionSerializer : ISerializer
    {
        public string Id => "ver";

        public bool CanSerialize(object data, SerializationManager manager) => data is Version;

        public object Deserialize(BinaryReader reader, SerializationManager manager)
        {
            var ma = reader.ReadInt32();
            var mi = reader.ReadInt32();
            var b = reader.ReadInt32();
            var r = reader.ReadInt32();
            if (b == -1)
                return new Version(ma, mi);
            return new Version(ma, mi, b, r);
        }

        public void Serialize(object data, BinaryWriter writer, SerializationManager manager)
        {
            var ver = (Version)data;
            writer.Write(ver.Major);
            writer.Write(ver.Minor);
            writer.Write(ver.Build);
            writer.Write(ver.Revision);
        }
    }
}
