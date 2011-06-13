using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using NUnit.Framework;
using ProtoBuf.Meta;
namespace Examples.Issues
{
    [TestFixture]
    public class Issue152
    {
        [Test]
        public void ExecuteWithOverwrite()
        {
            var a1 = new IntArray { Arr = new int[] { 5, 6, 7 }, List = new List<int> { 8, 9, 10 } };
            var model = TypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(IntArray), true)[1].OverwriteList = true;
            model.Add(typeof(IntArray), true)[2].OverwriteList = true;

            var clone = (IntArray)model.DeepClone(a1);
            AssertSequence(clone.Arr, "Runtime", 5, 6, 7);
            AssertSequence(clone.List, "Runtime", 8, 9, 10);

            model.CompileInPlace();
            clone = (IntArray)model.DeepClone(a1);
            AssertSequence(clone.Arr, "CompileInPlace", 5, 6, 7);
            AssertSequence(clone.List, "CompileInPlace", 8, 9, 10);

            var precomp = model.Compile();
            clone = (IntArray)precomp.DeepClone(a1);
            AssertSequence(clone.Arr, "Compile", 5, 6, 7);
            AssertSequence(clone.List, "Compile", 8, 9, 10);
        }

        [Test]
        public void ExecuteWithAppend()
        {
            var a1 = new IntArray { Arr = new int[] { 5, 6, 7 }, List = new List<int> { 8, 9, 10 } };
            var model = TypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(IntArray), true)[1].OverwriteList = false;
            model.Add(typeof(IntArray), true)[2].OverwriteList = false;

            var clone = (IntArray)model.DeepClone(a1);
            AssertSequence(clone.Arr, "Runtime", 1, 2, 5, 6, 7);
            AssertSequence(clone.List, "Runtime", 3, 4, 8, 9, 10);

            model.CompileInPlace();
            clone = (IntArray)model.DeepClone(a1);
            AssertSequence(clone.Arr, "CompileInPlace", 1, 2, 5, 6, 7);
            AssertSequence(clone.List, "CompileInPlace", 3, 4, 8, 9, 10);

            var precomp = model.Compile();
            clone = (IntArray)precomp.DeepClone(a1);
            AssertSequence(clone.Arr, "Compile", 1, 2, 5, 6, 7);
            AssertSequence(clone.List, "Compile", 3, 4, 8, 9, 10);
        }

        static void AssertSequence(IList<int> sequence, string caption, params int[] expected)
        {
            Assert.IsNotNull(sequence, caption + ":null sequence");
            Assert.AreEqual(sequence.Count, expected.Length, caption + " count");
            for(int i = 0 ; i < expected.Length ; i++)
            {
                Assert.AreEqual(expected[i], sequence[i], caption + ":" + i);
            }
        }

        [DataContract]
        public class IntArray
        {
            [DataMember(Order = 1)]
            public int[] Arr = new int[] { 1, 2 };

            [DataMember(Order = 2)]
            public List<int> List = new List<int> { 3, 4 };
        }
    }
}
