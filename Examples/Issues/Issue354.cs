using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue354
    {
        [Test]
        public void SerializeConcurrent()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            Execute(model, "Runtime");
            model.CompileInPlace();
            Execute(model, "CompileInPlace");
            Execute(model.Compile(), "Compile");
            model.Compile("Issue354", "Issue354.dll");
            PEVerify.AssertValid("Issue354.dll");
        }

        //[Test, Ignore("these have never been supported")] // concurrent stack is getting inverted; is this normal?
        //public void TestRegularStackQueue()
        //{
        //    var orig = new NonConcurrent();
        //    orig.Stack.Push(1);
        //    orig.Stack.Push(2);
        //    orig.Queue.Enqueue(3);
        //    orig.Queue.Enqueue(4);
        //    var clone = (NonConcurrent)RuntimeTypeModel.Create().DeepClone(orig);
        //    TestRegular(orig, "Original");
        //    TestRegular(clone, "Clone");

        //}
        //static void TestRegular(NonConcurrent data, string caption)
        //{
        //    Assert.AreEqual(2, data.Queue.Count, caption);
        //    Assert.AreEqual(2, data.Stack.Count, caption);
        //    Assert.AreEqual(1, data.Queue.Dequeue(), caption);
        //    Assert.AreEqual(2, data.Queue.Dequeue(), caption);
        //    Assert.AreEqual(4, data.Stack.Pop(), caption);
        //    Assert.AreEqual(3, data.Stack.Pop(), caption);
        //}
        //[ProtoContract]
        //public class NonConcurrent
        //{
        //    [ProtoMember(1)]
        //    public Queue<int> Queue { get; set; }
        //    [ProtoMember(2)]
        //    public Stack<int> Stack { get; set; }

        //    public NonConcurrent()
        //    {
        //        Queue = new Queue<int>();
        //        Stack = new Stack<int>();
        //    }
        //}

        [Test]
        public void TestInitialData()
        { // check that our test matches the generated data
            var orig = CreateData();
            TestData(orig, "TestInitialData");
        }
        static void Execute(TypeModel model, string caption)
        {
            var orig = CreateData();
            var clone = (CanHazConcurrent)model.DeepClone(orig);
            Assert.AreNotEqual(clone, orig);
            
            TestData(clone, caption);
        }
        static CanHazConcurrent CreateData()
        {
            var data = new CanHazConcurrent();

            data.Queue.Enqueue(1);
            data.Queue.Enqueue(2);

            data.Bag.Add("abc");
            data.Bag.Add("def");

            data.Dictionary.AddOrUpdate(1, "abc", (x, y) => y);
            data.Dictionary.AddOrUpdate(2, "def", (x, y) => y);

            data.Stack.Push(1);
            data.Stack.Push(2);

            return data;
        }
        static void TestData(CanHazConcurrent data, string caption)
        {
            //int val;
            //Assert.AreEqual(2, data.Queue.Count, caption + ":Queue.Count");
            //Assert.IsTrue(data.Queue.TryDequeue(out val), caption + ":Queue - try 1");
            //Assert.AreEqual(1, val, caption + ":Queue - val 1");
            //Assert.IsTrue(data.Queue.TryDequeue(out val), caption + ":Queue - try 2");
            //Assert.AreEqual(2, val, caption + ":Queue - val 2");
            //Assert.IsFalse(data.Queue.TryDequeue(out val), caption + ":Queue - try 3");

            Assert.AreEqual(2, data.Bag.Count, caption + ":Bag.Count");
            Assert.IsTrue(data.Bag.Contains("abc"), caption + ":Bag - try 1");
            Assert.IsTrue(data.Bag.Contains("def"), caption + ":Bag - try 2");

            Assert.AreEqual(2, data.Dictionary.Count, caption + ":Dictionary.Count");
            string s;
            Assert.IsTrue(data.Dictionary.TryGetValue(1, out s), caption + ":Dictionary - try 1");
            Assert.AreEqual("abc", s, caption + ":Dictionary - val 1");
            Assert.IsTrue(data.Dictionary.TryGetValue(2, out s), caption + ":Dictionary - try 2");
            Assert.AreEqual("def", s, caption + ":Dictionary - val 2");

            //Assert.AreEqual(2, data.Stack.Count, caption + ":Stack.Count");
            //Assert.IsTrue(data.Stack.TryPop(out val), caption + ":Stack - try 1");
            //Assert.AreEqual(2, val, caption + ":Stack - val 1");
            //Assert.IsTrue(data.Stack.TryPop(out val), caption + ":Stack - try 1");
            //Assert.AreEqual(1, val, caption + ":Stack - val 2");
            //Assert.IsFalse(data.Queue.TryDequeue(out val), caption + ":Stack - try 1");
        }

        [ProtoContract]
        public class CanHazConcurrent
        {
            //[ProtoMember(1)]
            public ConcurrentQueue<int> Queue { get; set; }

            [ProtoMember(2)]
            public ConcurrentBag<string> Bag { get; set; }

            [ProtoMember(3)]
            public ConcurrentDictionary<int, string> Dictionary { get; set; }

            //[ProtoMember(4)]
            public ConcurrentStack<int> Stack { get; set; }

            public CanHazConcurrent()
            {
                Queue = new ConcurrentQueue<int>();
                Stack = new ConcurrentStack<int>();
                Dictionary = new ConcurrentDictionary<int, string>();
                Bag = new ConcurrentBag<string>();
            }
        }
    }
}
