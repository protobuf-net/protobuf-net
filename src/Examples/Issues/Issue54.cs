using System.Collections.Generic;
using System.Linq;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class Issue54
    {
        [ProtoContract]
        public class Test54
        {
            [ProtoMember(1)]
            public Dictionary<float, List<int>> Lists { get; set; }
        }

        [Fact]
        public void TestNestedLists()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;


            DoIt(model);

            model.CompileInPlace();
            DoIt(model);

            DoIt(model.Compile());

            DoIt(model.Compile("TestNestedLists", "TestNestedLists.dll"));
            PEVerify.AssertValid("TestNestedLists.dll");

            static void DoIt(TypeModel model)
            {
                Test54 obj = new Test54
                {
                    Lists =
                        new Dictionary<float, List<int>> {
                {123.45F, new List<int> {1,2,3}},
                {678.90F, new List<int> {4,5,6}},
                }
                }, clone = model.DeepClone(obj);

                Assert.NotSame(obj, clone);
                Assert.NotNull(clone.Lists);
                Assert.Equal(obj.Lists.Count, clone.Lists.Count);
                foreach (var key in obj.Lists.Keys)
                {
                    Assert.True(clone.Lists.ContainsKey(key), key.ToString());
                    var list = clone.Lists[key];
                    Assert.NotNull(list); //, key.ToString());
                    Assert.True(obj.Lists[key].SequenceEqual(list), key.ToString());
                }
            }
        }

    }

}
