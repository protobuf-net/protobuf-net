// Empirical probe for PR #1213 TODO:
// "v2 impl kinda does the wrong thing and can double-serialize data - probably
//  combination of inheritance+surrogates not fully considered in v2".
//
// Hypothesis: the suspect v2 configuration is "primary-side inheritance
// (Base.AddSubType(Derived)) combined with surrogate on the derived type
// only (Derived.SetSurrogate(DerivedSurrogate))". In v2 MetaType.BuildSerializer,
// the surrogate branch early-returns and ignores primary-side subtypes; the
// derived type's SurrogateSerializer tail writes the *entire* surrogate
// (including base-mirrored fields) inside a 1001 wrapper, while the base
// TypeSerializer independently writes the same base fields at top level.
// Result: base fields appear twice on the wire (once top-level, once inside
// the subtype envelope after surrogate conversion).
//
// This test probes the PR #1213 (v3) behaviour for the same configuration.
// If v3 still double-emits, TODO #4 is a live bug. If v3 rejects the config
// or produces a single canonical encoding, TODO #4 is resolved structurally
// and we lock the behaviour here.
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Issues
{
    public class V2BackCompatDoubleSerializeTests
    {
        private readonly ITestOutputHelper _log;
        public V2BackCompatDoubleSerializeTests(ITestOutputHelper log) => _log = log;

        public class Base
        {
            public int Id { get; set; }
        }

        public class Derived : Base
        {
            public int Extra { get; set; }
        }

        [ProtoContract]
        public class DerivedSurrogate
        {
            [ProtoMember(1)] public int Id { get; set; }
            [ProtoMember(2)] public int Extra { get; set; }

            public static implicit operator DerivedSurrogate(Derived v)
                => v is null ? null : new DerivedSurrogate { Id = v.Id, Extra = v.Extra };

            public static implicit operator Derived(DerivedSurrogate v)
                => v is null ? null : new Derived { Id = v.Id, Extra = v.Extra };
        }

        private static RuntimeTypeModel BuildModel()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            var b = model.Add(typeof(Base), false);
            b.AddField(1, nameof(Base.Id));
            var d = model.Add(typeof(Derived), false);
            b.AddSubType(1001, typeof(Derived));
            d.SetSurrogate(typeof(DerivedSurrogate));
            return model;
        }

        // Hypothesis test: v2 would SILENTLY double-serialize this config on the
        // wire (Id appears once at top level via Base.TypeSerializer, again inside
        // the 1001 wrapper via DerivedSurrogate.TypeSerializer).
        //
        // v3 (PR #1213) result: rejected at model-build time with
        // InvalidOperationException "No suitable conversion operator found for
        // surrogate: Base / DerivedSurrogate". The SurrogateSerializer<TBase, T>
        // generic looks for an op(TBase -> surrogate) where TBase is the primary
        // inheritance root; users typically write op(T -> surrogate), so the
        // configuration fail-fasts rather than producing corrupted wire. This is
        // the preferred v3 outcome (strict > silent data loss).
        [Fact]
        public void PrimaryInheritance_DerivedOnlySurrogate_FailsFast()
        {
            var model = BuildModel();
            var ms = new MemoryStream();
            var ex = Record.Exception(() =>
                model.Serialize<Base>(ms, new Derived { Id = 42, Extra = 99 }));
            Assert.NotNull(ex);
            // Unwrap if the exception bubbled through Activator.CreateInstance.
            var inner = ex is System.Reflection.TargetInvocationException tie ? tie.InnerException : ex;
            _log.WriteLine("outer: " + ex.GetType().Name + " | inner: " + inner?.GetType().Name + " | msg: " + inner?.Message);
            Assert.IsType<InvalidOperationException>(inner);
            Assert.Contains("No suitable conversion operator", inner.Message);
            Assert.Contains(nameof(Base), inner.Message);
            Assert.Contains(nameof(DerivedSurrogate), inner.Message);
        }

        // v3-canonical: configure the surrogate on the primary ROOT type; the
        // derived type's surrogate is discovered via ProtoInclude on the surrogate
        // side. Same conceptual coverage, no double-serialize risk.
        public class BaseSurrogate2
        {
            public int Id { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(1001, typeof(DerivedSurrogate2))]
        public class RootBaseSurrogate
        {
            [ProtoMember(1)] public int Id { get; set; }

            public static implicit operator RootBaseSurrogate(Base v) => v switch
            {
                null => null,
                Derived d => new DerivedSurrogate2 { Id = d.Id, Extra = d.Extra },
                _ => new RootBaseSurrogate { Id = v.Id },
            };

            public static implicit operator Base(RootBaseSurrogate v) => v switch
            {
                null => null,
                DerivedSurrogate2 d => new Derived { Id = d.Id, Extra = d.Extra },
                _ => new Base { Id = v.Id },
            };
        }

        [ProtoContract]
        public class DerivedSurrogate2 : RootBaseSurrogate
        {
            [ProtoMember(1)] public int Extra { get; set; }
        }

        [Fact]
        public void Canonical_SurrogateOnRoot_RoundTrips()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            var b = model.Add(typeof(Base), false);
            model.Add(typeof(Derived), false);
            b.AddSubType(1001, typeof(Derived));
            b.SetSurrogate(typeof(RootBaseSurrogate));

            var ms = new MemoryStream();
            model.Serialize<Base>(ms, new Derived { Id = 42, Extra = 99 });
            var bytes = ms.ToArray();
            _log.WriteLine("wire bytes: " + System.BitConverter.ToString(bytes));
            _log.WriteLine("wire length: " + bytes.Length);

            ms.Position = 0;
            var result = model.Deserialize<Base>(ms);
            var d = Assert.IsType<Derived>(result);
            Assert.Equal(42, d.Id);
            Assert.Equal(99, d.Extra);

            // Verify no double-serialize: field 1 carrying value 42 (Id) must
            // appear exactly once on the wire. Pattern `0x08 0x2A` =
            // tag(field=1, wiretype=varint) + varint(42).
            int idOccurrences = 0;
            for (int i = 0; i < bytes.Length - 1; i++)
            {
                if (bytes[i] == 0x08 && bytes[i + 1] == 0x2A) idOccurrences++;
            }
            Assert.Equal(1, idOccurrences);
        }
    }
}
