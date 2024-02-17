using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Examples.Dictionary
{
    [ProtoContract]
    class DataWithDictionary<T>
    {
        public DataWithDictionary()
        {
            Data = new Dictionary<int, T>();
        }
        [ProtoMember(1)]
        public IDictionary<int, T> Data { get; private set; }
    }

    [ProtoContract]
    class SimpleData : IEquatable<SimpleData>
    {
        public SimpleData() {}
        public SimpleData(int value) {
            Value = value;}

        [ProtoMember(1)]
        public int Value { get; set; }

        public bool Equals(SimpleData other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object obj)
        {
            return Equals(obj as SimpleData);
        }
    }


    public class DictionaryTests
    {
        [Fact]
        public void TestNestedDictionaryWithStrings()
        {
            var obj = new DataWithDictionary<string>();
            obj.Data[0] = "abc";
            obj.Data[4] = "def";
            obj.Data[7] = "abc";

            var clone = Serializer.DeepClone(obj);
            Assert.NotSame(obj,clone);
            AssertEqual(obj.Data, clone.Data);
        }

        [Fact]
        public void TestNestedDictionaryWithSimpleData()
        {
            var obj = new DataWithDictionary<SimpleData>();
            obj.Data[0] = new SimpleData(5);
            obj.Data[4] = new SimpleData(72);
            obj.Data[7] = new SimpleData(72);

            var clone = Serializer.DeepClone(obj);
            Assert.NotSame(obj, clone);
            AssertEqual(obj.Data, clone.Data);
        }

        [Fact]
        public void RoundtripDictionary()
        {
            var lookup = new Dictionary<int, string>
            {
                [0] = "abc",
                [4] = "def",
                [7] = "abc"
            };

            var clone = Serializer.DeepClone(lookup);

            AssertEqual(lookup, clone);
        }
        static void AssertEqual<TKey, TValue>(
            IDictionary<TKey, TValue> expected,
            IDictionary<TKey, TValue> actual)
        {
            Assert.NotSame(expected, actual); //, "Instance");
            Assert.Equal(expected.Count, actual.Count); //, "Count");
            foreach (var pair in expected)
            {
                Assert.True(actual.TryGetValue(pair.Key, out TValue value)); //, "Missing: " + pair.Key);
                Assert.Equal(pair.Value, value); //, "Value: " + pair.Key);
            }
        }
    }

    public class DictionaryOfIReadOnlyDictionarySerializerTests
    {
        [ProtoContract]
        class ReadOnlyDictionaryData<T>
        {
            public ReadOnlyDictionaryData() {}

            public ReadOnlyDictionaryData(Dictionary<int, T> data)
            {
                Data = data;
            }

            [ProtoMember(1)]
            public IReadOnlyDictionary<int, T> Data { get; }
        }

        [ProtoContract]
        class NonEmptyReadOnlyDictionaryData
        {
            public NonEmptyReadOnlyDictionaryData()
            {
                var temp = new Dictionary<int, string>();
                temp[2] = "something";
                Data = temp;
            }

            public NonEmptyReadOnlyDictionaryData(Dictionary<int, string> data)
            {
                Data = data;
            }

            [ProtoMember(1)]
            public IReadOnlyDictionary<int, string> Data { get; }
        }

        [Fact]
        public void TestNestedDictionaryWithStrings()
        {
            var obj = new Dictionary<int, string>();
            obj[0] = "abc";
            obj[1] = "def";
            obj[7] = "abc";
            var input = new ReadOnlyDictionaryData<string>(obj);

            var clone = Serializer.DeepClone(input);
            Assert.NotSame(input, clone);
            AssertEqual(input.Data, clone.Data);
        }

        [Fact]
        public void TestNestedDictionaryWithSimpleData()
        {
            var obj = new Dictionary<int, SimpleData>();
            obj[0] = new SimpleData(5);
            obj[4] = new SimpleData(72);
            obj[7] = new SimpleData(72);
            var input = new ReadOnlyDictionaryData<SimpleData>(obj);

            var clone = Serializer.DeepClone(input);
            Assert.NotSame(input, clone);
            AssertEqual(input.Data, clone.Data);
        }

        [Fact]
        public void RoundtripDictionary()
        {
            IReadOnlyDictionary<int, string> lookup = new Dictionary<int, string>
            {
                [0] = "abc",
                [4] = "def",
                [7] = "abc"
            };

            var clone = Serializer.DeepClone(lookup);

            AssertEqual(lookup, clone);
        }

        [Fact]
        public void TestNonEmptyDictionaryStrings()
        {
            var obj = new Dictionary<int, string>();
            obj[0] = "abc";
            obj[1] = "def";
            var input = new NonEmptyReadOnlyDictionaryData(obj);

            var clone = Serializer.DeepClone(input);
            Assert.NotSame(input, clone);
            Assert.Equal(input.Data[0], clone.Data[0]);
            Assert.Equal(input.Data[1], clone.Data[1]);
            Assert.Equal("something", clone.Data[2]);
        }

        [Fact]
        public void TestNonEmptyDictionaryOverwriteStrings()
        {
            var obj = new Dictionary<int, string>();
            obj[0] = "abc";
            obj[2] = "def";
            var input = new NonEmptyReadOnlyDictionaryData(obj);

            var clone = Serializer.DeepClone(input);
            Assert.NotSame(input, clone);
            Assert.Equal(input.Data[0], clone.Data[0]);
            Assert.Equal("def", clone.Data[2]);
        }

        static void AssertEqual<TKey, TValue>(
            IReadOnlyDictionary<TKey, TValue> expected,
            IReadOnlyDictionary<TKey, TValue> actual)
        {
            Assert.NotSame(expected, actual);
            Assert.Equal(expected.Count, actual.Count);
            foreach (var pair in expected)
            {
                Assert.True(actual.TryGetValue(pair.Key, out TValue value));
                Assert.Equal(pair.Value, value);
            }
        }
    }

    public class EmptyDictionaryTests
    {
        [Fact]
        public void EmptyDictionaryShouldDeserializeAsNonNull()
        {
            using var ms = new MemoryStream();
            var data = new Dictionary<string, int>();

            Serializer.Serialize(ms, data);
            ms.Position = 0;
            var clone = Serializer.Deserialize<Dictionary<string, int>>(ms);

            Assert.NotNull(clone);
            Assert.Empty(clone);
        }
        [Fact]
        public void NonEmptyDictionaryShouldDeserialize()
        {
            using var ms = new MemoryStream();
            var data = new Dictionary<string, int> { { "abc", 123 } };

            Serializer.Serialize(ms, data);
            ms.Position = 0;
            var clone = Serializer.Deserialize<Dictionary<string, int>>(ms);

            Assert.NotNull(clone);
            Assert.Single(clone);
            Assert.Equal(123, clone["abc"]);
        }
        [Fact]
        public void EmptyDictionaryShouldDeserializeAsNonNullViaInterface()
        {
            using var ms = new MemoryStream();
            var data = new Dictionary<string, int>();

            Serializer.Serialize(ms, data);
            Assert.Equal(0, ms.Length);
            ms.Position = 0;
            var clone = Serializer.Deserialize<IDictionary<string, int>>(ms);

            Assert.NotNull(clone);
            Assert.Empty(clone);

        }
        [Fact]
        public void NonEmptyDictionaryShouldDeserializeViaInterface()
        {
            using var ms = new MemoryStream();
            var data = new Dictionary<string, int> { { "abc", 123 } };

            Serializer.Serialize(ms, data);
            ms.Position = 0;
            var clone = Serializer.Deserialize<IDictionary<string, int>>(ms);

            Assert.NotNull(clone);
            Assert.Single(clone);
            Assert.Equal(123, clone["abc"]);
        }
    }
    
    public class NestedDictionaryTests
    {
        const string ExpectedHex = "0A-11-0A-03-61-62-63-12-0A-0A-03-64-65-66-12-03-67-68-69";
        /*
0A = field 1, type String
11 = length 17
  0A = field 1, type String
  03 = length 3
  61-62-63 = "abc"
  12 = field 2, type String
  0A = length 10
    0A = field 1, type String
    03 = length 3
    64-65-66 = "def"
    12 = field 2, type String
    03 = length 3
    67-68-69 = "ghi"
etc
         */
        private ITestOutputHelper Log { get; }
        public NestedDictionaryTests(ITestOutputHelper _log) => Log = _log;

        [Fact]
        public void TestNestedConcreteConcreteDictionary()
        {
            Dictionary<string, Dictionary<string, string>> data = new Dictionary<string, Dictionary<string, string>>
            {
                { "abc", new Dictionary<string,string> {{"def","ghi"}}},
                { "jkl", new Dictionary<string,string> {{"mno","pqr"},{"stu","vwx"}}}
            };
            CheckNested(data, "original");
            // CheckHex(data, ExpectedHex);
            var clone = Serializer.DeepClone(data);
            CheckNested(clone, "clone");
        }

        private static void CheckHex<T>(T data, string expected)
        {
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, data);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal(expected, hex);
        }

        [Fact]
        public void TestNestedInterfaceInterfaceDictionary()
        {
            IDictionary<string, IDictionary<string, string>> data = new Dictionary<string, IDictionary<string, string>>
            {
                { "abc", new Dictionary<string,string> {{"def","ghi"}}},
                { "jkl", new Dictionary<string,string> {{"mno","pqr"},{"stu","vwx"}}}
            };
            CheckNested(data, "original");
            var clone = Serializer.DeepClone(data);
            CheckNested(clone, "clone");
        }

        [Fact]
        public void TestNestedInterfaceConcreteDictionary()
        {
            IDictionary<string, Dictionary<string, string>> data = new Dictionary<string, Dictionary<string, string>>
            {
                { "abc", new Dictionary<string,string> {{"def","ghi"}}},
                { "jkl", new Dictionary<string,string> {{"mno","pqr"},{"stu","vwx"}}}
            };
            CheckNested(data, "original");
            var clone = Serializer.DeepClone(data);
            CheckNested(clone, "clone");
        }

        [Fact]
        public void TestNestedConcreteInterfaceDictionary()
        {
            Dictionary<string, IDictionary<string, string>> data = new Dictionary<string, IDictionary<string, string>>
            {
                { "abc", new Dictionary<string,string> {{"def","ghi"}}},
                { "jkl", new Dictionary<string,string> {{"mno","pqr"},{"stu","vwx"}}}
            };
            CheckNested(data, "original");
            var clone = Serializer.DeepClone(data);
            CheckNested(clone, "clone");
        }

#pragma warning disable IDE0060
        static void CheckNested<TInner>(IDictionary<string, TInner> data, string message)
#pragma warning restore IDE0060
            where TInner : IDictionary<string, string>
        {
            Assert.NotNull(data); //, message);
            Assert.Equal(2, data.Keys.Count); //, message);
            var allKeys = string.Join(", ", data.Keys.OrderBy(x => x));
            Assert.Equal("abc, jkl", allKeys);

            var inner = data["abc"];
            Assert.Single(inner.Keys); //, message);
            Assert.Equal("ghi", inner["def"]); //, message);
            inner = data["jkl"];
            Assert.Equal(2, inner.Keys.Count); //, message);
            Assert.Equal("pqr", inner["mno"]); //, message);
            Assert.Equal("vwx", inner["stu"]); //, message);
        }

#if LONG_RUNNING
        [Fact]
        public void CheckPerformanceNotInsanelyBad()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof (PropsViaDictionaryDefault), true);
            model.Add(typeof (PropsViaDictionaryGrouped), true);
            model.Add(typeof (PropsViaProperties), true);
            model.CompileInPlace();
            var o1 = new PropsViaProperties { Field1 = 123, Field2 = 456, Field3 = 789 };

            var o2 = new PropsViaDictionaryDefault()
            {
                Values = new List<KeyValuePair> {
                new KeyValuePair {Key = "Field1", Value =123 },
                new KeyValuePair {Key = "Field2", Value =456 },
                new KeyValuePair {Key = "Field2", Value =789 },
            }
            };
            var o3 = new PropsViaDictionaryGrouped()
            {
                Values = new List<KeyValuePair> {
                new KeyValuePair {Key = "Field1", Value =123 },
                new KeyValuePair {Key = "Field2", Value =456 },
                new KeyValuePair {Key = "Field2", Value =789 },
            }
            };


            int l1 = BulkTest(model, o1, out int s1, out int d1);
            int l2 = BulkTest(model, o2, out int s2, out int d2);
            int l3 = BulkTest(model, o3, out int s3, out int d3);

            Log.WriteLine("Bytes (props)\t" + l1);
            Log.WriteLine("Ser (props)\t" + s1);
            Log.WriteLine("Deser (props)\t" + d1);
            Log.WriteLine("Bytes (kv-default)\t" + l2);
            Log.WriteLine("Ser (kv-default)\t" + s2);
            Log.WriteLine("Deser (kv-default)\t" + d2);
            Log.WriteLine("Bytes (kv-grouped)\t" + l3);
            Log.WriteLine("Ser (kv-grouped)\t" + s3);
            Log.WriteLine("Deser (kv-grouped)\t" + d3);

            using var state = ProtoWriter.State.Create(Stream.Null, null, null);
            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < LOOP; i++ ) {
                state.WriteFieldHeader(1, WireType.String);
                state.WriteString("Field1");
                state.WriteFieldHeader(1, WireType.String);
                state.WriteString("Field2");
                state.WriteFieldHeader(1, WireType.String);
                state.WriteString("Field3");
            }
            watch.Stop();
            state.Close();
            Log.WriteLine("Encoding: " + watch.ElapsedMilliseconds);
            
        }
