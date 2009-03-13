using NUnit.Framework;
using ProtoBuf.ServiceModel;
using ProtoBuf;
using System;
using ProtoBuf.ServiceModel.Client;
using ProtoBuf.ServiceModel.Server;
using System.ServiceModel;
using DAL;

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

    }
    class BasicService : IBasicService
    {
        public DAL.Database TestMethod(DAL.Database request)
        {
            return request;
        }
    }
    
    class BasicServiceHttpClient : ProtoClient, IBasicService
    {
        public BasicServiceHttpClient() : base(HttpBasic.ViaHttp(), typeof(IBasicService)) { }

        public void TestMethod(DAL.Database request, Action<DAL.Database> callback)
        {
            SendRequestAsync("TestMethod", request, callback);
        }
        public DAL.Database TestMethod(DAL.Database request)
        {
            return SendRequestSync<DAL.Database, DAL.Database>("TestMethod", request);
        }
    }

    [TestFixture]
    public class HttpBasic
    {
        const string HTTP_PREFIX = "http://localhost/myapp/";
        internal static ITransport ViaHttp() {
            return new HttpBasicTransport(HTTP_PREFIX);
        }
        static ProtoClient ClientViaHttp<T>() where T : class {
            return new ProtoClient(
                ViaHttp(),
                typeof(T));
        }
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestNullTransport()
        {
            new ProtoClient(null, typeof(IBasicService));
        }
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestNullContract() {
            new ProtoClient(ViaHttp(), null);
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
            return new HttpServer(HTTP_PREFIX, typeof(IBasicService), service);
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestCallTestMethodWithNull()
        {
            using (var server = CreateServer())
            using (var client = new BasicServiceHttpClient())
            {
                server.Start();
                client.TestMethod(null);
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

            }
        }

    }
}
