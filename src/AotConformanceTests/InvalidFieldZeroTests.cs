using AotFixtures.Field;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using Xunit;

namespace ProtoBuf.AotConformance
{
    /// <summary>
    /// A field-0 tag is invalid protobuf, and the exception CONTRACT matters: legacy reported
    /// ProtoException ("Invalid field in source data: 0"), and the raw read path must not
    /// downgrade that to InvalidOperationException, nor read a zero tag byte as a false
    /// end-of-message. This was a recorded divergence with no distinguishing test - which is
    /// exactly how it could have drifted silently - so both models are asserted here, over the
    /// three corrupt shapes: a bare zero byte, a field-0 tag with a data wire type, and the
    /// OVERLONG encoding of zero (0x80 0x00), which the minimal-encoding argument misses.
    /// </summary>
    public class InvalidFieldZeroTests
    {
        public static TheoryData<byte[]> Payloads => new()
        {
            new byte[] { 0x00 },                    // bare zero tag: field 0, wire 0
            new byte[] { 0x02, 0x01, 0x41 },        // field 0, wire 2 (length-prefixed)
            new byte[] { 0x80, 0x00 },              // overlong zero: decodes to tag 0
        };

        [Theory]
        [MemberData(nameof(Payloads))]
        public void GeneratedModelThrowsProtoException(byte[] payload)
        {
            var ex = Assert.Throws<ProtoException>(
                () => FieldModel.Instance.Deserialize<Fields>(new ReadOnlyMemory<byte>(payload)));
            Assert.Contains("Invalid field in source data: 0", ex.Message);
        }

        [Theory]
        [MemberData(nameof(Payloads))]
        public void RuntimeModelThrowsProtoException(byte[] payload)
        {
            var model = RuntimeTypeModel.Create();
            var ex = Assert.Throws<ProtoException>(
                () => model.Deserialize<Fields>(new ReadOnlyMemory<byte>(payload)));
            Assert.Contains("Invalid field in source data: 0", ex.Message);
        }
    }
}
