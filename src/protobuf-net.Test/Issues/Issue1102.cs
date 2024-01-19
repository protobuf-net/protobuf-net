using ProtoBuf.Meta;
using System.Reflection;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class Issue1102
    {

        [Fact]
        public void TestEnumOrderWithNegativesAndOutOfRangeValues()
        {
            var model = RuntimeTypeModel.Create();
            model.UseImplicitZeroDefaults = false;

            string proto = model.GetSchema(typeof(HazOutOfRangeAndNegatives), ProtoSyntax.Proto3);

            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test.Issues;

message HazOutOfRangeAndNegatives {
   int64 OutOfRange = 1; // declared as invalid enum: OutOfRangeEnum
   InRangeEnum InRange = 2;
}
enum InRangeEnum {
   ZERO = 0; // proto3 requires a zero value as the first item (it can be named anything)
   A = 1;
   C = 2147483647;
   E = -2147483647;
   B = -4;
}
/* for context only
enum OutOfRangeEnum {
   ZERO = 0; // proto3 requires a zero value as the first item (it can be named anything)
   A = 1;
   C = 2147483647;
   E = -2147483647;
   B = -4;
   // D = 2147483648; // note: enums should be valid 32-bit integers
   // F = -2147483649; // note: enums should be valid 32-bit integers
}
*/
", proto, ignoreLineEndingDifferences: true);
        }

        public enum InRangeEnum : long
        {
            A = 1,
            B = -4,
            C = int.MaxValue,
            E = -int.MaxValue,
        }

        public enum OutOfRangeEnum : long
        {
            A = 1,
            B = -4,
            C = int.MaxValue,
            D = ((long)int.MaxValue) + 1,
            E = -int.MaxValue,
            F = ((long)int.MinValue) - 1,
        }

        [ProtoContract]
        public class HazOutOfRangeAndNegatives
        {
            [ProtoMember(1)]
            public OutOfRangeEnum OutOfRange { get; set; }

            [ProtoMember(2)]
            public InRangeEnum InRange { get; set; }
        }


        [Fact]
        public void TestEnumOrderInProtoMatchesDefinitionOrder()
        {
            var letters = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"};

            MemberInfo[] foundList = typeof(FooEnum).GetMembers(BindingFlags.Public | BindingFlags.Static);

            Assert.Equal(letters.Length, foundList.Length);

            for (var i = 0; i < foundList.Length; i++)
            {
                var field = (FieldInfo)foundList[i];
                Assert.Equal(letters[i], field.Name);
            }

            var proto = Serializer.GetProto<FooEnum>();

            Assert.Equal(ExpectedOutput, proto, ignoreLineEndingDifferences: true);
        }

        private static string ExpectedOutput = @"syntax = ""proto3"";
package ProtoBuf.Test.Issues;

enum FooEnum {
   A = 0;
   B = 1;
   C = 2;
   D = 3;
   E = 4;
   F = 5;
   G = 6;
   H = 7;
   I = 8;
   J = 9;
   K = 10;
   L = 11;
   M = 12;
   N = 13;
   O = 14;
   P = 15;
   Q = 16;
   R = 17;
   S = 18;
   T = 19;
   U = 20;
   V = 21;
   W = 22;
   X = 23;
   Y = 24;
   Z = 25;
}
";

        [ProtoContract]
        public enum FooEnum
        {
            [ProtoEnum]
            A,
            [ProtoEnum]
            B,
            [ProtoEnum]
            C,
            [ProtoEnum]
            D,
            [ProtoEnum]
            E,
            [ProtoEnum]
            F,
            [ProtoEnum]
            G,
            [ProtoEnum]
            H,
            [ProtoEnum]
            I,
            [ProtoEnum]
            J,
            [ProtoEnum]
            K,
            [ProtoEnum]
            L,
            [ProtoEnum]
            M,
            [ProtoEnum]
            N,
            [ProtoEnum]
            O,
            [ProtoEnum]
            P,
            [ProtoEnum]
            Q,
            [ProtoEnum]
            R,
            [ProtoEnum]
            S,
            [ProtoEnum]
            T,
            [ProtoEnum]
            U,
            [ProtoEnum]
            V,
            [ProtoEnum]
            W,
            [ProtoEnum]
            X,
            [ProtoEnum]
            Y,
            [ProtoEnum]
            Z
        }
    }
}
