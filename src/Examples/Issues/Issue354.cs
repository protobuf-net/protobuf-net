using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace Examples.Issues
{

    public class Issue354
    {
        [Fact]
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

        [Fact]
        public void TestRegularStackQueue()
        {
            var orig = new NonConcurrent();
            orig.Stack.Push(1);
            orig.Stack.Push(2);
            orig.Queue.Enqueue(3);
            orig.Queue.Enqueue(4);
            var clone = (NonConcurrent)RuntimeTypeModel.Create().DeepClone(orig);
            TestRegular(orig, "Original");
            TestRegular(clone, "Clone");

        }
        static void TestRegular(NonConcurrent data, string caption)
        {
            Assert.Equal(2, data.Queue.Count); //, caption);
            Assert.Equal(2, data.Stack.Count); //, caption);
            Assert.Equal(3, data.Queue.Dequeue()); //, caption);
            Assert.Equal(4, data.Queue.Dequeue()); //, caption);
            Assert.Equal(2, data.Stack.Pop()); //, caption);
            Assert.Equal(1, data.Stack.Pop()); //, caption);
        }
        [ProtoContract]
        public class NonConcurrent
        {
            [ProtoMember(1)]
            public Queue<int> Queue { get; set; }
            [ProtoMember(2)]
            public Stack<int> Stack { get; set; }

            public NonConcurrent()
            {
                Queue = new Queue<int>();
                Stack = new Stack<int>();
            }
        }

        [Fact]
        public void TestInitialData()
        { // check that our test matches the generated data
            var orig = CreateData();
            TestData(orig, "TestInitialData");
        }
        static void Execute(TypeModel model, string caption)
        {
            var orig = CreateData();
            var clone = (CanHazConcurrent)model.DeepClone(orig);
            Assert.NotEqual(clone, orig);
            
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
            Assert.Equal(2, data.Queue.Count); //, caption + ":Queue.Count");
            Assert.True(data.Queue.TryDequeue(out int val), caption + ":Queue - try 1");
            Assert.Equal(1, val); //, caption + ":Queue - val 1");
            Assert.True(data.Queue.TryDequeue(out val), caption + ":Queue - try 2");
            Assert.Equal(2, val); //, caption + ":Queue - val 2");
            Assert.False(data.Queue.TryDequeue(out _), caption + ":Queue - try 3");

            Assert.Equal(2, data.Bag.Count); //, caption + ":Bag.Count");
            Assert.Contains("abc", data.Bag); //, caption + ":Bag - try 1");
            Assert.Contains("def", data.Bag); //, caption + ":Bag - try 2");

            Assert.Equal(2, data.Dictionary.Count); //, caption + ":Dictionary.Count");
            Assert.True(data.Dictionary.TryGetValue(1, out string s)); //, caption + ":Dictionary - try 1");
            Assert.Equal("abc", s); //, caption + ":Dictionary - val 1");
            Assert.True(data.Dictionary.TryGetValue(2, out s)); //, caption + ":Dictionary - try 2");
            Assert.Equal("def", s); //, caption + ":Dictionary - val 2");

            Assert.Equal(2, data.Stack.Count); //, caption + ":Stack.Count");
            Assert.True(data.Stack.TryPop(out val), caption + ":Stack - try 1");
            Assert.Equal(2, val); //, caption + ":Stack - val 1");
            Assert.True(data.Stack.TryPop(out val), caption + ":Stack - try 1");
            Assert.Equal(1, val); //, caption + ":Stack - val 2");
            Assert.False(data.Stack.TryPop(out _), caption + ":Stack - try 1");
        }

        static CanHazImmutable CreateImmutableData()
        {
            var data = new CanHazImmutable();

            data.Queue = data.Queue.Enqueue(1);
            data.Queue = data.Queue.Enqueue(2);

            data.Set = data.Set.Add("abc");
            data.Set = data.Set.Add("def");

            data.Dictionary = data.Dictionary.SetItem(1, "abc");
            data.Dictionary = data.Dictionary.SetItem(2, "def");

            data.Stack = data.Stack.Push(1);
            data.Stack = data.Stack.Push(2);

            data.List = data.List.Add(42);
            data.List = data.List.Add(43);

            return data;
        }


        [Fact]
        public void SerializeImmutable()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            ExecuteImmutable(model, "Runtime");
            model.CompileInPlace();
            ExecuteImmutable(model, "CompileInPlace");
            ExecuteImmutable(model.Compile(), "Compile");
            model.Compile("Issue354", "SerializeImmutable.dll");
            PEVerify.AssertValid("SerializeImmutable.dll");
        }

        static void ExecuteImmutable(TypeModel model, string caption)
        {
            var orig = CreateImmutableData();
            var clone = model.DeepClone(orig);
            Assert.NotEqual(clone, orig);
            TestData(orig, caption);
            TestData(clone, caption);
        }

        static void TestData(CanHazImmutable data, string caption)
        {
            Assert.False(data.Queue.IsEmpty);
            data.Queue = data.Queue.Dequeue(out var val);
            Assert.Equal(1, val);
            Assert.False(data.Queue.IsEmpty);
            data.Queue = data.Queue.Dequeue(out val);
            Assert.Equal(2, val);
            Assert.True(data.Queue.IsEmpty);

            Assert.Equal(2, data.Set.Count); //, caption + ":Bag.Count");
            Assert.Contains("abc", data.Set); //, caption + ":Bag - try 1");
            Assert.Contains("def", data.Set); //, caption + ":Bag - try 2");

            Assert.Equal(2, data.Dictionary.Count); //, caption + ":Dictionary.Count");
            Assert.True(data.Dictionary.TryGetValue(1, out string s)); //, caption + ":Dictionary - try 1");
            Assert.Equal("abc", s); //, caption + ":Dictionary - val 1");
            Assert.True(data.Dictionary.TryGetValue(2, out s)); //, caption + ":Dictionary - try 2");
            Assert.Equal("def", s); //, caption + ":Dictionary - val 2");

            Assert.False(data.Stack.IsEmpty);
            data.Stack = data.Stack.Pop(out val);
            Assert.Equal(2, val);
            Assert.False(data.Stack.IsEmpty);
            data.Stack = data.Stack.Pop(out val);
            Assert.Equal(1, val);
            Assert.True(data.Stack.IsEmpty);

            Assert.Equal(2, data.List.Count);
            Assert.Equal(42, data.List[0]);
            Assert.Equal(43, data.List[1]);
        }

        [ProtoContract]
        public class CanHazConcurrent
        {
            [ProtoMember(1)]
            public ConcurrentQueue<int> Queue { get; set; }

            [ProtoMember(2)]
            public ConcurrentBag<string> Bag { get; set; }

            [ProtoMember(3)]
            public ConcurrentDictionary<int, string> Dictionary { get; set; }

            [ProtoMember(4)]
            public ConcurrentStack<int> Stack { get; set; }

            public CanHazConcurrent()
            {
                Queue = new ConcurrentQueue<int>();
                Stack = new ConcurrentStack<int>();
                Dictionary = new ConcurrentDictionary<int, string>();
                Bag = new ConcurrentBag<string>();
            }
        }

        [ProtoContract]
        public class CanHazImmutable
        {
            [ProtoMember(1)]
            public IImmutableQueue<int> Queue { get; set; }

            [ProtoMember(2)]
            public IImmutableSet<string> Set { get; set; }

            [ProtoMember(3)]
            public IImmutableDictionary<int, string> Dictionary { get; set; }

            [ProtoMember(4)]
            public IImmutableStack<int> Stack { get; set; }

            [ProtoMember(5)]
            public IImmutableList<int> List { get; set; }

            public CanHazImmutable()
            {
                Queue = ImmutableQueue<int>.Empty;
                Stack = ImmutableStack<int>.Empty;
                Dictionary = ImmutableDictionary<int, string>.Empty;
                Set = ImmutableHashSet<string>.Empty;
                List = ImmutableList<int>.Empty;
            }
        }
    }
}
