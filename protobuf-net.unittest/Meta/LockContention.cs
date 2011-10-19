using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ProtoBuf.Meta;
using System.Diagnostics;

namespace ProtoBuf.unittest.Meta
{
    [TestFixture]
    public class LockContention
    {
        [Test]
        public void DeliberatelyCausedContentionShouldShow()
        {
            var model = RuntimeTypeModel.Create();
            model.MetadataTimeoutMilliseconds = 400;
            string eek = null;
            model.LockContended += (s, a) => eek = a.OwnerStackTrace;
            ManualResetEvent workerReady = new ManualResetEvent(false), workerComplete = new ManualResetEvent(false), mainComplete = new ManualResetEvent(false);
            ThreadPool.QueueUserWorkItem(delegate
            {
                int opaqueToken = 0;
                try
                {
                    model.TakeLock(ref opaqueToken);
                    workerReady.Set();
                    if (!mainComplete.WaitOne(5000)) throw new TimeoutException();
                }
                finally
                {
                    model.ReleaseLock(opaqueToken);
                    workerComplete.Set();
                }
            });
            if (!workerReady.WaitOne(5000)) throw new TimeoutException();
            try
            {
                model[typeof (ThreadRace.A)].CompileInPlace();
                Assert.Fail("Should have timed out");
            } catch (TimeoutException)
            {
                Debug.WriteLine("Expected timeout occurred");
            }
            mainComplete.Set();
            if (!workerComplete.WaitOne(5000)) throw new TimeoutException();
            Assert.IsNotNull(eek);
        }

        [Test]
        public void SingleModelShouldNotContent()
        {
            var model = RuntimeTypeModel.Create();
            string eek = null;
            int eekCount = 0;
            model.LockContended += (s, a) => { eek = a.OwnerStackTrace;
                                                 Interlocked.Increment(ref eekCount);
            };
            model[typeof(ThreadRace.A)].CompileInPlace();
            Assert.IsNull(eek);
            Assert.That(Interlocked.CompareExchange(ref eekCount, 0, 0), Is.EqualTo(0));
        }


        [Test]
        public void MultipleDeserializeCallsShouldNotContend()
        {
            var data = new ThreadRace.Y9();
            byte[] raw;
            using (var ms = new MemoryStream())
            {
                RuntimeTypeModel.Create().Serialize(ms, data);
                raw = ms.ToArray();
                Assert.That(raw.Length, Is.GreaterThan(0));
            }

            const int threads = 10, loop = 500;

            for (int loopIndex = 0; loopIndex < loop; loopIndex++)
            {
                int building = threads, error = 0;
                Thread[] threadArr = new Thread[threads];
                ManualResetEvent evt = new ManualResetEvent(false);
                var model = RuntimeTypeModel.Create();
                string eek = null;
                int eekCount = 0;
                model.LockContended += (s, a) => { eek = a.OwnerStackTrace;
                                                     Interlocked.Increment(ref eekCount);
                };
                for (int i = 0; i < threads; i++)
                {
                    threadArr[i] = new Thread(() =>
                    {
                        try
                        {
                            using (var ms = new MemoryStream(raw))
                            {
                                if (Interlocked.Decrement(ref building) == 0)
                                {
                                    evt.Set();
                                }
                                else
                                {
                                    evt.WaitOne();
                                }
                                ThreadRace.A a =
                                    (ThreadRace.A)model.Deserialize(ms, null, typeof(ThreadRace.A));
                                Assert.That(a, Is.InstanceOfType(typeof(ThreadRace.Y9)));
                            }

                        }
                        catch
                        {
                            Interlocked.Increment(ref error);
                        }

                    });
                }
                foreach (var thd in threadArr) thd.Start();
                foreach (var thd in threadArr)
                {
                    if (!thd.Join(4000)) throw new TimeoutException();
                }
                Assert.That(Interlocked.CompareExchange(ref error, 0, 0), Is.EqualTo(0));
                Assert.That(eek, Is.Null);
                Assert.That(Interlocked.CompareExchange(ref eekCount, 0, 0), Is.EqualTo(0));
            }
        }
    }
}
