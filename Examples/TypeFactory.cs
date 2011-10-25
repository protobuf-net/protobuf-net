using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System.Diagnostics;
using System.IO;

namespace Examples
{
    [TestFixture]
    public class TypeFactory
    {
        [Test]
        public void TestInternal()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof (CanHazFactory), true).SetFactory("MagicMaker");

            Check(model, null, 42, "Runtime");
            model.CompileInPlace();
            Check(model, null, 42, "CompileInPlace");
            Check(model.Compile(), null, 42, "Compile");
        }
        [Test]
        public void TestExternal()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(CanHazFactory), true).SetFactory(typeof(TypeFactory).GetMethod("ExternalFactory"));
            var ctx = new SerializationContext {Context = 12345};
            Check(model, ctx, 12345, "Runtime");
            model.CompileInPlace();
            Check(model, ctx, 12345, "CompileInPlace");
            Check(model.Compile(), ctx, 12345, "Compile");
        }
        private void Check(TypeModel model, SerializationContext ctx, int magicNumber, string caption)
        {
            try
            {
                CanHazFactory orig = new CanHazFactory {Foo = 123, Bar = 456}, clone;
                using(var ms = new MemoryStream())
                {
                    model.Serialize(ms, orig, ctx);
                    ms.Position = 0;
                    clone = (CanHazFactory) model.Deserialize(ms, null, typeof(CanHazFactory), ctx);
                }

                Assert.AreNotSame(orig, clone);

                Assert.AreEqual(123, orig.Foo, caption);
                Assert.AreEqual(456, orig.Bar, caption);
                Assert.AreEqual(0, orig.MagicNumber, caption);

                Assert.AreEqual(123, clone.Foo, caption);
                Assert.AreEqual(456, clone.Bar, caption);
                Assert.AreEqual(magicNumber, clone.MagicNumber, caption);

            } catch
            {
                Debug.WriteLine(caption);
                throw;
            }

        }

        public static CanHazFactory ExternalFactory(SerializationContext ctx)
        {
            return new CanHazFactory { MagicNumber = (int)ctx.Context };
        }
        [ProtoContract]
        public class CanHazFactory
        {
            public static CanHazFactory MagicMaker()
            {
                return new CanHazFactory {MagicNumber = 42};
            }

            public int MagicNumber { get; set; }

            [ProtoMember(1)]
            public int Foo { get; set; }

            [ProtoMember(2)]
            public int Bar { get; set; }
        }
    }
    
}
