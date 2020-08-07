using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Issues
{
    public class Issue692
    {
        [Fact]
        public void IgnoringIgnoresInvalidSubType()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            IgnoringSubTypes derived = new IgnoringSubTypesDerivedClass { Foo = 42 },
                @base = new IgnoringSubTypes { Foo = 42 };
            CheckSuccessGeneric(model, @base, "D0-02-2A");
            CheckSuccessGeneric(model, derived, "D0-02-2A");
            CheckSuccessNonGeneric(model, @base, "D0-02-2A");
            CheckSuccessNonGeneric(model, derived, "D0-02-2A");
            model.CompileInPlace();
            CheckSuccessGeneric(model, @base, "D0-02-2A");
            CheckSuccessGeneric(model, derived, "D0-02-2A");
            CheckSuccessNonGeneric(model, @base, "D0-02-2A");
            CheckSuccessNonGeneric(model, derived, "D0-02-2A");
            var compiled = model.Compile();
            CheckSuccessGeneric(compiled, @base, "D0-02-2A");
            CheckSuccessGeneric(compiled, derived, "D0-02-2A");
            CheckSuccessNonGeneric(compiled, @base, "D0-02-2A");
            CheckSuccessNonGeneric(compiled, derived, "D0-02-2A");
        }

        [Fact]
        public void DefaultDetectsInvalidSubType()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            DefaultConfiguration derived = new DefaultConfigurationDerivedClass { Foo = 42 },
                @base = new DefaultConfiguration { Foo = 42 };
            CheckSuccessGeneric(model, @base, "D0-02-2A");
            AssertThrowsGeneric(model, derived, "Unexpected sub-type: ProtoBuf.Issues.Issue692+DefaultConfigurationDerivedClass");
            CheckSuccessNonGeneric(model, @base, "D0-02-2A");
            AssertThrowsNonGeneric(model, derived, "Unexpected sub-type: ProtoBuf.Issues.Issue692+DefaultConfigurationDerivedClass");
            model.CompileInPlace();
            CheckSuccessGeneric(model, @base, "D0-02-2A");
            AssertThrowsGeneric(model, derived, "Unexpected sub-type: ProtoBuf.Issues.Issue692+DefaultConfigurationDerivedClass");
            CheckSuccessNonGeneric(model, @base, "D0-02-2A");
            AssertThrowsNonGeneric(model, derived, "Unexpected sub-type: ProtoBuf.Issues.Issue692+DefaultConfigurationDerivedClass");
            var compiled = model.Compile();
            CheckSuccessGeneric(compiled, @base, "D0-02-2A");
            AssertThrowsGeneric(compiled, derived, "Unexpected sub-type: ProtoBuf.Issues.Issue692+DefaultConfigurationDerivedClass");
            CheckSuccessNonGeneric(compiled, @base, "D0-02-2A");
            AssertThrowsNonGeneric(compiled, derived, "Unexpected sub-type: ProtoBuf.Issues.Issue692+DefaultConfigurationDerivedClass");
        }

        [Fact]
        public void RespectingDetectsInvalidSubType()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            RespectingSubTypes derived = new RespectingSubTypesDerivedClass { Foo = 42 },
                @base = new RespectingSubTypes { Foo = 42 };
            CheckSuccessGeneric(model, @base, "D0-02-2A");
            AssertThrowsGeneric(model, derived, "Unexpected sub-type: ProtoBuf.Issues.Issue692+RespectingSubTypesDerivedClass");
            CheckSuccessNonGeneric(model, @base, "D0-02-2A");
            AssertThrowsNonGeneric(model, derived, "Unexpected sub-type: ProtoBuf.Issues.Issue692+RespectingSubTypesDerivedClass");
            model.CompileInPlace();
            CheckSuccessGeneric(model, @base, "D0-02-2A");
            AssertThrowsGeneric(model, derived, "Unexpected sub-type: ProtoBuf.Issues.Issue692+RespectingSubTypesDerivedClass");
            CheckSuccessNonGeneric(model, @base, "D0-02-2A");
            AssertThrowsNonGeneric(model, derived, "Unexpected sub-type: ProtoBuf.Issues.Issue692+RespectingSubTypesDerivedClass");
            var compiled = model.Compile();
            CheckSuccessGeneric(compiled, @base, "D0-02-2A");
            AssertThrowsGeneric(compiled, derived, "Unexpected sub-type: ProtoBuf.Issues.Issue692+RespectingSubTypesDerivedClass");
            CheckSuccessNonGeneric(compiled, @base, "D0-02-2A");
            AssertThrowsNonGeneric(compiled, derived, "Unexpected sub-type: ProtoBuf.Issues.Issue692+RespectingSubTypesDerivedClass");
        }

        static void CheckSuccessGeneric<T>(TypeModel model, T obj, string expectedHex)
            where T : class, IHazFoo
        {
            // via serialize/deserialize
            using var ms = new MemoryStream();
            model.Serialize<T>(ms, obj);
            ms.Position = 0;
            var clone = model.Deserialize<T>(ms);
            Assert.NotSame(obj, clone);
            Assert.IsType<T>(clone);
            Assert.Equal(obj.Foo, clone.Foo);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal(expectedHex, hex, ignoreCase: true);

            // via clone
            clone = model.DeepClone<T>(obj);
            Assert.NotSame(obj, clone);
            Assert.IsType<T>(clone);
            Assert.Equal(obj.Foo, clone.Foo);

        }

        static void CheckSuccessNonGeneric<T>(TypeModel model, T obj, string expectedHex)
            where T : class, IHazFoo
        {
            // via serialize/deserialize
            using var ms = new MemoryStream();
            model.Serialize(ms, (object)obj);
            ms.Position = 0;
            var clone = (T)model.Deserialize(ms, null, typeof(T));
            Assert.NotSame(obj, clone);
            Assert.IsType<T>(clone);
            Assert.Equal(obj.Foo, clone.Foo);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal(expectedHex, hex, ignoreCase: true);

            // via clone
            clone = (T)model.DeepClone((object)obj);
            Assert.NotSame(obj, clone);
            Assert.IsType<T>(clone);
            Assert.Equal(obj.Foo, clone.Foo);
        }

        static void AssertThrowsGeneric<T>(TypeModel model, T obj, string expectedError)
            where T : class, IHazFoo
        {
            using var ms = new MemoryStream();
            var ex = Assert.Throws<InvalidOperationException>(() => model.Serialize<T>(ms, obj));
            Assert.Equal(expectedError, ex.Message);
        }

        static void AssertThrowsNonGeneric<T>(TypeModel model, T obj, string expectedError)
        {
            using var ms = new MemoryStream();
            var ex = Assert.Throws<InvalidOperationException>(() => model.Serialize(ms, (object)obj));
            Assert.Equal(expectedError, ex.Message);
        }


        interface IHazFoo
        {
            int Foo { get; }
        }
        [ProtoContract(IgnoreUnknownSubTypes = true)]
        public class IgnoringSubTypes : IHazFoo
        {
            [ProtoMember(42)]
            public int Foo { get; set; }
        }

        public class IgnoringSubTypesDerivedClass : IgnoringSubTypes { }

        [ProtoContract(IgnoreUnknownSubTypes = false)]
        public class RespectingSubTypes : IHazFoo
        {
            [ProtoMember(42)]
            public int Foo { get; set; }
        }

        public class RespectingSubTypesDerivedClass : RespectingSubTypes { }

        [ProtoContract]
        public class DefaultConfiguration : IHazFoo
        {
            [ProtoMember(42)]
            public int Foo { get; set; }
        }

        public class DefaultConfigurationDerivedClass : DefaultConfiguration { }
    }
}
