using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System;

namespace Examples.Issues
{
    
    public class SO17245073
    {
        [Fact]
        public void Exec()
        {
            var model = RuntimeTypeModel.Create();
#pragma warning disable CS0618 // we know that EnumPassthru is obsolete; we still want to check the output
            Assert.True(model[typeof(A)].EnumPassthru, "A");
            Assert.True(model[typeof(B)].EnumPassthru, "B");

            Assert.True(model[typeof(C)].EnumPassthru, "C");
            Assert.True(model[typeof(D)].EnumPassthru, "D");

            Assert.True(model[typeof(E)].EnumPassthru, "E");
            Assert.True(model[typeof(F)].EnumPassthru, "F");

            Assert.True(model[typeof(G)].EnumPassthru, "G");
            Assert.True(model[typeof(H)].EnumPassthru, "H");
#pragma warning restore CS0618
        }

        // no ProtoContract; with [Flags] is pass-thru, else not
        public enum A { X, Y, Z }
        [Flags]
        public enum B { X, Y, Z }

        // basic ProtoContract; with [Flags] is pass-thru, else not
        [ProtoContract]
        public enum C { X, Y, Z }
        [ProtoContract, Flags]
        public enum D { X, Y, Z }

        // ProtoContract with explicit pass-thru enabled; always pass-thru
        [ProtoContract]
        public enum E { X, Y, Z }
        [ProtoContract, Flags]
        public enum F { X, Y, Z }

        // ProtoContract with explicit pass-thru disabled; never pass-thru (even if [Flags])
        [ProtoContract]
        public enum G { X, Y, Z }
        [ProtoContract, Flags]
        public enum H { X, Y, Z }
    }
    
    
}
