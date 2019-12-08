using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Xunit;

namespace RemoteCommunication.Tests
{
    public class CollectionSerializerTests
    {
        public static IEnumerable<object[]> TypeData => new List<object[]> {
            new[] { new List<string>{ "A", "B", "C" } },
            new[] { new Collection<string>{ "D", "E", "F" } },
            new[] { new ObservableCollection<string>{ "G", "H", "I" } },
        };

        [Theory]
        [MemberData(nameof(TypeData))]
        public void SerializeTypes(object value)
        {
            var manager = new SerializationManager(new SimpleValueSerializer());
            using (var ms = new MemoryStream())
            {
                var s = new CollectionSerializer<string>();
                Assert.True(s.CanSerialize(value, manager));
                using (var bw = new BinaryWriter(ms, Encoding.UTF8, true))
                    s.Serialize(value, bw, manager);
                ms.Position = 0;
                using (var br = new BinaryReader(ms, Encoding.UTF8, true))
                {
                    var res = s.Deserialize(br, manager);
                    Assert.Equal(value, res);
                    Assert.Equal(value.GetType(), res.GetType());
                }
            }
        }
    }
}
