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
    }
}
