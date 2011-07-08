using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Threading;
using ProtoBuf.Meta;
using System.IO;

namespace ProtoBuf.unittest.Meta
{
    [TestFixture]
    public class ThreadRace
    {///the purpose here is to investigate thread-race issues with the initial reflection / compilation of a type, which has been
     ///an observed issue. We are only looking at implicit models here, as explicit models have much more controlled lives (caveat developer, etc)
     ///

        [ProtoContract]
        public class ModelWithNonTrivialProperties
        {
            [ProtoMember(1)]public int A { get; set; }
            [ProtoMember(2)]public string B { get; set; }
            [ProtoMember(3)]public DateTime C { get; set; }
            [ProtoMember(4)]public byte[] D { get; set; }
            [ProtoMember(5)]public float? E { get; set; }
            [ProtoMember(6)]public ModelWithNonTrivialProperties F { get; set; }
            [ProtoMember(7)]public List<decimal> G { get; set; }
            [ProtoMember(8)]public Dictionary<int, AnotherType> H { get; set; }
            [ProtoMember(9)]public AnotherType I{ get; set; }
            [ProtoMember(10)]public Dictionary<string,int> J { get; set; }
        }
        [ProtoContract]
        public class AnotherType
        {
            [ProtoMember(1)]public int A { get; set; }
            [ProtoMember(2)]public string B { get; set; }
        }

        [Test]
        public void DestructionTestModelInitialisation()
        {
            // we run this a few (100) times because threading isn't reliable ;p
            // each time (on a fresh model), we prepare a slew (20) threads - each eager to serialize one of two available types.
            // They all wait on a single gate, and then the fight begins! We track any failures, and Join all the threads
            // back together.
            var a = new ModelWithNonTrivialProperties()
            {
                J = new Dictionary<string, int> { { "abc", 123 } },
                D = new byte[] { 0, 1, 2, 3, 4 },
                G = new List<decimal> { 1, 2, 3, 4, 5 },
                H = new Dictionary<int, AnotherType> { { 1, new AnotherType { A = 456 } } },
                I = new AnotherType { B = "def" }
            };
            var b = new AnotherType() { A = 123 };
            for (int i = 0; i < 100; i++)
            {
                ManualResetEvent allGo = new ManualResetEvent(false);
                var model = TypeModel.Create();
                model.AutoCompile = true;
                object starter = new object();
                int waiting = 20;
                int failures = 0;
                Exception firstException = null;
                Thread[] threads = new Thread[20];
                for (int j = 0; j < 10; j++)
                {
                    threads[2 * j] = new Thread(() =>
                    {
                        try
                        {
                            if (Interlocked.Decrement(ref waiting) == 0) allGo.Set();
                            allGo.WaitOne();
                            model.Serialize(Stream.Null, a);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.CompareExchange(ref firstException, ex, null);
                            Interlocked.Increment(ref failures);
                        }
                    });
                    threads[(2 * j) + 1] = new Thread(() =>
                    {
                        try
                        {
                            if (Interlocked.Decrement(ref waiting) == 0) allGo.Set();
                            allGo.WaitOne();
                            model.Serialize(Stream.Null, b);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.CompareExchange(ref firstException, ex, null);
                            Interlocked.Increment(ref failures);
                        }
                    });
                }

                for (int j = 0; j < threads.Length; j++) threads[j].Start();
                for (int j = 0; j < threads.Length; j++) threads[j].Join();

                Assert.IsNull(firstException);
                Assert.AreEqual(0, Interlocked.CompareExchange(ref failures, 0, 0));

            }
        }



        [Test]
        public void DestructionTestDictionary()
        {
            byte[] raw;
            using (var ms = new MemoryStream())
            {
                var orig = new Dictionary<string,string>{{"a","b"},{"c","d"},{"e","f"}};
                var model = RuntimeTypeModel.Create();
                model.Serialize(ms, orig);
                raw = ms.ToArray();
            }

            for (int i = 0; i < 100; i++)
            {
                ManualResetEvent allGo = new ManualResetEvent(false);
                var model = TypeModel.Create();
                model.AutoCompile = true;
                object starter = new object();
                int waiting = 20;
                int failures = 0;
                Exception firstException = null;
                Thread[] threads = new Thread[20];
                for (int j = 0; j < 20; j++)
                {
                    threads[j] = new Thread(() =>
                    {
                        try
                        {
                            using (var ms = new MemoryStream(raw))
                            {
                                if (Interlocked.Decrement(ref waiting) == 0) allGo.Set();
                                allGo.WaitOne();
                                var data = (Dictionary<string,string>)model.Deserialize(ms, null, typeof(Dictionary<string,string>));
                                if (data == null || data.Count != 3) throw new InvalidDataException();
                                if (data["a"] != "b") throw new InvalidDataException();
                                if (data["c"] != "d") throw new InvalidDataException();
                                if (data["e"] != "f") throw new InvalidDataException();
                            }
                        }
                        catch (Exception ex)
                        {
                            Interlocked.CompareExchange(ref firstException, ex, null);
                            Interlocked.Increment(ref failures);
                        }
                    });
                }

                for (int j = 0; j < threads.Length; j++) threads[j].Start();
                for (int j = 0; j < threads.Length; j++) threads[j].Join();

                Assert.IsNull(firstException);
                Assert.AreEqual(0, Interlocked.CompareExchange(ref failures, 0, 0));

            }
        }

