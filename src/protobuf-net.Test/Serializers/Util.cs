using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf.Serializers;
using System.IO;
using Xunit;
using ProtoBuf.Meta;
using ProtoBuf.Compiler;

namespace ProtoBuf.unittest.Serializers
{
    static partial class Util
    {
#if !NO_INTERNAL_CONTEXT
        public static void Test(object value, Type innerType, Func<IProtoSerializer, IProtoSerializer> ctor,
            string expectedHex)
        {
            byte[] expected = new byte[expectedHex.Length / 2];
            for (int i = 0; i < expected.Length; i++)
            {
                expected[i] = (byte)Convert.ToInt32(expectedHex.Substring(i*2,2),16);
            }
            NilSerializer nil = new NilSerializer(innerType);
            var ser = ctor(nil);

            var model = RuntimeTypeModel.Create();
            var decorator = model.GetSerializer(ser, false);
            Test(value, decorator, "decorator", expected);

            var compiled = model.GetSerializer(ser, true);
            Test(value, compiled, "compiled", expected);
        }

        public static void Test(object obj, ProtoSerializer serializer, string message, byte[] expected)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                int reported;
                using (ProtoWriter writer = new ProtoWriter(ms, RuntimeTypeModel.Default, null))
                {
                    serializer(obj, writer);
                    reported = ProtoWriter.GetPosition(writer);
                }
                data = ms.ToArray();
                Assert.Equal(reported, data.Length); //, message + ":reported/actual");
            }
            Assert.Equal(expected.Length, data.Length); //, message + ":Length");
            for (int i = 0; i < data.Length; i++)
            {
                Assert.Equal(expected[i], data[i]); //, message + ":" + i);
            }
        }
#endif
        public static void TestModel(RuntimeTypeModel model, object value, string hex)
        {
            byte[] raw;
            using (MemoryStream ms = new MemoryStream())
            {
                model.Serialize(ms, value);
                raw = ms.ToArray();
            }

            Assert.Equal(hex, GetHex(raw));

            model.CompileInPlace();
            using (MemoryStream ms = new MemoryStream())
            {
                model.Serialize(ms, value);
                raw = ms.ToArray();
            }

            Assert.Equal(hex, GetHex(raw));

            TypeModel compiled = model.Compile("compiled", "compiled.dll");
            PEVerify.Verify("compiled.dll");
            using (MemoryStream ms = new MemoryStream())
            {
                compiled.Serialize(ms, value);
                raw = ms.ToArray();
            }
            Assert.Equal(hex, GetHex(raw));
        }
#if !NO_INTERNAL_CONTEXT
        public static void Test<T>(T value, Func<IProtoSerializer, IProtoSerializer> ctor, string expectedHex)
        {
            Test(value, typeof(T), ctor, expectedHex);
        }
#endif
        internal static string GetHex(byte[] bytes)
        {
            int len = bytes.Length;
            StringBuilder sb = new StringBuilder(len * 2);
            for (int i = 0; i < len; i++)
            {
                sb.Append(bytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
        public static void Test(Action<ProtoWriter> action, string expectedHex)
        {
            using (var ms = new MemoryStream())
            {
                using (var pw = ProtoWriter.Create(ms, RuntimeTypeModel.Default, null))
                {
                    action(pw);
                }
                string s = GetHex(ms.ToArray());               
                Assert.Equal(expectedHex, s);
            }
        }
    }
}