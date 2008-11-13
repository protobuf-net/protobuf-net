using System;
using NUnit.Framework;
using System.Reflection;
using ProtoBuf.ServiceModel;
using System.IO;
using System.Collections.Generic;
using System.Text;

[TestFixture]
public class RpcPacking
{
    
    [Test]
    public void TestPing()
    {
        var client = new Client<IWrapped>();
        Wrapped wrapped = new Wrapped();
        Assert.IsFalse(wrapped.PingCalled);
        
        object result = client.RoundTrip(wrapped, true, "Ping");
        CheckBytes(client.Request, "request");
        CheckBytes(client.Response, "response");
        Assert.IsTrue(wrapped.PingCalled, "called");
        Assert.IsNull(result, "result");
    }

    private static void CheckBytes(byte[] actual, string caption, params IEnumerable<byte>[] expectedFragments)
    {
        List<byte> list = new List<byte>();
        foreach(var fragment in expectedFragments)
        {
            list.AddRange(fragment);
        }
        byte[] expected = list.ToArray();

        Assert.IsNotNull(expected, caption + ":expected was null");
        Assert.IsNotNull(expected, caption + ":actual was null");
        Assert.AreNotSame(expected, actual, caption + ":expected/actual same");
        Assert.AreEqual(expected.Length, actual.Length, caption + ":length");
        for(int i = 0 ; i < expected.Length ; i++)
        {
            Assert.AreEqual(expected[i], actual[i], caption + ":" + i);
        }
    }


    [Test]
    public void TestNoResult()
    {
        var client = new Client<IWrapped>();
        Wrapped wrapped = new Wrapped();
        Assert.IsFalse(wrapped.NoResultCalled);

        object result = client.RoundTrip(wrapped, true, "NoResult", 12345, "abcde");

        CheckBytes(client.Request, "request",
                   GetTag(1, WireType.String), GetBytes(3), GetTag(1, WireType.Variant), GetBytes(0xB9, 0x60),
                   GetTag(2, WireType.String), GetBytes(7), GetTag(1, WireType.String), GetBytes(5), Encoding.UTF8.GetBytes("abcde"));
        CheckBytes(client.Response, "response");

        Assert.IsTrue(wrapped.NoResultCalled, "called");
        Assert.AreEqual(12345, wrapped.NoResultA, "in:a");
        Assert.AreEqual("abcde", wrapped.NoResultB, "in:b");
        Assert.IsNull(result, "result");
    }

    [Test]
    public void TestResultOnly()
    {
        var client = new Client<IWrapped>();
        Wrapped wrapped = new Wrapped();
        Assert.IsFalse(wrapped.ResultOnlyCalled);

        string result = (string) client.RoundTrip(wrapped, true, "ResultOnly");
        
        CheckBytes(client.Request, "request");
        int len = Encoding.UTF8.GetByteCount(Wrapped.ResultOnlyExpectedResult);
        CheckBytes(client.Response, "response",
                   GetTag(1, WireType.String),
                   GetBytes(2 + len),
                   GetTag(1, WireType.String),
                   GetBytes(len),
                   Encoding.UTF8.GetBytes(Wrapped.ResultOnlyExpectedResult));
        Assert.IsTrue(wrapped.ResultOnlyCalled, "called");
        Assert.AreEqual(Wrapped.ResultOnlyExpectedResult, result, "result");
    }

    [Test]
    public void TestInputOutputResult()
    {
        object[] args = { 3, "abc", true };
        var client = new Client<IWrapped>();
        Wrapped wrapped = new Wrapped();
        Assert.IsFalse(wrapped.InputOutputResultCalled);

        object result = client.RoundTrip(wrapped, true, "InputOutputResult", args);

        int len;
        CheckBytes(client.Request, "request",
            GetTag(1, WireType.String), GetBytes(2), GetTag(1, WireType.Variant), GetBytes(3),
            GetTag(2, WireType.String), GetBytes(2 + (len=Encoding.UTF8.GetByteCount("abc"))), GetTag(1, WireType.String), GetBytes(len), Encoding.UTF8.GetBytes("abc"));

        CheckBytes(client.Response, "response",
                   GetTag(1, WireType.String), GetBytes(2 + (len = Encoding.UTF8.GetByteCount(Wrapped.InputOutputResultExpectedResult))), GetTag(1, WireType.String), GetBytes(len), Encoding.UTF8.GetBytes(Wrapped.InputOutputResultExpectedResult),
                   GetTag(3, WireType.String), GetBytes(2 + (len = Encoding.UTF8.GetByteCount(Wrapped.InputOutputResultExpectedB))), GetTag(1, WireType.String), GetBytes(len), Encoding.UTF8.GetBytes(Wrapped.InputOutputResultExpectedB),
                   GetTag(4, WireType.String), GetBytes(2), GetTag(1, WireType.Variant), GetBytes(1));

        Assert.IsTrue(wrapped.InputOutputResultCalled, "called");
        Assert.AreEqual(Wrapped.InputOutputResultExpectedResult, result, "result");
        Assert.AreEqual(3, wrapped.InputOutputResultA, "in:a");
        Assert.AreEqual("abc", wrapped.InputOutputResultB, "in:b");
        Assert.AreEqual(Wrapped.InputOutputResultExpectedB, args[1], "out:b");
        Assert.AreEqual(Wrapped.InputOutputResultExpectedC, args[2], "out:c");
    }



