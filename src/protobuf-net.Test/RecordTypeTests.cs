using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Test
{
    public class RecordTypeTests
    {
        public partial record PositionalRecord0();
        public partial record PositionalRecord1(string FirstName);
        public partial record PositionalRecord2(string FirstName, string LastName);
        public partial record PositionalRecord3(string FirstName, string LastName, int Count);

        [Fact]
        public void PositionalRecordTypeCtorResolve0()
        {
            var ctor = MetaType.ResolveTupleConstructor(typeof(PositionalRecord0), out var members);
            Assert.NotNull(ctor);
            Assert.Empty(members);
        }

        [Fact]
        public void CanRoundTripPositionalRecord0()
        {
            var obj = new PositionalRecord0();
            var model = RuntimeTypeModel.Create();
            var clone = model.DeepClone(obj);
            Assert.NotSame(obj, clone);
        }

        [Fact]
        public void PositionalRecordTypeCtorResolve1()
        {
            var ctor = MetaType.ResolveTupleConstructor(typeof(PositionalRecord1), out var members);
            Assert.NotNull(ctor);
            Assert.Single(members);
            Assert.Equal(nameof(PositionalRecord1.FirstName), members[0].Name);
        }

        [Fact]
        public void CanRoundTripPositionalRecord1()
        {
            var obj = new PositionalRecord1("abc");
            var model = RuntimeTypeModel.Create();
            var clone = model.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.Equal("abc", clone.FirstName);
        }

        [Fact]
        public void PositionalRecordTypeCtorResolve2()
        {
            var ctor = MetaType.ResolveTupleConstructor(typeof(PositionalRecord2), out var members);
            Assert.NotNull(ctor);
            Assert.Equal(2, members.Length);
            Assert.Equal(nameof(PositionalRecord2.FirstName), members[0].Name);
            Assert.Equal(nameof(PositionalRecord2.LastName), members[1].Name);
        }

        [Fact]
        public void CanRoundTripPositionalRecord2()
        {
            var obj = new PositionalRecord2("abc", "def");
            var model = RuntimeTypeModel.Create();
            var clone = model.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.Equal("abc", clone.FirstName);
            Assert.Equal("def", clone.LastName);
        }

        [Fact]
        public void PositionalRecordTypeCtorResolve3()
        {
            var ctor = MetaType.ResolveTupleConstructor(typeof(PositionalRecord3), out var members);
            Assert.NotNull(ctor);
            Assert.Equal(3, members.Length);
            Assert.Equal(nameof(PositionalRecord3.FirstName), members[0].Name);
            Assert.Equal(nameof(PositionalRecord3.LastName), members[1].Name);
            Assert.Equal(nameof(PositionalRecord3.Count), members[2].Name);
        }

        [Fact]
        public void CanRoundTripPositionalRecord3()
        {
            var obj = new PositionalRecord3("abc", "def", 123);
            var model = RuntimeTypeModel.Create();
            var clone = model.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.Equal("abc", clone.FirstName);
            Assert.Equal("def", clone.LastName);
            Assert.Equal(123, clone.Count);
        }
    }
}
