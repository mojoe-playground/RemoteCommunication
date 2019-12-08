using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace RemoteCommunication.Tests
{
    public class ObjectSerializerTests
    {
        [Fact]
        public void CollectionTest()
        {
            var value = new CollectionSourceData { Items = new List<string> { "A", "B", "C" } };
            var manager = new SerializationManager(new BuiltInTypesSerializer(), new CollectionSerializer<string>());
            using (var ms = new MemoryStream())
            {
                var s = new ObjectSerializer<CollectionSourceData>();
                Assert.True(s.CanSerialize(value, manager));
                using (var bw = new BinaryWriter(ms, Encoding.UTF8, true))
                    s.Serialize(value, bw, manager);
                ms.Position = 0;
                using (var br = new BinaryReader(ms, Encoding.UTF8, true))
                {
                    var res = s.Deserialize(br, manager) as ICollectionData;
                    Assert.NotNull(res);
                    Assert.Equal(value.Items, res.Items);
                }
            }
        }

        [Fact]
        public void CollectionGetterOnlyTest()
        {
            var value = new CollectionSourceData { Items = new List<string> { "A", "B", "C" } };
            var manager = new SerializationManager(new BuiltInTypesSerializer(), new CollectionSerializer<string>());
            using (var ms = new MemoryStream())
            {
                var s = new ObjectSerializer<ICollectionData, CollectionGetterOnly>();
                Assert.True(s.CanSerialize(value, manager));
                using (var bw = new BinaryWriter(ms, Encoding.UTF8, true))
                    s.Serialize(value, bw, manager);
                ms.Position = 0;
                using (var br = new BinaryReader(ms, Encoding.UTF8, true))
                {
                    var res = s.Deserialize(br, manager) as ICollectionData;
                    Assert.NotNull(res);
                    Assert.Equal(value.Items, res.Items);
                }
            }
        }

        [Fact]
        public void CollectionArrayTest()
        {
            var value = new CollectionSourceData { Items = new List<string> { "A", "B", "C" } };
            var manager = new SerializationManager(new BuiltInTypesSerializer(), new CollectionSerializer<string>());
            using (var ms = new MemoryStream())
            {
                var s = new ObjectSerializer<ICollectionData, CollectionArrayData>();
                Assert.True(s.CanSerialize(value, manager));
                using (var bw = new BinaryWriter(ms, Encoding.UTF8, true))
                    s.Serialize(value, bw, manager);
                ms.Position = 0;
                using (var br = new BinaryReader(ms, Encoding.UTF8, true))
                {
                    var res = s.Deserialize(br, manager) as ICollectionData;
                    Assert.NotNull(res);
                    Assert.NotNull(res.Items);
                    Assert.Empty(res.Items);
                }
            }
        }
    }

    internal interface ICollectionData
    { 
        IEnumerable<string> Items { get; }
    }

    internal class CollectionSourceData: ICollectionData
    { 
        public IEnumerable<string> Items { get; set; }
    }

    internal class CollectionGetterOnly: ICollectionData
    {
        public IEnumerable<string> Items { get; } = new List<string>();
    }

    internal class CollectionArrayData : ICollectionData
    {
        public IEnumerable<string> Items { get; } = new string[0];
    }
}
