using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestNS;

class Program
{
    class Greeter : IGreeter
    {
        public global::System.Threading.Tasks.ValueTask<HelloReply> SayHelloAsync(HelloRequest value, global::ProtoBuf.Grpc.CallContext context = default)
        {
            throw new NotImplementedException();
        }
    }

    static void Main()
    {
        var greeter = new Greeter();
    }
}
