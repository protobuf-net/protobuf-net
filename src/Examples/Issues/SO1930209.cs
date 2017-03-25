using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class SO1930209
    {
        [Test]
        public void ExecuteSimpleNestedShouldNotBuffer()
        {
            var model = TypeModel.Create();
#if DEBUG
            model.ForwardsOnly = true;
#endif
            var obj = new BasicData {C = new C {Field1 = "abc", Field2 = "def"}};
            var clone = (BasicData)model.DeepClone(obj);
            Assert.AreEqual("abc", clone.C.Field1);
            Assert.AreEqual("def", clone.C.Field2);
            Assert.AreNotSame(obj, clone);
        }

        [ProtoContract]
        public class BasicData
        {
            [ProtoMember(1, DataFormat = DataFormat.Group)]
            public C C { get; set; }
        }

        [Test]
        public void ExecuteDeeplyNestedShouldNotBuffer()
        {
            var model = TypeModel.Create();
#if DEBUG
            model.ForwardsOnly = true;
#endif
            Console.WriteLine("Inventing data...");
            var watch = Stopwatch.StartNew();
            var arr = new B[5];
            for (int i = 0; i < arr.Length; i++)
            {
                var arr2 = new C[20000];
                arr[i] = new B {Array2 = arr2, Field1 = GetString(), Field2 = GetString()};
                for (int j = 0; j < arr2.Length; j++)
                {
                    arr2[j] = new C {Field1 = GetString(), Field2 = GetString()};
                }
            }
            var orig = new A {Array1 = arr};
            watch.Stop();
            Console.WriteLine("{0}ms", watch.ElapsedMilliseconds);
            Console.WriteLine("Serializing...");
            watch = Stopwatch.StartNew();
            using (var file = File.Create(@"big.file"))
            {
                model.Serialize(file, orig);
            }
            watch.Stop();
            Console.WriteLine("{0}ms", watch.ElapsedMilliseconds);
            var len = new FileInfo(@"big.file").Length;
            Console.WriteLine("{0} bytes", len);
            Console.WriteLine("Deserializing...");
            watch = Stopwatch.StartNew();
            A clone;
            using (var file = File.OpenRead(@"big.file"))
            {
                clone = (A) model.Deserialize(file, null, typeof(A));
            }
            watch.Stop();
            Console.WriteLine("{0}ms", watch.ElapsedMilliseconds);
            int chk1 = GetCheckSum(orig), chk2 = GetCheckSum(clone);
            Console.WriteLine("Checksum: {0} vs {1}", chk1, chk2);
            Assert.AreEqual(chk1, chk2);
            Console.WriteLine("All done...");
        }

        private static int GetCheckSum(A a)
        {
            unchecked
            {
                var arr = a.Array1;
                int chk = 0;
                for (int i = 0; i < arr.Length; i++)
                {
                    var b = arr[i];
                    chk += b.Field1.GetHashCode() + b.Field2.GetHashCode();
                    var arr2 = b.Array2;
                    for (int j = 0; j < arr2.Length; j++)
                    {
                        var c = arr2[j];
                        chk += c.Field1.GetHashCode() + c.Field2.GetHashCode();
                    }
                }
                return chk;
            }
        }

        private static int i;

        private static string GetString()
        {
            return "s " + Interlocked.Increment(ref i);
        }

        [ProtoContract]
        public class A
        {
            [ProtoMember(1, DataFormat = DataFormat.Group)]
            public B[] Array1 { get; set; }
        }

        [ProtoContract]
        public class B
        {
            [ProtoMember(1)]
            public string Field1 { get; set; }

            [ProtoMember(2)]
            public string Field2 { get; set; }

            [ProtoMember(3, DataFormat = DataFormat.Group)]
            public C[] Array2 { get; set; }
        }

        [ProtoContract]
        public class C
        {
            [ProtoMember(1)]
            public string Field1 { get; set; }

            [ProtoMember(2)]
            public string Field2 { get; set; }
        }
    }
}