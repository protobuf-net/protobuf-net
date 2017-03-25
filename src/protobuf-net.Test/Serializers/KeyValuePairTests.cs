using System.Collections.Generic;
using NUnit.Framework;
using ProtoBuf.Meta;

namespace ProtoBuf.unittest.Serializers
{
    [TestFixture]
    public class KeyValuePairTests
    {
        [Test]
        public void BasicPairTest()
        {
            var pair = new KeyValuePair<int, string>(123, "abc");
            var model = TypeModel.Create();
            var clone = (KeyValuePair<int, string>)model.DeepClone(pair);
            Assert.AreEqual(pair.Key, clone.Key, "Runtime");
            Assert.AreEqual(pair.Value, clone.Value, "Runtime");

            model.CompileInPlace();
            clone = (KeyValuePair<int, string>)model.DeepClone(pair);
            Assert.AreEqual(pair.Key, clone.Key, "CompileInPlace");
            Assert.AreEqual(pair.Value, clone.Value, "CompileInPlace");

            clone = (KeyValuePair<int, string>)model.Compile().DeepClone(pair);
            Assert.AreEqual(pair.Key, clone.Key, "Compile");
            Assert.AreEqual(pair.Value, clone.Value, "Compile");
        }
        
        [Test]
        public void DictionaryInt32KeyTest()
        {
            var data = new Dictionary<int, string> { { 123, "abc" }, { 456, "def" } };
            var model = TypeModel.Create();
            var clone = (Dictionary<int, string>)model.DeepClone(data);
            Assert.AreEqual(2, clone.Count);
            Assert.AreEqual("abc", clone[123], "Runtime");
            Assert.AreEqual("def", clone[456], "Runtime");

            model.CompileInPlace();
            clone = (Dictionary<int, string>)model.DeepClone(data);
            Assert.AreEqual(2, clone.Count);
            Assert.AreEqual("abc", clone[123], "CompileInPlace");
            Assert.AreEqual("def", clone[456], "CompileInPlace");

            clone = (Dictionary<int, string>)model.Compile().DeepClone(data);
            Assert.AreEqual(2, clone.Count);
            Assert.AreEqual("abc", clone[123], "Compile");
            Assert.AreEqual("def", clone[456], "Compile");
        }

        [Test]
        public void DictionarySingleKeyTest()
        {
            var data = new Dictionary<float, string> { { 123, "abc" }, { 456, "def" } };
            var model = TypeModel.Create();
            var clone = (Dictionary<float, string>)model.DeepClone(data);
            Assert.AreEqual(2, clone.Count);
            Assert.AreEqual("abc", clone[123], "Runtime");
            Assert.AreEqual("def", clone[456], "Runtime");

            model.CompileInPlace();
            clone = (Dictionary<float, string>)model.DeepClone(data);
            Assert.AreEqual(2, clone.Count);
            Assert.AreEqual("abc", clone[123], "CompileInPlace");
            Assert.AreEqual("def", clone[456], "CompileInPlace");

            clone = (Dictionary<float, string>)model.Compile().DeepClone(data);
            Assert.AreEqual(2, clone.Count);
            Assert.AreEqual("abc", clone[123], "Compile");
            Assert.AreEqual("def", clone[456], "Compile");
        }

        [Test]
        public void TypeWithPairTest()
        {
            var orig = new TypeWithPair { Pair = new KeyValuePair<string, decimal>("abc", 123.45M) };
            var model = TypeModel.Create();
            var clone = (TypeWithPair)model.DeepClone(orig);
            Assert.AreEqual("abc", clone.Pair.Key, "Runtime");
            Assert.AreEqual(123.45M, clone.Pair.Value, "Runtime");

            model.Compile("TypeWithPairTest", "TypeWithPairTest.dll");
            PEVerify.Verify("TypeWithPairTest.dll");

            model.CompileInPlace();
            clone = (TypeWithPair)model.DeepClone(orig);
            Assert.AreEqual("abc", clone.Pair.Key, "CompileInPlace");
            Assert.AreEqual(123.45M, clone.Pair.Value, "CompileInPlace");

            clone = (TypeWithPair)model.Compile().DeepClone(orig);
            Assert.AreEqual("abc", clone.Pair.Key, "Compile");
            Assert.AreEqual(123.45M, clone.Pair.Value, "Compile");
        }

