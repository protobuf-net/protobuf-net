#if !NO_INTERNAL_CONTEXT
using ProtoBuf.Meta;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Xunit;

namespace ProtoBuf.unittest.Meta
{

    public class LockContention
    {
        [Fact]
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
                Assert.Equal("oops", "Should have timed out");
            } catch (TimeoutException)
            {
                Debug.WriteLine("Expected timeout occurred");
            }
            mainComplete.Set();
            if (!workerComplete.WaitOne(5000)) throw new TimeoutException();
            Assert.NotNull(eek);
        }

        [Fact]
        public void SingleModelShouldNotContent()
        {
            var model = RuntimeTypeModel.Create();
            string eek = null;
            int eekCount = 0;
            model.LockContended += (s, a) => { eek = a.OwnerStackTrace;
                                                 Interlocked.Increment(ref eekCount);
            };
            model[typeof(ThreadRace.A)].CompileInPlace();
            Assert.Null(eek);
            Assert.Equal(0, Interlocked.CompareExchange(ref eekCount, 0, 0));
        }


        [Fact]
        public void MultipleDeserializeCallsShouldNotContend()
        {
            var data = new ThreadRace.Y9();
            byte[] raw;
            using (var ms = new MemoryStream())
            {
                RuntimeTypeModel.Create().Serialize(ms, data);
                raw = ms.ToArray();
                Assert.True(raw.Length > 0);
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
                                Assert.IsType(typeof(ThreadRace.Y9), a);
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
                Assert.Equal(0, Interlocked.CompareExchange(ref error, 0, 0));
                Assert.Null(eek);
                Assert.Equal(0, Interlocked.CompareExchange(ref eekCount, 0, 0));
            }
        }
    }
}
#endif