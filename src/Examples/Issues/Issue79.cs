using System;
using Xunit;
using ProtoBuf;

namespace Examples.Issues
{
    
    public class Issue79
    {
        [Flags]
        public enum MyFlagsEnum : ushort // just to make the tests awqward
        {
            None = 0, A = 1, B = 2, C = 4, D = 8
        }
        [ProtoContract]
        public class MyTypeWithFlags
        {
            [ProtoMember(1, DataFormat = DataFormat.TwosComplement)]
            public MyFlagsEnum SomeValue { get; set; }
        }

        [Fact]
        public void ShouldRoundtripIndividualValues()
        {
            TestRoundtrip(MyFlagsEnum.None);
            TestRoundtrip(MyFlagsEnum.A);
            TestRoundtrip(MyFlagsEnum.B);
            TestRoundtrip(MyFlagsEnum.C);
            TestRoundtrip(MyFlagsEnum.D);
        }
        [Fact]
        public void ShouldRoundtripCompositeValues()
        {
            MyFlagsEnum max = (MyFlagsEnum.A | MyFlagsEnum.B | MyFlagsEnum.C | MyFlagsEnum.D);
            for (MyFlagsEnum i = 0; i <= max; i++)
            {
                TestRoundtrip(i);
            }
        }
        private static void TestRoundtrip(MyFlagsEnum value)
        {

            MyTypeWithFlags obj = new MyTypeWithFlags { SomeValue = value }, clone;
            string caption = value + " (" + (int)value + ")";
            clone = Serializer.DeepClone(obj);
            Assert.Equal(value, clone.SomeValue); //, caption);
        }
    }

}
