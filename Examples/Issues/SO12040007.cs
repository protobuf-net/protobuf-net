using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Examples.Issues
{
    [TestFixture]
    public class SO12040007
    {
        [ProtoContract]
        public class ContractClass { }
        [ProtoContract]
        public struct ContractStruct { }

        public class NonContractClass { }

        public struct NonContractStruct { }

        [Test]
        public void BasicVersusContract()
        {
            var model = TypeModel.Create();
            Assert.IsTrue(model.CanSerialize(typeof(int)), "int Any");
            Assert.IsTrue(model.CanSerializeBasicType(typeof(int)), "int BasicType");
            Assert.IsFalse(model.CanSerializeContractType(typeof(int)), "int ContractType");

            Assert.IsTrue(model.CanSerialize(typeof(ContractClass)), "ContractClass Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(ContractClass)), "ContractClass BasicType");
            Assert.IsTrue(model.CanSerializeContractType(typeof(ContractClass)), "ContractClass ContractType");

            Assert.IsFalse(model.CanSerialize(typeof(NonContractClass)), "NonContractClass Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(NonContractClass)), "NonContractClass BasicType");
            Assert.IsFalse(model.CanSerializeContractType(typeof(NonContractClass)), "NonContractClass ContractType");

            Assert.IsTrue(model.CanSerialize(typeof(ContractStruct)), "ContractStruct Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(ContractStruct)), "ContractStruct BasicType");
            Assert.IsTrue(model.CanSerializeContractType(typeof(ContractStruct)), "ContractStruct ContractType");

            Assert.IsTrue(model.CanSerialize(typeof(ContractStruct?)), "ContractStruct? Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(ContractStruct?)), "ContractStruct? BasicType");
            Assert.IsTrue(model.CanSerializeContractType(typeof(ContractStruct?)), "ContractStruct? ContractType");

            Assert.IsFalse(model.CanSerialize(typeof(NonContractStruct)), "NonContractStruct Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(NonContractStruct)), "NonContractStruct BasicType");
            Assert.IsFalse(model.CanSerializeContractType(typeof(NonContractStruct)), "NonContractStruct ContractType");

            Assert.IsFalse(model.CanSerialize(typeof(NonContractStruct?)), "NonContractStruct? Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(NonContractStruct?)), "NonContractStruct? BasicType");
            Assert.IsFalse(model.CanSerializeContractType(typeof(NonContractStruct?)), "NonContractStruct? ContractType");
        }

        [Test]
        public void BasicVersusContractArrays()
        {
            var model = TypeModel.Create();
            Assert.IsTrue(model.CanSerialize(typeof(int[])), "int Any");
            Assert.IsTrue(model.CanSerializeBasicType(typeof(int[])), "int BasicType");
            Assert.IsFalse(model.CanSerializeContractType(typeof(int[])), "int ContractType");

            Assert.IsTrue(model.CanSerialize(typeof(ContractClass[])), "ContractClass Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(ContractClass[])), "ContractClass BasicType");
            Assert.IsTrue(model.CanSerializeContractType(typeof(ContractClass[])), "ContractClass ContractType");

            Assert.IsFalse(model.CanSerialize(typeof(NonContractClass[])), "NonContractClass Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(NonContractClass[])), "NonContractClass BasicType");
            Assert.IsFalse(model.CanSerializeContractType(typeof(NonContractClass[])), "NonContractClass ContractType");

            Assert.IsTrue(model.CanSerialize(typeof(ContractStruct[])), "ContractStruct Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(ContractStruct[])), "ContractStruct BasicType");
            Assert.IsTrue(model.CanSerializeContractType(typeof(ContractStruct[])), "ContractStruct ContractType");

            Assert.IsTrue(model.CanSerialize(typeof(ContractStruct?[])), "ContractStruct? Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(ContractStruct?[])), "ContractStruct? BasicType");
            Assert.IsTrue(model.CanSerializeContractType(typeof(ContractStruct?[])), "ContractStruct? ContractType");

            Assert.IsFalse(model.CanSerialize(typeof(NonContractStruct[])), "NonContractStruct Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(NonContractStruct[])), "NonContractStruct BasicType");
            Assert.IsFalse(model.CanSerializeContractType(typeof(NonContractStruct[])), "NonContractStruct ContractType");

            Assert.IsFalse(model.CanSerialize(typeof(NonContractStruct?[])), "NonContractStruct? Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(NonContractStruct?[])), "NonContractStruct? BasicType");
            Assert.IsFalse(model.CanSerializeContractType(typeof(NonContractStruct?[])), "NonContractStruct? ContractType");
        }

        [Test]
        public void BasicVersusContractLists()
        {
            var model = TypeModel.Create();
            Assert.IsTrue(model.CanSerialize(typeof(List<int>)), "int Any");
            Assert.IsTrue(model.CanSerializeBasicType(typeof(List<int>)), "int BasicType");
            Assert.IsFalse(model.CanSerializeContractType(typeof(List<int>)), "int ContractType");

            Assert.IsTrue(model.CanSerialize(typeof(List<ContractClass>)), "ContractClass Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(List<ContractClass>)), "ContractClass BasicType");
            Assert.IsTrue(model.CanSerializeContractType(typeof(List<ContractClass>)), "ContractClass ContractType");

            Assert.IsFalse(model.CanSerialize(typeof(List<NonContractClass>)), "NonContractClass Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(List<NonContractClass>)), "NonContractClass BasicType");
            Assert.IsFalse(model.CanSerializeContractType(typeof(List<NonContractClass>)), "NonContractClass ContractType");

            Assert.IsTrue(model.CanSerialize(typeof(List<ContractStruct>)), "ContractStruct Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(List<ContractStruct>)), "ContractStruct BasicType");
            Assert.IsTrue(model.CanSerializeContractType(typeof(List<ContractStruct>)), "ContractStruct ContractType");

            Assert.IsTrue(model.CanSerialize(typeof(List<ContractStruct?>)), "ContractStruct? Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(List<ContractStruct?>)), "ContractStruct? BasicType");
            Assert.IsTrue(model.CanSerializeContractType(typeof(List<ContractStruct?>)), "ContractStruct? ContractType");

            Assert.IsFalse(model.CanSerialize(typeof(List<NonContractStruct>)), "NonContractStruct Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(List<NonContractStruct>)), "NonContractStruct BasicType");
            Assert.IsFalse(model.CanSerializeContractType(typeof(List<NonContractStruct>)), "NonContractStruct ContractType");

            Assert.IsFalse(model.CanSerialize(typeof(List<NonContractStruct?>)), "NonContractStruct? Any");
            Assert.IsFalse(model.CanSerializeBasicType(typeof(List<NonContractStruct?>)), "NonContractStruct? BasicType");
            Assert.IsFalse(model.CanSerializeContractType(typeof(List<NonContractStruct?>)), "NonContractStruct? ContractType");
        }
        [Test]
        public void TestPrimitiveCanSerialize()
        {
            var model = TypeModel.Create();
            Assert.IsTrue(model.CanSerialize(typeof(int)));
            Assert.IsTrue(model.CanSerialize(typeof(int?)));
            Assert.IsTrue(model.CanSerialize(typeof(short)));
            Assert.IsTrue(model.CanSerialize(typeof(short?)));
            Assert.IsTrue(model.CanSerialize(typeof(byte[])));
            Assert.IsTrue(model.CanSerialize(typeof(string)));
            Assert.IsTrue(model.CanSerialize(typeof(DateTime)));
            Assert.IsTrue(model.CanSerialize(typeof(DateTime?)));

            Assert.IsFalse(model.CanSerialize(typeof(System.Windows.Media.Color)));
            Assert.IsFalse(model.CanSerialize(typeof(DateTimeOffset)));
            Assert.IsFalse(model.CanSerialize(typeof(Action)));
        }

        [Test]
        public void TestPrimitiveArraysCanSerialize()
        {
            var model = TypeModel.Create();
            Assert.IsTrue(model.CanSerialize(typeof(int[])), "int");
            Assert.IsTrue(model.CanSerialize(typeof(int?[])), "int?");
            Assert.IsTrue(model.CanSerialize(typeof(short[])), "short");
            Assert.IsTrue(model.CanSerialize(typeof(short?[])), "short?");
            Assert.IsFalse(model.CanSerialize(typeof(byte[][])), "byte[]");
            Assert.IsTrue(model.CanSerialize(typeof(string[])), "string");
            Assert.IsTrue(model.CanSerialize(typeof(DateTime[])), "DateTime");
            Assert.IsTrue(model.CanSerialize(typeof(DateTime?[])), "DateTime?");

            Assert.IsFalse(model.CanSerialize(typeof(System.Windows.Media.Color[])), "Color");
            Assert.IsFalse(model.CanSerialize(typeof(DateTimeOffset[])), "DateTimeOffset");
            Assert.IsFalse(model.CanSerialize(typeof(Action[])), "Action");
        }
        [Test]
        public void TestPrimitiveNestedArraysCannotSerialize()
        {
            var model = TypeModel.Create();
            Assert.IsFalse(model.CanSerialize(typeof(int[][])));
            Assert.IsFalse(model.CanSerialize(typeof(int?[][])));
            Assert.IsFalse(model.CanSerialize(typeof(short[][])));
            Assert.IsFalse(model.CanSerialize(typeof(short?[][])));
            Assert.IsFalse(model.CanSerialize(typeof(byte[][][])));
            Assert.IsFalse(model.CanSerialize(typeof(string[][])));
            Assert.IsFalse(model.CanSerialize(typeof(DateTime[][])));
            Assert.IsFalse(model.CanSerialize(typeof(DateTime?[][])));

            Assert.IsFalse(model.CanSerialize(typeof(System.Windows.Media.Color[][])));
            Assert.IsFalse(model.CanSerialize(typeof(DateTimeOffset[][])));
            Assert.IsFalse(model.CanSerialize(typeof(Action[][])));
        }
        [Test]
        public void TestPrimitiveMultidimArraysCannotSerialize()
        {
            var model = TypeModel.Create();
            Assert.IsFalse(model.CanSerialize(typeof(int[,])));
            Assert.IsFalse(model.CanSerialize(typeof(int?[,])));
            Assert.IsFalse(model.CanSerialize(typeof(short[,])));
            Assert.IsFalse(model.CanSerialize(typeof(short?[,])));
            Assert.IsFalse(model.CanSerialize(typeof(byte[,])));
            Assert.IsFalse(model.CanSerialize(typeof(string[,])));
            Assert.IsFalse(model.CanSerialize(typeof(DateTime[,])));
            Assert.IsFalse(model.CanSerialize(typeof(DateTime?[,])));

            Assert.IsFalse(model.CanSerialize(typeof(System.Windows.Media.Color[,])));
            Assert.IsFalse(model.CanSerialize(typeof(DateTimeOffset[,])));
            Assert.IsFalse(model.CanSerialize(typeof(Action[,])));
        }


    }
}
