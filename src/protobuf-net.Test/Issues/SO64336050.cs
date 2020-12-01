using ProtoBuf.Meta;
using System.Runtime.Serialization;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class SO64336050
    {
        [Fact]
        public void EnumNameUsedWithDefaults()
        {
            var schema = RuntimeTypeModel.Default.GetSchema(new SchemaGenerationOptions
            {
                Package = "",
                Syntax = ProtoSyntax.Proto2,
                Types = { typeof(MyClass) },
            });
            Assert.Equal(@"syntax = ""proto2"";

message MyClass {
   optional MyEnum m_myEnum = 1 [default = NotSet];
}
enum MyEnum {
   MyEnum_NotSet = 0;
   SomeValue = 1;
   AndSoOn = 2;
}
", schema, ignoreLineEndingDifferences: true);
        }

    }

    [DataContract]
    [ProtoContract]
    public enum MyEnum
    {
        [EnumMember]
        [ProtoEnum(Name = "MyEnum_NotSet")]
        NotSet,

        SomeValue,

        AndSoOn
    }

    [ProtoContract]
    public class MyClass
    {
        [DataMember]
        [ProtoMember(1)]
#pragma warning disable CS0169, IDE0044, IDE0051
        private MyEnum m_myEnum;
#pragma warning restore CS0169, IDE0044, IDE0051

    }
}
