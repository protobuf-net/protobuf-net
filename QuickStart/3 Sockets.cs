
using System.Net.Sockets;
using System;
using ProtoBuf;
using System.Net;
using System.Threading;
namespace QuickStart
{
    static class Sockets
    {

        const int PORT = 12345;

        /// <summary>
        /// Demonstrates cor sockets functionality for sending data;
        /// RPC is not covered by this sample.
        /// Note that this example should not be taken as best practice
        /// for working with sockets more generally - it is simply
        /// intended to illustrate basic reading/writing to/from a socket.
        /// </summary>
        internal static void ShowSockets()
        {
            TcpListener server = new TcpListener(IPAddress.Loopback, PORT);
            server.Start();
            server.BeginAcceptTcpClient(ClientConnected, server);
            Console.WriteLine("SERVER: Waiting for client...");

            ThreadPool.QueueUserWorkItem(RunClient);
            allDone.WaitOne();
            Console.WriteLine("SERVER: Exiting...");
            server.Stop();
            
            
        }
        static ManualResetEvent allDone = new ManualResetEvent(false);

        static void ClientConnected(IAsyncResult result) {
            try
            {
                TcpListener server = (TcpListener)result.AsyncState;
                using (TcpClient client = server.EndAcceptTcpClient(result))
                using (NetworkStream stream = client.GetStream())
                {
                    Console.WriteLine("SERVER: Client connected; reading customer");
                    Customer cust = Serializer.DeserializeWithLengthPrefix<Customer>(stream);
                    Console.WriteLine("SERVER: Got customer:");
                    cust.ShowCustomer();
                    cust.Name += " (from server)";
                    cust.Contacts.Add(new Contact { Name = "Server", ContactDetails = Environment.MachineName });
                    Console.WriteLine("SERVER: Returning updated customer:");
                    Serializer.SerializeWithLengthPrefix(stream, cust);

                    int final = stream.ReadByte();
                    if (final == 123)
                    {
                        Console.WriteLine("SERVER: Got client-happy marker");
                    }
                    else
                    {
                        Console.WriteLine("SERVER: OOPS! Something went wrong");
                    }
                    Console.WriteLine("SERVER: Closing connection...");
                    stream.Close();
                    client.Close();
                }
            }
            finally
            {
                allDone.Set();
            }

        }

        static void RunClient(object state)
        {
            Customer cust = Customer.Invent();
            Console.WriteLine("CLIENT: Opening connection...");
            using (TcpClient client = new TcpClient())
            {
                client.Connect(new IPEndPoint(IPAddress.Loopback, PORT));
                using (NetworkStream stream = client.GetStream())
                {
                    Console.WriteLine("CLIENT: Got connection; sending data...");
                    Serializer.SerializeWithLengthPrefix(stream, cust);
                    
                    Console.WriteLine("CLIENT: Attempting to read data...");
                    Customer newCust = Serializer.DeserializeWithLengthPrefix<Customer>(stream);
                    Console.WriteLine("CLIENT: Got customer:");
                    newCust.ShowCustomer();

                    Console.WriteLine("CLIENT: Sending happy...");
                    stream.WriteByte(123); // just to show all bidirectional comms are OK
                    Console.WriteLine("CLIENT: Closing...");
                    stream.Close();
                }
                client.Close();
            }
        }
    }
}
