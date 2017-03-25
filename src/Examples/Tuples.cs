using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples
{
    [TestFixture]
    public class Tuples
    {
        [Test]
        public void TestComplexNestedTupleWithCrazyMovingParts()
        {
            
            var model = TypeModel.Create();
            model.AutoCompile = false;
            Check(model);

            model.CompileInPlace();
            Check(model);

            Check(model.Compile());

            model.Compile("TestComplexNestedTupleWithCrazyMovingParts", "TestComplexNestedTupleWithCrazyMovingParts.dll");
            PEVerify.AssertValid("TestComplexNestedTupleWithCrazyMovingParts.dll");
        }
        void Check(TypeModel model)
        {
            
            var obj = Tuple.Create(
                123, new[] { Tuple.Create(1, 2, 3, 4, 5, 6, 7, new List<Tuple<float, float>> { Tuple.Create(1F,2F) }), Tuple.Create(9, 10, 11, 12, 13, 14, 15, new List<Tuple<float, float>> { Tuple.Create(3F,4F) }) }, true);

            var clone = (Tuple<int, Tuple<int, int, int, int, int, int, int, Tuple<List<Tuple<float, float>>>>[], bool>)model.DeepClone(obj);

            Assert.AreEqual(123, clone.Item1);
            Assert.AreEqual(2, clone.Item2.Length);
            Assert.AreEqual(1, clone.Item2[0].Item1);
            Assert.AreEqual(2, clone.Item2[0].Item2);
            Assert.AreEqual(3, clone.Item2[0].Item3);
            Assert.AreEqual(4, clone.Item2[0].Item4);
            Assert.AreEqual(5, clone.Item2[0].Item5);
            Assert.AreEqual(6, clone.Item2[0].Item6);
            Assert.AreEqual(7, clone.Item2[0].Item7);
            Assert.AreEqual(Tuple.Create(1F,2F), clone.Item2[0].Rest.Item1.Single());
            Assert.AreEqual(9, clone.Item2[1].Item1);
            Assert.AreEqual(10, clone.Item2[1].Item2);
            Assert.AreEqual(11, clone.Item2[1].Item3);
            Assert.AreEqual(12, clone.Item2[1].Item4);
            Assert.AreEqual(13, clone.Item2[1].Item5);
            Assert.AreEqual(14, clone.Item2[1].Item6);
            Assert.AreEqual(15, clone.Item2[1].Item7);
            Assert.AreEqual(Tuple.Create(3F, 4F), clone.Item2[1].Rest.Item1.Single());
            Assert.AreEqual(true, clone.Item3);
        }


        [Test]
        public void TestInt_IntArray_Dictionary()
        {
            var data = new Dictionary<int, int[]> { { 1, new[] { 2, 3 } }, { 4, new[] { 5, 6, 7 } } };
            var model = TypeModel.Create();
            model.CompileInPlace();
            var clone = (Dictionary<int, int[]>)model.DeepClone(data);
            Assert.AreEqual(2, clone.Count);
            Assert.IsTrue(clone[1].SequenceEqual(new[] { 2, 3 }));
            Assert.IsTrue(clone[4].SequenceEqual(new[] { 5, 6, 7 }));
        }

        [Test]
        public void TestMonoKeyValuePair()
        {
            var original = new WithQuasiMutableTuple {Value = new QuasiMutableTuple(123, "abc")};
            var clone = Serializer.DeepClone(original);
            Assert.AreEqual(123, clone.Value.Foo);
            Assert.AreEqual("abc", clone.Value.Bar);
        }
        // the mono version of KeyValuePair<,> has private setters
        public struct QuasiMutableTuple
        {
            public int Foo { get; private set; }
            public string Bar { get; private set; }
            public QuasiMutableTuple(int foo, string bar) : this()
            {
                Foo = foo;
                Bar = bar;
            }
        }
        [ProtoContract]
        public class WithQuasiMutableTuple
        {
            [ProtoMember(1)]
            public QuasiMutableTuple Value { get; set;} 
        }
    }
}
