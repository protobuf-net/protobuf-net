
using ProtoBuf.Meta;
using System.IO;
using System;
using ProtoBuf;
using SampleDto;
using NETCFClient;
namespace FX11
{
    
    public class FX11_Program
    {
        public static RuntimeTypeModel BuildMeta()
        {
            RuntimeTypeModel model;
#if !FX11
            model = TypeModel.Create("ThinkSerializer");
            model.Add(typeof(Order), false)
                .Add(1, "OrderID")
                .Add(2, "CustomerID")
                .Add(3, "EmployeeID")
                .Add(4, "OrderDate")
                .Add(5, "RequiredDate")
                .Add(6, "ShippedDate")
                .Add(7, "ShipVia")
                .Add(8, "Freight")
                .Add(9, "ShipName")
                .Add(10, "ShipAddress")
                .Add(11, "ShipCity")
                .Add(12, "ShipRegion")
                .Add(13, "ShipPostalCode")
                .Add(14, "ShipCountry");
            model.Add(typeof(Product), false)
                .Add(1, "ProductID")
                .Add(2, "ProductName")
                .Add(3, "SupplierID")
                .Add(4, "CategoryID")
                .Add(5, "QuantityPerUnit")
                .Add(6, "UnitPrice")
                .Add(7, "UnitsInStock")
                .Add(8, "UnitsOnOrder")
                .Add(9, "ReorderLevel")
                .Add(10, "Discontinued")
                .Add(11, "LastEditDate")
                .Add(12, "CreationDate");
            TypeModel compiled = model.Compile("ThinkSerializer.dll");

            Product prod = new Product();
            prod.CategoryID = 123;
            prod.ProductName = "abc";
            prod.CreationDate = new DateTime(2009, 1, 3);
            prod.ProductID = 13;
            prod.UnitPrice = 123.45M;
            using (MemoryStream ms = new MemoryStream())
            {
                compiled.Serialize(ms, prod);
                byte[] buffer = ms.GetBuffer();
                int len = (int)ms.Length;
                for (int i = 0; i < len; i++)
                {
                    Console.Write(buffer[i].ToString("x2"));                    
                }
                Console.WriteLine();
            }
            // 080d 1203(616263) 207b
            // 3205(08b9601804)
            // 5000 6204(08cede01)

            // 00   08 = 1|000 = field 1, variant
            // 01   0d = 13

            // 02   12 = 10|010 = field 2, string
            // 03   03 = length 3
            // 04   616263 = "abc"

            // 07   20 = 100|000 = field 4, variant
            // 08   7B = 123

            // 09   32 = 110|010 = field 6, string
            // 10   05 = length 5
            // 11     08 = 1|000 = field 1, variant
            // 12     b960 (le) = 1100000:0111001 = 12345
            // 14     18 = 11|000 = field 3, variant
            // 15     04 = 4 (signScale = scale 2, +ve)

            // 16   50 = 1010|000 = field 10, variant
            // 17   00 = false

            // 18   62 = 1100|010 = field 12, string
            // 19   04 = length 4
            // 20    08 = 1|000 = field 1, variant
            // 21    cede01 = 1:1011110:1001110 = 28494 (days, signed) = 14247 = 03/01/2009

            Product clone = (Product)compiled.DeepClone(prod);
            Console.WriteLine(clone.CategoryID);
            Console.WriteLine(clone.ProductName);
            Console.WriteLine(clone.CreationDate);
            Console.WriteLine(clone.ProductID);
            Console.WriteLine(clone.UnitPrice);



#endif
            model = TypeModel.Create("CustomerModel");
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
