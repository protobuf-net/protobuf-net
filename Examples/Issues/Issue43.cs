using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.ServiceModel;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue43
    {
        [ProtoContract]
        public class ProtoClass
        {
            [ProtoMember(1)]
            [IgnoreDataMember]
            public string String1 { get; set; }
        }

        [DataContract]
        public class NonProtoClass
        {
            [DataMember]
            public string String1 { get; set; }
        }

        [DataContract, ProtoContract]
        public class CompatibleClass
        {
            [DataMember, ProtoMember(1)]
            public string String1 { get; set; }
        }

        private const string RequestString = "request";
        private const string ReplyString = "reply";

        public class MyService : IMyService
        {
            public ProtoClass ProtoOp(ProtoClass value)
            {
                return new ProtoClass() { String1 = ReplyString };
            }

            public NonProtoClass NonProtoOp(NonProtoClass value)
            {
                return new NonProtoClass() { String1 = ReplyString };
            }

            public CompatibleClass CompatibleOp(CompatibleClass value)
            {
                return new CompatibleClass() { String1 = ReplyString };
            }
        }

        [ServiceContract, ProtoContract]
        public interface IMyService
        {
            [OperationContract]
            ProtoClass ProtoOp(ProtoClass value);

            [OperationContract]
            NonProtoClass NonProtoOp(NonProtoClass value);

            [OperationContract]
            CompatibleClass CompatibleOp(CompatibleClass value);
        }

        private ServiceHost host;
        private const string Hostname = "localhost";
        private const string ServiceAddress = "net.tcp://" + Hostname + ":89/MyService";
        private const string ProtoAddress = "net.tcp://" + Hostname + ":89/MyService/svc/proto";
        private const string NonProtoAddress = "net.tcp://" + Hostname + ":89/MyService/svc";

        [TestFixtureSetUp]
        public void StartServer()
        {
            try
            {
                StopServer();
                host = new ServiceHost(typeof(MyService),
                    new Uri(ServiceAddress));

                // to recreate the circumstances of issue43, the proto endpoint must be added first
                host.AddServiceEndpoint(typeof(IMyService), new NetTcpBinding(SecurityMode.None),
                    ProtoAddress).Behaviors.Add(
                        new ProtoEndpointBehavior());

                host.AddServiceEndpoint(typeof(IMyService), new NetTcpBinding(SecurityMode.None),
                    NonProtoAddress);

                host.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetType().FullName);
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        [TestFixtureTearDown]
        public void StopServer()
        {
            if (host != null)
            {
                host.Close();
                host = null;
            }
        }

        private IMyService GetProtoClient()
        {
            var endpoint =
                new ServiceEndpoint(
                ContractDescription.GetContract(typeof(IMyService)), new NetTcpBinding(SecurityMode.None),
                new EndpointAddress(ProtoAddress));

            endpoint.Behaviors.Add(new ProtoEndpointBehavior());
            return new ChannelFactory<IMyService>(endpoint).CreateChannel();
        }

        private IMyService GetDcsClient()
        {
            var endpoint =
                new ServiceEndpoint(
                ContractDescription.GetContract(typeof(IMyService)), new NetTcpBinding(SecurityMode.None),
                new EndpointAddress(NonProtoAddress));

            return new ChannelFactory<IMyService>(endpoint).CreateChannel();
        }

        // the NonProto* tests are the assertions that fail for issue43
        // the Proto* tests should always pass, and ensure nothing has broken post refactor
        [Test]
        public void NonProtoEndpoint_SerializesDtoClass()
        {
            var client = this.GetDcsClient();
            var response = client.NonProtoOp(new NonProtoClass() { String1 = RequestString });
            Assert.IsNotNull(response, "response should not be null");
            Assert.AreEqual(ReplyString, response.String1, "result should serialize correctly");
        }

        [Test]
        public void NonProtoEndpoint_DoesNotSerializeProtoClass()
        {
            var client = this.GetDcsClient();
            var response = client.ProtoOp(new ProtoClass() { String1 = RequestString });
            Assert.IsNotNull(response, "response should not be null");
            Assert.AreEqual(null, response.String1, "result should not serialize");
        }

        [Test]
        public void NonProtoEndpoint_SerializesCompatibleClass()
        {
            var client = this.GetDcsClient();
            var response = client.CompatibleOp(new CompatibleClass() { String1 = RequestString });
            Assert.IsNotNull(response, "response should not be null");
            Assert.AreEqual(ReplyString, response.String1, "result should serialize correctly");
        }

        [Test]
        public void ProtoEndpoint_DoesNotSerializeDtoClass()
        {
            var client = this.GetProtoClient();
            var response = client.NonProtoOp(new NonProtoClass() { String1 = RequestString });
            Assert.IsNotNull(response, "response should not be null");
            Assert.AreEqual(null, response.String1, "result should not serialize");
        }

        [Test]
        public void ProtoEndpoint_SerializesibleCompatibleClass()
        {
            var client = this.GetProtoClient();
            var response = client.CompatibleOp(new CompatibleClass() { String1 = RequestString });
            Assert.IsNotNull(response, "response should not be null");
            Assert.AreEqual(ReplyString, response.String1, "result should serialize correctly");
        }

        [Test]
        public void ProtoEndpoint_SerializesProtoClass()
        {
            var client = this.GetProtoClient();
            var response = client.ProtoOp(new ProtoClass() { String1 = RequestString });
            Assert.IsNotNull(response, "response should not be null");
            Assert.AreEqual(ReplyString, response.String1, "result should serialize correctly");
        }
    }
}
