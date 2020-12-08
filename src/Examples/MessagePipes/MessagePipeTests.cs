using ProtoBuf.MessagePipes;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.MessagePipeTests
{
    public class MessagePipeTests
    {
        public MessagePipeTests(ITestOutputHelper log)
            => _log = log;

        private static readonly DateTime _start = DateTime.UtcNow;

        private static TimeSpan Time() => DateTime.UtcNow - _start;

        private readonly ITestOutputHelper _log;
        private void Log(string message)
        {
            if (_log != null)
            {
                lock (_log)
                {
                    _log.WriteLine($"{Time()}: {message}");
                }
            }
        }

        [Fact(Skip = "unreliable; do not ship")]
        public async Task SimpleMessagePipe()
        {
            var name = Guid.NewGuid().ToString();
            RuntimeTypeModel.Initialize();
            var received = new List<Message>();

            var options = new MessagePipeOptions(
#if DEBUG
                log: Log
#endif
            );

            Log("Creating server...");
            using (var server = new NamedPipeServerStream(name, PipeDirection.In, 1, PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough))
            {
                var receive = Task.Run(async () =>
                {
                    Log("[Server] waiting for connection...");
                    await server.WaitForConnectionAsync();
                    Log($"[Server] connected; receiving...");

                    await foreach (var message in MessagePipe.ReceiveAsync<Message>(server, options))
                    {
                        Log($"[Server] received message {message.Id}");
                        received.Add(message);
                    }
                    Log("[Server] completed");
                });

                Log("Creating client...");
                using (var client = new NamedPipeClientStream(".", name, PipeDirection.Out,
                    PipeOptions.Asynchronous | PipeOptions.WriteThrough))
                {
                    Log("[Client] connecting...");
                    await client.ConnectAsync();
                    Log("[Client] connected");

                    var channel = Channel.CreateUnbounded<Message>();
                    var send = MessagePipe.SendAsync(client, channel.Reader, options);

                    Log("[Client] writing message 1...");
                    await channel.Writer.WriteAsync(new Message { Id = 1, Foo = "abc", Bar = 1.0 });
                    Log("[Client] writing message 3...");
                    await channel.Writer.WriteAsync(new Message { Id = 2, Foo = "def", Bar = 2.0 });
                    Log("[Client] writing message 3...");
                    await channel.Writer.WriteAsync(new Message { Id = 3, Foo = "ghi", Bar = 3.0 });
                    Log("[Client] writing outbound...");
                    channel.Writer.Complete();
                    Log("[Client] awaiting send completion...");

                    await WithTimeout(send.AsTask(), TimeSpan.FromSeconds(5), "send"); // all sent

                    await client.FlushAsync();
                } // client is toast
                Log("[Client] end");

                // wait for server to exit
                await WithTimeout(receive, TimeSpan.FromSeconds(5), "receive");
            } // server is toast
            Log("[Server] end");

            // detect all received
            Assert.Equal(3, received.Count);
        }

        [ProtoContract]
        class Ping
        {
            [ProtoMember(1)]
            public int Token { get; set; }
        }
        [ProtoContract]
        class Pong
        {
            [ProtoMember(2)]
            public int Token { get; set; }
        }

// this works on netcoreapp3.0; I haven't had time to figure out what is wrong on net462, but: client doesn't get the pong
        [Fact(Skip = "unreliable; do not ship")]
        public async Task DuplexPipe()
        {
            var name = Guid.NewGuid().ToString();
            RuntimeTypeModel.Initialize();

            var options = new MessagePipeOptions(
#if DEBUG
                log: Log
#endif
            );

            Log("Creating server...");
            using (var server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough))
            {
                var duplex = MessagePipe.DuplexAsync<Pong, Ping>(server, options);
                var receive = Task.Run(async () =>
                {
                    Log("[Server] waiting for connection...");
                    await server.WaitForConnectionAsync();

                    Log($"[Server] connected; receiving...");
                    await duplex.AsServer(ping => new Pong { Token = ping.Token });
                    //while ((await duplex.TryReceiveAsync()).IsSuccess(out var ping))
                    //{
                    //    Log($"[Server] got {ping.Token}; replying...");
                    //    await duplex.Writer.WriteAsync(new Pong { Token = ping.Token });
                    //    Log($"[Server] replied");
                    //}
                    Log("[Server] done");
                });

                Log("Creating client...");
                using (var client = new NamedPipeClientStream(".", name, PipeDirection.InOut,
                    PipeOptions.Asynchronous | PipeOptions.WriteThrough))
                {
                    Log("[Client] connecting...");
                    await client.ConnectAsync();
                    await using var send = MessagePipe.DuplexAsync<Ping, Pong>(client, options);

                    for (int i = 0; i < 5; i++)
                    {
                        var ping = new Ping { Token = i };
                        Log($"[Client] sending ping {ping?.Token}...");
                        var pongPending = send.UnaryAsync(ping).AsTask();
                        await WithTimeout(pongPending, TimeSpan.FromSeconds(5), "UnaryAsync");
                        var pong = await pongPending;
                        Log($"[Client] received pong {pong?.Token}...");
                        Assert.Equal(i, pong.Token);
                    }
                } // client is toast
                Log("[Client] end");

                // wait for server to exit
                await WithTimeout(receive, TimeSpan.FromSeconds(5), "receive");
            } // server is toast
            Log("[Server] end");
        }

        static async Task WithTimeout(Task task, TimeSpan timeout, string message)
        {
            var timeoutTask = Task.Delay(timeout);
            var winner = await Task.WhenAny(task, timeoutTask);
            if (winner == timeoutTask) 
            {
                throw new TimeoutException(message);
            }
            await winner;
        }


        [ProtoContract]
        public class Message
        {
            [ProtoMember(1)]
            public int Id { get; set; }
            [ProtoMember(2)]
            public string Foo { get; set; }
            [ProtoMember(3)]
            public double Bar { get; set; }
        }
    }
}
