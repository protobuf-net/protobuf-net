using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
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

        [Fact]
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

            using var stream = new MemoryStream();
            var model = RuntimeTypeModel.Create();
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
            Assert.IsType<A>(clone);
            Assert.IsType<B>(clone.B);
            Assert.Equal(sum, clone.B.Data.Sum(x => x.Sum(b => (int)b)));
        }


        [Fact]
        public void TestProtoIncludeWithStringKnownTypeName()
        {
            NamedProtoInclude.Foo foo = new NamedProtoInclude.Bar();
            var clone = Serializer.DeepClone(foo);

            Assert.IsType<NamedProtoInclude.Bar>(clone);
        }

    }
}

namespace Examples.Issues.NamedProtoInclude
{
    [ProtoContract]
    [ProtoInclude(1, "Examples.Issues.NamedProtoInclude.Bar")]
    internal class Foo
    {

    }

    [ProtoContract]
    internal class Bar : Foo
    {

    }
}