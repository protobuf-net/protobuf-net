using System;
using System.IO;
using NUnit.Framework;
using ProtoBuf;

namespace Issue41
{

    [ProtoContract]
    [ProtoInclude(3, typeof(B))]
    public class A
    {
        [ProtoMember(1, Name = "PropA")]
        public string PropA { get; set; }

        [ProtoMember(2, Name = "PropB")]
        public string PropB { get; set; }
    }

    [ProtoContract]
    public class B : A
    {
        [ProtoMember(1, Name = "PropAB")]
        public string PropAB { get; set; }
        [ProtoMember(2, Name = "PropBB")]
        public string PropBB { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(2, typeof(B_Orig))]
    public class A_Orig
    {
        [ProtoMember(1, Name = "PropA")]
        public string PropA { get; set; }

        [ProtoMember(2, Name = "PropB")]
        public string PropB { get; set; }
    }

    [ProtoContract]
    public class B_Orig : A_Orig
    {
        [ProtoMember(1, Name = "PropAB")]
        public string PropAB { get; set; }
        [ProtoMember(2, Name = "PropBB")]
        public string PropBB { get; set; }
    }
    [TestFixture]
    public class Issue41Rig
    {
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void Issue41TestOriginalSubClassShouldComplainAboutDuplicateTags()
        {
            Serializer.Serialize<B_Orig>(Stream.Null, new B_Orig());
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void Issue41TestOriginalBaseClassShouldComplainAboutDuplicateTags()
        {
            Serializer.Serialize<A_Orig>(Stream.Null, new A_Orig());
        }

        [Test]
        public void Issue41InheritedPropertiesAsBaseClass()
        {
            B b = new B {PropA = "a", PropB = "b", PropAB = "ab", PropBB = "bb"};
            using (var s = new MemoryStream ())
            {
              Serializer.Serialize<A>(s, b);
              s.Position = 0;
              B bb = (B)Serializer.Deserialize<A>(s);
              Assert.AreEqual(b.PropA, bb.PropA, "PropA");
              Assert.AreEqual(b.PropB, bb.PropB, "PropB");
              Assert.AreEqual(b.PropAB, bb.PropAB, "PropAB");
              Assert.AreEqual(b.PropBB, bb.PropBB, "PropBB");
            }
        }
        [Test]
        public void Issue41InheritedPropertiesAsSubClass()
        {
            B b = new B { PropA = "a", PropB = "b", PropAB = "ab", PropBB = "bb" };
            using (var s = new MemoryStream())
            {
                Serializer.Serialize<B>(s, b);
                s.Position = 0;
                B bb = Serializer.Deserialize<B>(s);
                Assert.AreEqual(b.PropA, bb.PropA, "PropA");
                Assert.AreEqual(b.PropB, bb.PropB, "PropB");
                Assert.AreEqual(b.PropAB, bb.PropAB, "PropAB");
                Assert.AreEqual(b.PropBB, bb.PropBB, "PropBB");
            }
        }
    }
}