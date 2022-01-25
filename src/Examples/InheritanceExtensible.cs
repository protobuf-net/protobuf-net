using Examples;
using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf
{
    public class InheritanceExtensible
    {
        [Fact]
        public void CanRoundTripTrivialExtensibleModel_NonCompiled()
            => CanRoundTrip<SomeTrivialType>(model => model);

        [Fact]
        public void CanRoundTripTrivialExtensibleModel_CompileInPlace()
            => CanRoundTrip<SomeTrivialType>(model => { model.CompileInPlace(); return model; });

        [Fact]
        public void CanRoundTripTrivialExtensibleModel_Compile()
            => CanRoundTrip<SomeTrivialType>(model => model.Compile());

        [Fact]
        public void CanRoundTripTrivialExtensibleModel_CompileDll()
            => CanRoundTrip<SomeTrivialType>(model =>
            {
                var compiled = model.Compile(nameof(CanRoundTripTrivialExtensibleModel_CompileDll), nameof(CanRoundTripTrivialExtensibleModel_CompileDll) + ".dll");
                PEVerify.AssertValid(nameof(CanRoundTripTrivialExtensibleModel_CompileDll) + ".dll");
                return compiled;
            });

        [Fact]
        public void CanReliablyRoundTripRetainingData_NonCompiled()
            => CanReliablyRoundTripRetainingData(model => model);

        [Fact]
        public void CanReliablyRoundTripRetainingData_CompileInPlace()
            => CanReliablyRoundTripRetainingData(model => { model.CompileInPlace(); return model; });

        [Fact]
        public void CanReliablyRoundTripRetainingData_Compile()
            => CanReliablyRoundTripRetainingData(model => model.Compile());

        [Fact]
        public void CanReliablyRoundTripRetainingData_CompileDll()
            => CanReliablyRoundTripRetainingData(model =>
            {
                var compiled = model.Compile(nameof(CanRoundTripTrivialExtensibleModel_CompileDll), nameof(CanRoundTripTrivialExtensibleModel_CompileDll) + ".dll");
                PEVerify.AssertValid(nameof(CanRoundTripTrivialExtensibleModel_CompileDll) + ".dll");
                return compiled;
            });

        [Fact]
        public void CanRoundTripBasicExtensibleModel_NonCompiled()
            => CanRoundTrip<SomeMiddleType>(model => model);

        [Fact]
        public void CanRoundTripBasicExtensibleModel_CompileInPlace()
            => CanRoundTrip<SomeMiddleType>(model => { model.CompileInPlace(); return model; });

        [Fact]
        public void CanRoundTripBasicExtensibleModel_Compile()
            => CanRoundTrip<SomeMiddleType>(model => model.Compile());

        [Fact]
        public void CanRoundTripBasicExtensibleModel_CompileDll()
            => CanRoundTrip<SomeMiddleType>(model =>
            {
                var compiled = model.Compile(nameof(CanRoundTripBasicExtensibleModel_CompileDll), nameof(CanRoundTripBasicExtensibleModel_CompileDll) + ".dll");
                PEVerify.AssertValid(nameof(CanRoundTripBasicExtensibleModel_CompileDll) + ".dll");
                return compiled;
            });

        private void CanRoundTrip<T>(Func<RuntimeTypeModel, TypeModel> callback) where T : class, new()
        {
            var original = new T();
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add<T>();
            var finalModel = callback(model);
            var clone = finalModel.DeepClone(original);
            Assert.NotSame(original, clone);
        }

        private void CanReliablyRoundTripRetainingData(Func<RuntimeTypeModel, TypeModel> callback)
        {
            var original = new SomeRichLeafType
            {
                A = 12,
                B = 42,
                C = 92
            };
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add<SomeRichBaseType>();
            model.Add<SomeBaseType>();
            var finalModel = callback(model);

            SomeRichLeafType clone;
            using (var ms = new MemoryStream())
            {
                finalModel.Serialize(ms, original);
                ms.Position = 0;
                var raw = finalModel.Deserialize<SomeBaseType>(ms);

                ms.Position = 0;
                ms.SetLength(0);

                finalModel.Serialize(ms, raw);
                ms.Position = 0;
                clone = (SomeRichLeafType)finalModel.Deserialize<SomeRichBaseType>(ms);
            }
            Assert.NotSame(original, clone);

            Assert.Equal(12, clone.A);
            Assert.Equal(42, clone.B);
            Assert.Equal(92, clone.C);
        }


        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public sealed class SomeTrivialType : Extensible
        {
            [ProtoMember(1)]
            public int Id { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(10, typeof(SomeMiddleType))]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public abstract class SomeBaseType : Extensible
        {
            [ProtoMember(1)]
            public int Id { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(10, typeof(SomeLeafType))]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public class SomeMiddleType : SomeBaseType
        {
            [ProtoMember(1)]
            public string Message { get; set; }
        }

        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public sealed class SomeLeafType : SomeMiddleType
        {
            [ProtoMember(1)]
            public DateTime When { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(10, typeof(SomeRichMiddleType))]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public abstract class SomeRichBaseType : Extensible
        {
            [ProtoMember(1)]
            public int Id { get; set; }

            [ProtoMember(2)]
            public int A { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(10, typeof(SomeRichLeafType))]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public class SomeRichMiddleType : SomeRichBaseType
        {
            [ProtoMember(1)]
            public string Message { get; set; }

            [ProtoMember(2)]
            public int B { get; set; }
        }

        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public sealed class SomeRichLeafType : SomeRichMiddleType
        {
            [ProtoMember(1)]
            public DateTime When { get; set; }

            [ProtoMember(2)]
            public int C { get; set; }
        }
    }
}
