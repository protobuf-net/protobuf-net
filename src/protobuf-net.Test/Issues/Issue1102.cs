using System.Reflection;
using Xunit;

namespace ProtoBuf.Test.Issues
{
#if !NET7_0_OR_GREATER
    public class Issue1102
    {
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
#endif
}
