using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Examples
{
    
    public class NonPublic_Compile
    {
        private static void Compile<T>()
        {
            var model = TypeModel.Create();
            model.Add(typeof(T), true);
            string name = typeof(T).Name + "Serializer", path = name + ".dll";
            model.Compile(name, path);
            Assert.Throws<Exception>(() =>
            {
                PEVerify.AssertValid(path);
            });
        }
        [ProtoContract]
        private class PrivateType
        {
        }
        [Fact]
        public void PrivateTypeShouldFail()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                Compile<PrivateType>();
            }, "Non-public type cannot be used with full dll compilation: Examples.NonPublic_Compile+PrivateType");
        }
        private class NonPublicWrapper
        {
            [ProtoContract]
            internal class IndirectlyPrivateType
            {
            }
        }
        [Fact]
        public void IndirectlyPrivateTypeShouldFail()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                Compile<NonPublicWrapper.IndirectlyPrivateType>();
            }, "Non-public type cannot be used with full dll compilation: Examples.NonPublic_Compile+NonPublicWrapper+IndirectlyPrivateType");
        }
        [ProtoContract]
        public class PrivateCallback
        {
            [ProtoBeforeSerialization]
            private void OnDeserialize() { }
        }
        [Fact]
        public void PrivateCallbackShouldFail()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                Compile<PrivateCallback>();
            }, "Non-public member cannot be used with full dll compilation: Examples.NonPublic_Compile+PrivateCallback.OnDeserialize");
        }

        [ProtoContract]
        public class PrivateField
        {
#pragma warning disable 0169
            [ProtoMember(1)]
            private int Foo;
#pragma warning restore 0169
        }
        [Fact]
        public void PrivateFieldShouldFail()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                Compile<PrivateField>();
            }, "Non-public member cannot be used with full dll compilation: Examples.NonPublic_Compile+PrivateField.Foo");
        }
        [ProtoContract]
        public class PrivateProperty
        {
            [ProtoMember(1)]
            private int Foo { get; set; }
        }
        [Fact]
        public void PrivatePropertyShouldFail()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                Compile<PrivateProperty>();
            }, "Non-public member cannot be used with full dll compilation: Examples.NonPublic_Compile+PrivateProperty.get_Foo");
        }
        [ProtoContract]
        public class PrivatePropertyGet
        {
            [ProtoMember(1)]
            public int Foo { private get; set; }
        }
        [Fact]
        public void PrivatePropertyGetShouldFail()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                Compile<PrivatePropertyGet>();
            }, "Non-public member cannot be used with full dll compilation: Examples.NonPublic_Compile+PrivatePropertyGet.get_Foo");
        }
        [ProtoContract]
        public class PrivatePropertySet
        {
            [ProtoMember(1)]
            public int Foo { get; private set; }
        }
        [Fact]
        public void PrivatePropertySetShouldFail()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                Compile<PrivatePropertySet>();
            }, "Cannot apply changes to property Examples.NonPublic_Compile+PrivatePropertySet.Foo");
        }
        [ProtoContract]
        public class PrivateConditional
        {
            [ProtoMember(1)]
            public int Foo { get; set; }

            private bool ShouldSerializeFoo() { return true; }
        }
        [Fact]
        public void PrivateConditionalSetShouldFail()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                Compile<PrivateConditional>();
            }, "Non-public member cannot be used with full dll compilation: Examples.NonPublic_Compile+PrivateConditional.ShouldSerializeFoo");
        }
        [ProtoContract]
        public class PrivateConstructor
        {
            private PrivateConstructor() { }
            [ProtoMember(1)]
            public int Foo { get; set; }
        }
        [Fact]
        public void PrivateConstructorShouldFail()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                Compile<PrivateConstructor>();
            }, "Non-public member cannot be used with full dll compilation: Examples.NonPublic_Compile+PrivateConstructor..ctor");
        }
    }
}
