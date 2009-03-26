using System;
using System.Linq;
using System.ServiceModel;
using DAL;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.ServiceModel.Client;
using ProtoBuf.ServiceModel.Server;

namespace Examples.Rpc
{
    [ProtoContract]
    class TestRequest {
        [ProtoMember(1)] public int RequestBody { get; set; }
    }
    [ProtoContract] class TestResponse {
        [ProtoMember(1)] public int ResponseBody { get; set; }
    }
    interface IBasicService
    {
        [OperationContract(Action="Foo")]
        DAL.Database TestMethod(DAL.Database request);

        void Ping();

        int Test(int inOnly, ref int inOut, out int outOnly);
    }
    class BasicService : IBasicService
    {
        public DAL.Database TestMethod(DAL.Database request)
        {
            return request;
        }
        public void Ping() { }

        public int Test(int inOnly, ref int inOut, out int outOnly)
        {
            outOnly = inOut;
            inOut = inOnly;
            return outOnly + inOut;
        }
    }

    class BasicServiceHttpClient : ProtoClient<IBasicService>, IBasicService
    {
        public BasicServiceHttpClient() : base(HttpBasic.ViaHttp()) { }
        public void Ping() { }
        public void TestMethod(DAL.Database request, Action<DAL.Database> callback)
        {
            InvokeAsync("TestMethod", delegate(AsyncResult result) { callback((DAL.Database)result()); }, request);
        }
        public DAL.Database TestMethod(DAL.Database request)
        {
            return (DAL.Database) Invoke("TestMethod", request);
        }
        public int Test(int inOnly, ref int inOut, out int outOnly)
        {
            outOnly = inOut;
            inOut = inOnly;
            return outOnly + inOut;
        }
    }

    [TestFixture]
    public class HttpWithLambda
    {
        private HttpServer server;
        [SetUp]
        public void StartServer()
        {
            StopServer();
            server = new HttpServer(HTTP_PREFIX);
            server.Add<IBasicService>(new BasicService());
            server.Start();
        }

        [TearDown]
        public void StopServer()
        {
            if (server != null)
            {
                try { server.Close(); }
                catch { }
                server = null;
            }
        }


        [Test]
        public void TestPing() {
            using (var client = CreateClient())
            {
                client.Invoke(svc => svc.Ping());
            }
        }

        [Test]
        public void TestSwapArgs()
        {
            using (var client = CreateClient())
            {
                int i = 13, j = 0;
                int sum = client.Invoke(svc => svc.Test(27, ref i, out j));
                Assert.AreEqual(27, i);
                Assert.AreEqual(13, j);
                Assert.AreEqual(40, sum);
            }
        }

        const string HTTP_PREFIX = "http://localhost:8080/myapp/";
        static ProtoClient<IBasicService> CreateClient()
        {
            return new ProtoClient<IBasicService>(new HttpBasicTransport(HTTP_PREFIX));
        }
    }

    [TestFixture]
    public class HttpBasic
    {
        const string HTTP_PREFIX = "http://localhost:8080/myapp/";
        internal static ITransport ViaHttp() {
            return new HttpBasicTransport(HTTP_PREFIX);
        }
        static ProtoClient<T> ClientViaHttp<T>() where T : class {
            return new ProtoClient<T>(
                ViaHttp());
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestNullTransport()
        {
            new ProtoClient<IBasicService>(null);
        }
        class NotAContract { }
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestNotAContract() {
            new ProtoClient<NotAContract>(ViaHttp());
        }

        [Test]
        public void TestCreationAndDispoesWithBoth() {
           using(ClientViaHttp<IBasicService>()) {}
        }

        [Test]
        public void TestCreateDisposeClient()
        {
            using (var client = new BasicServiceHttpClient()) { }
        }

        [Test]
        public void StartStopServer()
        {
            using (var server = CreateServer())
            {
                server.Start();
                server.Close();
            }
        }
        static HttpServer CreateServer() { return CreateServer(new BasicService()); }
        static HttpServer CreateServer(IBasicService service)
        {
            HttpServer server = new HttpServer(HTTP_PREFIX);
            server.Add<IBasicService>(service);
            return server;
        }

        [Test]
        public void TestCallTestMethodWithNull()
        {
            using (var server = CreateServer())
            using (var client = new BasicServiceHttpClient())
            {
                server.Start();
                var result = client.TestMethod(null);
                Assert.IsNull(result);
            }
        }

        [Test]
        public void TestCallTestMethodWithDatabase()
        {
            using (var server = CreateServer())
            using (var client = new BasicServiceHttpClient())
            {
                server.Start();
                DAL.Database request = NWindTests.LoadDatabaseFromFile<DAL.Database>();
                DAL.Database response = client.TestMethod(request);

                Assert.IsNotNull(response);
                Assert.AreNotSame(request, response);

                Assert.AreEqual(request.Orders.Count, response.Orders.Count, "Orders");
                Assert.AreEqual(
                    request.Orders.SelectMany(ord => ord.Lines).Count(),
                    response.Orders.SelectMany(ord => ord.Lines).Count(), "Lines");
                Assert.AreEqual(
                    request.Orders.SelectMany(ord => ord.Lines).Sum(line => line.Quantity),
                    response.Orders.SelectMany(ord => ord.Lines).Sum(line => line.Quantity), "Quantity");
                Assert.AreEqual(
                    request.Orders.SelectMany(ord => ord.Lines).Sum(line => line.Quantity * line.UnitPrice),
                    response.Orders.SelectMany(ord => ord.Lines).Sum(line => line.Quantity * line.UnitPrice), "Value");

            }
        }

    }
}
