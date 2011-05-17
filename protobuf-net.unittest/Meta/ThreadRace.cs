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
    }
}
