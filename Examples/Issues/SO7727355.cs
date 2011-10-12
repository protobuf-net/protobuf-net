using System;
using System.Data;
using System.IO;
using System.Threading;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class SO7727355 // see http://stackoverflow.com/questions/7727355/no-parameterless-constructor-found
    {                      // and http://www.filejumbo.com/Download/899EB797CE084C7F
        [ProtoContract]
        public sealed class Web2PdfTest : WebEntityTest
        {
            [ProtoMember(1)]
            public string Prop1 { get; set; }
        }

        [ProtoContract, ProtoInclude(20, typeof(Web2PdfTest))]
        public abstract class WebEntityTest : EntityBaseTest
        {
            [ProtoMember(2)]
            public string Prop2 { get; set; }
        }

        [ProtoContract, ProtoInclude(10, typeof(WebEntityTest))]
        public abstract class EntityBaseTest
        {
            [ProtoMember(3)]
            public string Prop3 { get; set; }
        }

        private int success;
        private Exception firstException;
        private ManualResetEvent gate = new ManualResetEvent(false);
        private int waitingThreads;
        [Test]
        public void Execute()
        {
            for(int test = 0 ; test < 1000 ; test++)
            {
                var model = RuntimeTypeModel.Create();
                Thread[] threads = new Thread[20];

                Interlocked.Exchange(ref success, 0);
                Interlocked.Exchange(ref firstException, null);
                Interlocked.Exchange(ref waitingThreads, threads.Length);

                gate.Reset();
                for (int i = 0; i < threads.Length; i++ )
                    threads[i] = new Thread(PistonThread);
                for (int i = 0 ; i < threads.Length; i++)
                    threads[i].Start(model);

                for (int i = 0; i < threads.Length; i++)
                    if (!threads[i].Join(5000)) throw new TimeoutException();

                Assert.AreEqual(20, Interlocked.CompareExchange(ref success, 0, 0));
                var exVal = Interlocked.CompareExchange(ref firstException, null, null);
                if (exVal != null) throw firstException;
            }
        }

        public void PistonThread(object state)
        {
            try
            {
                var model = (TypeModel) state;
                Web2PdfTest clone,
                            web2PdfTestEntity = new Web2PdfTest
                            {Prop1 = "1234567", Prop2 = "28383847474", Prop3 = "83837272626"};

                if (Interlocked.Decrement(ref waitingThreads) == 0) gate.Set();
                else gate.WaitOne();

                using (var stream = new MemoryStream())
                {
                    model.Serialize(stream, web2PdfTestEntity);
                    stream.Position = 0;
                    clone = (Web2PdfTest) model.Deserialize(stream, null, typeof (Web2PdfTest));
                }
                if (clone.Prop1 != web2PdfTestEntity.Prop1) throw new DataException("Prop1");
                if (clone.Prop2 != web2PdfTestEntity.Prop2) throw new DataException("Prop2");
                if (clone.Prop3 != web2PdfTestEntity.Prop3) throw new DataException("Prop3");
                Interlocked.Increment(ref success);
            } catch (Exception ex)
            {
                Interlocked.CompareExchange(ref firstException, ex, null);
            }
        }
    }
}
