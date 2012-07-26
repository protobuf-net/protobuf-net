using System.Collections.Generic;
using System.ComponentModel;
using NUnit.Framework;
using ProtoBuf;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue303
    {
        [Test]
        public void TestInheritanceStartingWithBaseType()
        {
            Assert.AreEqual(
                @"package Examples.Issues;

message animal {
   optional int32 numberOfLegs = 1 [default = 4];
   // the following represent sub-types; at most 1 should have a value
   optional cat cat = 4;
}
message cat {
   repeated animal animalsHunted = 1;
}
",
#pragma warning disable 0618
                Serializer.GetProto<Animal>()
#pragma warning restore 0618
                );
        }
        [Test]
        public void TestInheritanceStartingWithDerivedType()
        {
            Assert.AreEqual(
                @"package Examples.Issues;

message animal {
   optional int32 numberOfLegs = 1 [default = 4];
   // the following represent sub-types; at most 1 should have a value
   optional cat cat = 4;
}
message cat {
   repeated animal animalsHunted = 1;
}
",
#pragma warning disable 0618
                Serializer.GetProto<Cat>()
#pragma warning restore 0618
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
    }
}
