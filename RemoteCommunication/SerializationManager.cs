namespace RemoteCommunication
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class SerializationManager
    {
        public SerializationManager(params ISerializer[] serializers)
        {
            Serializers = serializers;
        }

        public SerializationManager(IEnumerable<ISerializer> serializers)
        {
            Serializers = new List<ISerializer>(serializers);
        }

        public IReadOnlyList<ISerializer> Serializers { get; }

        public object Deserialize(BinaryReader reader)
        {
            var sid = reader.ReadString();

            var serializer = Serializers.FirstOrDefault(s => s.Id == sid);
            if (serializer == null)
                throw new InvalidOperationException($"Unknown serializer {sid}");

            return serializer.Deserialize(reader, this);
        }

        public void Serialize(object data, BinaryWriter writer)
        {
            var targetSerializer = Serializers.FirstOrDefault(s => s.CanSerialize(data, this));
            if (targetSerializer == null)
                throw new InvalidOperationException($"Serializer not found for object {data} ({data?.GetType()})");

            writer.Write(targetSerializer.Id);
            targetSerializer.Serialize(data, writer, this);
        }

        public bool CanSerialize(object data) => Serializers.FirstOrDefault(s => s.CanSerialize(data, this)) != null;
    }
}
