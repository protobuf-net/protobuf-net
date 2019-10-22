using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.Collections.Generic;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class SO_DictionaryFail
    {
        [Fact]
        public void TupleDictionary()
        {


            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add<Tuple<Dictionary<string,double>, Dictionary<string,double>>>();
            //Test(model);

            var dll = model.CompileAndVerify();

            //model.CompileInPlace();
            //Test(model);

            //Test(model.Compile());

            Test(dll);
        }

        private static void Test(TypeModel model)
        {
            var data = Tuple.Create(
                new Dictionary<string, double>
                {
                                {"abc", 123 },
                                {"def", 456 },
                                {"ghi", 789 },
                },
                new Dictionary<string, double>
                {
                                {"jkl", 1011 },
                                {"mno", 1213 },
                });

            var clone = model.DeepClone(data);
            Assert.NotSame(data, clone);
            var x = clone.Item1;
            Assert.Equal(3, x.Count);
            Assert.True(x.TryGetValue("abc", out var val));
            Assert.Equal(123, val);
            Assert.True(x.TryGetValue("def", out val));
            Assert.Equal(456, val);
            Assert.True(x.TryGetValue("ghi", out val));
            Assert.Equal(789, val);

            var y = clone.Item2;
            Assert.Equal(2, y.Count);
            Assert.True(y.TryGetValue("jkl", out val));
            Assert.Equal(1011, val);
            Assert.True(y.TryGetValue("mno", out val));
            Assert.Equal(1213, val);
        }
    }
}
