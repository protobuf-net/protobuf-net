
using ProtoBuf.Meta;
using System.IO;
using System;
using ProtoBuf;
using SampleDto;
using NETCFClient;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
namespace FX11
{
    
    [TestFixture]
    public class FX11_Program
    {
        [Test]
        public void CheckThisTestRunIsForCLR2()
        {   // because TestDriven.NET on VS2010 gets uppity
            Assert.AreEqual(2, Environment.Version.Major);
        }

        private static void DumpObject(string header, PropertyInfo[] props, object obj) {
            Console.WriteLine();
            Console.WriteLine(header);
            Console.WriteLine();
            foreach(PropertyInfo prop in props) {
                Console.WriteLine("\t" + prop.Name + "\t" + Convert.ToString(prop.GetValue(obj, null)));
            }
        }
        public static RuntimeTypeModel BuildMeta()
        {
            
            RuntimeTypeModel model;
#if !FX11
            model = TypeModel.Create();
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

            TypeModel compiled = model.Compile();
            Type type = typeof(Product);
            PropertyInfo[] props = type.GetProperties();

            Product prod = new Product();
            prod.ProductID = 123;
            prod.ProductName = "abc devil";
            prod.SupplierID = 456;
            prod.CategoryID = 13;
            prod.QuantityPerUnit = "1";
            prod.UnitPrice = 12.99M;
            prod.UnitsInStock = 96;
            prod.UnitsOnOrder = 12;
            prod.ReorderLevel = 30;
            prod.Discontinued = false;
            prod.LastEditDate = new DateTime(2009, 5, 7);
            prod.CreationDate = new DateTime(2009, 1, 3);

            DumpObject("Original", props, prod);
            
            const int loop = 100000;
            Console.WriteLine("Iterations: " + loop);
            Stopwatch watch;
            MemoryStream reuseDump = new MemoryStream(100 * 1024);
#if FX30
            System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(type);
            
            using (MemoryStream ms = new MemoryStream()) {
                dcs.WriteObject(ms, prod);
                Console.WriteLine("DataContractSerializer: {0} bytes", ms.Length);
            }
            
            watch = Stopwatch.StartNew();
            for (int i = 0; i < loop; i++)
            {
                reuseDump.SetLength(0);
                dcs.WriteObject(reuseDump, prod);                
            }
            watch.Stop();
            Console.WriteLine("DataContractSerializer serialize: {0} ms", watch.ElapsedMilliseconds);
            watch = Stopwatch.StartNew();
            for (int i = 0; i < loop; i++)
            {
                reuseDump.Position = 0;
                dcs.ReadObject(reuseDump);
            }
            watch.Stop();
            Console.WriteLine("DataContractSerializer deserialize: {0} ms", watch.ElapsedMilliseconds);

            {
            reuseDump.Position = 0;
            Product p1 = (Product) dcs.ReadObject(reuseDump);
            DumpObject("DataContractSerializer", props, p1);
            }

            System.Runtime.Serialization.NetDataContractSerializer ndcs = new System.Runtime.Serialization.NetDataContractSerializer();
            
            using (MemoryStream ms = new MemoryStream()) {
                ndcs.Serialize(ms, prod);
                Console.WriteLine("NetDataContractSerializer: {0} bytes", ms.Length);
            }
            
            watch = Stopwatch.StartNew();
            for (int i = 0; i < loop; i++)
            {
                reuseDump.SetLength(0);
                ndcs.Serialize(reuseDump, prod);                
            }
            watch.Stop();
            Console.WriteLine("NetDataContractSerializer serialize: {0} ms", watch.ElapsedMilliseconds);
            watch = Stopwatch.StartNew();
            for (int i = 0; i < loop; i++)
            {
                reuseDump.Position = 0;
                ndcs.Deserialize(reuseDump);
            }
            watch.Stop();
            Console.WriteLine("NetDataContractSerializer deserialize: {0} ms", watch.ElapsedMilliseconds);

            {
            reuseDump.Position = 0;
            Product p1 = (Product) ndcs.Deserialize(reuseDump);
            DumpObject("NetDataContractSerializer", props, p1);
            }
#endif

            using (MemoryStream ms = new MemoryStream())
            {
                compiled.Serialize(ms, prod);
                byte[] buffer = ms.GetBuffer();
                int len = (int)ms.Length;
                Console.WriteLine("protobuf-net v2: {0} bytes", len);
                for (int i = 0; i < len; i++)
                {
                    Console.Write(buffer[i].ToString("x2"));                    
                }
                Console.WriteLine();
            }
            watch = Stopwatch.StartNew();
            for (int i = 0; i < loop; i++)
            {
                reuseDump.SetLength(0);
                compiled.Serialize(reuseDump, prod);
            }
            watch.Stop();
            Console.WriteLine("protobuf-net v2 serialize: {0} ms", watch.ElapsedMilliseconds);
            
            watch = Stopwatch.StartNew();
            for (int i = 0; i < loop; i++)
            {
                reuseDump.Position = 0;
                compiled.Deserialize(reuseDump, null, type);
            }
            watch.Stop();
            
            Console.WriteLine("protobuf-net v2 deserialize: {0} ms", watch.ElapsedMilliseconds);
            {
            reuseDump.Position = 0;
            Product p1 = (Product)compiled.Deserialize(reuseDump, null, type);
            DumpObject("protobuf-net v2", props, p1);
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
            model = TypeModel.Create();
            model.Add(typeof(Customer), false)
               .Add(1, "Id")
               .Add(3, "Name")
#if !FX11
               .Add(5, "HowMuch")
               .Add(6, "HasValue")
#endif
               ;
        ;
            model.Add(typeof(CustomerStruct), false)
                .Add(1, "Id")
                .Add(3, "Name")
#if !FX11
                .Add(5, "HowMuch")
                .Add(6, "HasValue")
#endif
                ;
            return model;
        }

        private static Product Read(Product product1, ProtoReader reader1)
        {
            int num;
            while ((num = reader1.ReadFieldHeader()) > 0)
            {
                switch (num)
                {
                    case 1:
                        if (product1 == null)
                        {
                            product1 = new Product();
                        }
                        product1.ProductID = reader1.ReadInt32();
                        continue;
                    case 2:
                        if (product1 == null)
                        {
                            product1 = new Product();
                        }
                        product1.ProductName = reader1.ReadString();
                        continue;
                    case 3:
                        if (product1 == null)
                        {
                            product1 = new Product();
                        }
                        product1.SupplierID = new int?(reader1.ReadInt32());
                        continue;
                }
            }
            return product1;
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

            TypeModel compiled = model.Compile("CustomerModel", "CustomerModel.dll");
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
