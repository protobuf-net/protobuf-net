using System;
using System.Collections.Generic;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System.Linq;
namespace Examples.Issues
{
    
    public class Issue243
    {
        [ProtoContract]
        public class NullableSequences
        {
            [ProtoMember(1)]
            public List<int?> Int32List { get; set; }

            [ProtoMember(2)]
            public List<string> StringList { get; set; }

            [ProtoMember(3)]
            public int?[] Int32Array { get; set; }

            [ProtoMember(4)]
            public string[] StringArray { get; set; }
        }
        [Fact]
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
            model.Add(typeof(NullableSequences), true);
            return model;
        }
        [Fact]
        public void ExecuteWithNullRuntime()
        {
            Program.ExpectFailure<NullReferenceException>(() =>
            {
                var model = GetModel();
                RunTestNull(model, "Runtime");
            });
        }
        [Fact]
        public void ExecuteWithNullCompileInPlace()
        {
            Program.ExpectFailure<NullReferenceException>(() =>
            {
                var model = GetModel();
                model.CompileInPlace();
                RunTestNull(model, "CompileInPlace");
            });
        }
        [Fact]
        public void ExecuteWithNullCompile()
        {
            Program.ExpectFailure<NullReferenceException>(() =>
            {
                var model = GetModel();
                RunTestNull(model.Compile(), "Compile");
            });
        }
        [Fact]
        public void CompilesCleanly()
        {
            var model = GetModel();
            model.Compile("Issue243_a", "Issue243_a.dll");
            PEVerify.AssertValid("Issue243_a.dll");
        }

        private void RunTestNonNull(TypeModel model, string caption)
        {
            NullableSequences l = new NullableSequences();
            l.Int32List = new List<int?>(new int?[] { 2, 3 });
            l.StringList = new List<string> {"a", "b", ""};
            l.Int32Array = new int?[] {4, 5};
            l.StringArray = new string[] { "c", "", "d" };
            NullableSequences clone = (NullableSequences) model.DeepClone(l);
            Assert.Equal("2,3", string.Join(",", clone.Int32List)); //, caption);
            Assert.True(clone.StringList.SequenceEqual(new[] { "a", "b", "" }));
            Assert.Equal("4,5", string.Join(",", clone.Int32Array)); //, caption);
            Assert.True(clone.StringArray.SequenceEqual(new[] { "c", "", "d" }));
        }

        private void RunTestNull(TypeModel model, string caption)
        {
            NullableSequences l = new NullableSequences();
            l.Int32List = new List<int?>(new int?[] { 2, null, 3 });
            l.StringList = new List<string> { "a", null, "b", "" };
            l.Int32Array = new int?[] { 4, null, 5, null };
            l.StringArray = new string[] { "c", null, "", "d", null };
            NullableSequences clone = (NullableSequences)model.DeepClone(l);
            Assert.Equal("2,,3", string.Join(",", clone.Int32List)); //, caption);
            Assert.True(clone.StringList.SequenceEqual(new[] { "a", null, "b", "" }));
            Assert.Equal("4,,5,", string.Join(",", clone.Int32Array)); //, caption);
            Assert.True(clone.StringArray.SequenceEqual(new[] { "c", null, "", "d", null }));
        }

        static RuntimeTypeModel GetModelWithSupportForNulls()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(NullableSequences), true);
            var metaType = model[typeof(NullableSequences)];
#pragma warning disable CS0618
            metaType[1].SupportNull = true;
            metaType[2].SupportNull = true;
            metaType[3].SupportNull = true;
            metaType[4].SupportNull = true;
#pragma warning restore CS0618
            return model;
        }
        [Fact]
        public void TestWithSupportForNullsCompilesCleanly()
        {
            var model = GetModelWithSupportForNulls();
            model.Compile("Issue243_b", "Issue243_b.dll");
            PEVerify.AssertValid("Issue243_b.dll");
        }
        [Fact]
        public void TestWithSupportForNulls()
        {
            var model = GetModelWithSupportForNulls();
            RunTestNull(model, "Runtime");

            model.CompileInPlace();
            RunTestNull(model, "CompileInPlace");

            RunTestNull(model.Compile(), "Compile");
        }
    }
}
