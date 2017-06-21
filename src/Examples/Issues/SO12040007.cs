using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Examples.Issues
{
    
    public class SO12040007
    {
        [ProtoContract]
        public class ContractClass { }
        [ProtoContract]
        public struct ContractStruct { }

        public class NonContractClass { }

        public struct NonContractStruct { }

        [Fact]
        public void BasicVersusContract()
        {
            var model = TypeModel.Create();
            Assert.True(model.CanSerialize(typeof(int)), "int Any");
            Assert.True(model.CanSerializeBasicType(typeof(int)), "int BasicType");
            Assert.False(model.CanSerializeContractType(typeof(int)), "int ContractType");

            Assert.True(model.CanSerialize(typeof(ContractClass)), "ContractClass Any");
            Assert.False(model.CanSerializeBasicType(typeof(ContractClass)), "ContractClass BasicType");
            Assert.True(model.CanSerializeContractType(typeof(ContractClass)), "ContractClass ContractType");

            Assert.False(model.CanSerialize(typeof(NonContractClass)), "NonContractClass Any");
            Assert.False(model.CanSerializeBasicType(typeof(NonContractClass)), "NonContractClass BasicType");
            Assert.False(model.CanSerializeContractType(typeof(NonContractClass)), "NonContractClass ContractType");

            Assert.True(model.CanSerialize(typeof(ContractStruct)), "ContractStruct Any");
            Assert.False(model.CanSerializeBasicType(typeof(ContractStruct)), "ContractStruct BasicType");
            Assert.True(model.CanSerializeContractType(typeof(ContractStruct)), "ContractStruct ContractType");

            Assert.True(model.CanSerialize(typeof(ContractStruct?)), "ContractStruct? Any");
            Assert.False(model.CanSerializeBasicType(typeof(ContractStruct?)), "ContractStruct? BasicType");
            Assert.True(model.CanSerializeContractType(typeof(ContractStruct?)), "ContractStruct? ContractType");

            Assert.False(model.CanSerialize(typeof(NonContractStruct)), "NonContractStruct Any");
            Assert.False(model.CanSerializeBasicType(typeof(NonContractStruct)), "NonContractStruct BasicType");
            Assert.False(model.CanSerializeContractType(typeof(NonContractStruct)), "NonContractStruct ContractType");

            Assert.False(model.CanSerialize(typeof(NonContractStruct?)), "NonContractStruct? Any");
            Assert.False(model.CanSerializeBasicType(typeof(NonContractStruct?)), "NonContractStruct? BasicType");
            Assert.False(model.CanSerializeContractType(typeof(NonContractStruct?)), "NonContractStruct? ContractType");
        }

        [Fact]
        public void BasicVersusContractArrays()
        {
            var model = TypeModel.Create();
            Assert.True(model.CanSerialize(typeof(int[])), "int Any");
            Assert.True(model.CanSerializeBasicType(typeof(int[])), "int BasicType");
            Assert.False(model.CanSerializeContractType(typeof(int[])), "int ContractType");

            Assert.True(model.CanSerialize(typeof(ContractClass[])), "ContractClass Any");
            Assert.False(model.CanSerializeBasicType(typeof(ContractClass[])), "ContractClass BasicType");
            Assert.True(model.CanSerializeContractType(typeof(ContractClass[])), "ContractClass ContractType");

            Assert.False(model.CanSerialize(typeof(NonContractClass[])), "NonContractClass Any");
            Assert.False(model.CanSerializeBasicType(typeof(NonContractClass[])), "NonContractClass BasicType");
            Assert.False(model.CanSerializeContractType(typeof(NonContractClass[])), "NonContractClass ContractType");

            Assert.True(model.CanSerialize(typeof(ContractStruct[])), "ContractStruct Any");
            Assert.False(model.CanSerializeBasicType(typeof(ContractStruct[])), "ContractStruct BasicType");
            Assert.True(model.CanSerializeContractType(typeof(ContractStruct[])), "ContractStruct ContractType");

            Assert.True(model.CanSerialize(typeof(ContractStruct?[])), "ContractStruct? Any");
            Assert.False(model.CanSerializeBasicType(typeof(ContractStruct?[])), "ContractStruct? BasicType");
            Assert.True(model.CanSerializeContractType(typeof(ContractStruct?[])), "ContractStruct? ContractType");

            Assert.False(model.CanSerialize(typeof(NonContractStruct[])), "NonContractStruct Any");
            Assert.False(model.CanSerializeBasicType(typeof(NonContractStruct[])), "NonContractStruct BasicType");
            Assert.False(model.CanSerializeContractType(typeof(NonContractStruct[])), "NonContractStruct ContractType");

            Assert.False(model.CanSerialize(typeof(NonContractStruct?[])), "NonContractStruct? Any");
            Assert.False(model.CanSerializeBasicType(typeof(NonContractStruct?[])), "NonContractStruct? BasicType");
            Assert.False(model.CanSerializeContractType(typeof(NonContractStruct?[])), "NonContractStruct? ContractType");
        }

        [Fact]
        public void BasicVersusContractLists()
        {
            var model = TypeModel.Create();
            Assert.True(model.CanSerialize(typeof(List<int>)), "int Any");
            Assert.True(model.CanSerializeBasicType(typeof(List<int>)), "int BasicType");
            Assert.False(model.CanSerializeContractType(typeof(List<int>)), "int ContractType");

            Assert.True(model.CanSerialize(typeof(List<ContractClass>)), "ContractClass Any");
            Assert.False(model.CanSerializeBasicType(typeof(List<ContractClass>)), "ContractClass BasicType");
            Assert.True(model.CanSerializeContractType(typeof(List<ContractClass>)), "ContractClass ContractType");

            Assert.False(model.CanSerialize(typeof(List<NonContractClass>)), "NonContractClass Any");
            Assert.False(model.CanSerializeBasicType(typeof(List<NonContractClass>)), "NonContractClass BasicType");
            Assert.False(model.CanSerializeContractType(typeof(List<NonContractClass>)), "NonContractClass ContractType");

            Assert.True(model.CanSerialize(typeof(List<ContractStruct>)), "ContractStruct Any");
            Assert.False(model.CanSerializeBasicType(typeof(List<ContractStruct>)), "ContractStruct BasicType");
            Assert.True(model.CanSerializeContractType(typeof(List<ContractStruct>)), "ContractStruct ContractType");

            Assert.True(model.CanSerialize(typeof(List<ContractStruct?>)), "ContractStruct? Any");
            Assert.False(model.CanSerializeBasicType(typeof(List<ContractStruct?>)), "ContractStruct? BasicType");
            Assert.True(model.CanSerializeContractType(typeof(List<ContractStruct?>)), "ContractStruct? ContractType");

            Assert.False(model.CanSerialize(typeof(List<NonContractStruct>)), "NonContractStruct Any");
            Assert.False(model.CanSerializeBasicType(typeof(List<NonContractStruct>)), "NonContractStruct BasicType");
            Assert.False(model.CanSerializeContractType(typeof(List<NonContractStruct>)), "NonContractStruct ContractType");

            Assert.False(model.CanSerialize(typeof(List<NonContractStruct?>)), "NonContractStruct? Any");
            Assert.False(model.CanSerializeBasicType(typeof(List<NonContractStruct?>)), "NonContractStruct? BasicType");
            Assert.False(model.CanSerializeContractType(typeof(List<NonContractStruct?>)), "NonContractStruct? ContractType");
        }
        [Fact]
        public void TestPrimitiveCanSerialize()
        {
            var model = TypeModel.Create();
            Assert.True(model.CanSerialize(typeof(int)));
            Assert.True(model.CanSerialize(typeof(int?)));
            Assert.True(model.CanSerialize(typeof(short)));
            Assert.True(model.CanSerialize(typeof(short?)));
            Assert.True(model.CanSerialize(typeof(byte[])));
            Assert.True(model.CanSerialize(typeof(string)));
            Assert.True(model.CanSerialize(typeof(DateTime)));
            Assert.True(model.CanSerialize(typeof(DateTime?)));
#if !COREFX
            Assert.False(model.CanSerialize(typeof(System.Windows.Media.Color)));
#endif
            Assert.False(model.CanSerialize(typeof(DateTimeOffset)));
            Assert.False(model.CanSerialize(typeof(Action)));
        }

        [Fact]
        public void TestPrimitiveArraysCanSerialize()
        {
            var model = TypeModel.Create();
            Assert.True(model.CanSerialize(typeof(int[])), "int");
            Assert.True(model.CanSerialize(typeof(int?[])), "int?");
            Assert.True(model.CanSerialize(typeof(short[])), "short");
            Assert.True(model.CanSerialize(typeof(short?[])), "short?");
            Assert.True(model.CanSerialize(typeof(byte[][])), "byte[]");
            Assert.True(model.CanSerialize(typeof(string[])), "string");
            Assert.True(model.CanSerialize(typeof(DateTime[])), "DateTime");
            Assert.True(model.CanSerialize(typeof(DateTime?[])), "DateTime?");
#if !COREFX
            Assert.False(model.CanSerialize(typeof(System.Windows.Media.Color[])), "Color");
#endif
            Assert.False(model.CanSerialize(typeof(DateTimeOffset[])), "DateTimeOffset");
            Assert.False(model.CanSerialize(typeof(Action[])), "Action");
        }
        [Fact]
        public void TestPrimitiveNestedArraysCannotSerialize()
        {
            var model = TypeModel.Create();
            Assert.False(model.CanSerialize(typeof(int[][])));
            Assert.False(model.CanSerialize(typeof(int?[][])));
            Assert.False(model.CanSerialize(typeof(short[][])));
            Assert.False(model.CanSerialize(typeof(short?[][])));
            Assert.False(model.CanSerialize(typeof(byte[][][])));
            Assert.False(model.CanSerialize(typeof(string[][])));
            Assert.False(model.CanSerialize(typeof(DateTime[][])));
            Assert.False(model.CanSerialize(typeof(DateTime?[][])));
#if !COREFX
            Assert.False(model.CanSerialize(typeof(System.Windows.Media.Color[][])));
#endif
            Assert.False(model.CanSerialize(typeof(DateTimeOffset[][])));
            Assert.False(model.CanSerialize(typeof(Action[][])));
        }
        [Fact]
        public void TestPrimitiveMultidimArraysCannotSerialize()
        {
            var model = TypeModel.Create();
            Assert.False(model.CanSerialize(typeof(int[,])));
            Assert.False(model.CanSerialize(typeof(int?[,])));
            Assert.False(model.CanSerialize(typeof(short[,])));
            Assert.False(model.CanSerialize(typeof(short?[,])));
            Assert.False(model.CanSerialize(typeof(byte[,])));
            Assert.False(model.CanSerialize(typeof(string[,])));
            Assert.False(model.CanSerialize(typeof(DateTime[,])));
            Assert.False(model.CanSerialize(typeof(DateTime?[,])));
#if !COREFX
            Assert.False(model.CanSerialize(typeof(System.Windows.Media.Color[,])));
#endif
            Assert.False(model.CanSerialize(typeof(DateTimeOffset[,])));
            Assert.False(model.CanSerialize(typeof(Action[,])));
        }


    }
}
