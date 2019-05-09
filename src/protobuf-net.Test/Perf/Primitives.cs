using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Perf
{
    public class Primitives
    {
        [Fact]
        public void DecimalIsOptimized()
        {
            Assert.True(BclHelpers.DecimalOptimized);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
#pragma warning disable xUnit1026
        public void CheckDecimalLayout(bool optimized)
#pragma warning restore xUnit1026
        {
            bool oldVal = BclHelpers.GuidOptimized;
            decimal value = 1.0000000000045000000003000000M;
            try
            {
#if DEBUG
                BclHelpers.DecimalOptimized = optimized;
#endif
                using (var ms = new MemoryStream())
                {
                    using (var writer = ProtoWriter.Create(out var state, ms, RuntimeTypeModel.Default))
                    {
                        ProtoWriter.WriteFieldHeader(1, WireType.String, writer, ref state);
                        BclHelpers.WriteDecimal(value, writer, ref state);
                        writer.Close(ref state);
                    }
                    var hex = BitConverter.ToString(
                        ms.GetBuffer(), 0, (int)ms.Length);
                    Assert.Equal("0A-12-08-C0-8D-C9-B8-C0-B4-B8-E2-3E-10-DE-9C-BF-82-02-18-38", hex);

                    ms.Position = 0;
                    using(var reader = ProtoReader.Create(out var state, ms, RuntimeTypeModel.Default))
                    {
                        Assert.Equal(1, reader.ReadFieldHeader(ref state));
                        var result = BclHelpers.ReadDecimal(reader, ref state);
                        Assert.Equal(value, result);
                        Assert.Equal(0, reader.ReadFieldHeader(ref state));
                    }
                }

            }
            finally
            {
#if DEBUG
                BclHelpers.DecimalOptimized = oldVal;
#endif
            }
        }

        [Fact]
        public void GuidIsOptimized()
        {
            Assert.True(BclHelpers.GuidOptimized);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
#pragma warning disable xUnit1026
        public void CheckGuidLayout(bool optimized)
#pragma warning restore xUnit1026
        {
            Assert.True(Guid.TryParse("12345678-2345-3456-4567-56789a6789ab", out var value));

            bool oldVal = BclHelpers.GuidOptimized;
            try
            {
#if DEBUG
                BclHelpers.GuidOptimized = optimized;
#endif
                using (var ms = new MemoryStream())
                {
                    CheckGuidLayoutImpl(ms, value, "0A-12-09-78-56-34-12-45-23-56-34-11-45-67-56-78-9A-67-89-AB");
                    for(int i = 0; i < 100; i++)
                    {
                        value = Guid.NewGuid();
                        CheckGuidLayoutImpl(ms, value, null);
                    }
                }

            }
            finally
            {
#if DEBUG
                BclHelpers.GuidOptimized = oldVal;
#endif
            }

        }

        private void CheckGuidLayoutImpl(MemoryStream ms, Guid value, string expected)
        {
            ms.Position = 0;
            ms.SetLength(0);
            using (var writer = ProtoWriter.Create(out var state, ms, RuntimeTypeModel.Default))
            {
                ProtoWriter.WriteFieldHeader(1, WireType.String, writer, ref state);
                BclHelpers.WriteGuid(value, writer, ref state);
                writer.Close(ref state);
            }

            if (expected != null)
            {
                var hex = BitConverter.ToString(
                    ms.GetBuffer(), 0, (int)ms.Length);
                Assert.Equal(expected, hex);
            }

            ms.Position = 0;
            using (var reader = ProtoReader.Create(out var state, ms, RuntimeTypeModel.Default))
            {
                Assert.Equal(1, reader.ReadFieldHeader(ref state));
                var result = BclHelpers.ReadGuid(reader, ref state);
                Assert.Equal(value, result);
                Assert.Equal(0, reader.ReadFieldHeader(ref state));
            }
        }
    }
}
