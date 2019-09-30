using ProtoBuf.Internal;
using ProtoBuf.Meta;
using ProtoBuf.WellKnownTypes;
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
            Assert.True(PrimaryTypeProvider.DecimalOptimized);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
#pragma warning disable xUnit1026
        public void CheckDecimalLayout(bool optimized)
#pragma warning restore xUnit1026
        {
            bool oldVal = PrimaryTypeProvider.GuidOptimized;
            decimal value = 1.0000000000045000000003000000M;
            try
            {
#if DEBUG
                PrimaryTypeProvider.DecimalOptimized = optimized;
#endif
                using var ms = new MemoryStream();
                var writeState = ProtoWriter.State.Create(ms, RuntimeTypeModel.Default);
                try
                {
                    writeState.WriteFieldHeader(1, WireType.String);
                    BclHelpers.WriteDecimal(ref writeState, value);
                    writeState.Close();
                }
                finally
                {
                    writeState.Dispose();
                }
                var hex = BitConverter.ToString(
                    ms.GetBuffer(), 0, (int)ms.Length);
                Assert.Equal("0A-12-08-C0-8D-C9-B8-C0-B4-B8-E2-3E-10-DE-9C-BF-82-02-18-38", hex);

                ms.Position = 0;
                var readState = ProtoReader.State.Create(ms, RuntimeTypeModel.Default);
                try
                {
                    Assert.Equal(1, readState.ReadFieldHeader());
                    var result = BclHelpers.ReadDecimal(ref readState);
                    Assert.Equal(value, result);
                    Assert.Equal(0, readState.ReadFieldHeader());
                }
                finally
                {
                    readState.Dispose();
                }
            }
            finally
            {
#if DEBUG
                PrimaryTypeProvider.DecimalOptimized = oldVal;
#endif
            }
        }

        [Fact]
        public void GuidIsOptimized()
        {
            Assert.True(PrimaryTypeProvider.GuidOptimized);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
#pragma warning disable xUnit1026
        public void CheckGuidLayout(bool optimized)
#pragma warning restore xUnit1026
        {
            Assert.True(Guid.TryParse("12345678-2345-3456-4567-56789a6789ab", out var value));

            bool oldVal = PrimaryTypeProvider.GuidOptimized;
            try
            {
#if DEBUG
                PrimaryTypeProvider.GuidOptimized = optimized;
#endif
                using var ms = new MemoryStream();
                CheckGuidLayoutImpl(ms, value, "0A-12-09-78-56-34-12-45-23-56-34-11-45-67-56-78-9A-67-89-AB");
                for (int i = 0; i < 100; i++)
                {
                    value = Guid.NewGuid();
                    CheckGuidLayoutImpl(ms, value, null);
                }
            }
            finally
            {
#if DEBUG
                PrimaryTypeProvider.GuidOptimized = oldVal;
#endif
            }

        }

        private void CheckGuidLayoutImpl(MemoryStream ms, Guid value, string expected)
        {
            ms.Position = 0;
            ms.SetLength(0);
            var state = ProtoWriter.State.Create(ms, RuntimeTypeModel.Default);
            try
            {
                state.WriteFieldHeader(1, WireType.String);
                BclHelpers.WriteGuid(ref state, value);
                state.Close();
            }
            finally
            {
                state.Dispose();
            }

            if (expected != null)
            {
                var hex = BitConverter.ToString(
                    ms.GetBuffer(), 0, (int)ms.Length);
                Assert.Equal(expected, hex);
            }

            ms.Position = 0;
            var readState = ProtoReader.State.Create(ms, RuntimeTypeModel.Default);
            try
            {
                Assert.Equal(1, readState.ReadFieldHeader());
                var result = BclHelpers.ReadGuid(ref readState);
                Assert.Equal(value, result);
                Assert.Equal(0, readState.ReadFieldHeader());
            }
            finally
            {
                readState.Dispose();
            }
        }
    }
}
