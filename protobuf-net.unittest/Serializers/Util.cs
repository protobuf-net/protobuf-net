using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf.Serializers;
using System.IO;
using NUnit.Framework;
using ProtoBuf.Meta;
using ProtoBuf.Compiler;

namespace ProtoBuf.unittest.Serializers
{
    static class Util
    {
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

            var decorator = RuntimeTypeModel.GetSerializer(ser, false);
            Test(value, decorator, "decorator", expected);

            var compiled = RuntimeTypeModel.GetSerializer(ser, true);
            Test(value, compiled, "compiled", expected);
        }
        public static void Test(object obj, ProtoSerializer serializer, string message, byte[] expected)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                int reported;
                using (ProtoWriter writer = new ProtoWriter(ms, RuntimeTypeModel.Default))
                {
                    serializer(obj, writer);
                    reported = ProtoWriter.GetPosition(writer);
                }
                data = ms.ToArray();
                Assert.AreEqual(reported, data.Length, message + ":reported/actual");
            }
            Assert.AreEqual(expected.Length, data.Length, message + ":Length");
            for (int i = 0; i < data.Length; i++)
            {
                Assert.AreEqual(expected[i], data[i], message + ":" + i);
            }
        }

        public static void TestModel(RuntimeTypeModel model, object value, string hex)
        {
            byte[] raw;
            using (MemoryStream ms = new MemoryStream())
            {
                model.Serialize(ms, value);
                raw = ms.ToArray();
            }

            Assert.AreEqual(hex, GetHex(raw));

            model.CompileInPlace();
            using (MemoryStream ms = new MemoryStream())
            {
                model.Serialize(ms, value);
                raw = ms.ToArray();
            }

            Assert.AreEqual(hex, GetHex(raw));

            TypeModel compiled = model.Compile("compiled", "compiled.dll");
            PEVerify.Verify("compiled.dll");
            using (MemoryStream ms = new MemoryStream())
            {
                compiled.Serialize(ms, value);
                raw = ms.ToArray();
            }
            Assert.AreEqual(hex, GetHex(raw));

        }
        
        public static void Test<T>(T value, Func<IProtoSerializer, IProtoSerializer> ctor, string expectedHex)
        {
            Test(value, typeof(T), ctor, expectedHex);
        }
        static string GetHex(byte[] bytes)
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
                using (var pw = new ProtoWriter(ms, RuntimeTypeModel.Default))
                {
                    action(pw);
                }
                string s = GetHex(ms.ToArray());               
                Assert.AreEqual(expectedHex, s);
            }
        }
    }
}
