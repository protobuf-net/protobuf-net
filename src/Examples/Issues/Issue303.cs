using System.Collections.Generic;
using System.ComponentModel;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class Issue303
    {
        static TypeModel GetModel()
        {
            var model = TypeModel.Create();
            model.Add(typeof (Vegetable), true);
            model.Add(typeof (Animal), true);
            return model;
        }

        [Fact]
        public void TestEntireModel_Proto2()
        {
            var model = GetModel();
            Assert.Equal(
                @"syntax = ""proto2"";
package Examples.Issues;

message animal {
   optional int32 numberOfLegs = 1 [default = 4];
   oneof subtype {
      cat cat = 4;
   }
}
message cat {
   repeated animal animalsHunted = 1;
}
message vegetable {
   optional int32 size = 1 [default = 0];
}
",

 model.GetSchema(null)

);
        }
        [Fact]
        public void TestEntireModel_Proto3()
        {
            var model = GetModel();
            Assert.Equal(
                @"syntax = ""proto3"";
package Examples.Issues;

message animal {
   int32 numberOfLegs = 1; // default value could not be applied: 4
   oneof subtype {
      cat cat = 4;
   }
}
message cat {
   repeated animal animalsHunted = 1;
}
message vegetable {
   int32 size = 1;
}
",

 model.GetSchema(null, ProtoSyntax.Proto3)

);
        }
        [Fact]
        public void TestEntireModelWithMultipleNamespaces()
        {
            var model = (RuntimeTypeModel)GetModel();
            model.Add(typeof (Examples.Issues.CompletelyUnrelated.Mineral), true);
            Assert.Equal(
                @"syntax = ""proto2"";

message animal {
   optional int32 numberOfLegs = 1 [default = 4];
   oneof subtype {
      cat cat = 4;
   }
}
message cat {
   repeated animal animalsHunted = 1;
}
message mineral {
}
message vegetable {
   optional int32 size = 1 [default = 0];
}
",

 model.GetSchema(null)

);
        }
        [Fact]
        public void TestInheritanceStartingWithBaseType()
        {
            var model = GetModel();
            Assert.Equal(
                @"syntax = ""proto2"";
package Examples.Issues;

message animal {
   optional int32 numberOfLegs = 1 [default = 4];
   oneof subtype {
      cat cat = 4;
   }
}
message cat {
   repeated animal animalsHunted = 1;
}
",

                model.GetSchema(typeof(Animal))

                );
        }
        [Fact]
        public void TestInheritanceStartingWithDerivedType()
        {
            var model = GetModel();
            Assert.Equal(
                @"syntax = ""proto2"";
package Examples.Issues;

message animal {
   optional int32 numberOfLegs = 1 [default = 4];
   oneof subtype {
      cat cat = 4;
   }
}
message cat {
   repeated animal animalsHunted = 1;
}
",

                model.GetSchema(typeof(Animal))

                );
        }

        [ProtoContract(Name="animal"), ProtoInclude(4, typeof(Cat))]
        public abstract class Animal
        {
            [ProtoMember(1, Name="numberOfLegs"), DefaultValue(4)]
            public int NumberOfLegs = 4;
        }

        [ProtoContract(Name="cat")]
        public class Cat : Animal
        {
            [ProtoMember(1, Name = "animalsHunted")]
            public List<Animal> AnimalsHunted;
        }
        [ProtoContract(Name = "vegetable")]
        public class Vegetable
        {
            [ProtoMember(1, Name = "size")]
            public int Size { get; set; }
        }
    }

    namespace CompletelyUnrelated
    {
        [ProtoContract(Name = "mineral")]
        public class Mineral {}
    }    
}

