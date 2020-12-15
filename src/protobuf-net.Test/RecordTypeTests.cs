using System;
using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Test
{
    public class RecordTypeTests
    {
        public partial record PositionalRecord(string FirstName, string LastName, int Count);

        [Fact]
        public void PositionalRecordTypeCtorResolve()
        {
            var ctor = MetaType.ResolveTupleConstructor(typeof(PositionalRecord), out var members);
            Assert.NotNull(ctor);
            Assert.Equal(3, members.Length);
            Assert.Equal(nameof(PositionalRecord.FirstName), members[0].Name);
            Assert.Equal(nameof(PositionalRecord.LastName), members[1].Name);
            Assert.Equal(nameof(PositionalRecord.Count), members[2].Name);
        }

        [Fact]
        public void CanRoundTripPositionalRecord()
        {
            var obj = new PositionalRecord("abc", "def", 123);
            var model = RuntimeTypeModel.Create();
            var clone = model.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.Equal("abc", clone.FirstName);
            Assert.Equal("def", clone.LastName);
            Assert.Equal(123, clone.Count);
        }
        
        [ProtoInclude(1, typeof(PositionalRecordWithInheritance))]
        public record BasePositionalRecordWithInheritance(string FirstName, string LastName);
        
        public record PositionalRecordWithInheritance(string FirstName, string LastName, int Count) : BasePositionalRecordWithInheritance(FirstName, LastName);

        [Fact]
        public void CanRoundTripPositionalRecordWithInheritance()
        {
            var obj = new PositionalRecordWithInheritance("abc", "def", 123);
            var model = RuntimeTypeModel.Create();
            var clone = model.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.Equal("abc", clone.FirstName);
            Assert.Equal("def", clone.LastName);
            Assert.Equal(123, clone.Count);
        }
        
        [ProtoContract]
        public record PositionalRecordWithAttributes(
            [property: ProtoMember(1)] string FirstName,
            [property: ProtoMember(2)] string LastName,
            [property: ProtoMember(3)] int Count
        );
        
        [Fact]
        public void CanRoundTripPositionalRecordWithAttributes()
        {
            var obj = new PositionalRecordWithAttributes("abc", "def", 123);
            var model = RuntimeTypeModel.Create();
            var clone = model.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.Equal("abc", clone.FirstName);
            Assert.Equal("def", clone.LastName);
            Assert.Equal(123, clone.Count);
        }
        
        [ProtoContract]
        [ProtoInclude(1, typeof(PositionalRecordWithAttributesWithInheritance))]
        public abstract record BasePositionalRecordWithAttributesWithInheritance(
            [property: ProtoMember(2)] string FirstName,
            [property: ProtoMember(3)] string LastName
        );
        
        [ProtoContract]
        public record PositionalRecordWithAttributesWithInheritance (
            string FirstName,
            string LastName,
            [property: ProtoMember(1)] int Count
        ) : BasePositionalRecordWithAttributesWithInheritance(FirstName, LastName);
        
        [Fact]
        public void CanRoundTripPositionalRecordWithAttributesWithInheritance()
        {
            var obj = new PositionalRecordWithAttributesWithInheritance("abc", "def", 123);
            var model = RuntimeTypeModel.Create();
            var clone = model.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.Equal("abc", clone.FirstName);
            Assert.Equal("def", clone.LastName);
            Assert.Equal(123, clone.Count);
        }
    }
}
