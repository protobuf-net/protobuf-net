using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
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

            // Will throw System.ArgumentException in v3.0.0-alpha.43
            // "Data of this type has inbuilt behaviour, and cannot be added to a model in this way: SciTech.Rpc.BaseClass[]"
            string schema = typeModel.GetSchema(null);

            Assert.NotEmpty(schema);
        }


        [Theory]
        [InlineData(typeof(ClassWithGenericField<SimpleClass[]>), "repeated SimpleClass Value = 1;", "ClassWithGenericField_Array_SimpleClass")]
        [InlineData(typeof(ClassWithGenericField<byte[]>), "bytes Value = 1;", "ClassWithGenericField_Array_Byte")]
        [InlineData(typeof(ClassWithGenericField<int[]>), "repeated int32 Value = 1;", "ClassWithGenericField_Array_Int32")]
        public void HasValidGenericArraySchema( Type genericArrayType ,string expectedValueDecl, string expectedMessageName)
        {
            // Combined generic test similar to CanGenerateGenericArraySchema and 
            // HasValidGenericArrayMessageName, for different array types
            var typeModel = TypeModel.Create();

            typeModel.Add(genericArrayType, true);

            // Will throw System.ArgumentException in v3.0.0-alpha.43 (except for byte[])
            string schema = typeModel.GetSchema(null);

            // Validate schema. Can be significantly improved, but should suffice for this 
            // bug fix I think.
            Assert.Contains(expectedValueDecl, schema);
            Assert.Contains(expectedMessageName, schema);

            Assert.DoesNotContain("[]", schema);
        }


        [Theory]
        [InlineData(typeof(ClassWithGenericBytesMember), new string[] {
            "message ClassWithGenericBytesMember",
            "ClassWithGenericField_Array_Byte BytesInMember = 1;",
            "bytes BytesValue = 2;",
            "message ClassWithGenericField_Array_Byte",
            "bytes Value = 1;" }
            )]
        [InlineData(typeof(ClassWithGenericClassMember), new string[] {
            "message ClassWithGenericClassMember",
            "ClassWithGenericField_Array_SimpleClass ClassArrayInMember = 1;",
            "repeated SimpleClass ClassArrayValue = 2;",
            "message ClassWithGenericField_Array_SimpleClass",
            "repeated SimpleClass Value = 1;" })]
        public void HasValidGenericArrayMemberSchema(Type genericArrayType, string[] expectedSchemaElements)
        {
            // Combined generic test similar to CanGenerateGenericArraySchema and 
            // HasValidGenericArrayMessageName, for different array types
            var typeModel = TypeModel.Create();

            typeModel.Add(genericArrayType, true);

            // Will throw System.ArgumentException in v3.0.0-alpha.43 (except for byte[])
            string schema = typeModel.GetSchema(null);

            // Validate schema. Can be significantly improved, but should suffice for this 
            // bug fix I think.
            foreach (var schemaElement in expectedSchemaElements)
            {
                Assert.Contains(schemaElement, schema);
            }

            Assert.DoesNotContain("[]", schema);
        }

        [Theory]
        [InlineData(typeof(ClassWithGenericField<List<SimpleClass[]>>))]
        [InlineData(typeof(ClassWithGenericField<List<SimpleClass>[]>))]
        [InlineData(typeof(ClassWithGenericField<SimpleClass[][]>))]
        [InlineData(typeof(ClassWithGenericField<SimpleClass[,]>))]
        // byte[][] does not throw NotSupportedException, which it should probably do.
        //[InlineData(typeof(ClassWithGenericField<byte[][]>))]
        // byte[,] does not throw NotSupportedException, which it should probably do.
        //[InlineData(typeof(ClassWithGenericField<byte[,]>))]
        [InlineData(typeof(ClassWithGenericField<int[][]>))]
        [InlineData(typeof(ClassWithGenericField<int[,]>))]
        public void InvalidNestedGenericField(Type genericArrayType)
        {
            // Combined generic test similar to CanGenerateGenericArraySchema and 
            // HasValidGenericArrayMessageName, for byte arrays
            var typeModel = TypeModel.Create();
            var schema = typeModel.GetSchema(null);
            Assert.Throws<NotSupportedException>( ()=>typeModel.Add(genericArrayType, true) );
        }
    }



    [ProtoContract]
    public sealed class ClassWithGenericBytesMember
    {
        [ProtoMember(1)]
        public ClassWithGenericField<byte[]> BytesInMember;

        [ProtoMember(2)]
        public byte[] BytesValue;
    }

    [ProtoContract]
    public sealed class ClassWithGenericClassMember
    {
        [ProtoMember(1)]
        public ClassWithGenericField<SimpleClass[]> ClassArrayInMember;

        [ProtoMember(2)]
        public SimpleClass[] ClassArrayValue;
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
