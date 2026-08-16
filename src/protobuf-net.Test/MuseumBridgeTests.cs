using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Tests
{
    /// <summary>
    /// The obsolete class-based writer API - <c>ProtoWriter.WriteFieldHeader(n, wt, writer)</c>
    /// and friends - against a real stream, byte-compared with the same document written through
    /// the state API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists for the buffer core (notes/nano-writer.md). That API is written as
    /// <c>writer.DefaultState().DoTheThing(...)</c>: a State per call, discarded at the
    /// semicolon, which is only correct while every durable thing lives on the WRITER. Moving
    /// the stream backend's buffer position into State breaks that, so each call now liquifies
    /// and solidifies around its work.
    /// </para>
    /// <para>
    /// A missed pairing does not throw - it silently drops whatever that one call wrote. So the
    /// shape here is deliberate: several calls in sequence, each *followed* by more output, so
    /// that dropping any single one shifts everything after it and the byte comparison fails.
    /// A test that wrote one field would only catch a miss on that field.
    /// </para>
    /// </remarks>
    public class MuseumBridgeTests
    {
        private static byte[] ViaStateApi()
        {
            using var ms = new MemoryStream();
            var state = ProtoWriter.State.Create(ms, RuntimeTypeModel.Default);
            try
            {
                WriteDocument(ref state);
                state.Close();
            }
            finally
            {
                state.Dispose();
            }
            return ms.ToArray();
        }

        private static void WriteDocument(ref ProtoWriter.State state)
        {
            state.WriteFieldHeader(1, WireType.Varint);
            state.WriteInt32(42);
            state.WriteFieldHeader(2, WireType.String);
            state.WriteString("the quick brown fox");
            state.WriteFieldHeader(3, WireType.Fixed64);
            state.WriteDouble(3.14159);
            state.WriteFieldHeader(4, WireType.Varint);
            state.WriteBoolean(true);
            state.WriteFieldHeader(5, WireType.String);
            state.WriteBytes(new byte[] { 1, 2, 3, 4, 5 });
            state.WriteFieldHeader(6, WireType.Varint);
            state.WriteInt64(long.MaxValue);
        }

#pragma warning disable CS0618 // the whole point of this test is the obsolete surface
        private static byte[] ViaMuseumApi()
        {
            using var ms = new MemoryStream();
            var writer = ProtoWriter.Create(ms, RuntimeTypeModel.Default);
            try
            {
                ProtoWriter.WriteFieldHeader(1, WireType.Varint, writer);
                ProtoWriter.WriteInt32(42, writer);
                ProtoWriter.WriteFieldHeader(2, WireType.String, writer);
                ProtoWriter.WriteString("the quick brown fox", writer);
                ProtoWriter.WriteFieldHeader(3, WireType.Fixed64, writer);
                ProtoWriter.WriteDouble(3.14159, writer);
                ProtoWriter.WriteFieldHeader(4, WireType.Varint, writer);
                ProtoWriter.WriteBoolean(true, writer);
                ProtoWriter.WriteFieldHeader(5, WireType.String, writer);
                ProtoWriter.WriteBytes(new byte[] { 1, 2, 3, 4, 5 }, writer);
                ProtoWriter.WriteFieldHeader(6, WireType.Varint, writer);
                ProtoWriter.WriteInt64(long.MaxValue, writer);
                writer.Close();
            }
            finally
            {
                writer.Dispose();
            }
            return ms.ToArray();
        }

        /// <summary>
        /// A sub-item through the obsolete API, which is the case that back-fills a length
        /// prefix into bytes already in the buffer - so the buffer position has to survive
        /// StartSubItem, every member inside it, and EndSubItem.
        /// </summary>
        private static byte[] SubItemViaMuseumApi()
        {
            using var ms = new MemoryStream();
            var writer = ProtoWriter.Create(ms, RuntimeTypeModel.Default);
            try
            {
                ProtoWriter.WriteFieldHeader(1, WireType.String, writer);
                var token = ProtoWriter.StartSubItem(null, writer);
                ProtoWriter.WriteFieldHeader(1, WireType.Varint, writer);
                ProtoWriter.WriteInt32(7, writer);
                ProtoWriter.WriteFieldHeader(2, WireType.String, writer);
                ProtoWriter.WriteString("nested", writer);
                ProtoWriter.EndSubItem(token, writer);
                ProtoWriter.WriteFieldHeader(2, WireType.Varint, writer);
                ProtoWriter.WriteInt32(99, writer);
                writer.Close();
            }
            finally
            {
                writer.Dispose();
            }
            return ms.ToArray();
        }
#pragma warning restore CS0618

#pragma warning disable CS0618 // StartSubItem/EndSubItem: the state-API half of the same comparison
        private static byte[] SubItemViaStateApi()
        {
            using var ms = new MemoryStream();
            var state = ProtoWriter.State.Create(ms, RuntimeTypeModel.Default);
            try
            {
                state.WriteFieldHeader(1, WireType.String);
                var token = state.StartSubItem(null, PrefixStyle.Base128);
                state.WriteFieldHeader(1, WireType.Varint);
                state.WriteInt32(7);
                state.WriteFieldHeader(2, WireType.String);
                state.WriteString("nested");
                state.EndSubItem(token, PrefixStyle.Base128);
                state.WriteFieldHeader(2, WireType.Varint);
                state.WriteInt32(99);
                state.Close();
            }
            finally
            {
                state.Dispose();
            }
            return ms.ToArray();
        }
#pragma warning restore CS0618

        [Fact]
        public void MuseumApiWritesTheSameBytesAsTheStateApi()
        {
            var expected = ViaStateApi();
            Assert.NotEmpty(expected); // guard against both sides writing nothing
            Assert.Equal(BitConverter.ToString(expected), BitConverter.ToString(ViaMuseumApi()));
        }

        [Fact]
        public void MuseumApiSubItemsWriteTheSameBytesAsTheStateApi()
        {
            var expected = SubItemViaStateApi();
            Assert.NotEmpty(expected);
            Assert.Equal(BitConverter.ToString(expected), BitConverter.ToString(SubItemViaMuseumApi()));
        }

        /// <summary>
        /// The museum root path: a whole model-driven serialize through the obsolete writer,
        /// with the writer closed by the obsolete <c>Close()</c> - three separate liquify points
        /// (create, serialize, close), which is what makes this the sharpest of the three.
        /// </summary>
        [Fact]
        public void MuseumModelSerializeMatches()
        {
            var model = RuntimeTypeModel.Create();
            model.Add<Payload>();
            var value = new Payload { Id = 12345, Name = "a name long enough to matter", Ratio = 2.71828 };

            byte[] expected;
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, value);
                expected = ms.ToArray();
            }

            byte[] actual;
            using (var ms = new MemoryStream())
            {
#pragma warning disable CS0618
                var writer = ProtoWriter.Create(ms, model);
                try
                {
                    model.Serialize(writer, value);
                    writer.Close();
                }
                finally
                {
                    writer.Dispose();
                }
#pragma warning restore CS0618
                actual = ms.ToArray();
            }

            Assert.NotEmpty(expected);
            Assert.Equal(BitConverter.ToString(expected), BitConverter.ToString(actual));
        }

        [ProtoContract]
        public class Payload
        {
            [ProtoMember(1)] public int Id { get; set; }
            [ProtoMember(2)] public string Name { get; set; }
            [ProtoMember(3)] public double Ratio { get; set; }
        }
    }
}
