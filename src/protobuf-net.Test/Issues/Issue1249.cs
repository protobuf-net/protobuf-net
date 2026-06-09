using System.IO;
using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    /// <summary>
    /// https://github.com/protobuf-net/protobuf-net/issues/1249
    /// Misleading "No parameterless constructor found" on abstract Base class when the
    /// inheritance sub-type wrapper (e.g. tag 301) is missing for a strongly-typed
    /// concrete property.
    /// </summary>
    public class Issue1249
    {
        
        private static byte[] BuildFlatPayload()
        {
            var flat = new FlatRoot
            {
                A = new FlatDerivedA
                {
                    Name = "Test A",
                    B = new FlatDerivedB { Name = "Test B" }
                }
            };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, flat);
            return ms.ToArray();
        }

        /// <summary>
        /// Before the fix this threw <see cref="System.InvalidOperationException"/>:
        /// "No parameterless constructor found for Base" – a misleading message
        /// because Base is abstract and should never be instantiated directly.
        /// After the fix, the correct concrete factory (DerivedB) is used, and the
        /// expected <see cref="ProtoException"/> about wire-type mismatch is thrown
        /// instead (the flat payload's string field conflicts with Base's int field).
        /// </summary>
        [Fact]
        public void FlatPayload_MissingInheritanceWrapper_ThrowsProtoException_NotInvalidOperationException()
        {
            var bytes = BuildFlatPayload();

            var ex = Assert.Throws<ProtoException>(() =>
            {
                using var ms = new MemoryStream(bytes);
                Serializer.Deserialize<Base>(ms);
            });

            // The error should be about wire-type mismatch, not about
            // "No parameterless constructor found for Base".
            Assert.DoesNotContain("No parameterless constructor", ex.Message);
            Assert.Contains("wire-type", ex.Message, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies the normal round-trip still works after the factory fix:
        /// a properly-encoded <see cref="DerivedA"/> (with the 301 wrapper for its
        /// nested <see cref="DerivedB"/> property) deserialises correctly.
        /// </summary>
        [Fact]
        public void RoundTrip_WithProperInheritanceWrappers_Works()
        {
            var original = new DerivedA
            {
                Id = 1,
                Name = "Test A",
                B = new DerivedB { Id = 2, Name = "Test B" }
            };

            using var ms = new MemoryStream();
            Serializer.Serialize<Base>(ms, original);
            ms.Position = 0;
            var result = Serializer.Deserialize<Base>(ms) as DerivedA;

            Assert.NotNull(result);
            Assert.Equal(original.Id, result.Id);
            Assert.Equal(original.Name, result.Name);
            Assert.NotNull(result.B);
            Assert.IsType<DerivedB>(result.B);
            Assert.Equal(original.B.Id, result.B.Id);
            Assert.Equal(original.B.Name, result.B.Name);
        }

        /// <summary>
        /// Verifies that deserializing a <see cref="DerivedB"/> directly (without
        /// going through the Base hierarchy) still works after the factory fix.
        /// </summary>
        [Fact]
        public void RoundTrip_DerivedB_Direct_Works()
        {
            var original = new DerivedB { Id = 42, Name = "hello" };

            using var ms = new MemoryStream();
            Serializer.Serialize<Base>(ms, original);
            ms.Position = 0;
            var result = Serializer.Deserialize<Base>(ms) as DerivedB;

            Assert.NotNull(result);
            Assert.Equal(original.Id, result.Id);
            Assert.Equal(original.Name, result.Name);
        }

        /// <summary>
        /// Verifies round-trip works with AutoCompile and CompileInPlace as well.
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RoundTrip_WithProperInheritanceWrappers_Works_AllModes(bool compileInPlace)
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add<Base>(applyDefaultBehaviour: true);
            if (compileInPlace) model.CompileInPlace();

            var original = new DerivedA
            {
                Id = 1,
                Name = "Test A",
                B = new DerivedB { Id = 2, Name = "Test B" }
            };

            using var ms = new MemoryStream();
            model.Serialize<Base>(ms, original);
            ms.Position = 0;
            var result = model.Deserialize<Base>(ms) as DerivedA;

            Assert.NotNull(result);
            Assert.Equal(original.Id, result.Id);
            Assert.Equal(original.Name, result.Name);
            Assert.NotNull(result.B);
            Assert.IsType<DerivedB>(result.B);
            Assert.Equal(original.B.Id, result.B.Id);
            Assert.Equal(original.B.Name, result.B.Name);
        }

        // ── Domain models ────────────────────────────────────────────────────────
        [ProtoContract]
        [ProtoInclude(300, typeof(DerivedA))]
        [ProtoInclude(301, typeof(DerivedB))]
        public abstract class Base
        {
            [ProtoMember(1)]
            public int Id { get; set; }
        }

        [ProtoContract]
        public class DerivedA : Base
        {
            [ProtoMember(1)]
            public string Name { get; set; }

            // Strongly typed to concrete class DerivedB
            [ProtoMember(2)]
            public DerivedB B { get; set; }
        }

        [ProtoContract]
        public class DerivedB : Base
        {
            [ProtoMember(1)]
            public string Name { get; set; }
        }

        // ── Shadow DTOs that generate a "flat" payload (no 301 wrapper) ──────────
        [ProtoContract]
        private class FlatRoot
        {
            [ProtoMember(300)]
            public FlatDerivedA A { get; set; }
        }

        [ProtoContract]
        private class FlatDerivedA
        {
            [ProtoMember(1)]
            public string Name { get; set; }

            [ProtoMember(2)]
            public FlatDerivedB B { get; set; }
        }

        [ProtoContract]
        private class FlatDerivedB
        {
            // Simulates an external payload: just Tag 1, missing the 301 wrapper
            [ProtoMember(1)]
            public string Name { get; set; }
        }
    }
}
