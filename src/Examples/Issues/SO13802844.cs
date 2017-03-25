using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Examples.Issues
{
    [TestFixture]
    public class SO13802844
    {
        enum AnimationCode {
            [ProtoEnum(Name = "AnimationCode_None")]
            None = 0,
            Idle = 1
        }

        [Test]
        public void Execute()
        {
            string s = Serializer.GetProto<AnimationCode>();

            Assert.AreEqual(@"package Examples.Issues;

enum AnimationCode {
   AnimationCode_None = 0;
   Idle = 1;
}
", s);
        }
    }
}
