// Regression tests for PR #1213 TODO: "check behaviour of non-root deserialize
// (where payload is not explicitly the subclass, or example: empty stream)".
//
// Findings: current behaviour is consistent between plain inheritance and
// inheritance+surrogates (this PR's addition). In both setups:
//   * Deserialize as the root type always succeeds, even on empty stream
//     (autoCreate yields a default root instance).
//   * Deserialize as a subclass when the payload lacks the subtype marker
//     (or is empty) fails with System.InvalidCastException at the API
//     boundary, because the deserializer materialises a root instance and
//     the final cast to the requested subclass fails.
//
// The error message is a generic CLR InvalidCastException, not a
// protobuf-net-specific one; improving that would be a separate follow-up
// and is out of scope for PR #1213 (the inheritance+surrogate change does
// not regress the behaviour or the message). These tests lock the current
// semantics so any future change surfaces intentionally.
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class NonRootDeserializeTests
    {
        // --- user types ---
        public class Base
        {
            public int Id { get; set; }
        }

        public class Derived : Base
        {
            public int Extra { get; set; }
        }

        // --- surrogates (inheritance + surrogate scenario) ---
        [ProtoContract]
        [ProtoInclude(1001, typeof(DerivedSurrogate))]
        public class BaseSurrogate
        {
            [ProtoMember(1)] public int Id { get; set; }

            public static implicit operator BaseSurrogate(Base value) => value switch
            {
                null => null,
                Derived d => new DerivedSurrogate { Id = d.Id, Extra = d.Extra },
                _ => new BaseSurrogate { Id = value.Id },
            };

            public static implicit operator Base(BaseSurrogate value) => value switch
            {
                null => null,
                DerivedSurrogate d => new Derived { Id = d.Id, Extra = d.Extra },
                _ => new Base { Id = value.Id },
            };
        }

        [ProtoContract]
        public class DerivedSurrogate : BaseSurrogate
        {
            [ProtoMember(1)] public int Extra { get; set; }

            public static implicit operator DerivedSurrogate(Derived value)
                => value is null ? null : new DerivedSurrogate { Id = value.Id, Extra = value.Extra };

            public static implicit operator Derived(DerivedSurrogate value)
                => value is null ? null : new Derived { Id = value.Id, Extra = value.Extra };
        }

        private static RuntimeTypeModel BuildPlainModel()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            var b = model.Add(typeof(Base), false);
            b.AddField(1, nameof(Base.Id));
            var d = model.Add(typeof(Derived), false);
            d.AddField(2, nameof(Derived.Extra));
            b.AddSubType(1001, typeof(Derived));
            return model;
        }

        private static RuntimeTypeModel BuildSurrogateModel()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            var b = model.Add(typeof(Base), false);
            var d = model.Add(typeof(Derived), false);
            b.AddSubType(1001, typeof(Derived));
            b.SetSurrogate(typeof(BaseSurrogate));
            return model;
        }

        // --- plain inheritance ---

        [Fact]
        public void Plain_PayloadIsBase_DeserializeAsDerived_Throws()
        {
            var model = BuildPlainModel();
            var ms = new MemoryStream();
            model.Serialize(ms, new Base { Id = 7 });
            ms.Position = 0;

            var ex = Assert.Throws<InvalidCastException>(
                () => model.Deserialize(ms, null, typeof(Derived)));
            Assert.Contains("Base", ex.Message);
            Assert.Contains("Derived", ex.Message);
        }

        [Fact]
        public void Plain_EmptyStream_DeserializeAsDerived_Throws()
        {
            var model = BuildPlainModel();
            var ms = new MemoryStream();
            Assert.Throws<InvalidCastException>(
                () => model.Deserialize(ms, null, typeof(Derived)));
        }

        [Fact]
        public void Plain_EmptyStream_DeserializeAsBase_ReturnsDefault()
        {
            var model = BuildPlainModel();
            var ms = new MemoryStream();
            var result = model.Deserialize(ms, null, typeof(Base));
            var b = Assert.IsType<Base>(result);
            Assert.Equal(0, b.Id);
        }

        // --- inheritance + surrogate (PR #1213 addition) ---

        [Fact]
        public void Surrogate_PayloadIsBase_DeserializeAsDerived_Throws()
        {
            var model = BuildSurrogateModel();
            var ms = new MemoryStream();
            model.Serialize(ms, new Base { Id = 9 });
            ms.Position = 0;

            var ex = Assert.Throws<InvalidCastException>(
                () => model.Deserialize(ms, null, typeof(Derived)));
            Assert.Contains("Base", ex.Message);
            Assert.Contains("Derived", ex.Message);
        }

        [Fact]
        public void Surrogate_EmptyStream_DeserializeAsDerived_Throws()
        {
            var model = BuildSurrogateModel();
            var ms = new MemoryStream();
            Assert.Throws<InvalidCastException>(
                () => model.Deserialize(ms, null, typeof(Derived)));
        }

        [Fact]
        public void Surrogate_EmptyStream_DeserializeAsBase_ReturnsDefault()
        {
            var model = BuildSurrogateModel();
            var ms = new MemoryStream();
            var result = model.Deserialize(ms, null, typeof(Base));
            var b = Assert.IsType<Base>(result);
            Assert.Equal(0, b.Id);
        }
    }
}
