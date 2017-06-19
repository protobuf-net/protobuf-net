using System;
using System.IO;
using Xunit;
using ProtoBuf;
using Examples;

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
    
    public class Issue41Rig
    {
        [Fact]
        public void Issue41TestOriginalSubClassShouldComplainAboutDuplicateTags()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                Serializer.Serialize<B_Orig>(Stream.Null, new B_Orig());
            });
        }

        [Fact]
        public void Issue41TestOriginalBaseClassShouldComplainAboutDuplicateTags()
        {
            Program.ExpectFailure<InvalidOperationException>(() => { 
                Serializer.Serialize<A_Orig>(Stream.Null, new A_Orig());
            });
        }

        [Fact]
        public void Issue41InheritedPropertiesAsBaseClass()
        {
            B b = new B {PropA = "a", PropB = "b", PropAB = "ab", PropBB = "bb"};
            using (var s = new MemoryStream ())
            {
              Serializer.Serialize<A>(s, b);
              s.Position = 0;
              B bb = (B)Serializer.Deserialize<A>(s);
              Assert.Equal(b.PropA, bb.PropA); //, "PropA");
              Assert.Equal(b.PropB, bb.PropB); //, "PropB");
              Assert.Equal(b.PropAB, bb.PropAB); //, "PropAB");
              Assert.Equal(b.PropBB, bb.PropBB); //, "PropBB");
            }
        }
        [Fact]
        public void Issue41InheritedPropertiesAsSubClass()
        {
            B b = new B { PropA = "a", PropB = "b", PropAB = "ab", PropBB = "bb" };
            using (var s = new MemoryStream())
            {
                Serializer.Serialize<B>(s, b);
                s.Position = 0;
                B bb = Serializer.Deserialize<B>(s);
                Assert.Equal(b.PropA, bb.PropA); //, "PropA");
                Assert.Equal(b.PropB, bb.PropB); //, "PropB");
                Assert.Equal(b.PropAB, bb.PropAB); //, "PropAB");
                Assert.Equal(b.PropBB, bb.PropBB); //, "PropBB");
            }
        }
    }
}