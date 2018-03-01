using System.Collections.Generic;
using System.IO;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues.Issue48
{
    
    public class Issue202
    {
        [Fact]
        public void TestListsAsFields()
        { 
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            ExecuteTest(model, "runtime");

            model.CompileInPlace();
            ExecuteTest(model, "CompileInPlace");

            ExecuteTest(model.Compile(), "Compile");
        }
        void ExecuteTest(TypeModel model, string test)
        {
            A a = new A { flags = new List<string> { "abc", "def" } }, c;
            Assert.Equal(2, a.flags.Count); //, test);
            Assert.Equal("abc", a.flags[0]); //, test);
            Assert.Equal("def", a.flags[1]); //, test);

            B b;
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, a);
                ms.Position = 0;
                b = (B)model.Deserialize(ms, null, typeof(B));
            }

            Assert.Equal(2, b.flags.Count); //, test);
            Assert.Equal("abc", b.flags[0]); //, test);
            Assert.Equal("def", b.flags[1]); //, test);

            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, b);
                ms.Position = 0;
                c = (A)model.Deserialize(ms, null, typeof(A));
            }

            Assert.Equal(2, c.flags.Count); //, test);
            Assert.Equal("abc", c.flags[0]); //, test);
            Assert.Equal("def", c.flags[1]); //, test);
        }

        [ProtoContract]
        public class A //property version
        {
            [ProtoMember(3)]
            public List<string> flags { get; set; }
        }
        [ProtoContract]
        public class B //field version
        {
            [ProtoMember(3)]
            public List<string> flags;
        }

    }
}
