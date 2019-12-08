using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace RemoteCommunication.Tests
{
    public class VersionSerializerTests
    {
        [Theory]
        [MemberData(nameof(VersionData))]
        public void Serializing(Version value)
        {
            var s = new VersionSerializer();

            Assert.False(s.CanSerialize(s, null));
            Assert.True(s.CanSerialize(new Version(1, 2, 3, 4), null));

            using (var ms = new MemoryStream())
            {
                Assert.True(s.CanSerialize(value, null));
                using (var bw = new BinaryWriter(ms, Encoding.UTF8, true))
                    s.Serialize(value, bw, null);
                ms.Position = 0;
                using (var br = new BinaryReader(ms, Encoding.UTF8, true))
                    Assert.Equal(value, s.Deserialize(br, null));
            }
        }

        public static IEnumerable<object[]> VersionData => new List<object[]> { new[] { new Version() }, new[] { new Version(1, 2) }, new[] { new Version(6, 5, 4, 3) } };
    }
}
