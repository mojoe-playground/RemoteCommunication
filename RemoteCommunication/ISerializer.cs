namespace RemoteCommunication
{
    using System.IO;

    public interface ISerializer
    {
        string Id { get; }
        bool CanSerialize(object data, SerializationManager manager);
        void Serialize(object data, BinaryWriter writer, SerializationManager manaager);
        object Deserialize(BinaryReader reader, SerializationManager manager);
    }
}
