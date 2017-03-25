using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf.Meta;

namespace ProtoBuf.unittest.Attribs
{
    
    public class Basic
    {
        [ProtoContract]
        public class BasicContract
        {
            [ProtoMember(1)]
            public int Expected { get; set; }

            [ProtoIgnore]
            [ProtoMember(2)]
            public int Ignored { get; set; }

            public int NotExpected { get; set; }
        }

        [Fact]
        public void CheckThatBasicAttributesAreRespected()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(BasicContract), true);
            BasicContract obj = new BasicContract { Expected = 123, Ignored = 456, NotExpected = 789 },
                clone = (BasicContract)model.DeepClone(obj);

            Assert.NotSame(obj, clone);
            Assert.Equal(123, clone.Expected);
            Assert.Equal(0, clone.Ignored);
            Assert.Equal(0, clone.NotExpected);

 
        }
    }
}
