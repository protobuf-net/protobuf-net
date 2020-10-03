using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class Issue310
    {
        [Fact]
        public void Execute()
        {
#pragma warning disable  0618
            string proto = Serializer.GetProto<Animal>(ProtoSyntax.Proto2);

            Assert.Equal(@"syntax = ""proto2"";
package Examples.Issues;

message Animal {
   optional int32 NumberOfLegs = 1 [default = 0];
   oneof subtype {
      Cat Cat = 2;
      Dog Dog = 3;
   }
}
message Cat {
   repeated Animal AnimalsHunted = 1;
}
message Dog {
   optional string OwnerName = 1;
}
", proto, ignoreLineEndingDifferences: true);
        }

        [ProtoContract]
        [ProtoInclude(2, typeof(Cat))]
        [ProtoInclude(3, typeof(Dog))]
        public class Animal
        {
            [ProtoMember(1)]
            public int NumberOfLegs { get; set; }
        }

        [ProtoContract]
        public class Dog : Animal {
            [ProtoMember(1)]
            public string OwnerName { get; set; }
        }
        
        [ProtoContract]
        public class Cat : Animal
        {
            [ProtoMember(1)]
            public List<Animal> AnimalsHunted { get; set; }
        }
    }
}
