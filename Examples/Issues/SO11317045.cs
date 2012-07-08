using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class SO11317045
    {
        [ProtoContract]
        [ProtoInclude(1, typeof(A), DataFormat = DataFormat.Group)]
        public class ABase
        {
        }

        [ProtoContract]
        public class A : ABase
        {
            [ProtoMember(1, DataFormat = DataFormat.Group)]
            public B B
            {
                get;
                set;
            }
        }

        [ProtoContract]
        public class B
        {
            [ProtoMember(1, DataFormat = DataFormat.Group)]
            public List<byte[]> Data
            {
                get;
                set;
            }
        }

        [Test]
        public void Execute()
        {
            var a = new A();
            var b = new B();
            a.B = b;
            
            b.Data = new List<byte[]>
            {
                Enumerable.Range(0, 1999).Select(v => (byte)v).ToArray(),
                Enumerable.Range(2000, 3999).Select(v => (byte)v).ToArray(),
            };

            var stream = new MemoryStream();
            var model = TypeModel.Create();
            model.AutoCompile = false;
#if DEBUG // this is only available in debug builds; if set, an exception is
          // thrown if the stream tries to buffer
            model.ForwardsOnly = true;
#endif
            CheckClone(model, a);
            model.CompileInPlace();
            CheckClone(model, a);
            CheckClone(model.Compile(), a);
        }
        void CheckClone(TypeModel model, A original)
        {
            int sum = original.B.Data.Sum(x => x.Sum(b => (int)b));
            var clone = (A)model.DeepClone(original);
            Assert.IsInstanceOfType(typeof(A), clone);
            Assert.IsInstanceOfType(typeof(B), clone.B);
            Assert.AreEqual(sum, clone.B.Data.Sum(x => x.Sum(b => (int)b)));
        }
    }
}
