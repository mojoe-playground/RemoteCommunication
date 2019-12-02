using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace RemoteCommunication.Tests
{
    public class SimpleValueSerializerTest
    {
        [Fact]
        public void SerializationTest()
        {
            var value = new int?[] { 1600000, null, -32 };

            using (var ms = new MemoryStream())
            {
                var s = new SimpleValueSerializer();
                Assert.True(s.CanSerialize(value, null));
                using (var bw = new BinaryWriter(ms, Encoding.UTF8, true))
                    s.Serialize(value, bw, null);
                ms.Position = 0;
                using (var br = new BinaryReader(ms, Encoding.UTF8, true))
                    Assert.Equal(value, s.Deserialize(br, null));
            }
        }


        [Theory]
        [MemberData(nameof(SerializationData))]
        public void Serialization(object value)
        {
            using (var ms = new MemoryStream())
            {
                var s = new SimpleValueSerializer();
                Assert.True(s.CanSerialize(value, null));
                using (var bw = new BinaryWriter(ms, Encoding.UTF8, true))
                    s.Serialize(value, bw, null);
                ms.Position = 0;
                using (var br = new BinaryReader(ms, Encoding.UTF8, true))
                    Assert.Equal(value, s.Deserialize(br, null));
            }
        }

        [Fact]
        public void UnknownType()
        {
            using (var ms = new MemoryStream())
            {
                var s = new SimpleValueSerializer();
                Assert.False(s.CanSerialize(ms, null));
                using (var bw = new BinaryWriter(ms, Encoding.UTF8, true))
                    Assert.Throws<InvalidOperationException>(() => s.Serialize(ms, bw, null));
            }
        }

        public static IEnumerable<object[]> SerializationData => new List<object[]>
        {
            new object[]{ null },
            new object[]{ true },
            new object[]{ new bool[] { true, false } },
            new object[]{ new bool?[] { true, null, false } },
            new object[]{ (byte)250 },
            new object[]{ new byte[] { 3, 44 } },
            new object[]{ new byte?[] { 55, null, 2 } },
            new object[]{ '4' },
            new object[]{ new char[] { '░', 'ű' } },
            new object[]{ new char?[] { 'é', null, 'Y' } },
            new object[]{ 5m },
            new object[]{ new decimal[] { 58.487m, 698m } },
            new object[]{ new decimal?[] { -99.698m, null, 32m } },
            new object[]{ 5.2 },
            new object[]{ new double[] { 58.487, 698 } },
            new object[]{ new double?[] { -99.698, null, 32 } },
            new object[]{ (short)-5 },
            new object[]{ new short[] { -32767, 698 } },
            new object[]{ new short?[] { 16000, null, -32 } },
            new object[]{ (int)-5 },
            new object[]{ new int[] { -3276700, 698 } },
            new object[]{ new int?[] { 1600000, null, -32 } },
            new object[]{ new int?[] { 1600000, null, -32, 45, 234, 234, 243, 65354, 2346, 3245234, 532353, 53345, 24, 2, null, null, 23 } },
            new object[]{ new int?[] { 1600000, null, -32, 45, 234, 234, null, 65354 } },
            new object[]{ (long)-5 },
            new object[]{ new long[] { -8000000000, 698 } },
            new object[]{ new long?[] { 5600000000, null, -32 } },
            new object[]{ (sbyte)-120 },
            new object[]{ new sbyte[] { 3, -44 } },
            new object[]{ new sbyte?[] { -55, null, 2 } },
            new object[]{ 5.2f },
            new object[]{ new float[] { 58.487f, 698f } },
            new object[]{ new float?[] { -99.698f, null, 32f } },
            new object[]{ "Hello" },
            new object[]{ new string[] { "-99░698", null, "3😀2" } },
            new object[]{ (ushort)5 },
            new object[]{ new ushort[] { 32767, 698 } },
            new object[]{ new ushort?[] { 16000, null, 32 } },
            new object[]{ (uint)5 },
            new object[]{ new uint[] { 3276700, 4000000000 } },
            new object[]{ new uint?[] { 1600000, null, 32 } },
            new object[]{ (ulong)5 },
            new object[]{ new ulong[] { 8000000000, 698 } },
            new object[]{ new long?[] { 5600000000, null, 32 } },
        };
    }
}