        [ProtoContract,ProtoInclude(1, typeof(B)), ProtoInclude(2, typeof(C))] class A { }
        [ProtoContract,ProtoInclude(1, typeof(D)), ProtoInclude(2, typeof(E))] class B : A { }
        [ProtoContract,ProtoInclude(1, typeof(F)), ProtoInclude(2, typeof(G))] class C : A { }
        [ProtoContract,ProtoInclude(1, typeof(H)), ProtoInclude(2, typeof(I))] class D : B { }
        [ProtoContract,ProtoInclude(1, typeof(J)), ProtoInclude(2, typeof(K))] class E : B { }
        [ProtoContract,ProtoInclude(1, typeof(L)), ProtoInclude(2, typeof(M))] class F : C { }
        [ProtoContract,ProtoInclude(1, typeof(N)), ProtoInclude(2, typeof(O))] class G : C { }
        [ProtoContract,ProtoInclude(1, typeof(P)), ProtoInclude(2, typeof(Q))] class H : D { }
        [ProtoContract,ProtoInclude(1, typeof(R)), ProtoInclude(2, typeof(S))] class I : D { }
        [ProtoContract,ProtoInclude(1, typeof(T)), ProtoInclude(2, typeof(U))] class J : E { }
        [ProtoContract,ProtoInclude(1, typeof(V)), ProtoInclude(2, typeof(W))] class K : E { }
        [ProtoContract,ProtoInclude(1, typeof(X)), ProtoInclude(2, typeof(Y))] class L : F { }
        [ProtoContract]class M : F { }
        [ProtoContract]class N : G { }
        [ProtoContract]class O : G { }
        [ProtoContract]class P : H { }
        [ProtoContract]class Q : H { }
        [ProtoContract]class R : I { }
        [ProtoContract]class S : I { }
        [ProtoContract]class T : J { }
        [ProtoContract]class U : J { }
        [ProtoContract]class V : K { }
        [ProtoContract]class W : K { }
        [ProtoContract]class X : L { }
        [ProtoContract, ProtoInclude(1, typeof(Y0))]class Y : L { }
        [ProtoContract, ProtoInclude(1, typeof(Y1))]class Y0 : Y { }
        [ProtoContract, ProtoInclude(1, typeof(Y2))]class Y1 : Y0 { }
        [ProtoContract, ProtoInclude(1, typeof(Y3))]class Y2 : Y1 { }
        [ProtoContract, ProtoInclude(1, typeof(Y4))]class Y3 : Y2 { }
        [ProtoContract, ProtoInclude(1, typeof(Y5))]class Y4 : Y3 { }
        [ProtoContract, ProtoInclude(1, typeof(Y6))]class Y5 : Y4 { }
        [ProtoContract, ProtoInclude(1, typeof(Y7))]class Y6 : Y5 { }
        [ProtoContract, ProtoInclude(1, typeof(Y8))]class Y7 : Y6 { }
        [ProtoContract, ProtoInclude(1, typeof(Y9))]class Y8 : Y7 { }
        [ProtoContract]class Y9 : Y8 { }

