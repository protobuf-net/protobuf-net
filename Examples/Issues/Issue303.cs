using System.Collections.Generic;
using System.ComponentModel;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue303
    {
        static TypeModel GetModel()
        {
            var model = TypeModel.Create();
            model.Add(typeof (Vegetable), true);
            model.Add(typeof (Animal), true);
            return model;
        }

        [Test]
        public void TestEntireModel()
        {
            var model = GetModel();
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
message vegetable {
   optional int32 size = 1 [default = 0];
}
",
#pragma warning disable 0618
 model.GetSchema(null)
#pragma warning restore 0618
);
        }
        [Test]
        public void TestEntireModelWithMultipleNamespaces()
        {
            var model = (RuntimeTypeModel)GetModel();
            model.Add(typeof (Examples.Issues.CompletelyUnrelated.Mineral), true);
            Assert.AreEqual(
                @"
message animal {
   optional int32 numberOfLegs = 1 [default = 4];
   // the following represent sub-types; at most 1 should have a value
   optional cat cat = 4;
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
#pragma warning disable 0618
 model.GetSchema(null)
#pragma warning restore 0618
);
        }
        [Test]
        public void TestInheritanceStartingWithBaseType()
        {
            var model = GetModel();
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
                model.GetSchema(typeof(Animal))
#pragma warning restore 0618
                );
        }
        [Test]
        public void TestInheritanceStartingWithDerivedType()
        {
            var model = GetModel();
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
                model.GetSchema(typeof(Animal))
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

