using Examples.SimpleStream;
using NUnit.Framework;
using ProtoBuf;
using System.ComponentModel;
using ProtoBuf.Meta;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System;

namespace Examples
{
    [TestFixture]
    public class ProtoGeneration
    {
        [Test]
        public void GetProtoTest1()
        {
            var model = TypeModel.Create();
            model.UseImplicitZeroDefaults = false;

            string proto = model.GetSchema(typeof(Test1));

            Assert.AreEqual(
@"package Examples.SimpleStream;

message Test1 {
   required int32 a = 1;
}
", proto);
        }

        [Test]
        public void GetProtoTest2()
        {
            var model = TypeModel.Create();
            model.UseImplicitZeroDefaults = false;

            string proto = model.GetSchema(typeof(Test2));

            Assert.AreEqual(
@"package Examples;

message abc {
   required uint32 ghi = 2;
   required bytes def = 3;
}
", proto);
        }

        [DataContract(Name="abc")]
        public class Test2
        {
            [DataMember(Name = "def", IsRequired = true, Order = 3)]
            public byte[] X { get; set; }

            [DataMember(Name = "ghi", IsRequired = true, Order = 2)]
            public char Y { get; set; }
        }

        [Test]
        public void TestProtoGenerationWithDefaultString()
        {

            string proto = Serializer.GetProto<MyClass>();

            Assert.AreEqual(@"
message MyClass {
   optional string TestString = 1 [default = ""Test Test TEst""];
}
", proto);
        }

        [Test]
        public void GenericsWithoutExplicitNamesShouldUseTheTypeName()
        {
            string proto = Serializer.GetProto<ProtoGenerationTypes.BrokenProto.ExampleContract>();

            Assert.AreEqual(@"package ProtoGenerationTypes.BrokenProto;

message ExampleContract {
   repeated Info ListOfInfo = 1;
}
message Info {
   optional string Name = 1;
   // the following represent sub-types; at most 1 should have a value
   optional Info_Type1 Info_Type1 = 2;
   optional Info_Type2 Info_Type2 = 3;
}
message Info_Type1 {
   optional Type1 Details = 2;
}
message Info_Type2 {
   optional Type2 Details = 2;
}
message Type1 {
   optional string Value1 = 1;
   optional string Value2 = 2;
}
message Type2 {
   optional string Value3 = 1;
   optional string Value4 = 2;
}
", proto);
        }

        [Test]
        public void SelfReferntialGenericsShouldNotExplode()
        {
            string proto = Serializer.GetProto<ProtoGenerationTypes.SelfGenericProto.EvilParent>();

            Assert.AreEqual(@"package ProtoGenerationTypes.SelfGenericProto;

message EvilGeneric_EvilParent {
   optional int32 X = 1 [default = 0];
}
message EvilParent {
   optional EvilGeneric_EvilParent X = 1;
}
", proto);
        }

        [Test, ExpectedException(typeof(ArgumentException), ExpectedMessage=@"The type specified is a list; schema-generation requires a non-list contract type
Parameter name: type")]
        public void ProtoForListsShouldThrowException()
        {
            Serializer.GetProto<List<ProtoGenerationTypes.BrokenProto.Type2>>();
        }
        [Test, ExpectedException(typeof(ArgumentException), ExpectedMessage = @"The type specified is not a contract-type
Parameter name: type")]
        public void ProtoForNonContractTypeShouldThrowException()
        {
            var model = TypeModel.Create();
            model.AutoAddMissingTypes = false;
            model.GetSchema(typeof(ProtoGenerationTypes.BrokenProto.Type2));
        }

        [Test]
        public void BclImportsAreAddedWhenNecessary()
        {
            string proto = Serializer.GetProto<ProtoGenerationTypes.BclImports.HasPrimitives>();

            Assert.AreEqual(@"package ProtoGenerationTypes.BclImports;
import ""bcl.proto"" // schema for protobuf-net's handling of core .NET types

message HasPrimitives {
   optional bcl.DateTime When = 1;
}
", proto);
        }

        static TypeModel GetSurrogateModel() {

            var model = TypeModel.Create();
            model.AutoAddMissingTypes = false;
            model.Add(typeof(MySurrogate), true);
            model.Add(typeof(MyNonSurrogate), false).SetSurrogate(typeof(MySurrogate));
            model.Add(typeof(UsesSurrogates), true);
            return model;
        }
        [Test]
        public void SchemaNameForSurrogateShouldBeSane()
        {
            
            string proto = GetSurrogateModel().GetSchema(typeof(MySurrogate));

            Assert.AreEqual(@"package Examples;

message MySurrogate {
}
", proto);
        }
        [Test]
        public void SchemaNameForNonSurrogateShouldBeSane()
        {
            string proto = GetSurrogateModel().GetSchema(typeof(MyNonSurrogate));

            Assert.AreEqual(@"package Examples;

message MySurrogate {
}
", proto);
        }
        [Test]
        public void SchemaNameForTypeUsingSurrogatesShouldBeSane()
        {
            string proto = GetSurrogateModel().GetSchema(typeof(UsesSurrogates));

            Assert.AreEqual(@"package Examples;

message MySurrogate {
}
message UsesSurrogates {
   optional MySurrogate A = 1;
   optional MySurrogate B = 2;
}
", proto);
        }
        [Test]
        public void EntireSchemaShouldNotIncludeNonSurrogates()
        {
            string proto = GetSurrogateModel().GetSchema(null);

            Assert.AreEqual(@"package Examples;

message MySurrogate {
}
message UsesSurrogates {
   optional MySurrogate A = 1;
   optional MySurrogate B = 2;
}
", proto);
        }

        [ProtoContract]
        public class UsesSurrogates
        {
            [ProtoMember(1)]
            public MySurrogate A { get; set; }

            [ProtoMember(2)]
            public MyNonSurrogate B { get; set; }
        }
        [ProtoContract]
        public class MySurrogate
        {
            public static implicit operator MyNonSurrogate(MySurrogate value)
            {
                return value == null ? null : new MyNonSurrogate();
            }
            public static implicit operator MySurrogate(MyNonSurrogate value)
            {
                return value == null ? null : new MySurrogate();
            }
        }
        public class MyNonSurrogate { }
    }
}

[ProtoContract]
class MyClass
{
    [ProtoMember(1), DefaultValue("Test Test TEst")]
    public string TestString { get; set; }
}
namespace ProtoGenerationTypes.BclImports
{
    [ProtoContract]
    public class HasPrimitives
    {
        [ProtoMember(1)]
        public DateTime When { get; set; }
    }
}
namespace ProtoGenerationTypes.SelfGenericProto
{
    [ProtoContract]
    public class EvilParent
    {
        [ProtoMember(1)]
        public EvilGeneric<EvilParent> X { get; set; }
    }
    [ProtoContract]
    public class EvilGeneric<T>
    {
        [ProtoMember(1)]
        public int X { get; set; }
    }
}

namespace ProtoGenerationTypes.BrokenProto
{
	[ProtoContract]
	public class ExampleContract
	{
		[ProtoMember(1)]
		public List<Info> ListOfInfo { get; set; }
	}

	[ProtoContract]
	[ProtoInclude(2, typeof(Info<Type1>))]
	[ProtoInclude(3, typeof(Info<Type2>))]
	public abstract class Info
	{
		[ProtoMember(1)]
		public string Name { get; set; }
	}

	[ProtoContract]
	public class Info<T> : Info
		where T : DetailsBase, new()
	{
		public Info()
		{
			Details = new T();
		}

		[ProtoMember(2)]
		public T Details { get; set; }
	}

	public abstract class DetailsBase
	{
	}

	[ProtoContract]
	public class Type1 : DetailsBase
	{
		[ProtoMember(1)]
		public string Value1 { get; set; }

		[ProtoMember(2)]
		public string Value2 { get; set; }
	}

	[ProtoContract]
	public class Type2 : DetailsBase
	{
		[ProtoMember(1)]
		public string Value3 { get; set; }

		[ProtoMember(2)]
		public string Value4 { get; set; }
	}
}