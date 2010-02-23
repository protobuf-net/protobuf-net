
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
#if !FX11
        private double? when;
        public double? When { get { return when; } set { when = value; } }
        public bool? HasValue;
#endif
        
    }
    public struct CustomerStruct
    {
        private int id;
        public int Id { get { return id; } set { id = value; } }
        public string Name;
#if !FX11
        private double? when;
        public double? When { get { return when; } set { when = value; } }
        public bool? HasValue;
#endif
    }
    public class FX11_Program
    {
        private static Customer Read(Customer cust, ProtoReader reader)
        {
            int fieldNumber;
            while ((fieldNumber = reader.ReadFieldHeader()) > 0)
            {
                if (fieldNumber == 1)
                {
                    cust.Id = reader.ReadInt32();
                    continue;
                }
                if (fieldNumber == 2)
                {
                    cust.Name = reader.ReadString();
                    continue;
                }
                reader.SkipField();
            }
            return cust;
        }
        public static RuntimeTypeModel BuildMeta()
        {
            RuntimeTypeModel model = TypeModel.Create("CustomerModel");
            model.Add(typeof(Customer), false)
               .Add(1, "Id")
               .Add(2, "Name")
#if !FX11
               .Add(3,"When")
               .Add(4, "HasValue")
#endif
               ;
        ;
            model.Add(typeof(CustomerStruct), false)
                .Add(1, "Id")
                .Add(2, "Name")
#if !FX11
                .Add(3,"When")
                .Add(4, "HasValue")
#endif
                ;
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
#if FEAT_COMPILER
            TypeModel compiled = model.Compile("CustomerModel.dll");
            WriteCustomer(compiled, "Compiled - class", cust2);
            WriteCustomer(compiled, "Compiled - struct", cust2);
#endif
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
