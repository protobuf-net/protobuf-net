using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using NUnit.Framework;
using ProtoBuf.ServiceModel;
using System.Net.Sockets;
using System.Runtime.Serialization;

#if DEBUG
namespace Examples
{
    interface IFoo
    {
        void Bar(string name);

        Customer MultiArg(int id, Customer cust);
    }
    [DataContract]
    class Customer
    {
        [DataMember(Order=1)]
        public string Name { get; set;}   
    }
    [TestFixture]
    public class Rpc
    {
        [Test]
        public void TestRpc()
        {
            TcpListener server = new TcpListener(IPAddress.Loopback, 8999);
            server.Start();
            server.BeginAcceptTcpClient(ClientConnected, server);

            using (RpcClient client = new RpcClient(typeof (IFoo)))
            {
                client.Open(new IPEndPoint(IPAddress.Loopback, 8999));
                client.Send("Bar", "abc");
                
                Customer cust = new Customer {Name = "Frodo"};
                client.Send("MultiArg", 5, cust);
            }
        }

        static void ClientConnected(IAsyncResult result)
        {
            TcpListener server = (TcpListener)result.AsyncState;
            using (TcpClient client = server.EndAcceptTcpClient(result))
            using (NetworkStream stream = client.GetStream())
            {
                Console.WriteLine("I have no clue whether this is right...");
                int b;
                while ((b = stream.ReadByte()) >= 0)
                {
                    Console.Write(Convert.ToString(b, 16).PadLeft(2, '0'));
                }
            }
            server.Stop();
        }
    }
}
#endif