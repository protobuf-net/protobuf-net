using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using ProtoBuf.Meta;
using ProtoBuf.unittest;
using Xunit;

namespace ProtoBuf.Issues
{
    public class Immutables
    {

        [Fact]
        public void ImmutableArrayValidIL()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(ImmutableArrayTestClass));
            model.CompileAndVerify();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanDeserialiseImmutableArray(bool autoCompile)
        {
            var testClass = new ImmutableArrayTestClass(ImmutableArray.Create("a", "b", "c"));
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = autoCompile;

            ImmutableArrayTestClass testClassClone;
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, testClass);
                ms.Position = 0;
#pragma warning disable CS0618
                testClassClone = (ImmutableArrayTestClass)model.Deserialize(ms, null, testClass.GetType());
#pragma warning restore CS0618
            }

            Assert.Equal((IEnumerable<string>)testClass.Array, (IEnumerable<string>)testClassClone.Array);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanDeserialiseImmutableList(bool autoCompile)
        {
            var testClass = new ImmutableListTestClass(ImmutableList.Create("a", "b", "c"));
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = autoCompile;

            ImmutableListTestClass testClassClone;
            using var ms = new MemoryStream();
            model.Serialize(ms, testClass);
            ms.Position = 0;
#pragma warning disable CS0618
            testClassClone = (ImmutableListTestClass)model.Deserialize(ms, null, testClass.GetType());
#pragma warning restore CS0618

            Assert.Equal((IEnumerable<string>)testClass.List, (IEnumerable<string>)testClassClone.List);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanDeserialiseImmutableHashSet(bool autoCompile)
        {
            var testClass = new ImmutableHashSetTestClass(ImmutableHashSet.Create("a", "b", "c"));
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = autoCompile;

            ImmutableHashSetTestClass testClassClone;
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, testClass);
                ms.Position = 0;
#pragma warning disable CS0618
                testClassClone = (ImmutableHashSetTestClass)model.Deserialize(ms, null, testClass.GetType());
#pragma warning restore CS0618
            }

            Assert.True(testClass.Set.SetEquals(testClassClone.Set));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanDeserialiseImmutableSortedSet(bool autoCompile)
        {
            var testClass = new ImmutableSortedSetTestClass(ImmutableSortedSet.Create("a", "b", "c"));
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = autoCompile;

            ImmutableSortedSetTestClass testClassClone;
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, testClass);
                ms.Position = 0;
#pragma warning disable CS0618
                testClassClone = (ImmutableSortedSetTestClass)model.Deserialize(ms, null, testClass.GetType());
#pragma warning restore CS0618
            }

            Assert.Equal((IEnumerable<string>)testClass.Set, (IEnumerable<string>)testClassClone.Set);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanDeserialiseImmutableDictionary(bool autoCompile)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder.Add("a", "1");
            builder.Add("b", "2");
            builder.Add("c", "2");
            var testClass = new ImmutableDictionaryTestClass(builder.ToImmutable());
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = autoCompile;

            ImmutableDictionaryTestClass testClassClone;
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, testClass);
                ms.Position = 0;
#pragma warning disable CS0618
                testClassClone = (ImmutableDictionaryTestClass)model.Deserialize(ms, null, testClass.GetType());
#pragma warning restore CS0618
            }

            Assert.Equal((IEnumerable<KeyValuePair<string, string>>)testClass.Dictionary.OrderBy(x => x.Key), (IEnumerable<KeyValuePair<string, string>>)testClassClone.Dictionary.OrderBy(x => x.Key));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanDeserialiseImmutableSortedDictionary(bool autoCompile)
        {
            var builder = ImmutableSortedDictionary.CreateBuilder<string, string>();
            builder.Add("a", "1");
            builder.Add("b", "2");
            builder.Add("c", "2");
            var testClass = new ImmutableSortedDictionaryTestClass(builder.ToImmutable());
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = autoCompile;
            model.Add(typeof(ImmutableSortedDictionaryTestClass));

            model.CompileAndVerify();

            ImmutableSortedDictionaryTestClass testClassClone;
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, testClass);
                ms.Position = 0;
#pragma warning disable CS0618
                testClassClone = (ImmutableSortedDictionaryTestClass)model.Deserialize(ms, null, testClass.GetType());
#pragma warning restore CS0618
            }

            Assert.Equal((IEnumerable<KeyValuePair<string, string>>)testClass.Dictionary, (IEnumerable<KeyValuePair<string, string>>)testClassClone.Dictionary);
        }

        [ProtoContract(SkipConstructor = true)]
        public class ImmutableArrayTestClass
        {
            public ImmutableArrayTestClass(ImmutableArray<string> array)
            {
                Array = array;
            }

            [ProtoMember(1)]
            public ImmutableArray<string> Array {get;}
        }

        [ProtoContract(SkipConstructor = true)]
        public class ImmutableListTestClass
        {
            public ImmutableListTestClass(ImmutableList<string> list)
            {
                List = list;
            }

            [ProtoMember(1)]
            public ImmutableList<string> List { get; }
        }

        [ProtoContract(SkipConstructor = true)]
        public class ImmutableHashSetTestClass
        {
            public ImmutableHashSetTestClass(ImmutableHashSet<string> set)
            {
                Set = set;
            }

            [ProtoMember(1)]
            public ImmutableHashSet<string> Set { get; }
        }

        [ProtoContract(SkipConstructor = true)]
        public class ImmutableSortedSetTestClass
        {
            public ImmutableSortedSetTestClass(ImmutableSortedSet<string> set)
            {
                Set = set;
            }

            [ProtoMember(1)]
            public ImmutableSortedSet<string> Set { get; }
        }

        [ProtoContract(SkipConstructor = true)]
        public class ImmutableDictionaryTestClass
        {
            public ImmutableDictionaryTestClass(ImmutableDictionary<string, string> dictionary)
            {
                Dictionary = dictionary;
            }

            [ProtoMember(1)]
            public ImmutableDictionary<string, string> Dictionary { get; }
        }

        [ProtoContract(SkipConstructor = true)]
        public class ImmutableSortedDictionaryTestClass
        {
            public ImmutableSortedDictionaryTestClass(ImmutableSortedDictionary<string, string> dictionary)
            {
                Dictionary = dictionary;
            }

            [ProtoMember(1)]
            public ImmutableSortedDictionary<string, string> Dictionary { get; }
        }
    }
}