    [Test]
    public void TestInputOutputNoResult()
    {
        object[] args = {3, "abc", true};
        var client = new Client<IWrapped>();
        Wrapped wrapped = new Wrapped();
        Assert.IsFalse(wrapped.InputOutputNoResultCalled);

        object result = client.RoundTrip(wrapped, true, "InputOutputNoResult", args);

        int len = Encoding.UTF8.GetByteCount("abc");
        CheckBytes(client.Request, "request",
            GetTag(1, WireType.String), GetBytes(2), GetTag(1, WireType.Variant), GetBytes(3),
            GetTag(2, WireType.String), GetBytes(2 + len), GetTag(1, WireType.String), GetBytes(len), Encoding.UTF8.GetBytes("abc"));

        len = Encoding.UTF8.GetByteCount(Wrapped.InputOutputNoResultExpectedB);
        CheckBytes(client.Response, "response",
                   GetTag(3, WireType.String), GetBytes(2 + len), GetTag(1, WireType.String), GetBytes(len),
                   Encoding.UTF8.GetBytes(Wrapped.InputOutputNoResultExpectedB),
                   GetTag(4, WireType.String), GetBytes(2), GetTag(1, WireType.Variant), GetBytes(1));

        Assert.IsTrue(wrapped.InputOutputNoResultCalled, "called");
        Assert.IsNull(result, "result");
        Assert.AreEqual(3, wrapped.InputOutputNoResultA, "in:a");
        Assert.AreEqual("abc", wrapped.InputOutputNoResultB, "in:b");
        Assert.AreEqual(Wrapped.InputOutputNoResultExpectedB, args[1], "out:b");
        Assert.AreEqual(Wrapped.InputOutputNoResultExpectedC, args[2], "out:c");

    }

    static byte[] GetBytes(params int[] values)
    {
        return Array.ConvertAll(values, x => (byte) x);
    }
    internal enum WireType
    {
        /// <summary>
        /// Base-128 variant-length encoding
        /// </summary>
        Variant = 0,

        /// <summary>
        /// Fixed-length 8-byte encoding
        /// </summary>
        Fixed64 = 1,

        /// <summary>
        /// Length-variant-prefixed encoding
        /// </summary>
        String = 2,

        /// <summary>
        /// Indicates the start of a group
        /// </summary>
        StartGroup = 3,

        /// <summary>
        /// Indicates the end of a group
        /// </summary>
        EndGroup = 4,

        /// <summary>
        /// Fixed-length 4-byte encoding
        /// </summary>
        Fixed32 = 5
    }
    static byte[] GetTag(int field, WireType wireType)
    {
        return GetBytes((field << 3) | (int)wireType);
    }

    class Client<T> : RpcClient
    {
        public Client() : base(typeof(T)) {}

        public byte[] Request { get; private set; }
        public byte[] Response { get; private set; }
        public object RoundTrip(T implementation, bool wrapped, string methodName, params object[] args)
        {
            MethodInfo method = typeof (T).GetMethod(methodName);
            object[] serverArgs = new object[args.Length];
            using(MemoryStream ms = new MemoryStream())
            {
                PackRequestParameters(wrapped, method, args, ms);
                ms.Position = 0;
                Request = ms.ToArray();
                UnpackRequestParameters(wrapped, method, serverArgs, ms);
            }
            object serverResult = method.Invoke(implementation, serverArgs), clientResult;
            using(MemoryStream ms = new MemoryStream())
            {
                PackResponseParameters(wrapped, method, serverResult, serverArgs, ms);
                ms.Position = 0;
                Response = ms.ToArray();
                clientResult = UnpackResponseParameters(wrapped, method, args, ms);
            }
            return clientResult;
        }
    }

    interface IWrapped
    {
        void Ping();
        void NoResult(int a, string b);
        string ResultOnly();
        string InputOutputResult(int a, ref string b, out bool c);
        void InputOutputNoResult(int a, ref string b, out bool c);
    }

    class Wrapped : IWrapped
    {
        public bool PingCalled { get; private set;}
        public void Ping()
        {
            PingCalled = true;
        }

        public bool NoResultCalled { get; private set;}
        public int NoResultA { get; private set;}
        public string NoResultB { get; private set;}
        public void NoResult(int a, string b)
        {
            NoResultCalled = true;
            NoResultA = a;
            NoResultB = b;
        }

        public bool ResultOnlyCalled { get; private set;}
        public string ResultOnly()
        {
            ResultOnlyCalled = true;
            return ResultOnlyExpectedResult;
        }
        public const string ResultOnlyExpectedResult = "ResultOnlyExpectedResult";

        public bool InputOutputResultCalled { get; private set;}
        public int InputOutputResultA { get; private set;}
        public string InputOutputResultB { get; private set;}
        public string InputOutputResult(int a, ref string b, out bool c)
        {
            InputOutputResultCalled = true;
            InputOutputResultA = a;
            InputOutputResultB = b;
            b = InputOutputResultExpectedB;
            c = InputOutputResultExpectedC;
            return InputOutputResultExpectedResult;
        }
        public const string InputOutputResultExpectedResult = "InputOutputResultExpectedResult";
        public const string InputOutputResultExpectedB = "InputOutputResultExpectedB";
        public const bool InputOutputResultExpectedC = true;

        public bool InputOutputNoResultCalled { get; private set; }
        public int InputOutputNoResultA { get; private set; }
        public string InputOutputNoResultB { get; private set; }
        public void InputOutputNoResult(int a, ref string b, out bool c)
        {
            InputOutputNoResultCalled = true;
            InputOutputNoResultA = a;
            InputOutputNoResultB = b;
            b = InputOutputNoResultExpectedB;
            c = InputOutputNoResultExpectedC;
        }
        public const string InputOutputNoResultExpectedB = "InputOutputNoResultExpectedB";
        public const bool InputOutputNoResultExpectedC = true;
    }
}

