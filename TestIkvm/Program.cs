using System;
using ProtoBuf;
using ProtoBuf.Meta;
using Examples;

namespace TestIkvm
{
    class Program
    {
        static void Main()
        {
            var model = TypeModel.Create();
            model.Add(typeof (Foo), true);
            model.Compile("ViaIKVM", "ViaIKVM.dll");
            if(!PEVerify.AssertValid("ViaIKVM.dll"))
            {
                Console.WriteLine("epic fail");
            }
            Console.WriteLine("done");
            Console.ReadKey();
        }
    }

    [ProtoContract]
    public class Foo
    {
        [ProtoMember(1)]
        public int Bar { get; set; }
    }
}
