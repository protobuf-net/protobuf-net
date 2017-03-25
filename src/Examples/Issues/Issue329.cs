using NUnit.Framework;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue329
    {
        [Flags]
        public enum ETheoFlags
        {
            TF_P = 1,
            TF_D = 2,
            TF_G = 4,
            TF_SP = 8,
            TF_SKD = 16,
            TF_SKG = 32,
            TF_VE = 64,
            TF_VO = 128,
            TF_RH = 256,
            TF_AC = 512,
            TF_FO = 1024,
            TF_ALL = 2147483647,
        }
        [ProtoContract]
        public class Foo
        {
            [ProtoMember(1), DefaultValue(ETheoFlags.TF_P)]
            public ETheoFlags Flags { get; set; }
        }
        [Test]
        public void FlagsEnumGeneration()
        {
            string proto = Serializer.GetProto<Foo>();
            Assert.AreEqual(@"package Examples.Issues;

enum ETheoFlags {
   // this is a composite/flags enumeration
   TF_P = 1;
   TF_D = 2;
   TF_G = 4;
   TF_SP = 8;
   TF_SKD = 16;
   TF_SKG = 32;
   TF_VE = 64;
   TF_VO = 128;
   TF_RH = 256;
   TF_AC = 512;
   TF_FO = 1024;
   TF_ALL = 2147483647;
}
message Foo {
   optional ETheoFlags Flags = 1 [default = TF_P];
}
", proto);
        }
    }
}
