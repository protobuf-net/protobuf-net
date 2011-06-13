using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using NUnit.Framework;
using ProtoBuf.Meta;
using System.IO;

namespace Examples.Issues
{
    public interface I { int N { get; } }

    public class O : I
    {
        public O(int n) { N = n; }
        public int N { get; private set; }
    }

    [ProtoContract]
    public class OS
    {
        public static implicit operator O(OS o)
        { return o == null ? null : new O(o.N); }
        public static implicit operator OS(O o)
        { return o == null ? null : new OS { N = o.N }; }
        [ProtoMember(1)]
        public int N { get; set; }
    }

    public class C
    {
        public static implicit operator CS(C o)
        { return o == null ? null : new CS { Unknown = o.Unknown }; }
        public static implicit operator C(CS o)
        { return o == null ? null : new C { Unknown = o.Unknown }; }
        public void PopulateRun() { Unknown = new O(43); }
        public I Unknown { get; private set; }
    }

    [ProtoContract]
    public class CS
    {
        [ProtoMember(1)]
        public I Unknown { get; set; }
    }
    [TestFixture]
    public class Issue185
    {
        [Test, ExpectedException(typeof(ArgumentException), ExpectedMessage = @"The supplied default implementation cannot be created: Examples.Issues.O
Parameter name: constructType")]
        public void ExecuteWithConstructType()
        {
            var m = RuntimeTypeModel.Create();
            m.AutoCompile = false;
            m.Add(typeof(C), false).SetSurrogate(typeof(CS));
            m.Add(typeof(O), false).SetSurrogate(typeof(OS));
            m.Add(typeof(I), false).ConstructType = typeof(O);

            var c = new C();
            c.PopulateRun();

            Test(m, c, "Runtime");
            m.CompileInPlace();
            Test(m, c, "CompileInPlace");
            Test(m.Compile(), c, "Compile");

        }
        static void Test(TypeModel model, C c, string caption)
        {
            Assert.AreEqual(43, c.Unknown.N, "braindead");
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, c);
                Assert.Greater(1, 0, "args fail");
                Assert.Greater(ms.Length, 0, "Nothing written");
                ms.Position = 0;
                var c2 = (C)model.Deserialize(ms, null, typeof(C));
                Assert.AreEqual(c.Unknown.N, c2.Unknown.N, caption);
            }
        }
        [Test]
        public void ExecuteWithSubType()
        {
            var m = RuntimeTypeModel.Create();
            m.AutoCompile = false;
            m.Add(typeof(C), false).SetSurrogate(typeof(CS));
            m.Add(typeof(O), false).SetSurrogate(typeof(OS));
            m.Add(typeof(I), false).AddSubType(1, typeof(O));

            var c = new C();
            c.PopulateRun();

            Test(m, c, "Runtime");
            m.CompileInPlace();
            Test(m, c, "CompileInPlace");
            Test(m.Compile(), c, "Compile");
        }
    }
}

