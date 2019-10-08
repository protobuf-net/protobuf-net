using ProtoBuf.Compiler;
using ProtoBuf.Internal.Serializers;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace ProtoBuf.unittest.Serializers
{
    internal static partial class Util
    {
#if !NO_INTERNAL_CONTEXT
        public static void Test<T>(T value, Type innerType, Func<IRuntimeProtoSerializerNode, IRuntimeProtoSerializerNode> ctor,
            string expectedHex)
        {
            Assert.NotEqual(typeof(object), typeof(T));
            byte[] expected = new byte[expectedHex.Length / 2];
            for (int i = 0; i < expected.Length; i++)
            {
                expected[i] = (byte)Convert.ToInt32(expectedHex.Substring(i*2,2),16);
            }
            NilSerializer nil = new NilSerializer(innerType);
            var ser = ctor(nil);

            var model = RuntimeTypeModel.Create();
            var decorator = model.GetSerializer<T>(ser, false);
            Test(value, decorator, "decorator", expected);

            var compiled = model.GetSerializer<T>(ser, true);
            Test(value, compiled, "compiled", expected);
        }

#pragma warning disable RCS1163, IDE0060 // Unused parameter.
        public static void Test<T>(T obj, ProtoSerializer<T> serializer, string message, byte[] expected)
#pragma warning restore RCS1163, IDE0060 // Unused parameter.
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                long reported;
                var state = ProtoWriter.State.Create(ms, RuntimeTypeModel.Default, null);
                try
                {
                    serializer(ref state, obj);
                    state.Close();
#pragma warning disable CS0618
                    reported = state.GetPosition();
#pragma warning restore CS0618
                }
                finally
                {
                    state.Dispose();
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
#pragma warning disable CS0618
                model.Serialize(ms, value);
#pragma warning restore CS0618
                raw = ms.ToArray();
            }

            Assert.Equal(hex, GetHex(raw));

            model.CompileInPlace();
            using (MemoryStream ms = new MemoryStream())
            {
#pragma warning disable CS0618
                model.Serialize(ms, value);
#pragma warning restore CS0618
                raw = ms.ToArray();
            }

            Assert.Equal(hex, GetHex(raw));

            TypeModel compiled = model.Compile("compiled", "compiled.dll");
            PEVerify.Verify("compiled.dll");
            using (MemoryStream ms = new MemoryStream())
            {
#pragma warning disable CS0618
                compiled.Serialize(ms, value);
#pragma warning restore CS0618
                raw = ms.ToArray();
            }
            Assert.Equal(hex, GetHex(raw));
        }
#if !NO_INTERNAL_CONTEXT
        public static void Test<T>(T value, Func<IRuntimeProtoSerializerNode, IRuntimeProtoSerializerNode> ctor, string expectedHex)
        {
            Test<T>(value, typeof(T), ctor, expectedHex);
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

        public delegate void WriterRunner(ref ProtoWriter.State state);

        public static void Test(WriterRunner action, string expectedHex)
        {
            using var ms = new MemoryStream();
            var state = ProtoWriter.State.Create(ms, RuntimeTypeModel.Default, null);
            try
            {
                action(ref state);
                state.Close();
            }
            finally
            {
                state.Dispose();
            }
            string s = GetHex(ms.ToArray());
            Assert.Equal(expectedHex, s);
        }
    }
}