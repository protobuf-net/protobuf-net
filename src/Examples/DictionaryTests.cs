using System.Linq;
using Xunit;
using System.Collections.Generic;
using ProtoBuf;
using System;
using ProtoBuf.Meta;
using System.IO;
using System.Diagnostics;
using System.Text;

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

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
        public override bool Equals(object other)
        {
            return Equals(other as SimpleData);
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
    
    public class EmptyDictionaryTests
    {
        [Fact]
        public void EmptyDictionaryShouldDeserializeAsNonNull()
        {
            using (var ms = new MemoryStream())
            {
                var data = new Dictionary<string, int>();

                Serializer.Serialize(ms, data);
                ms.Position = 0;
                var clone = Serializer.Deserialize<Dictionary<string, int>>(ms);

                Assert.NotNull(clone);
                Assert.Empty(clone);
            }
        }
        [Fact]
        public void NonEmptyDictionaryShouldDeserialize()
        {
            using (var ms = new MemoryStream())
            {
                var data = new Dictionary<string, int> { { "abc", 123 } };

                Serializer.Serialize(ms, data);
                ms.Position = 0;
                var clone = Serializer.Deserialize<Dictionary<string, int>>(ms);

                Assert.NotNull(clone);
                Assert.Single(clone);
                Assert.Equal(123, clone["abc"]);
            }
        }
        [Fact]
        public void EmptyDictionaryShouldDeserializeAsNonNullViaInterface()
        {
            using (var ms = new MemoryStream())
            {
                var data = new Dictionary<string, int>();

                Serializer.Serialize(ms, data);
                ms.Position = 0;
                var clone = Serializer.Deserialize<IDictionary<string, int>>(ms);

                Assert.NotNull(clone);
                Assert.Equal(0, clone.Count);
            }

        }
        [Fact]
        public void NonEmptyDictionaryShouldDeserializeViaInterface()
        {
            using (var ms = new MemoryStream())
            {
                var data = new Dictionary<string, int> { { "abc", 123 } };

                Serializer.Serialize(ms, data);
                ms.Position = 0;
                var clone = Serializer.Deserialize<IDictionary<string, int>>(ms);

                Assert.NotNull(clone);
                Assert.Equal(1, clone.Count);
                Assert.Equal(123, clone["abc"]);
            }
        }
    }
    
    public class NestedDictionaryTests {

        [Fact]
        public void TestNestedConcreteConcreteDictionary()
        {
            Dictionary<string, Dictionary<string, String>> data = new Dictionary<string, Dictionary<string, string>>
            {
                { "abc", new Dictionary<string,string> {{"def","ghi"}}},
                { "jkl", new Dictionary<string,string> {{"mno","pqr"},{"stu","vwx"}}}
            };
            CheckNested(data, "original");
            var clone = Serializer.DeepClone(data);
            CheckNested(clone, "clone");
        }

        [Fact]
        public void TestNestedInterfaceInterfaceDictionary()
        {
            IDictionary<string, IDictionary<string, String>> data = new Dictionary<string, IDictionary<string, string>>
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
            IDictionary<string, Dictionary<string, String>> data = new Dictionary<string, Dictionary<string, string>>
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
            Dictionary<string, IDictionary<string, String>> data = new Dictionary<string, IDictionary<string, string>>
            {
                { "abc", new Dictionary<string,string> {{"def","ghi"}}},
                { "jkl", new Dictionary<string,string> {{"mno","pqr"},{"stu","vwx"}}}
            };
            CheckNested(data, "original");
            var clone = Serializer.DeepClone(data);
            CheckNested(clone, "clone");
        }

        static void CheckNested<TInner>(IDictionary<string, TInner> data, string message)
            where TInner : IDictionary<string, string>
        {
            Assert.NotNull(data); //, message);
            Assert.Equal(2, data.Keys.Count); //, message);
            var inner = data["abc"];
            Assert.Equal(1, inner.Keys.Count); //, message);
            Assert.Equal("ghi", inner["def"]); //, message);
            inner = data["jkl"];
            Assert.Equal(2, inner.Keys.Count); //, message);
            Assert.Equal("pqr", inner["mno"]); //, message);
            Assert.Equal("vwx", inner["stu"]); //, message);

        }

        [Fact]
        public void CheckPerformanceNotInsanelyBad()
        {
            var model = TypeModel.Create();
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
            int l3 = BulkTest(model, o2, out int s3, out int d3);

            Console.WriteLine("Bytes (props)\t" + l1);
            Console.WriteLine("Ser (props)\t" + s1);
            Console.WriteLine("Deser (props)\t" + d1);
            Console.WriteLine("Bytes (kv-default)\t" + l2);
            Console.WriteLine("Ser (kv-default)\t" + s2);
            Console.WriteLine("Deser (kv-default)\t" + d2);
            Console.WriteLine("Bytes (kv-grouped)\t" + l3);
            Console.WriteLine("Ser (kv-grouped)\t" + s3);
            Console.WriteLine("Deser (kv-grouped)\t" + d3);

            var pw = ProtoWriter.Create(Stream.Null, null, null);
            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < LOOP; i++ ) {
                ProtoWriter.WriteFieldHeader(1, WireType.String, pw);
                ProtoWriter.WriteString("Field1", pw);
                ProtoWriter.WriteFieldHeader(1, WireType.String, pw);
                ProtoWriter.WriteString("Field2", pw);
                ProtoWriter.WriteFieldHeader(1, WireType.String, pw);
                ProtoWriter.WriteString("Field3", pw);
            }
            watch.Stop();
            pw.Close();
            Console.WriteLine("Encoding: " + watch.ElapsedMilliseconds);
            
        }
        const int LOOP = 500000;
        static int BulkTest<T>(TypeModel model, T obj, out int serialize, out int deserialize) where T: class
        {
            
            using(MemoryStream ms = new MemoryStream())
            {
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
                Type type = typeof (T);
                watch.Start();
                for (int i = 0; i < LOOP; i++)
                {
                    ms.Position = 0;
                    model.Deserialize(ms, null, type);
                }
                watch.Stop();
                deserialize = (int)watch.ElapsedMilliseconds;
                return (int)ms.Length;
            }
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
