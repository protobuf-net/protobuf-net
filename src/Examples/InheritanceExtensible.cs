using ProtoBuf.Meta;
using System;
using Xunit;

namespace ProtoBuf
{
    public class InheritanceExtensible
    {
        [Fact]
        public void CanRoundTripTrivialExtensibleModel_NonCompiled()
            => CanRoundTripTrivialExtensibleModelImpl(model => model);

        [Fact]
        public void CanRoundTripTrivialExtensibleModel_CompileInPlace()
            => CanRoundTripTrivialExtensibleModelImpl(model => { model.CompileInPlace(); return model; });

        [Fact]
        public void CanRoundTripTrivialExtensibleModel_Compile()
            => CanRoundTripTrivialExtensibleModelImpl(model => model.Compile());

        [Fact]
        public void CanRoundTripBasicExtensibleModel_NonCompiled()
            => CanRoundTripBasicExtensibleModelImpl(model => model);

        [Fact]
        public void CanRoundTripBasicExtensibleModel_CompileInPlace()
            => CanRoundTripBasicExtensibleModelImpl(model => { model.CompileInPlace(); return model; });

        [Fact]
        public void CanRoundTripBasicExtensibleModel_Compile()
            => CanRoundTripBasicExtensibleModelImpl(model => model.Compile());

        private void CanRoundTripBasicExtensibleModelImpl(Func<RuntimeTypeModel, TypeModel> callback)
        {
            var original = new SomeLeafType();
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add<SomeBaseType>();
            var clone = callback(model).DeepClone(original);
            Assert.NotSame(original, clone);
        }

        private void CanRoundTripTrivialExtensibleModelImpl(Func<RuntimeTypeModel, TypeModel> callback)
        {
            var original = new SomeTrivialType();
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add<SomeTrivialType>();
            var clone = callback(model).DeepClone(original);
            Assert.NotSame(original, clone);
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
    }
}
