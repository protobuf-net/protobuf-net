using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class Issue306
    {
        [Fact]
        public void TestTuple()
        {
            var model = TypeModel.Create();
            model.Add(typeof (Foo), true);

            string schema = model.GetSchema(typeof (Foo));

            Assert.Equal(@"syntax = ""proto2"";
package Examples.Issues;

message Foo {
   map<int32,string> Lookup = 1;
}
", schema);
        }

        [ProtoContract]
        public class Foo
        {
            [ProtoMember(1)]
            public Dictionary<int, string> Lookup { get; set; }
        }
    }
}