#endif

        const int LOOP = 500000;
        static int BulkTest<T>(TypeModel model, T obj, out int serialize, out int deserialize) where T: class
        {

            using MemoryStream ms = new MemoryStream();
            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < LOOP; i++)
            {
                ms.Position = 0;
                ms.SetLength(0);
                model.Serialize(ms, obj);
            }
            watch.Stop();
            serialize = (int)watch.ElapsedMilliseconds;
            watch.Reset();
            Type type = typeof(T);
            watch.Start();
            for (int i = 0; i < LOOP; i++)
            {
                ms.Position = 0;
#pragma warning disable CS0618
                model.Deserialize(ms, null, type);
#pragma warning restore CS0618
            }
            watch.Stop();
            deserialize = (int)watch.ElapsedMilliseconds;
            return (int)ms.Length;
        }


        [ProtoContract]
        public class PropsViaDictionaryDefault
        {
            [ProtoMember(1, DataFormat = DataFormat.Default)] public List<KeyValuePair> Values { get; set;}
        }
        [ProtoContract]
        public class PropsViaDictionaryGrouped
        {
            [ProtoMember(1, DataFormat = DataFormat.Group)]
            public List<KeyValuePair> Values { get; set; }
        }
        [ProtoContract]
        public class KeyValuePair
        {
            [ProtoMember(1)] public string Key { get; set;}
            [ProtoMember(2)] public int Value { get; set;}
        }
        [ProtoContract]
        class PropsViaProperties
        {
            [ProtoMember(1)] public int Field1 { get; set;}
            [ProtoMember(2)] public int Field2 { get; set;}
            [ProtoMember(3)] public int Field3 { get; set;}
        }
    }
}
