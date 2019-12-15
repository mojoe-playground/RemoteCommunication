using System;
using System.IO;
using System.Text;
using Xunit;

namespace RemoteCommunication.Tests
{
    internal enum TestEnum { A, B, C };

    [Flags]
    internal enum TestFlags { A, B, C, D };

    internal enum OldTestEnum { A, B };

    public class EnumSerializerTests
    {
        [Fact]
        public void EnumTest()
        {
            var value = TestEnum.B;
            using (var ms = new MemoryStream())
            {
                var s = new EnumSerializer<TestEnum>();
                Assert.True(s.CanSerialize(value, null));
                using (var bw = new BinaryWriter(ms, Encoding.UTF8, true))
                    s.Serialize(value, bw, null);
                ms.Position = 0;
                using (var br = new BinaryReader(ms, Encoding.UTF8, true))
                {
                    var res = s.Deserialize(br, null);
                    Assert.NotNull(res);
                    Assert.Equal(value, res);
                }
            }
        }

        [Fact]
        public void FlagsTest()
        {
            var value = TestFlags.B | TestFlags.D;
            using (var ms = new MemoryStream())
            {
                var s = new EnumSerializer<TestFlags>();
                Assert.True(s.CanSerialize(value, null));
                using (var bw = new BinaryWriter(ms, Encoding.UTF8, true))
                    s.Serialize(value, bw, null);
                ms.Position = 0;
                using (var br = new BinaryReader(ms, Encoding.UTF8, true))
                {
                    var res = s.Deserialize(br, null);
                    Assert.NotNull(res);
                    Assert.Equal(value, res);
                }
            }
        }

        [Fact]
        public void OldEnumTest()
        {
            var value = TestEnum.C;
            using (var ms = new MemoryStream())
            {
                var sm = new SerializationManager(new EnumSerializer<TestEnum>());
                Assert.True(sm.CanSerialize(value));
                using (var bw = new BinaryWriter(ms, Encoding.UTF8, true))
                    sm.Serialize(value, bw);
                ms.Position = 0;

                sm = new SerializationManager(new EnumSerializer<OldTestEnum>(typeof(TestEnum).Name));
                using (var br = new BinaryReader(ms, Encoding.UTF8, true))
                {
                    var res = sm.Deserialize(br);
                    Assert.NotNull(res);
                    Assert.Equal(value, (TestEnum)res);
                    Assert.Equal((int)value, (int)res);
                }
            }
        }
    }
}
