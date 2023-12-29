using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Xunit;

namespace Examples
{
    public class HashSetSerializerTests
    {
        [ProtoContract]
        class HashSetData<T>
        {
            [ProtoMember(1)]
            public HashSet<T> Data { get; set; }
        }

        [ProtoContract]
        class NotEmptyHashSetData
        {
            public NotEmptyHashSetData() => Data = new HashSet<int>(new[] { 1 });

            public NotEmptyHashSetData(HashSet<int> data) => Data = data;

            [ProtoMember(1)]
            public HashSet<int> Data { get; }
        }

        [Fact]
        public void TestEmptyNestedSetWithStrings()
        {
            var set = new HashSet<string>();
            var input = new HashSetData<string>() { Data = set };

            var clone = Serializer.DeepClone(input);

            Assert.NotSame(input, clone);
            Assert.Null(clone.Data);
        }

        [Fact]
        public void TestNullNestedSetWithStrings()
        {
            var input = new HashSetData<string>() { Data = null };

            var clone = Serializer.DeepClone(input);

            Assert.NotSame(input, clone);
            Assert.Equal(input.Data, clone.Data);
        }

        [Fact]
        public void TestNestedSetWithStrings()
        {
            var set = new HashSet<string>();
            set.Add("hello");
            set.Add("world");
            var input = new HashSetData<string>() { Data = set };

            var clone = Serializer.DeepClone(input);

            Assert.NotSame(input, clone);
            AssertEqual(input.Data, clone.Data);
        }

        [Fact]
        public void TestNestedSetWithInt32()
        {
            var set = new HashSet<int>();
            set.Add(1);
            set.Add(2);
            set.Add(3);
            set.Add(3);
            var input = new HashSetData<int>() { Data = set };

            var clone = Serializer.DeepClone(input);

            Assert.NotSame(input, clone);
            AssertEqual(input.Data, clone.Data);
        }

        [Fact]
        public void RoundtripHashSet()
        {
            HashSet<int> lookup = new HashSet<int>(new[] { 1, 2, 3 });

            var clone = Serializer.DeepClone(lookup);

            AssertEqual(lookup, clone);
        }

        [Fact]
        public void TestNonEmptySetStrings()
        {
            var set = new HashSet<int>();
            set.Add(1);
            set.Add(2);
            set.Add(2);
            var input = new NotEmptyHashSetData(set);

            var clone = Serializer.DeepClone(input);

            Assert.NotSame(input, clone);
            AssertEqual(input.Data, clone.Data);
        }

        [Fact]
        public void TestDefaultCtorValueOverwritten()
        {
            var set = new HashSet<int>();
            set.Add(3);
            var input = new NotEmptyHashSetData(set);

            var clone = Serializer.DeepClone(input);
            Assert.NotSame(input, clone);
            Assert.Contains(new NotEmptyHashSetData().Data.Single(), clone.Data);
            Assert.Contains(3, clone.Data);
        }

        static void AssertEqual<T>(
            HashSet<T> expected,
            HashSet<T> actual)
        {
            Assert.Equal(expected.Count, actual.Count);
            foreach (var value in expected)
                Assert.Contains(value, actual);
        }
    }

    public class SetSerializerTests
    {
        [ProtoContract]
        class SetData<T>
        {
            [ProtoMember(1)]
            public ISet<T> Data { get; set; }
        }

        [ProtoContract]
        class NotEmptySetData
        {
            public NotEmptySetData() => Data = new HashSet<int>(new[] { 1 });

            public NotEmptySetData(ISet<int> data) => Data = data;

            [ProtoMember(1)]
            public ISet<int> Data { get; }
        }

        [Fact]
        public void TestEmptyNestedSetWithStrings()
        {
            var set = new HashSet<string>();
            var input = new SetData<string>() { Data = set };

            var clone = Serializer.DeepClone(input);

            Assert.NotSame(input, clone);
            Assert.Null(clone.Data);
        }

        [Fact]
        public void TestNullNestedSetWithStrings()
        {
            var input = new SetData<string>() { Data = null };

            var clone = Serializer.DeepClone(input);

            Assert.NotSame(input, clone);
            Assert.Equal(input.Data, clone.Data);
        }

        [Fact]
        public void TestNestedSetWithStrings()
        {
            var set = new HashSet<string>();
            set.Add("hello");
            set.Add("world");
            var input = new SetData<string>() { Data = set };

            var clone = Serializer.DeepClone(input);

            Assert.NotSame(input, clone);
            AssertEqual(input.Data, clone.Data);
        }

        [Fact]
        public void TestNestedSetWithInt32()
        {
            var set = new HashSet<int>();
            set.Add(1);
            set.Add(2);
            set.Add(3);
            set.Add(3);
            var input = new SetData<int>() { Data = set };

            var clone = Serializer.DeepClone(input);

            Assert.NotSame(input, clone);
            AssertEqual(input.Data, clone.Data);
        }

