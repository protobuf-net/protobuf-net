using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    /// <summary>
    /// Pins how often a serialization callback fires on the <b>measure + serialize</b> route of
    /// the runtime model — the behaviour the AOT measure path has to mirror.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The classic measure pass is a real write to a counting writer, so a callback fires
    /// <b>once per pass</b>. That is not a wart to be tidied away: the two passes must observe the
    /// <i>same object</i>, or the measured length will not match the bytes subsequently written and
    /// the length check throws. <c>ProtoWriter.IsMeasuring(context)</c> exists so a callback whose
    /// side effect is not part of the message can tell the passes apart.
    /// </para>
    /// <para>
    /// It matters for the AOT generator because <c>RawMeasurableShape</c> currently <b>refuses</b>
    /// any contract with a before-serialize callback, dropping it (and, by cascade, everything
    /// referencing it) off the measure-first path. If that is ever relaxed, the generated
    /// <c>Measure_</c> must fire the callback too — firing only in the write would let the object
    /// change between measuring and writing, which is precisely the failure this pins.
    /// </para>
    /// </remarks>
    public class CallbackMeasurePassTests
    {
        private readonly ITestOutputHelper _out;
        public CallbackMeasurePassTests(ITestOutputHelper output) => _out = output;
        private void _output(string text) => _out.WriteLine(text);

        [ProtoContract]
        public class Counted
        {
            [ProtoMember(1)] public int Value { get; set; }

            [ProtoIgnore] public List<bool> BeforeCalls { get; } = [];
            [ProtoIgnore] public int AfterCalls { get; set; }

            [ProtoBeforeSerialization]
            public void Before(ISerializationContext context)
                => BeforeCalls.Add(ProtoWriter.IsMeasuring(context));

            [ProtoAfterSerialization]
            public void After() => AfterCalls++;
        }

        private static RuntimeTypeModel Model()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Counted), true);
            return model;
        }

        [Fact]
        public void PlainSerializeFiresBeforeOnce()
        {
            var obj = new Counted { Value = 42 };
            using var ms = new MemoryStream();
            Model().Serialize(ms, obj);

            Assert.Single(obj.BeforeCalls);
            Assert.False(obj.BeforeCalls[0]);   // not a measuring pass
            Assert.Equal(1, obj.AfterCalls);
        }

        [Fact]
        public void MeasureThenSerializeFiresBeforeTwice_OncePerPass()
        {
            var obj = new Counted { Value = 42 };
            using var ms = new MemoryStream();

            var output = (IMeasuredProtoOutput<Stream>)Model();
            using var measured = output.Measure(obj);
            output.Serialize(measured, ms);

            // THE point: once for the measure, once for the write - and the callback can tell
            Assert.Equal(2, obj.BeforeCalls.Count);
            Assert.True(obj.BeforeCalls[0], "the first pass should report IsMeasuring == true");
            Assert.False(obj.BeforeCalls[1], "the second pass is the real write");
            Assert.True(ms.Length > 0);
        }

        [ProtoContract]
        public class Outer
        {
            [ProtoMember(1)] public Counted Inner { get; set; }
        }

        /// <summary>
        /// Does a NESTED contract's callback fire twice without anyone asking to measure? The
        /// answer is per-BACKEND, which is the part that is easy to miss.
        /// </summary>
        /// <remarks>
        /// The stream writer reserves, writes, and back-fills the length (shuffling bytes when the
        /// varint width changes), so it crawls once. The buffer-writer path computes the length
        /// first, writes the prefix, then writes for real and asserts the two agree - throwing
        /// "Length mismatch" if not - so it crawls twice.
        /// </remarks>
        [Fact]
        public void NestedCallbackFiringIsPerBackend()
        {
            var model = Model();
            model.Add(typeof(Outer), true);

            var viaStream = new Outer { Inner = new Counted { Value = 42 } };
            using (var ms = new MemoryStream()) model.Serialize(ms, viaStream);

            var viaBuffer = new Outer { Inner = new Counted { Value = 42 } };
            var bw = new System.Buffers.ArrayBufferWriter<byte>();
            ((IProtoOutput<System.Buffers.IBufferWriter<byte>>)model).Serialize(bw, viaBuffer);

            _output($"stream       : {viaStream.Inner.BeforeCalls.Count} call(s) "
                + $"[{string.Join(", ", viaStream.Inner.BeforeCalls)}]");
            _output($"buffer-writer: {viaBuffer.Inner.BeforeCalls.Count} call(s) "
                + $"[{string.Join(", ", viaBuffer.Inner.BeforeCalls)}]");

            // the stream back-fills the length, so it crawls ONCE and never measures
            Assert.Equal([false], viaStream.Inner.BeforeCalls);

            // the buffer-writer computes the length first and then validates, so it crawls TWICE -
            // and the first crawl IS a measuring pass, with nobody having asked to measure
            Assert.Equal([true, false], viaBuffer.Inner.BeforeCalls);
        }
    }
}
