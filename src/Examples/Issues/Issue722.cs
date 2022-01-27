extern alias gpb;

using System;
using Xunit;

namespace ProtoBuf.Issues
{
    public class Issue722
    {
        [Fact]
        public void ReportGoogleTypesUsefully()
        {
            var obj = new HazGoogleTypes();
            var ex = Assert.Throws<InvalidOperationException>(() => Serializer.DeepClone(obj));
            Assert.Equal("Type 'Google.Protobuf.WellKnownTypes.FloatValue' looks like a Google.Protobuf type; it cannot be used directly with protobuf-net without manual configuration; it may be possible to generate a protobuf-net type instead; see https://protobuf-net.github.io/protobuf-net/contract_first", ex.Message);
            var inner = Assert.IsType<InvalidOperationException>(ex.InnerException);
            Assert.Equal("No serializer defined for type: Google.Protobuf.WellKnownTypes.FloatValue", inner.Message);
        }

        [ProtoContract]
        public class HazGoogleTypes
        {
            [ProtoMember(1)]
            gpb::Google.Protobuf.WellKnownTypes.FloatValue Foo { get; set; }
        }
    }
}
