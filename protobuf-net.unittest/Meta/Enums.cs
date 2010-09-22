using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf.Meta;
using System.ComponentModel;
using System.IO;

namespace ProtoBuf.unittest.Meta
{
    [TestFixture]
    public class Enums
    {
        public enum I8 : sbyte { A, B, C }
        public enum U8 : byte { A, B, C }
        public enum I16 : short { A, B, C }
        public enum U16 : ushort { A, B, C }
        public enum I32 : int { A, B, C }
        public enum U32 : uint { A, B, C }
        public enum I64 : long { A, B, C }
        public enum U64 : ulong { A, B, C }

        [ProtoContract]
        public class AllTheEnums {
            [ProtoMember(1)] public I8 I8 { get; set; }
            [ProtoMember(2)] public U8 U8 { get; set; }
            [ProtoMember(3), DefaultValue(I16.C)] public I16 I16 { get; set; }
            [ProtoMember(4), DefaultValue("C")] public U16 U16 { get; set; }
            [ProtoMember(5), DefaultValue(3)] public I32 I32 { get; set; }
            [ProtoMember(6)] public U32 U32 { get; set; }
            [ProtoMember(7)] public I64 I64 { get; set; }
            [ProtoMember(8)] public U64 U64 { get; set; }
        }
        static RuntimeTypeModel BuildModel(bool withPassThru) {
            var model = TypeModel.Create();
            if (withPassThru)
            {
                model.Add(typeof (I8), true).EnumPassthru = true;
                model.Add(typeof (U8), true).EnumPassthru = true;
                model.Add(typeof (I16), true).EnumPassthru = true;
                model.Add(typeof (U16), true).EnumPassthru = true;
                model.Add(typeof (I32), true).EnumPassthru = true;
                model.Add(typeof (U32), true).EnumPassthru = true;
                model.Add(typeof (I64), true).EnumPassthru = true;
                model.Add(typeof (U64), true).EnumPassthru = true;
            }
            model.Add(typeof(AllTheEnums), true);
            return model;
        }
        [Test]
        public void CanCompileEnumsAsPassthru()
        {
            var model = BuildModel(true);
            model.Compile("AllTheEnumsPassThru", "AllTheEnumsPassThru.dll");
            PEVerify.Verify("AllTheEnumsPassThru.dll");
        }
        [Test]
        public void CanCompileEnumsAsMapped()
        {
            var model = BuildModel(false);
            model.Compile("AllTheEnumsMapped", "AllTheEnumsMapped.dll");
            PEVerify.Verify("AllTheEnumsMapped.dll");
        }

        [Test]
        public void CanRoundTripAsPassthru()
        {
            var model = BuildModel(true);

            AllTheEnums ate = new AllTheEnums
            {
                 I8 = I8.B, U8 = U8.B,
                 I16 = I16.B, U16 = U16.B,
                 I32 = I32.B, U32 = U32.B,
                 I64 = I64.B, U64 = U64.B
            }, clone;

            clone = (AllTheEnums)model.DeepClone(ate);
            CompareAgainstClone(ate, clone, "Runtime");

            model.CompileInPlace();
            clone = (AllTheEnums)model.DeepClone(ate);
            CompareAgainstClone(ate, clone, "CompileInPlace");

            clone = (AllTheEnums)model.Compile().DeepClone(ate);
            CompareAgainstClone(ate, clone, "Compile");
        }
        [Test]
        public void CanRoundTripAsMapped()
        {
            var model = BuildModel(false);

            AllTheEnums ate = new AllTheEnums
            {
                I8 = I8.B,
                U8 = U8.B,
                I16 = I16.B,
                U16 = U16.B,
                I32 = I32.B,
                U32 = U32.B,
                I64 = I64.B,
                U64 = U64.B
            }, clone;

            clone = (AllTheEnums)model.DeepClone(ate);
            CompareAgainstClone(ate, clone, "Runtime");

            model.CompileInPlace();
            clone = (AllTheEnums)model.DeepClone(ate);
            CompareAgainstClone(ate, clone, "CompileInPlace");

            clone = (AllTheEnums)model.Compile().DeepClone(ate);
            CompareAgainstClone(ate, clone, "Compile");
        }
        static void CompareAgainstClone(AllTheEnums original, AllTheEnums clone, string caption)
        {
            Assert.IsNotNull(original, caption + " (original)");
            Assert.IsNotNull(clone, caption + " (clone)");
            Assert.AreNotSame(original, clone, caption);
            Assert.AreEqual(original.I8, clone.I8, caption);
            Assert.AreEqual(original.U8, clone.U8, caption);
            Assert.AreEqual(original.I16, clone.I16, caption);
            Assert.AreEqual(original.U16, clone.U16, caption);
            Assert.AreEqual(original.I32, clone.I32, caption);
            Assert.AreEqual(original.U32, clone.U32, caption);
            Assert.AreEqual(original.I64, clone.I64, caption);
            Assert.AreEqual(original.U64, clone.U64, caption);
        }

        [ProtoContract]
        public class MappedValuesA
        {
            [ProtoMember(1)]
            public EnumA Value { get; set; }
        }
        [ProtoContract]
        public class MappedValuesB
        {
            [ProtoMember(1)]
            public EnumB Value { get; set; }
        }
        public enum EnumA : short
        {
            [ProtoEnum(Value = 7)] X = 0,
            [ProtoEnum(Value = 8)] Y = 1,
            [ProtoEnum(Value = 9)] Z = 2,
        }
        public enum EnumB : long
        {
            [ProtoEnum(Value = 9)] X = 3,
            [ProtoEnum(Value = 10)] Y = 4,
            [ProtoEnum(Value = 11)] Z = 5,
        }
        RuntimeTypeModel CreateRemappingModel()
        {
            var model = TypeModel.Create();
            model.Add(typeof(EnumA), true);
            model.Add(typeof(EnumB), true);
            model.Add(typeof(MappedValuesA), true);
            model.Add(typeof(MappedValuesB), true);
            return model;
        }
        [Test]
        public void RemappingCanCompile()
        {
            var model = CreateRemappingModel();
            model.Compile("CreateRemappingModel", "CreateRemappingModel.dll");
            PEVerify.Verify("CreateRemappingModel.dll");
        }
        TTo ChangeType<TTo>(TypeModel model, object value)
        {
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, value);
                ms.Position = 0;
                return (TTo)model.Deserialize(ms, null, typeof (TTo));
            }
        }
        [Test]
        public void RemapValuesMakeSense()
        {
            var model = BuildModel(true);

            var orig = new MappedValuesA {Value = EnumA.Z};

            var clone = ChangeType<MappedValuesB>(model, orig);
            Assert.AreEqual(EnumB.X, clone.Value, "Runtime");

            model.CompileInPlace();
            clone = ChangeType<MappedValuesB>(model, orig);
            Assert.AreEqual(EnumB.X, clone.Value, "CompileInPlace");

            clone = ChangeType<MappedValuesB>(model.Compile(), orig);
            Assert.AreEqual(EnumB.X, clone.Value, "Compile");
        }
    }
}
