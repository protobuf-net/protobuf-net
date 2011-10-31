using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue243
    {
        [ProtoContract]
        public class NullableIntList
        {
            [ProtoMember(1)]
            public List<int?> List { get; set; }
        }
        [Test]
        public  void ExecuteNonNullTests()
        {
            var model = GetModel();
            RunTestNonNull(model, "Runtime");
            model.CompileInPlace();
            RunTestNonNull(model, "CompileInPlace");
            RunTestNonNull(model.Compile(), "Compile");
        }
        static RuntimeTypeModel GetModel()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(NullableIntList), true);
            return model;
        }
        [Test, ExpectedException(typeof(NullReferenceException))]
        public void ExecuteWithNullRuntime()
        {
            var model = GetModel();
            RunTestNull(model, "Runtime");
        }
        [Test, ExpectedException(typeof(NullReferenceException))]
        public void ExecuteWithNullCompileInPlace()
        {
            var model = GetModel();
            model.CompileInPlace();
            RunTestNull(model, "CompileInPlace");
        }
        [Test, ExpectedException(typeof(NullReferenceException))]
        public void ExecuteWithNullCompile()
        {
            var model = GetModel();
            RunTestNull(model.Compile(), "Compile");
        }
        [Test]
        public void CompilesCleanly()
        {
            var model = GetModel();
            model.Compile("Issue243", "Issue243.dll");
            PEVerify.AssertValid("Issue243.dll");
        }

        private void RunTestNonNull(TypeModel model, string caption)
        {
            NullableIntList l = new NullableIntList();
            l.List = new List<int?>(new int?[] {2, 3});
            NullableIntList clone = (NullableIntList) model.DeepClone(l);
            Assert.AreEqual("2,3", string.Join(",", clone.List), caption);
        }

        private void RunTestNull(TypeModel model, string caption)
        {
            NullableIntList l = new NullableIntList();
            l.List = new List<int?>(new int?[] { 2, null, 3 });
            NullableIntList clone = (NullableIntList)model.DeepClone(l);
            Assert.AreEqual("2,3", string.Join(",", clone.List), caption);
        }
    }
}
