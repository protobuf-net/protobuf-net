using System.Collections.Generic;
using Xunit;
using ProtoBuf;
using Examples.Ppt;

namespace Examples.SimpleStream
{
    [ProtoContract]
    class NoddyExtends : Extensible { }

    [ProtoContract]
    class Noddy
    {
        [ProtoMember(2)]
        public int Foo { get; set; }
    }

    
    public class GroupedData
    {
        [Fact]
        public void TestGroup()
        {
            Test3 t3 = Program.Build<Test3>(0x1B, 0x08, 0x96, 0x01, 0x1C);// [start group 3] [test1] [end group 3]
            Assert.Equal(150, t3.C.A);
        }

        [Fact]
        public void TestGroupAsExtension()
        {
            NoddyExtends ne = Program.Build<NoddyExtends>(0x1B, 0x08, 0x96, 0x01, 0x1C);// [start group 3] [test1] [end group 3]

            Assert.True(Program.CheckBytes(ne, 0x1B, 0x08, 0x96, 0x01, 0x1C), "Round trip");

            Test1 t1 = Extensible.GetValue<Test1>(ne, 3);
            Assert.NotNull(t1); //, "Got an object?");
            Assert.Equal(150, t1.A); //, "Value");
        }

        [Fact]
        public void TestGroupIgnore()
        {
            // 0x1B = 11 011 = start group 3
            // 0x08 = 1000 = varint 1
            // 0x96 0x01 = 10010110 = 150
            // 0x1c = 011 100 = end group 3
            // 0x10 = 10 000 = varint 2
            // 0x96 0x01 = 10010110 = 150
            Noddy no = Program.Build<Noddy>(0x1B, 0x08, 0x96, 0x01, 0x1C, 0x10, 0x96, 0x01);
            Assert.Equal(150, no.Foo);
        }

        [Fact]
        public void TestUnterminatedGroup()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                Test3 t3 = Program.Build<Test3>(0x1B, 0x08, 0x96, 0x01);// [start group 3] [test1]
            });
        }
        [Fact]
        public void TestWrongGroupClosed()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                Test3 t3 = Program.Build<Test3>(0x1B, 0x08, 0x96, 0x01, 0x24);// [start group 3] [test1] [end group 4]
            });
        }

        [ProtoContract]
        class Test3List
        {
            [ProtoMember(3)]
            public List<Test1> C { get; set; }
        }

        [ProtoContract]
        class Test1List
        {
            [ProtoMember(1)]
            public List<int> A { get; set; }
        }
        [Fact]
        public void TestEntityList()
        {
            Test3List t3 = Program.Build<Test3List>(
                0x1B, 0x08, 0x96, 0x01, 0x1C, // start 3: A=150; end 3
                0x1B, 0x08, 0x82, 0x01, 0x1C);// start 3: A=130; end 3
            Assert.Equal(2, t3.C.Count);
            Assert.Equal(150, t3.C[0].A);
            Assert.Equal(130, t3.C[1].A);
        }

        [Fact]
        public void TestPrimativeList()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                Test1List t1 = Program.Build<Test1List>(0x0B, 0x96, 0x01, 0x0C); // [start:1] [150] [end:1]
            });
        }
    }
}
