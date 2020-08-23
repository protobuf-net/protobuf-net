using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Examples.Issues
{
    
    public class SO13802844
    {
        enum AnimationCode {
            [ProtoEnum(Name = "AnimationCode_None")]
            None = 0,
            Idle = 1
        }

        [Fact]
        public void Execute()
        {
            string s = Serializer.GetProto<AnimationCode>(ProtoSyntax.Proto2);

            Assert.Equal(@"syntax = ""proto2"";
package Examples.Issues;

enum AnimationCode {
   AnimationCode_None = 0;
   Idle = 1;
}
", s, ignoreLineEndingDifferences: true);
        }
    }
}
