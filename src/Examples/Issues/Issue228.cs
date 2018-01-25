using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Xunit;

namespace Examples.Issues
{
    public class Issue228
    {
        [ProtoContract]
        public class BaseModel<T>
        {
            [ProtoMember(1, IsRequired = true),]
            public int Timestamp { get; set; }

            [ProtoMember(2, IsRequired = true)]
            public T Data { get; set; }
        }

        [ProtoContract]
        public class IdName
        {
            [ProtoMember(1)]
            public int Id { get; set; }

            [ProtoMember(2)]
            public string Name { get; set; }
        }

        [Fact]
        public void ShouldHandleGenerics()
        {
            var protoFile = Serializer.GetProto<BaseModel<List<IdName>>>();
            Assert.Equal(@"syntax = ""proto2"";
package Examples.Issues;

message BaseModel_List_IdName {
   required int32 Timestamp = 1;
   repeated IdName Data = 2;
}
message IdName {
   optional int32 Id = 1 [default = 0];
   optional string Name = 2;
}
", protoFile);
        }
    }
}
