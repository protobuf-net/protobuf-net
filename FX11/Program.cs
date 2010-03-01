
using ProtoBuf.Meta;
using System.IO;
using System;
using ProtoBuf;
using SampleDto;
namespace FX11
{
    
    public class FX11_Program
    {
        public static RuntimeTypeModel BuildMeta()
        {
            RuntimeTypeModel model = TypeModel.Create("CustomerModel");
            model.Add(typeof(Customer), false)
               .Add(1, "Id")
               .Add(2, "Name")
#if !FX11
               .Add(3, "HowMuch")
               .Add(4, "HasValue")
#endif
               ;
        ;
            model.Add(typeof(CustomerStruct), false)
                .Add(1, "Id")
                .Add(2, "Name")
#if !FX11
                .Add(3, "HowMuch")
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
#if !FX11
            cust1.HasValue = cust2.HasValue = true;
            cust1.HowMuch = cust2.HowMuch = 0.123;
#endif
            WriteCustomer(model, "Runtime - class", cust1);
            WriteCustomer(model, "Runtime - struct", cust2);

#if FEAT_COMPILER && !FX11
            model.CompileInPlace();
            WriteCustomer(model, "InPlace- class", cust1);
            WriteCustomer(model, "InPlace - struct", cust2);
#endif
#if FEAT_COMPILER

           TypeModel compiled = model.Compile("CustomerModel.dll");
           WriteCustomer(compiled, "Compiled - class", cust2);
           WriteCustomer(compiled, "Compiled - struct", cust2);
            /*
            CustomerModel serializer = new CustomerModel();
            using (MemoryStream ms = new MemoryStream())
            {
                Customer cust = new Customer();
                cust.Id = 123;
                cust.Name = "Fred";
                serializer.Serialize(ms, cust);
                ms.Position = 0;
                Customer clone = (Customer)serializer.Deserialize(ms, null, typeof(Customer));
                Console.WriteLine(clone.Id);
                Console.WriteLine(clone.Name);
            }
             */


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
            
            using (MemoryStream ms = new MemoryStream(blob))
            {
                object clone = model.Deserialize(ms, null, obj.GetType());
                string oldS = Convert.ToString(obj), newS = Convert.ToString(clone);
                Console.WriteLine(oldS == newS ? ("match: " + newS) : ("delta" + oldS + " vs " + newS));

            }
            Console.WriteLine();
            
        }
    }
}
