using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ProtoBuf.Issues
{
    public class GenericArraySchemaErrorTest
    {
        [Fact]
        public void CanGenerateGenericArraySchema()
        {
            var typeModel = TypeModel.Create();

            typeModel.Add(typeof(ClassWithGenericField<SimpleClass[]>), true);

            // Will throw System.ArgumentException
            // "Data of this type has inbuilt behaviour, and cannot be added to a model in this way: SciTech.Rpc.BaseClass[]"
            string schema = typeModel.GetSchema(null);

            Assert.NotEmpty(schema);
        }

        [Fact]
        public void HasValidGenericArrayMessageName()
        {
            var typeModel = TypeModel.Create();

            typeModel.Add(typeof(ClassWithGenericField<SimpleClass[]>), true);

            string schema = typeModel.GetSchema(null);
            Assert.Contains("ClassWithGenericField_Array_SimpleClass", schema);
            Assert.DoesNotContain("[]", schema);
        }
    }

    [ProtoContract]
    public sealed class ClassWithGenericField<T>
    {
        [ProtoMember(1)]
        public T Value;
    }

    [ProtoContract]
    public sealed class SimpleClass
    {
        [ProtoMember(1)]
        public int Value;
    }
}
