using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace ProtoBuf.AotConformance
{
    /// <summary>
    /// Serialize callbacks on a <c>[ProtoInclude]</c> hierarchy: gap B46.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generated model fired <b>none of them</b> until 2026-08-22 - <c>WriteSubType</c> simply
    /// never called <c>EmitCallback</c> - so a consumer's before-serialize hook silently never ran
    /// for any type in a hierarchy. That is the failure mode worth having a test for: it is
    /// invisible unless the callback has an observable side-effect.
    /// </para>
    /// <para>
    /// <b>The rule was probed against ref-emit, not reasoned about</b>, and the obvious guess is
    /// wrong. Members work per layer - each layer writes its own - so one expects callbacks to as
    /// well. They do not: with a distinct callback on each of three layers,
    /// <c>RuntimeTypeModel</c> fires the <b>root's</b> and nothing else, whatever the runtime type
    /// and whatever declared type it is serialized as. Hence <c>HookedDerived</c> carries callbacks
    /// that must never appear.
    /// </para>
    /// </remarks>
    public class CallbackHierarchyTests
    {
        private static readonly Assembly Fixtures = typeof(CallbackHierarchyTests).Assembly;

        private static TypeModel Generated() => (TypeModel)Activator.CreateInstance(
            Fixtures.GetType("AotFixtures.Callbacks.CallbacksModel")!, nonPublic: true)!;

        private static TypeModel Reference()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(Fixtures.GetType("AotFixtures.Callbacks.HookedBase")!, true);
            model.Add(Fixtures.GetType("AotFixtures.Callbacks.HookedHolder")!, true);
            model.CompileInPlace();
            return model;
        }

        private static object Derived(int value, int extra)
        {
            var type = Fixtures.GetType("AotFixtures.Callbacks.HookedDerived")!;
            var instance = Activator.CreateInstance(type)!;
            type.GetProperty("Value")!.SetValue(instance, value);
            type.GetProperty("Extra")!.SetValue(instance, extra);
            return instance;
        }

        private static string TraceOf(object instance)
            => (string)instance.GetType().GetProperty("Trace")!.GetValue(instance)!;

        private static void ToStream(TypeModel model, object value)
        {
            using var ms = new MemoryStream();
            model.Serialize(ms, value);
        }

        [Fact]
        public void TheRootsCallbacksFireAndTheDerivedLayersDoNot()
        {
            var value = Derived(11, 12);
            ToStream(Generated(), value);

            var trace = TraceOf(value);
            Assert.Contains("base-bs;", trace);
            Assert.Contains("base-as;", trace);
            // the whole point of the fixture: a per-layer rule would put these here
            Assert.DoesNotContain("derived-", trace);
        }

        [Fact]
        public void TheGeneratedModelAgreesWithRefEmitOnWhichCallbacksRun()
        {
            // "which", not "how many" - the count is a property of the PATH (a measure-first
            // contract crawls twice), and AGENTS.md records that as the intended alignment. What
            // must not differ is the SET, which is what silently regressed.
            static string[] Distinct(string trace)
                => trace.Split(';', StringSplitOptions.RemoveEmptyEntries).Distinct().OrderBy(x => x).ToArray();

            var mine = Derived(11, 12);
            ToStream(Generated(), mine);

            var theirs = Derived(11, 12);
            ToStream(Reference(), theirs);

            Assert.Equal(Distinct(TraceOf(theirs)), Distinct(TraceOf(mine)));
            Assert.NotEmpty(Distinct(TraceOf(theirs))); // the reference really does fire something
        }

        [Fact]
        public void ANestedHierarchyStillFiresTheRootsCallbacks()
        {
            // nested is where the measure pass genuinely runs over the hierarchy, rather than the
            // root case where nothing above it needs a length
            var inner = Derived(13, 14);
            var holderType = Fixtures.GetType("AotFixtures.Callbacks.HookedHolder")!;
            var holder = Activator.CreateInstance(holderType)!;
            holderType.GetProperty("Inner")!.SetValue(holder, inner);

            ToStream(Generated(), holder);

            var trace = TraceOf(inner);
            Assert.Contains("base-bs;", trace);
            Assert.Contains("base-as;", trace);
            Assert.DoesNotContain("derived-", trace);
        }

        [Fact]
        public void BothPassesOfAMeasureFirstHierarchyObserveTheSameObject()
        {
            // the invariant that makes a measured length trustworthy: if the two passes fired a
            // different number of callbacks, they would be walking different objects. Writing to an
            // IBufferWriter is the path that measures, so before-serialize is expected TWICE - and
            // symmetrically after-serialize twice, never a 2/1 split.
            var value = Derived(11, 12);
            var writer = new Discard();
            Generated().Serialize<object>(writer, value);

            var trace = TraceOf(value);
            var before = trace.Split(new[] { "base-bs;" }, StringSplitOptions.None).Length - 1;
            var after = trace.Split(new[] { "base-as;" }, StringSplitOptions.None).Length - 1;
            Assert.Equal(before, after);
            Assert.InRange(before, 1, 2); // "at most twice", however deep - never more
        }

        [Fact]
        public void TheRootsDESERIALIZECallbacksFireAndTheDerivedLayersDoNot()
        {
            // ref-emit routes these through SubTypeState<T>.OnBeforeDeserialize, which fires at
            // MATERIALISATION - probed: the hook sees the instance already constructed as the
            // SUB-TYPE, with the root's own fields not yet read. There is no fixed point in the
            // field loop that matches, which is why it needs that mechanism rather than a
            // statement in the emitted loop.
            var payload = new MemoryStream();
            Generated().Serialize(payload, Derived(11, 12));
            payload.Position = 0;

            var baseType = Fixtures.GetType("AotFixtures.Callbacks.HookedBase")!;
            var read = Generated().Deserialize(payload, null, baseType)!;

            var trace = TraceOf(read);
            Assert.Contains("base-bd;", trace);
            Assert.Contains("base-ad;", trace);
            Assert.DoesNotContain("derived-", trace);
        }

        [Fact]
        public void TheDeserializeCallbackSeesTheSubTypeAlreadyConstructed()
        {
            // the positioning, not merely the firing: the reference fires the root's before-hook
            // once the payload has named the layer to build, so the callback observes a Derived
            var payload = new MemoryStream();
            Reference().Serialize(payload, Derived(11, 12));
            payload.Position = 0;

            var baseType = Fixtures.GetType("AotFixtures.Callbacks.HookedBase")!;
            var theirs = Reference().Deserialize(payload, null, baseType)!;
            Assert.Equal("AotFixtures.Callbacks.HookedDerived", theirs.GetType().FullName);
            Assert.Contains("base-bd;", TraceOf(theirs));
        }

        private sealed class Discard : IBufferWriter<byte>
        {
            private readonly byte[] _array = new byte[64 * 1024];
            private int _index;
            public void Advance(int count) => _index += count;
            public Memory<byte> GetMemory(int sizeHint = 0) => _array.AsMemory(_index);
            public Span<byte> GetSpan(int sizeHint = 0) => _array.AsSpan(_index);
        }
    }
}
