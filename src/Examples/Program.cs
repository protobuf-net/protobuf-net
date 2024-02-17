﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using DAL;
using Examples.SimpleStream;
using ProtoBuf;
using ProtoBuf.Meta;
using System.Reflection;
using Xunit;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace Examples
{
    public static class Program
    {

#pragma warning disable IDE1006
#if COREFX
        public static bool _IsValueType(this Type type)
        {
            return type.GetTypeInfo().IsValueType;
        }
#else
        public static bool _IsValueType(this Type type)
        {
            return type.IsValueType;
        }
#endif
#pragma warning restore IDE1006

        //        static void Main() {
        //#if !COREFX
        //            Console.WriteLine("CLR: " + Environment.Version);

        //            new NWindTests().PerfTestDb();
        //#endif
        //        }

        //static void Main2() {
        //    SimpleStreamDemo demo = new SimpleStreamDemo();
        //    //const int COUNT = 1000000;
        //    const bool RUN_LEGACY = true;
        //    //demo.PerfTestSimple(COUNT, RUN_LEGACY);
        //    //demo.PerfTestString(COUNT, RUN_LEGACY);
        //    //demo.PerfTestEmbedded(COUNT, RUN_LEGACY);
        //    //demo.PerfTestEnum(COUNT, true);
        //    //demo.PerfTestArray(COUNT, true);

        //    const int NWIND_COUNT = 1000;
        //    DAL.Database db = DAL.NWindTests.LoadDatabaseFromFile<DAL.Database>(RuntimeTypeModel.Default);
        //    Console.WriteLine("Sub-object format: {0}", DAL.Database.SubObjectFormat);
        //    SimpleStreamDemo.LoadTestItem(db, NWIND_COUNT, NWIND_COUNT, false, false, false, true, false, false, null);

        //    DatabaseCompat compat = DAL.NWindTests.LoadDatabaseFromFile<DatabaseCompat>(RuntimeTypeModel.Default);
        //    SimpleStreamDemo.LoadTestItem(compat, NWIND_COUNT, NWIND_COUNT, RUN_LEGACY, false, RUN_LEGACY, true, false, true, null);

        //    DatabaseCompatRem compatRem = DAL.NWindTests.LoadDatabaseFromFile<DatabaseCompatRem>(RuntimeTypeModel.Default);
        //    SimpleStreamDemo.LoadTestItem(compatRem, NWIND_COUNT, NWIND_COUNT, true, false, true, false, false, false, null);

        //}

        public static string GetByteString(byte[] buffer)
        {
            if (buffer == null) return "[null]";
            if (buffer.Length == 0) return "[empty]";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < buffer.Length; i++)
            {
                sb.Append(buffer[i].ToString("X2")).Append(' ');
            }
            sb.Length--;
            return sb.ToString();
        }
        public static string GetByteString<T>(T item) where T : class,new()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, item);
                byte[] actual = ms.ToArray();
                return GetByteString(actual);
            }
        }

        public static void CheckBytes<T>(T item, TypeModel model, string expected)
            => CheckBytes<T>(null, item, model, expected, null);

        public static bool CheckBytes<T>(T item, TypeModel model, params byte[] expected)
            => CheckBytes<T>(null, item, model, null, expected);

        public static bool CheckBytes<T>(ITestOutputHelper output, T item, TypeModel model, params byte[] expected)
            => CheckBytes<T>(output, item, model, null, expected);

        public static bool CheckBytes<T>(ITestOutputHelper output, T item, TypeModel model, string hex, params byte[] expected)
        {
            model ??= RuntimeTypeModel.Default;
            using (MemoryStream ms = new MemoryStream())
            {
                model.Serialize(ms, item);

                if (expected != null)
                {
                    byte[] actual = ms.ToArray();
                    bool equal = Program.ArraysEqual(actual, expected);
                    if (!equal)
                    {
                        string exp = GetByteString(expected), act = GetByteString(actual);
                        output?.WriteLine("Expected: {0}", exp);
                        output?.WriteLine("Actual: {0}", act);
                    }
                    return equal;
                }
                else if (hex != null)
                {
                    var actualHex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
                    Assert.Equal(hex, actualHex);
                    return hex == actualHex;
                }
                else
                {
                    throw new InvalidOperationException("hex or expected needs to be set");
                }
            }
        }
        public static bool CheckBytes<T>(ITestOutputHelper output, T item, params byte[] expected)
            => CheckBytes<T>(output, item, null, expected);

        public static bool CheckBytes<T>(T item, params byte[] expected)
            => CheckBytes<T>(null, item, null, null, expected);
        public static void CheckBytes<T>(T item, string hex)
            => CheckBytes<T>(null, item, null, hex, null);

        public static T Build<T>(params byte[] raw) where T : class, new()
        {
            using (MemoryStream ms = new MemoryStream(raw))
            {
                return Serializer.Deserialize<T>(ms);
            }
        }
        public static bool ArraysEqual(byte[] actual, byte[] expected)
        {
            if (ReferenceEquals(actual, expected)) return true;
            if (actual == null || expected == null) return false;
            if (actual.Length != expected.Length) return false;
            for (int i = 0; i < actual.Length; i++)
            {
                if (actual[i] != expected[i]) return false;
            }
            return true;
        }

        public static TException ExpectFailure<TException>(Action action, string message = null)
            where TException : Exception
        {
            var ex = Assert.Throws<TException>(action);
            if (message != null) Assert.Equal(DeVersion(message), DeVersion(ex.Message));
            return ex;
        }
        private static string DeVersion(string input) => Regex.Replace(input, "Version=([0-9.]+)", "Version=*");
        private static string NormalizeParameterName(string input)
        {   // ArgumentException changes format between runtimes
            return Regex.Replace(input, @" \(Parameter '(.*)'\)", @"
Parameter name: $1");
        }
        public static void ExpectFailure<TException>(Action action, Func<TException, bool> check)
            where TException : Exception
        {
            try
            {
                action();
                Assert.Equal("throw", "ok");
            }
            catch(TException ex)
            {
                if (check != null) Assert.True(check(ex));
            }
        }
    }
}