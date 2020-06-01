using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using Xunit.Abstractions;

namespace Examples.Issues
{
    public class SO1930209
    {
        private ITestOutputHelper Log { get; }
        public SO1930209(ITestOutputHelper _log) => Log = _log;

        [Fact]
        public void ExecuteSimpleNestedShouldNotBuffer()
        {
            var model = RuntimeTypeModel.Create();
#if DEBUG
            model.ForwardsOnly = true;
#endif
            var obj = new BasicData {C = new C {Field1 = "abc", Field2 = "def"}};
            var clone = (BasicData)model.DeepClone(obj);
            Assert.Equal("abc", clone.C.Field1);
            Assert.Equal("def", clone.C.Field2);
            Assert.NotSame(obj, clone);
        }

        [ProtoContract]
        public class BasicData
        {
            [ProtoMember(1, DataFormat = DataFormat.Group)]
            public C C { get; set; }
        }

        [Fact]
        public void ExecuteDeeplyNestedShouldNotBuffer()
        {
            var model = RuntimeTypeModel.Create();
#if DEBUG
            model.ForwardsOnly = true;
#endif
            Log.WriteLine("Inventing data...");
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
            Log.WriteLine("{0}ms", watch.ElapsedMilliseconds);
            Log.WriteLine("Serializing...");
            watch = Stopwatch.StartNew();
            using (var file = File.Create(@"big.file"))
            {
                model.Serialize(file, orig);
            }
            watch.Stop();
            Log.WriteLine("{0}ms", watch.ElapsedMilliseconds);
            var len = new FileInfo(@"big.file").Length;
            Log.WriteLine("{0} bytes", len);
            Log.WriteLine("Deserializing...");
            watch = Stopwatch.StartNew();
            A clone;
            using (var file = File.OpenRead(@"big.file"))
            {
#pragma warning disable CS0618
                clone = (A) model.Deserialize(file, null, typeof(A));
#pragma warning restore CS0618
            }
            watch.Stop();
            Log.WriteLine("{0}ms", watch.ElapsedMilliseconds);
            int chk1 = GetCheckSum(orig), chk2 = GetCheckSum(clone);
            Log.WriteLine("Checksum: {0} vs {1}", chk1, chk2);
            Assert.Equal(chk1, chk2);
            Log.WriteLine("All done...");
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