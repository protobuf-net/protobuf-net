using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Xunit;

namespace ProtoBuf.Test
{
    public partial class NullWrappedValueTests
    {
        [Fact]
        public void Test()
        {
            var model = new BasicNullableListModel();
            _log.WriteLine(Serializer.GetProto<BasicNullableListModel>());
        }

        [ProtoContract]
        public class BasicNullableListModel
        {
            [ProtoMember(1)]
            public List<Bar?> Items { get; } = new();
        }

        [ProtoContract]
        public class Bar
        {
        }

        #region LegacyBehaviour

        [Fact]
        public void ExistingListsBehaviour()
        {
            using var ms = new MemoryStream(new byte[] { 0x08, 0x00, 0x08, 0x01, 0x08, 0x02 });
            var clone = Serializer.Deserialize<LegacyBehaviourPoco>(ms);
            if (!ms.TryGetBuffer(out var buffer)) buffer = new ArraySegment<byte>(ms.ToArray());
            Assert.Equal("08-00-08-01-08-02", BitConverter.ToString(buffer.Array, buffer.Offset, buffer.Count));
        }

        [DataContract]
        public class LegacyBehaviourPoco
        {
            [DataMember(Order = 1)]
            public List<int?> Items { get; } = new List<int?>();
        }

        #endregion
    }
}
