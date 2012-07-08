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
            model.Load(@"C:\Dev\protobuf-net\MetroDto\bin\x86\Debug\MetroDto");
            model.Add("DAL.DatabaseCompat, MetroDto", false);
            
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
