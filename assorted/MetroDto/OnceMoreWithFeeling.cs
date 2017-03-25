using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace MetroDto
{
    [DataContract, ProtoContract]
    public class TestClass
    {
        [DataMember(Order = 1), ProtoMember(1)]
        public int IntVal { get; set; }
        [DataMember(Order = 2), ProtoMember(2)]
        public string StrVal { get; set; }
        [DataMember(Order = 3), ProtoMember(3)]
        public MyEnum EnumVal { get; set; }
    }

    public enum MyEnum
    {
        Foo,
        Bar
    }
}