        [Test]
        public void TypeWithDictionaryTest()
        {
            var orig = new TypeWithDictionary { Data = new Dictionary<string, decimal> { { "abc", 123.45M } } };
            var model = TypeModel.Create();
            var clone = (TypeWithDictionary)model.DeepClone(orig);
            Assert.AreEqual(1, clone.Data.Count);
            Assert.AreEqual(123.45M, clone.Data["abc"], "Runtime");

            model.Compile("TypeWithDictionaryTest", "TypeWithDictionaryTest.dll");
            PEVerify.Verify("TypeWithDictionaryTest.dll");

            model.CompileInPlace();
            clone = (TypeWithDictionary)model.DeepClone(orig);
            Assert.AreEqual(1, clone.Data.Count);
            Assert.AreEqual(123.45M, clone.Data["abc"], "Runtime");

            clone = (TypeWithDictionary)model.Compile().DeepClone(orig);
            Assert.AreEqual(1, clone.Data.Count);
            Assert.AreEqual(123.45M, clone.Data["abc"], "Runtime");
        }

        [Test]
        public void ShouldWorkWithAutoLoadDisabledRuntime()
        {
            var orig = new TypeWithDictionary {Data = new Dictionary<string, decimal> {{"abc", 123.45M}}};
            var model = TypeModel.Create();
            model.AutoAddMissingTypes = false;
            model.Add(typeof (TypeWithDictionary), true);
            var clone = (TypeWithDictionary) model.DeepClone(orig);
            Assert.AreEqual(1, clone.Data.Count);
            Assert.AreEqual(123.45M, clone.Data["abc"]);
        }

        [Test]
        public void ShouldWorkWithAutoLoadDisabledAndAddedExplicitlyRuntime()
        {
            var orig = new TypeWithDictionary { Data = new Dictionary<string, decimal> { { "abc", 123.45M } } };
            var model = TypeModel.Create();
            model.AutoAddMissingTypes = false;
            model.Add(typeof(TypeWithDictionary), true);
            model.Add(typeof(KeyValuePair<string,decimal>), true);
            var clone = (TypeWithDictionary)model.DeepClone(orig);
            Assert.AreEqual(1, clone.Data.Count);
            Assert.AreEqual(123.45M, clone.Data["abc"]);
        }
        [Test]
        public void ShouldWorkWithAutoLoadDisabledCompileInPlace()
        {
            var orig = new TypeWithDictionary {Data = new Dictionary<string, decimal> {{"abc", 123.45M}}};
            var model = TypeModel.Create();
            model.AutoAddMissingTypes = false;
            model.Add(typeof (TypeWithDictionary), true);
            model.CompileInPlace();
            var clone = (TypeWithDictionary) model.DeepClone(orig);
            Assert.AreEqual(1, clone.Data.Count);
            Assert.AreEqual(123.45M, clone.Data["abc"]);
        }
        [Test]
        public void ShouldWorkWithAutoLoadDisabledCompile()
        {
            var orig = new TypeWithDictionary { Data = new Dictionary<string, decimal> { { "abc", 123.45M } } };
            var model = TypeModel.Create();
            model.AutoAddMissingTypes = false;
            model.Add(typeof(TypeWithDictionary), true);
            var clone = (TypeWithDictionary)model.Compile().DeepClone(orig);
            Assert.AreEqual(1, clone.Data.Count);
            Assert.AreEqual(123.45M, clone.Data["abc"]);
        }

        [Test]
        public void TypeWithIDictionaryTest()
        {
            var orig = new TypeWithIDictionary { Data = new Dictionary<string, decimal> { { "abc", 123.45M } } };
            var model = TypeModel.Create();
            var clone = (TypeWithIDictionary)model.DeepClone(orig);
            Assert.AreEqual(1, clone.Data.Count);
            Assert.AreEqual(123.45M, clone.Data["abc"], "Runtime");

            model.Compile("TypeWithIDictionary", "TypeWithIDictionary.dll");
            PEVerify.Verify("TypeWithIDictionary.dll");

            model.CompileInPlace();
            clone = (TypeWithIDictionary)model.DeepClone(orig);
            Assert.AreEqual(1, clone.Data.Count);
            Assert.AreEqual(123.45M, clone.Data["abc"], "Runtime");

            clone = (TypeWithIDictionary)model.Compile().DeepClone(orig);
            Assert.AreEqual(1, clone.Data.Count);
            Assert.AreEqual(123.45M, clone.Data["abc"], "Runtime");
        }

        [ProtoContract]
        public class TypeWithPair
        {
            [ProtoMember(1)]
            public KeyValuePair<string, decimal> Pair { get; set; }
        }

        [ProtoContract]
        public class TypeWithDictionary
        {
            [ProtoMember(1)]
            public Dictionary<string, decimal> Data { get; set; }
        }

        [ProtoContract]
        public class TypeWithIDictionary
        {
            [ProtoMember(1)]
            public IDictionary<string, decimal> Data { get; set; }
        }
    }
}
