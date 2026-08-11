using ProtoBuf.Meta;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    /// <summary>
    /// <c>[ProtoPartialMember(OverwriteList = true)]</c>, which was read from the wrong attribute map
    /// and so silently did nothing.
    /// </summary>
    /// <remarks>
    /// <c>OverwriteList</c> makes a repeated member *replace* rather than append when read into an
    /// existing collection. The partial branch read it from the member's own <c>[ProtoMember]</c>,
    /// which is necessarily null there, so it was always false however it was spelled.
    /// </remarks>
    public class PartialMemberOverwriteListTests
    {
        [ProtoContract]
        [ProtoPartialMember(1, nameof(Appends))]
        [ProtoPartialMember(2, nameof(Replaces), OverwriteList = true)]
        public class Holder
        {
            public List<int> Appends { get; set; } = new();
            public List<int> Replaces { get; set; } = new();
        }

        /// <summary>Reading into an instance that already has values is what the flag governs.</summary>
        private static Holder RoundTripInto(Holder existing, Holder payload)
        {
            using var ms = new MemoryStream();
            RuntimeTypeModel.Default.Serialize(ms, payload);
            ms.Position = 0;
            return RuntimeTypeModel.Default.Deserialize<Holder>(ms, existing);
        }

        [Fact]
        public void OverwriteListOnAPartialMemberIsHonoured()
        {
            var result = RoundTripInto(
                new Holder { Appends = { 1 }, Replaces = { 1 } },
                new Holder { Appends = { 2 }, Replaces = { 2 } });

            Assert.Equal(new[] { 1, 2 }, result.Appends);   // default: append
            Assert.Equal(new[] { 2 }, result.Replaces);     // OverwriteList: replace
        }
    }
}
