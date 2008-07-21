using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using ProtoBuf;
using System.IO;

namespace Examples
{
    class Program
    {
        static void Main() { }
        public static bool CheckBytes<T>(T item, params byte[] expected) where T : class, new()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, item);
                byte[] actual = ms.ToArray();
                return Program.ArraysEqual(actual, expected);
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

    }
}
