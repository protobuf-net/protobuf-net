using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class Issue536
    {
        [Fact]
        public void CanUseSurrogateForScalarPassThru()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.SetSurrogate<CustomerID, int>(ToInt32, ToCustomerID, DataFormat.ZigZag, CompatibilityLevel.Level300);

            Execute(model); // runtime-only
            model.CompileInPlace();
            Execute(model); // in-place compile
            Execute(model.Compile()); // in-proc compile
            Execute(PEVerify.CompileAndVerify(model, deleteOnSuccess: false)); // on-disk compile
            static void Execute(TypeModel model)
            {
                var ms = new MemoryStream();
                model.Serialize(ms, new I64_Message { CustomerID = new CustomerID(42), ID = 16 });
                ms.Position = 0;
                var clone = model.Deserialize<I64_Message>(ms);
                Assert.Equal(16, clone.ID);
                Assert.Equal(42, clone.CustomerID.Value);

                ms.Position = 0;
                var compat = model.Deserialize<I64_Message_Compat>(ms); // checks against expectation
                Assert.Equal(16, compat.ID);
                Assert.Equal(42, compat.CustomerID);
            }
        }

        [Fact]
        public void CanUseSurrogateForScalarPassThruViaOperators()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.SetSurrogate<CustomerIDWithOperators, int>(dataFormat: DataFormat.ZigZag);

            Execute(model); // runtime-only
            model.CompileInPlace();
            Execute(model); // in-place compile
            Execute(model.Compile()); // in-proc compile
            Execute(PEVerify.CompileAndVerify(model, deleteOnSuccess: false)); // on-disk compile

            static void Execute(TypeModel model)
            {
                var ms = new MemoryStream();
                model.Serialize(ms, new I64_Message_Operators { CustomerID = 42, ID = 16 });
                ms.Position = 0;
                var clone = model.Deserialize<I64_Message_Operators>(ms);
                Assert.Equal(16, clone.ID);
                Assert.Equal<int>(42, clone.CustomerID);

                ms.Position = 0;
                var compat = model.Deserialize<I64_Message_Compat>(ms); // checks against expectation
                Assert.Equal(16, compat.ID);
                Assert.Equal(42, compat.CustomerID);
            }
        }

        [Fact]
        public void CanUseSurrogateForScalarPassThruViaDecoratedOperators()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            Execute(model); // runtime-only
            model.CompileInPlace();
            Execute(model); // in-place compile
            Execute(model.Compile()); // in-proc compile
            Execute(PEVerify.CompileAndVerify(model, deleteOnSuccess: false)); // on-disk compile

            static void Execute(TypeModel model)
            {
                var ms = new MemoryStream();
                model.Serialize(ms, new I64_Message_DecoratedOperators { CustomerID = 42, ID = 16 });
                ms.Position = 0;
                var clone = model.Deserialize<I64_Message_DecoratedOperators>(ms);
                Assert.Equal(16, clone.ID);
                Assert.Equal<int>(42, clone.CustomerID);

                ms.Position = 0;
                var compat = model.Deserialize<I64_Message_Compat_Base128>(ms); // checks against expectation
                Assert.Equal(16, compat.ID);
                Assert.Equal(42, compat.CustomerID);
            }
        }

        public static int ToInt32(CustomerID value) => value.Value;
        public static CustomerID ToCustomerID(int value) => new CustomerID(value);

        [Fact]
        public void DetectInstanceLambda()
        {
            var model = RuntimeTypeModel.Create();
            var ex = Assert.Throws<ArgumentException>(() => model.SetSurrogate<CustomerID, int>(x => x.Value, x => new CustomerID(x)));
            Assert.StartsWith("A delegate to a static method was expected. The conversion 'ProtoBuf.Test.Issues.Issue536+<>c.<DetectInstanceLambda>b__5_1' is compiler-generated (possibly a lambda); an explicit static method should be used instead.", ex.Message);
        }

        [ProtoContract]
        public sealed class I64_Message
        {
            [ProtoMember(1)] public long ID;

            [ProtoMember(10)] public CustomerID CustomerID;
        }


        [ProtoContract]
        public sealed class I64_Message_Operators
        {
            [ProtoMember(1)] public long ID;

            [ProtoMember(10)] public CustomerIDWithOperators CustomerID;
        }


        [ProtoContract]
        public sealed class I64_Message_Compat
        {
            [ProtoMember(1)] public long ID;

            [ProtoMember(10, DataFormat = DataFormat.ZigZag)] public int CustomerID;
        }

        [ProtoContract]
        public sealed class I64_Message_Compat_Base128 // no zig-zag in this test
        {
            [ProtoMember(1)] public long ID;

            [ProtoMember(10)] public int CustomerID;
        }

        public readonly struct CustomerID
        {
            public int Value { get; }
            public CustomerID(int value) => Value = value;
        }

        public readonly struct CustomerIDWithOperators
        {
            public int Value { get; }
            public CustomerIDWithOperators(int value) => Value = value;

            public static implicit operator int(CustomerIDWithOperators value) => value.Value;
            public static implicit operator CustomerIDWithOperators(int value) => new CustomerIDWithOperators(value);
        }

        [ProtoContract(Surrogate = typeof(int))]
        public readonly struct DecoratedCustomerIDWithOperators
        {
            public int Value { get; }
            public DecoratedCustomerIDWithOperators(int value) => Value = value;

            public static implicit operator int(DecoratedCustomerIDWithOperators value) => value.Value;
            public static implicit operator DecoratedCustomerIDWithOperators(int value) => new DecoratedCustomerIDWithOperators(value);
        }

        [ProtoContract]
        public sealed class I64_Message_DecoratedOperators
        {
            [ProtoMember(1)] public long ID { get; set; }

            [ProtoMember(10)] public DecoratedCustomerIDWithOperators CustomerID { get; set; }
        }
    }
}
