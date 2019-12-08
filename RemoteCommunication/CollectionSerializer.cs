namespace RemoteCommunication
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;

    public class CollectionSerializer<T> : ISerializer
    {
        public CollectionSerializer() : this(null)
        { }

        public CollectionSerializer(string id)
        {
            if (string.IsNullOrEmpty(id))
                id = typeof(T).Name;

            Id = "col:" + id;
        }

        public string Id { get; }

        public bool CanSerialize(object data, SerializationManager manager)
        {
            var l = ToList(data);
            if (l == null)
                return false;
            return l.All(i => manager.CanSerialize(i));
        }

        public object Deserialize(BinaryReader reader, SerializationManager manager)
        {
            var listType = reader.ReadInt32();
            var count = reader.ReadInt32();
            ICollection<T> res;
            if (listType == 2)
                res = new ObservableCollection<T>();
            else if (listType == 3)
                res = new Collection<T>();
            else
                res = new List<T>();

            for (var i = 0; i < count; i++)
                res.Add((T)manager.Deserialize(reader));

            if (listType == 1)
                return res.ToArray();

            return res;
        }

        public void Serialize(object data, BinaryWriter writer, SerializationManager manager)
        {
            var l = ToList(data);
            writer.Write(ListType(data));
            writer.Write(l.Count);
            foreach (var i in l)
                manager.Serialize(i, writer);
        }

        private List<T> ToList(object data)
        {
            if (data is List<T> list)
                return list;
            if (data is T[] array)
                return array.ToList();
            if (data is IEnumerable<T> col)
                return col.ToList();
            return null;
        }

        private int ListType(object data)
        {
            if (data is T[])
                return 1;
            if (data is ObservableCollection<T>)
                return 2;
            if (data is Collection<T>)
                return 3;
            return 0;
        }
    }
}
