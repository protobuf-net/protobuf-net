using System;
using System.Data.SqlClient;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples
{
    [TestFixture]
    public class TypeTests
    {
        [ProtoContract]
        public class MyModel
        {
            [ProtoMember(1)]
            public Type Type { get; set; }
        }

        [Test]
        public void ShouldRoundtripTypeWithoutEvent()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            var orig = new MyModel {Type = typeof (SqlCommand)};

            var clone = (MyModel) model.DeepClone(orig);
            Assert.AreSame(typeof(SqlCommand), clone.Type);

            string s = typeof (SqlCommand).AssemblyQualifiedName;
            byte[] expected = new byte[Encoding.UTF8.GetByteCount(s) + 2];
            Encoding.UTF8.GetBytes(s, 0, s.Length, expected, 2);
            expected[0] = 0x0A; // field-header
            expected[1] = 0x70; // length
            Program.CheckBytes(orig, model, expected);

            model.CompileInPlace();
            clone = (MyModel)model.DeepClone(orig);
            Assert.AreSame(typeof(SqlCommand), clone.Type);
            Program.CheckBytes(orig, model, expected);

            var compiled = model.Compile();
            clone = (MyModel)compiled.DeepClone(orig);
            Assert.AreSame(typeof(SqlCommand), clone.Type);
            Program.CheckBytes(orig, compiled, expected);
        }

        [Test]
        public void ShouldRoundtripTypeWithEvent()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.DynamicTypeFormatting += new TypeFormatEventHandler(model_DynamicTypeFormatting);
            var orig = new MyModel { Type = typeof(SqlCommand) };

            var clone = (MyModel)model.DeepClone(orig);
            Assert.AreSame(typeof(SqlCommand), clone.Type);

            string s = "abc";
            byte[] expected = new byte[Encoding.UTF8.GetByteCount(s) + 2];
            Encoding.UTF8.GetBytes(s, 0, s.Length, expected, 2);
            expected[0] = 0x0A; // field-header
            expected[1] = 0x03; // length
            Program.CheckBytes(orig, model, expected);

            model.CompileInPlace();
            clone = (MyModel)model.DeepClone(orig);
            Assert.AreSame(typeof(SqlCommand), clone.Type);
            Program.CheckBytes(orig, model, expected);

            var compiled = model.Compile();
            compiled.DynamicTypeFormatting += new TypeFormatEventHandler(model_DynamicTypeFormatting);
            clone = (MyModel)compiled.DeepClone(orig);
            Assert.AreSame(typeof(SqlCommand), clone.Type);
            Program.CheckBytes(orig, compiled, expected);
            
        }

        void model_DynamicTypeFormatting(object sender, TypeFormatEventArgs args)
        {
            if(args.Type == typeof(SqlCommand))
            {
                args.FormattedName = "abc";
            } else if (args.FormattedName == "abc")
            {
                args.Type = typeof (SqlCommand);
            }
        }
    }
}