        [Test]
        public void TestDeserializeModelFromRoot()
        {
            byte[] raw;
            using (var ms = new MemoryStream())
            {
                var orig = new Y();
                var model = RuntimeTypeModel.Create();
                model.Serialize(ms, orig);
                raw = ms.ToArray();
            }

            for (int i = 0; i < 100; i++)
            {
                ManualResetEvent allGo = new ManualResetEvent(false);
                var model = TypeModel.Create();
                model.AutoCompile = true;
                object starter = new object();
                int waiting = 20;
                int failures = 0;
                Exception firstException = null;
                Thread[] threads = new Thread[20];
                for (int j = 0; j < 20; j++)
                {
                    threads[j] = new Thread(() =>
                    {
                        try
                        {
                            using (var ms = new MemoryStream(raw))
                            {
                                if (Interlocked.Decrement(ref waiting) == 0) allGo.Set();
                                else allGo.WaitOne();
                                object obj = model.Deserialize(ms, null, typeof(A));
                                if (obj.GetType() != typeof(Y)) throw new InvalidDataException("Should be a Y");
                            }
                        }
                        catch (Exception ex)
                        {
                            Interlocked.CompareExchange(ref firstException, ex, null);
                            Interlocked.Increment(ref failures);
                        }
                    });
                }

                for (int j = 0; j < threads.Length; j++) threads[j].Start();
                for (int j = 0; j < threads.Length; j++) threads[j].Join();

                Assert.IsNull(firstException);
                Assert.AreEqual(0, Interlocked.CompareExchange(ref failures, 0, 0));

            }
        }

        [Test]
        public void TestSerializeModelFromRoot()
        {
            byte[] raw;
            using (var ms = new MemoryStream())
            {
                var orig = new Y();
                var model = RuntimeTypeModel.Create();
                model.Serialize(ms, orig);
                raw = ms.ToArray();
            }

            for (int i = 0; i < 100; i++)
            {
                ManualResetEvent allGo = new ManualResetEvent(false);
                var model = TypeModel.Create();
                model.AutoCompile = true;
                object starter = new object();
                int waiting = 20;
                int failures = 0;
                Exception firstException = null;
                Thread[] threads = new Thread[20];
                for (int j = 0; j < 20; j++)
                {
                    threads[j] = new Thread(() =>
                    {
                        try
                        {
                            using (var ms = new MemoryStream())
                            {
                                if (Interlocked.Decrement(ref waiting) == 0) allGo.Set();
                                else allGo.WaitOne();
                                model.Serialize(ms, new Y());
                                if (!ms.ToArray().SequenceEqual(raw)) throw new InvalidDataException("Bad serialization; " + GetHex(raw) + " vs " + GetHex(ms.ToArray()));
                            }
                        }
                        catch (Exception ex)
                        {
                            Interlocked.CompareExchange(ref firstException, ex, null);
                            Interlocked.Increment(ref failures);
                        }
                    });
                }

                for (int j = 0; j < threads.Length; j++) threads[j].Start();
                for (int j = 0; j < threads.Length; j++) threads[j].Join();

                Assert.IsNull(firstException);
                Assert.AreEqual(0, Interlocked.CompareExchange(ref failures, 0, 0));

            }
        }
        string GetHex(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }


        [Test]
        public void TestSerializeModelFromLeaf()
        {
            var objs = new object[] {
                    new Y9(), new Y8(), new Y7(), new Y6(), new Y5(),
                    new Y4(), new Y3(), new Y2(), new Y1(), new Y0(),
                    new Y(), new L(), new F(), new C(), new A()
                };
            var expected = new string[objs.Length];
            for(int i = 0 ; i < expected.Length ; i++)
            {
                var model = RuntimeTypeModel.Create();
                using (var ms = new MemoryStream())
                {
                    model.Serialize(ms, objs[i], null);
                    expected[i] = GetHex(ms.ToArray());
                }
            }
            for (int i = 0; i < 250; i++)
            {
                
                ManualResetEvent allGo = new ManualResetEvent(false);
                var model = TypeModel.Create();
                model.AutoCompile = true;
                object starter = new object();
                int waiting = 20;
                int failures = 0;
                Exception firstException = null;
                Thread[] threads = new Thread[objs.Length * 3];
                for (int j = 0; j < threads.Length; j++)
                {
                    threads[j] = new Thread(oIndex =>
                    {  
                        try
                        {
                            object obj = objs[(int)oIndex];
                            string exp = expected[(int)oIndex];
                            using (var ms = new MemoryStream())
                            {
                                if (Interlocked.Decrement(ref waiting) == 0) allGo.Set();
                                else allGo.WaitOne();
                                model.Serialize(ms, obj, null);
                                string hex = GetHex(ms.ToArray());
                                if (hex != exp) throw new InvalidDataException("Bad serialization; " + hex + " vs " + exp);
                            }
                        }
                        catch (Exception ex)
                        {
                            Interlocked.CompareExchange(ref firstException, ex, null);
                            Interlocked.Increment(ref failures);
                        }
                    });
                }

                for (int j = 0; j < threads.Length; j++) threads[j].Start(j % objs.Length);
                for (int j = 0; j < threads.Length; j++) threads[j].Join();

                Assert.IsNull(firstException);
                Assert.AreEqual(0, Interlocked.CompareExchange(ref failures, 0, 0));

            }
        }

    }
}
