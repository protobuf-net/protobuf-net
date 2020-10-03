using System.Collections.Generic;
using System.ComponentModel;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using Xunit.Abstractions;

namespace Examples.Issues
{
    
    public class Issue303
    {
        static TypeModel GetModel()
        {
            var model = RuntimeTypeModel.Create();
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

 model.GetSchema(null, ProtoSyntax.Proto2), ignoreLineEndingDifferences: true

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

 model.GetSchema(null, ProtoSyntax.Proto3), ignoreLineEndingDifferences: true

);
        }

        public Issue303(ITestOutputHelper log) => _log = log;
        private readonly ITestOutputHelper _log;
        private void Log(string message) => _log?.WriteLine(message);

        [Theory]
        [InlineData(SchemaGenerationFlags.None, @"syntax = ""proto2"";

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
")]
        [InlineData(SchemaGenerationFlags.MultipleNamespaceSupport, @"syntax = ""proto2"";
import ""protobuf-net/protogen.proto""; // custom protobuf-net options

message animal {
   option (.protobuf_net.msgopt).namespace = ""Examples.Issues"";
   optional int32 numberOfLegs = 1 [default = 4];
   oneof subtype {
      cat cat = 4;
   }
}
message cat {
   option (.protobuf_net.msgopt).namespace = ""Examples.Issues"";
   repeated animal animalsHunted = 1;
}
message mineral {
   option (.protobuf_net.msgopt).namespace = ""Examples.Issues.CompletelyUnrelated"";
}
message vegetable {
   option (.protobuf_net.msgopt).namespace = ""Examples.Issues"";
   optional int32 size = 1 [default = 0];
}
")]
        public void TestEntireModelWithMultipleNamespaces(SchemaGenerationFlags flags, string expected)
        {
            var model = (RuntimeTypeModel)GetModel();
            model.Add(typeof (Examples.Issues.CompletelyUnrelated.Mineral), true);
            var actual = model.GetSchema(new SchemaGenerationOptions { Syntax = ProtoSyntax.Proto2, Flags = flags });
            Log(actual);
            Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
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

                model.GetSchema(typeof(Animal), ProtoSyntax.Proto2), ignoreLineEndingDifferences: true

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

                model.GetSchema(typeof(Animal), ProtoSyntax.Proto2), ignoreLineEndingDifferences: true

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

