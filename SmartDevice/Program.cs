

using ProtoBuf;
using System.IO;
using System;
using System.Diagnostics;
using System.Text;
namespace SmartDevice
{
    [ProtoContract]
    public sealed class Test1
    {
        [ProtoMember(1, Name = "a", IsRequired = true, DataFormat = DataFormat.TwosComplement)]
        public int A { get; set; }
    }
    [ProtoContract]
    public sealed class Test2
    {
        [ProtoMember(2, Name = "b", IsRequired = true)]
        public string B { get; set; }
    }
    [ProtoContract]
    public sealed class Test3
    {
        [ProtoMember(3, Name = "c", IsRequired = true)]
        public Test1 C { get; set; }
    }
    class Program
    {
        static void Main()
        {
            Test1 a;
            WriteObject(a = new Test1 { A = 150 });
            WriteObject(new Test2 { B = "testing" });
            WriteObject(new Test3 { C = a });
            Console.WriteLine("## End ##");
            Console.ReadLine();

        }
        static void WriteObject<T>(T instance) where T : class, new()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, instance);
                byte[] buffer = ms.ToArray();
                StringBuilder sb = new StringBuilder();
                foreach (byte b in buffer)
                {
                    sb.Append(" 0x").Append(b.ToString("X2"));
                }
                Debug.WriteLine(sb, instance.GetType().Name);
            }
        }
    }
}
