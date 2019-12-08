namespace RemoteCommunication
{
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public class ObjectSerializer<TInformation, TConcrete> : ISerializer where TConcrete : TInformation, new()
    {
        public ObjectSerializer() : base() { }

        public string Id => "obj:" + typeof(TInformation).Name;

        private static Dictionary<string, PropertyInfo> Properties { get; } = typeof(TInformation).GetProperties().Where(p => p.CanRead).ToDictionary(p => p.Name);
        private static Dictionary<string, PropertyInfo> TargetProperties { get; } = typeof(TConcrete).GetProperties().Where(p => p.CanWrite).ToDictionary(p => p.Name);
        private static Dictionary<string, PropertyInfo> TargetCollectionProperties { get; } = typeof(TConcrete).GetProperties().Where(CollectionInfo).ToDictionary(p => p.Name);

        private static bool CollectionInfo(PropertyInfo property)
        {
            if (property.PropertyType == typeof(string))
                return false;

            return typeof(IEnumerable).IsAssignableFrom(property.PropertyType);
        }

        private IEnumerable<(string name, object value)> GetProperties(TInformation obj)
        {
            foreach (var p in Properties)
                if (SerializeAllProperties || TargetProperties.ContainsKey(p.Key) || TargetCollectionProperties.ContainsKey(p.Key))
                    yield return (p.Key, p.Value.GetValue(obj));
        }

        public bool CanSerialize(object data, SerializationManager manager) => data is TInformation t && GetProperties(t).All(p => manager.CanSerialize(p.value));

        public bool SerializeAllProperties { get; set; }

        public object Deserialize(BinaryReader reader, SerializationManager manager)
        {
            var res = new TConcrete();

            while (true)
            {
                var name = reader.ReadString();
                if (string.IsNullOrEmpty(name))
                    break;

                var value = manager.Deserialize(reader);

                if (TargetProperties.TryGetValue(name, out var pi))
                    pi.SetValue(res, value);
                else if (value is IEnumerable enumerable && TargetCollectionProperties.TryGetValue(name, out pi))
                {
                    var collection = pi.GetValue(res);

                    if (collection == null)
                        continue;

                    foreach (var i in value.GetType().GetInterfaces())
                        if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        {
                            var add = collection.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).Where(m => m.Name == "Add" && m.GetParameters() is ParameterInfo[] parameters && parameters.Length == 1 && parameters[0].ParameterType == i.GenericTypeArguments[0]).FirstOrDefault();
                            if (add == null)
                                continue;

                            var enumeratorMethod = i.GetMethod("GetEnumerator");
                            var enumerator = (IEnumerator)enumeratorMethod.Invoke(value, null);
                            while (enumerator.MoveNext())
                                add.Invoke(collection, new object[] { enumerator.Current });

                            break;
                        }
                }
            }

            return res;
        }

        public void Serialize(object data, BinaryWriter writer, SerializationManager manager)
        {
            foreach (var (name, value) in GetProperties((TInformation)data))
            {
                writer.Write(name);
                manager.Serialize(value, writer);
            }

            writer.Write(string.Empty);
        }
    }

    public class ObjectSerializer<T> : ObjectSerializer<T, T> where T : new()
    { }
}
