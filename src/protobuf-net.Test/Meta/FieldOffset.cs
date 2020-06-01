using System;
using Xunit;

namespace ProtoBuf.Meta
{
    public class FieldOffset
    {
        [Fact]
        public void ZeroOffsetIsFineEvenWhenCompiled()
        {
            var model = RuntimeTypeModel.Create();
            var mt = model.Add(typeof(Foo), true);
            model.CompileInPlace();
            mt.ApplyFieldOffset(0); // fine because no-op

            var proto = model.GetSchema(typeof(Foo), ProtoSyntax.Proto3);
            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Meta;

message Bar {
   int32 X = 1;
   string Y = 2;
}
message Foo {
   int32 A = 5;
   string B = 6;
   oneof subtype {
      Bar Bar = 42;
   }
}
", proto, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void NonZeroOffsetFailsWhenCompiled()
        {
            var model = RuntimeTypeModel.Create();
            var mt = model.Add(typeof(Foo), true);
            model.CompileInPlace();
            Assert.Throws<InvalidOperationException>(() => mt.ApplyFieldOffset(1));
        }

        [Fact]
        public void NegativeOffsetFailsIfMakesInvalid()
        {
            var model = RuntimeTypeModel.Create();
            var mt = model.Add(typeof(Foo), true);
            Assert.Throws<ArgumentOutOfRangeException>(() => mt.ApplyFieldOffset(-10));
        }

        [Fact]
        public void NegativeOffsetIsFineWhenLeavesValid()
        {
            var model = RuntimeTypeModel.Create();
            var mt = model.Add(typeof(Foo), true);
            mt.ApplyFieldOffset(-4); // leaves everything +ve

            var proto = model.GetSchema(typeof(Foo), ProtoSyntax.Proto3);
            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Meta;

message Bar {
   int32 X = 1;
   string Y = 2;
}
message Foo {
   int32 A = 1;
   string B = 2;
   oneof subtype {
      Bar Bar = 38;
   }
}
", proto, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void PositiveOffsetIsFineWhenLeavesValid()
        {
            var model = RuntimeTypeModel.Create();
            var mt = model.Add(typeof(Foo), true);
            mt.ApplyFieldOffset(4);

            var proto = model.GetSchema(typeof(Foo), ProtoSyntax.Proto3);
            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Meta;

message Bar {
   int32 X = 1;
   string Y = 2;
}
message Foo {
   int32 A = 9;
   string B = 10;
   oneof subtype {
      Bar Bar = 46;
   }
}
", proto, ignoreLineEndingDifferences: true);
        }
    }

    [ProtoContract]
    [ProtoInclude(42, typeof(Bar))]
    public class Foo
    {
        [ProtoMember(5)]
        public int A { get; set; }
        [ProtoMember(6)]
        public string B { get; set; }
    }
    [ProtoContract]
    public class Bar : Foo
    {
        [ProtoMember(1)]
        public int X { get; set; }
        [ProtoMember(2)]
        public string Y { get; set; }
    }
}