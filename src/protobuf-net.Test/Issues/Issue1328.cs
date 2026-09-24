using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    /// <summary>
    /// The extension APIs see one field at a time, so they used to read a repeated scalar
    /// one value per field header - which rejects the packed encoding outright. protoc has
    /// always written repeated packable custom options packed, so this refused data that
    /// every other implementation treats as ordinary; #1328 hit it via our own schema parser,
    /// which started emitting packed enum options to match protoc.
    /// </summary>
    public class Issue1328
    {
        [ProtoContract]
        public class Extended : Extensible { }

        // exactly what protoc 35.1 emits for
        //   `option (my_opt) = ALPHA; option (my_opt) = BETA;`  (repeated enum @50000)
        //   `option (my_ints) = 3;    option (my_ints) = 4;`    (repeated int32 @50001)
        private static readonly byte[] Packed = { 0x82, 0xB5, 0x18, 0x02, 0x01, 0x02, 0x8A, 0xB5, 0x18, 0x02, 0x03, 0x04 };

        // the same values in the expanded encoding, which must keep working unchanged
        private static readonly byte[] Expanded = { 0x80, 0xB5, 0x18, 0x01, 0x80, 0xB5, 0x18, 0x02, 0x88, 0xB5, 0x18, 0x03, 0x88, 0xB5, 0x18, 0x04 };

        // a single value, packed; protoc packs even a unary run
        private static readonly byte[] PackedUnary = { 0x82, 0xB5, 0x18, 0x01, 0x01 };

        // both encodings for the same field, interleaved - legal, and the merged result is 1,2,3,4
        private static readonly byte[] Mixed = { 0x80, 0xB5, 0x18, 0x01, 0x82, 0xB5, 0x18, 0x02, 0x02, 0x03, 0x80, 0xB5, 0x18, 0x04 };

        private static Extended For(byte[] payload)
        {
            var obj = new Extended();
            Extensible.AppendValue(obj, 1, 0); // force an extension object into being
            var extn = ((IExtensible)obj).GetExtensionObject(true);
            (extn as IExtensionResettable)?.Reset();
            var stream = extn.BeginAppend();
            try
            {
                stream.Write(payload, 0, payload.Length);
                extn.EndAppend(stream, true);
            }
            catch
            {
                extn.EndAppend(stream, false);
                throw;
            }
            return obj;
        }

        [Theory]
        [InlineData(nameof(Packed), 50000, "1,2")]
        [InlineData(nameof(Packed), 50001, "3,4")]
        [InlineData(nameof(Expanded), 50000, "1,2")]
        [InlineData(nameof(Expanded), 50001, "3,4")]
        [InlineData(nameof(PackedUnary), 50000, "1")]
        [InlineData(nameof(Mixed), 50000, "1,2,3,4")]
        [InlineData(nameof(Packed), 50002, "")] // absent tag yields nothing rather than throwing
        public void GetValuesReadsEitherEncoding(string payload, int tag, string expected)
        {
            var obj = For(Payload(payload));
            Assert.Equal(expected, string.Join(",", Extensible.GetValues<int>(obj, tag)));
        }

        [Theory]
        [InlineData(nameof(Packed), 50000, 2)]
        [InlineData(nameof(Packed), 50001, 4)]
        [InlineData(nameof(Expanded), 50000, 2)]
        [InlineData(nameof(PackedUnary), 50000, 1)]
        [InlineData(nameof(Mixed), 50000, 4)]
        public void GetValueMergesEitherEncodingLastWins(string payload, int tag, int expected)
        {
            var obj = For(Payload(payload));
            Assert.Equal(expected, Extensible.GetValue<int>(obj, tag));
        }

        /// <summary>
        /// Generated extension accessors pass a <see cref="DataFormat"/> whenever the field
        /// declares one, which routes through the reflective reader rather than the typed one;
        /// both need the same tolerance.
        /// </summary>
        [Theory]
        [InlineData(nameof(Packed), "1,2")]
        [InlineData(nameof(Expanded), "1,2")]
        public void ReflectiveReaderReadsEitherEncoding(string payload, string expected)
        {
            var obj = For(Payload(payload));
            Assert.Equal(expected, string.Join(",", Extensible.GetValues<int>(obj, 50000, DataFormat.TwosComplement)));
        }

        [Theory]
        [InlineData(nameof(Packed), "1,2")]
        [InlineData(nameof(Expanded), "1,2")]
        public void NonGenericReaderReadsEitherEncoding(string payload, string expected)
        {
            var obj = For(Payload(payload));
            var values = Extensible.GetValues(null, typeof(int), obj, 50000).Cast<object>();
            Assert.Equal(expected, string.Join(",", values));
        }

        /// <summary>
        /// A length-delimited payload only means "packed" for a type whose own wire type is not
        /// length-delimited; strings, byte arrays and messages must be untouched by the inference.
        /// </summary>
        [Fact]
        public void LengthDelimitedTypesAreNotMistakenForPackedRuns()
        {
            var obj = new Extended();
            Extensible.AppendValue(obj, 50010, "hello");
            Assert.Equal("hello", Extensible.GetValue<string>(obj, 50010));

            var bytes = new Extended();
            Extensible.AppendValue(bytes, 50011, new byte[] { 1, 2, 3 });
            Assert.Equal(new byte[] { 1, 2, 3 }, Extensible.GetValue<byte[]>(bytes, 50011));
        }

        /// <summary>
        /// Round-tripping our own <c>AppendValue</c> output is unaffected: it writes the expanded
        /// form, one value per call, and that is not changing here.
        /// </summary>
        [Fact]
        public void AppendValueRoundTripIsUnchanged()
        {
            var obj = new Extended();
            Extensible.AppendValue(obj, 50000, 1);
            Extensible.AppendValue(obj, 50000, 2);
            Assert.Equal("1,2", string.Join(",", Extensible.GetValues<int>(obj, 50000)));
        }

        private static byte[] Payload(string name) => name switch
        {
            nameof(Packed) => Packed,
            nameof(Expanded) => Expanded,
            nameof(PackedUnary) => PackedUnary,
            nameof(Mixed) => Mixed,
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
    }
}
