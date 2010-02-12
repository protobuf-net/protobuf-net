
using ProtoBuf.Meta;
using System.IO;
using System;
using ProtoBuf;
namespace FX11
{
    public class Customer
    {
        private int id;
        public int Id { get { return id; } set { id = value; } }
        public string Name;
    }
    public struct CustomerStruct
    {
        private int id;
        public int Id { get { return id; } set { id = value; } }
        public string Name;        
    }
    public class FX11_Program
    {
        public static void Serialize(int i, object obj, ProtoWriter writer)
        {
            switch (i)
            {
                case 1:
                    Serialize((CustomerStruct)obj, writer); return;
            }
        }
        public static void Serialize(CustomerStruct obj, ProtoWriter writer)
        {
            CustomerStruct cs = obj;
            int id = cs.Id;
            writer.WriteFieldHeader(1, WireType.Variant);
            writer.WriteInt32(id);
            string s = cs.Name;
            if (s != null)
            {
                writer.WriteFieldHeader(2, WireType.String);
                writer.WriteString(cs.Name);
            }
        }
        public static RuntimeTypeModel BuildMeta()
        {
            RuntimeTypeModel model = TypeModel.Create("CustomerModel");
            model.Add(typeof(Customer), false)
               .Add(1, "Id")
               .Add(2, "Name");
            model.Add(typeof(CustomerStruct), false)
                .Add(1, "Id")
                .Add(2, "Name");
            return model;
        }
        public static void Main()
        {
            WriteHeading(".NET version");
            Console.WriteLine(Environment.Version);
            
            RuntimeTypeModel model = BuildMeta();
            Customer cust1 = new Customer();
            CustomerStruct cust2 = new CustomerStruct();
            cust2.Id = cust1.Id = 123;
            cust2.Name = cust1.Name = "Fred";
            
            WriteCustomer(model, "Runtime - class", cust1);
            WriteCustomer(model, "Runtime - struct", cust2);

            TypeModel compiled = model.Compile("CustomerModel.dll");
            WriteCustomer(compiled, "Compiled - class", cust2);
            WriteCustomer(compiled, "Compiled - struct", cust2);
        }
        static void Assign(ref CustomerStruct cs)
        {
            cs.Id = 123;
        }
        static void WriteHeading(string caption)
        {
            Console.WriteLine();
            Console.WriteLine(caption);
            Console.WriteLine(new string('=', caption.Length));
            Console.WriteLine();
        }
        
        private static void WriteCustomer(TypeModel model, string caption, object obj)
        {
            WriteHeading(caption);
            byte[] blob;
            using (MemoryStream ms = new MemoryStream())
            {
                model.Serialize(ms, obj);
                blob = ms.ToArray();
            }
            foreach (byte b in blob)
            {
                Console.Write(b.ToString("x2"));
            }
            Console.WriteLine();
        }
    }
}
