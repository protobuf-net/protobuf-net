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
        static void Main()
        {

            SimpleStream.Collections.Bar bar = new SimpleStream.Collections.Bar { Value = 128 }, clone;

            using(MemoryStream ms = new MemoryStream()) {
                Serializer.Serialize(ms, bar);
                byte[] buffer = ms.ToArray();
                ms.Position = 0;
                clone = Serializer.Deserialize<SimpleStream.Collections.Bar>(ms);
            }



            #region Demo Runner
            int index = 0, passCount = 0, failCount = 0;
            List<string> failList = new List<string>();
            Action<Func<bool>> run = demo =>
            {
                index++;
                bool pass = false;
                string name = demo.Method.DeclaringType.Name + "."
                    + demo.Method.Name;
                try
                {
                    Console.WriteLine("[demo {0}; {1}]", index, name);
                    Console.WriteLine();
                    pass = demo();
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("!!!!! Exception in test {0}", index);
                    Console.WriteLine("\t" + ex.GetType().Name);
                    while (ex != null)
                    {
                        Console.WriteLine("\t" + ex.Message);
                        ex = ex.InnerException;
                    }
                    Console.WriteLine();
                }
                finally
                {
                    Console.WriteLine();
                    Console.WriteLine("[end demo {0}: {1}]", index, (pass ? "pass" : "FAIL"));
                    Console.WriteLine();
                }
                if (pass) { passCount++; }
                else { failCount++; failList.Add(name); }
            };
            #endregion

            run(TestNumbers.SignTests.RunSignTests);
            run(SimpleStream.Collections.RunCollectionTests);
            run(TestNumbers.NumberTests.RunNumberTests);
            run(SimpleStream.SimplePerfTests.RunSimplePerfTests);
            run(SimpleStream.SimpleStreamDemo.RunSimpleStreams);
#if REMOTING
            run(Remoting.RemotingDemo.RunRemotingDemo);
#endif

            Console.WriteLine();
            Console.WriteLine("Tests complete; {0} passed, {1} failed", passCount, failCount);
            foreach (string failName in failList)
            {
                Console.WriteLine("Failed: {0}", failName);
            }
            Console.WriteLine("(Press any key to exit)");
            Console.ReadKey();
        }
        static void RunDemo(Action<int> demo)
        {

        }
    }
}
