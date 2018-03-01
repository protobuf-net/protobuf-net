using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples
{
    
    public class Tuples
    {
        [Fact]
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

            Assert.Equal(123, clone.Item1);
            Assert.Equal(2, clone.Item2.Length);
            Assert.Equal(1, clone.Item2[0].Item1);
            Assert.Equal(2, clone.Item2[0].Item2);
            Assert.Equal(3, clone.Item2[0].Item3);
            Assert.Equal(4, clone.Item2[0].Item4);
            Assert.Equal(5, clone.Item2[0].Item5);
            Assert.Equal(6, clone.Item2[0].Item6);
            Assert.Equal(7, clone.Item2[0].Item7);
            Assert.Equal(Tuple.Create(1F,2F), clone.Item2[0].Rest.Item1.Single());
            Assert.Equal(9, clone.Item2[1].Item1);
            Assert.Equal(10, clone.Item2[1].Item2);
            Assert.Equal(11, clone.Item2[1].Item3);
            Assert.Equal(12, clone.Item2[1].Item4);
            Assert.Equal(13, clone.Item2[1].Item5);
            Assert.Equal(14, clone.Item2[1].Item6);
            Assert.Equal(15, clone.Item2[1].Item7);
            Assert.Equal(Tuple.Create(3F, 4F), clone.Item2[1].Rest.Item1.Single());
            Assert.True(clone.Item3);
        }


        [Fact]
        public void TestInt_IntArray_Dictionary()
        {
            var data = new Dictionary<int, int[]> { { 1, new[] { 2, 3 } }, { 4, new[] { 5, 6, 7 } } };
            var model = TypeModel.Create();
            model.CompileInPlace();
            var clone = (Dictionary<int, int[]>)model.DeepClone(data);
            Assert.Equal(2, clone.Count);
            Assert.True(clone[1].SequenceEqual(new[] { 2, 3 }));
            Assert.True(clone[4].SequenceEqual(new[] { 5, 6, 7 }));
        }

        [Fact]
        public void TestMonoKeyValuePair()
        {
            var original = new WithQuasiMutableTuple {Value = new QuasiMutableTuple(123, "abc")};
            var clone = Serializer.DeepClone(original);
            Assert.Equal(123, clone.Value.Foo);
            Assert.Equal("abc", clone.Value.Bar);
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