        [Fact]
        public void RoundtripSet()
        {
            ISet<int> lookup = new HashSet<int>(new[] { 1, 2, 3 });

            var clone = Serializer.DeepClone(lookup);

            AssertEqual(lookup, clone);
        }

        [Fact]
        public void TestNonEmptySetStrings()
        {
            var set = new HashSet<int>();
            set.Add(1);
            set.Add(2);
            set.Add(2);
            var input = new NotEmptySetData(set);

            var clone = Serializer.DeepClone(input);

            Assert.NotSame(input, clone);
            AssertEqual(input.Data, clone.Data);
        }

        [Fact]
        public void TestDefaultCtorValueOverwritten()
        {
            var set = new HashSet<int>();
            set.Add(3);
            var input = new NotEmptySetData(set);

            var clone = Serializer.DeepClone(input);
            Assert.NotSame(input, clone);
            Assert.True(clone.Data.Contains(new NotEmptySetData().Data.Single()));
            Assert.True(clone.Data.Contains(3));
        }

        static void AssertEqual<T>(
            ISet<T> expected,
            ISet<T> actual)
        {
            Assert.Equal(expected.Count, actual.Count);
            foreach (var value in expected)
                Assert.True(actual.Contains(value));
        }
    }

#if NET6_0_OR_GREATER
    public class ReadOnlySetSerializerTests
    {
        [ProtoContract]
        class ReadOnlySetData<T>
        {
            [ProtoMember(1)]
            public IReadOnlySet<T> Data { get; init; }
        }

        [ProtoContract]
        class NotEmptyReadOnlySetData
        {
            public NotEmptyReadOnlySetData()
            {
                Data = new HashSet<int>(new[] { 1 });
            }

            public NotEmptyReadOnlySetData(IReadOnlySet<int> data)
            {
                Data = data;
            }

            [ProtoMember(1)]
            public IReadOnlySet<int> Data { get; }
        }

        [Fact]
        public void TestEmptyNestedSetWithStrings()
        {
            var set = new HashSet<string>();
            var input = new ReadOnlySetData<string>() { Data = set };

            var clone = Serializer.DeepClone(input);

            Assert.NotSame(input, clone);
            Assert.Null(clone.Data);
        }

        [Fact]
        public void TestNullNestedSetWithStrings()
        {
            var input = new ReadOnlySetData<string>() { Data = null };

            var clone = Serializer.DeepClone(input);

            Assert.NotSame(input, clone);
            Assert.Equal(input.Data, clone.Data);
        }

        [Fact]
        public void TestNestedSetWithStrings()
        {
            var set = new HashSet<string>();
            set.Add("hello");
            set.Add("world");
            var input = new ReadOnlySetData<string>() { Data = set };

            var clone = Serializer.DeepClone(input);

            Assert.NotSame(input, clone);
            AssertEqual(input.Data, clone.Data);
        }

        [Fact]
        public void TestNestedSetWithInt32()
        {
            var set = new HashSet<int>();
            set.Add(1);
            set.Add(2);
            set.Add(3);
            set.Add(3);
            var input = new ReadOnlySetData<int>() { Data = set };

            var clone = Serializer.DeepClone(input);

            Assert.NotSame(input, clone);
            AssertEqual(input.Data, clone.Data);
        }

        [Fact]
        public void RoundtripReadOnlySet()
        {
            IReadOnlySet<int> lookup = new HashSet<int>(new[] { 1, 2, 3 });

            var clone = Serializer.DeepClone(lookup);

            AssertEqual(lookup, clone);
        }

        [Fact]
        public void TestNonEmptySetStrings()
        {
            var set = new HashSet<int>();
            set.Add(1);
            set.Add(2);
            set.Add(2);
            var input = new NotEmptyReadOnlySetData(set);

            var clone = Serializer.DeepClone(input);

            Assert.NotSame(input, clone);
            AssertEqual(input.Data, clone.Data);
        }

        [Fact]
        public void TestDefaultCtorValueOverwritten()
        {
            var set = new HashSet<int>();
            set.Add(3);
            var input = new NotEmptyReadOnlySetData(set);

            var clone = Serializer.DeepClone(input);
            Assert.NotSame(input, clone);
            Assert.True(clone.Data.Contains(new NotEmptyReadOnlySetData().Data.Single()));
            Assert.True(clone.Data.Contains(3));
        }

        static void AssertEqual<T>(
            IReadOnlySet<T> expected,
            IReadOnlySet<T> actual)
        {
            Assert.Equal(expected.Count, actual.Count);
            foreach (var value in expected)
                Assert.True(actual.Contains(value));
        }
    }
#endif
}