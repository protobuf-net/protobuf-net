using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf.Meta;
using System.ComponentModel;

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
        static RuntimeTypeModel BuildModel() {
            var model = TypeModel.Create();
            model.Add(typeof(AllTheEnums), true);
            return model;
        }
        [Test]
        public void CanCompileEnums()
        {
            var model = BuildModel();
            model.Compile("AllTheEnums","AllTheEnums.dll");
            PocoListTests.VerifyPE("AllTheEnums.dll");
        }

        [Test]
        public void CanRoundTrip()
        {
            var model = BuildModel();

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
        static void CompareAgainstClone(AllTheEnums original, AllTheEnums clone, string caption)
        {
            Assert.AreEqual(original.I8, clone.I8, caption);
            Assert.AreEqual(original.U8, clone.U8, caption);
            Assert.AreEqual(original.I16, clone.I16, caption);
            Assert.AreEqual(original.U16, clone.U16, caption);
            Assert.AreEqual(original.I32, clone.I32, caption);
            Assert.AreEqual(original.U32, clone.U32, caption);
            Assert.AreEqual(original.I64, clone.I64, caption);
            Assert.AreEqual(original.U64, clone.U64, caption);
        }
    }
}
