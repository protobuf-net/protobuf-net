using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class Issue1012
    {
        public enum Scenario
        {
            Runtime,
            CompileInPlace,
            Compile,
        }

        [Theory]
        [InlineData(Scenario.Runtime)]
        [InlineData(Scenario.CompileInPlace)]
        [InlineData(Scenario.Compile)]
        public void TestInner(Scenario scenario)
        {
            var obj = GetModel(scenario, c => c.Add<Inner>()).DeepClone(new Inner { Id = 42 });
            Assert.Equal(42, obj.Id);
        }

        [Theory]
        [InlineData(Scenario.Runtime)]
        [InlineData(Scenario.CompileInPlace)]
        [InlineData(Scenario.Compile)]
        public void TestList(Scenario scenario)
        {
            var obj = GetModel(scenario, c => c.Add<HazList>()).DeepClone(new HazList { Items = { new Inner { Id = 42 } } });
            Assert.Equal(42, Assert.Single(obj.Items).Id);
        }

        [Theory]
        [InlineData(Scenario.Runtime)]
        [InlineData(Scenario.CompileInPlace)]
        [InlineData(Scenario.Compile)]
        public void TestDictionary(Scenario scenario)
        {
            var obj = GetModel(scenario, c => c.Add<HazDictionary>()).DeepClone(new HazDictionary { Items = { { 12, new Inner { Id = 42 } } } });
            var pair = Assert.Single(obj.Items);
            Assert.Equal(12, pair.Key);
            Assert.Equal(42, pair.Value.Id);
        }

        [Theory]
        [InlineData(Scenario.Runtime)]
        [InlineData(Scenario.CompileInPlace)]
        [InlineData(Scenario.Compile)]
        public void TestNestedInt32(Scenario scenario)
        {
            var innerDict = new Dictionary<int, int> { { 3, 5 } };
            var obj = GetModel(scenario, c => c.Add<NestedInt32>()).DeepClone(new NestedInt32 { Data = { { 12, innerDict } } });
            var pair = Assert.Single(obj.Data);
            Assert.Equal(12, pair.Key);
            var innerPair = Assert.Single(pair.Value);
            Assert.Equal(3, innerPair.Key);
            Assert.Equal(5, innerPair.Value);
        }

        [Theory]
        [InlineData(Scenario.Runtime)]
        [InlineData(Scenario.CompileInPlace)]
        [InlineData(Scenario.Compile)]
        public void TestNestedInner(Scenario scenario)
        {
            var innerDict = new Dictionary<int, Inner> { { 3, new Inner { Id = 42 } } };
            var obj = GetModel(scenario, c => c.Add<NestedInner>()).DeepClone(new NestedInner { Data = { { 12, innerDict } } });
            var pair = Assert.Single(obj.Data);
            Assert.Equal(12, pair.Key);
            var innerPair = Assert.Single(pair.Value);
            Assert.Equal(3, innerPair.Key);
            Assert.Equal(42, innerPair.Value.Id);
        }

        static TypeModel GetModel(Scenario scenario, Action<RuntimeTypeModel> configure)
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            configure(model);
            switch (scenario)
            {
                case Scenario.Runtime:
                    return model;
                case Scenario.CompileInPlace:
                    model.CompileInPlace();
                    return model;
                case Scenario.Compile:
                    return model.Compile();
                default:
                    throw new ArgumentOutOfRangeException(nameof(scenario));
            }
        }

        [ProtoContract]
        public class Inner
        {
            [ProtoMember(1)]
            public int Id { get; set; }
        }

        [ProtoContract]
        public class HazList
        {
            [ProtoMember(1)]
            public List<Inner> Items { get; } = new();
        }

        [ProtoContract]
        public class HazDictionary
        {
            [ProtoMember(1)]
            public Dictionary<int, Inner> Items { get; } = new();
        }

        [ProtoContract]
        public class NestedInt32 // from https://stackoverflow.com/questions/69354007/protobuf-net-no-serializer-for-type-is-available-for-model-when-using-compiled-t
        {
            [ProtoMember(1)]
            public Dictionary<int, Dictionary<int, int>> Data { get; } = new();
        }

        [ProtoContract]
        public class NestedInner 
        {
            [ProtoMember(1)]
            public Dictionary<int, Dictionary<int, Inner>> Data { get; } = new();
        }
    }
}
