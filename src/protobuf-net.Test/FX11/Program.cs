using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NETCFClient;
using ProtoBuf;
using ProtoBuf.Meta;
using SampleDto;
using Xunit;
using ProtoBuf.unittest;
using Xunit.Abstractions;
using System.Text;

namespace FX11
{
    public class FX11_Program
    {
        private ITestOutputHelper Log { get; }
        public FX11_Program(ITestOutputHelper _log) => Log = _log;
        /*
        public void CheckThisTestRunIsForCLR2()
        {   // because TestDriven.NET on VS2010 gets uppity
            Assert.AreEqual(2, Environment.Version.Major);
        }*/

        private void DumpObject(string header, PropertyInfo[] props, object obj) {
            Log.WriteLine("");
            Log.WriteLine(header);
            Log.WriteLine("");
            foreach(PropertyInfo prop in props) {
                Log.WriteLine("\t" + prop.Name + "\t" + Convert.ToString(prop.GetValue(obj, null)));
            }
        }
        public RuntimeTypeModel BuildMeta(int loop = 100000)
        {
            RuntimeTypeModel model;
#if !FX11
            model = RuntimeTypeModel.Create();
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

            Product prod = new Product
            {
                ProductID = 123,
                ProductName = "abc devil",
                SupplierID = 456,
                CategoryID = 13,
                QuantityPerUnit = "1",
                UnitPrice = 12.99M,
                UnitsInStock = 96,
                UnitsOnOrder = 12,
                ReorderLevel = 30,
                Discontinued = false,
                LastEditDate = new DateTime(2009, 5, 7),
                CreationDate = new DateTime(2009, 1, 3)
            };

            DumpObject("Original", props, prod);

            Log.WriteLine("Iterations: " + loop);
            Stopwatch watch;
            using MemoryStream reuseDump = new MemoryStream(100 * 1024);
#if FX30
            System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(type);

            using (MemoryStream ms = new MemoryStream()) {
                dcs.WriteObject(ms, prod);
                Log.WriteLine("DataContractSerializer: {0} bytes", ms.Length);
            }

            watch = Stopwatch.StartNew();
            for (int i = 0; i < loop; i++)
            {
                reuseDump.SetLength(0);
                dcs.WriteObject(reuseDump, prod);
            }
            watch.Stop();
            Log.WriteLine("DataContractSerializer serialize: {0} ms", watch.ElapsedMilliseconds);
            watch = Stopwatch.StartNew();
            for (int i = 0; i < loop; i++)
            {
                reuseDump.Position = 0;
                dcs.ReadObject(reuseDump);
            }
            watch.Stop();
            Log.WriteLine("DataContractSerializer deserialize: {0} ms", watch.ElapsedMilliseconds);

            {
            reuseDump.Position = 0;
            Product p1 = (Product) dcs.ReadObject(reuseDump);
            DumpObject("DataContractSerializer", props, p1);
            }

            System.Runtime.Serialization.NetDataContractSerializer ndcs = new System.Runtime.Serialization.NetDataContractSerializer();

            using (MemoryStream ms = new MemoryStream()) {
                ndcs.Serialize(ms, prod);
                Log.WriteLine("NetDataContractSerializer: {0} bytes", ms.Length);
            }

            watch = Stopwatch.StartNew();
            for (int i = 0; i < loop; i++)
            {
                reuseDump.SetLength(0);
                ndcs.Serialize(reuseDump, prod);
            }
            watch.Stop();
            Log.WriteLine("NetDataContractSerializer serialize: {0} ms", watch.ElapsedMilliseconds);
            watch = Stopwatch.StartNew();
            for (int i = 0; i < loop; i++)
            {
                reuseDump.Position = 0;
                ndcs.Deserialize(reuseDump);
            }
            watch.Stop();
            Log.WriteLine("NetDataContractSerializer deserialize: {0} ms", watch.ElapsedMilliseconds);

            {
            reuseDump.Position = 0;
            Product p1 = (Product) ndcs.Deserialize(reuseDump);
            DumpObject("NetDataContractSerializer", props, p1);
            }
#endif

            using (MemoryStream ms = new MemoryStream())
            {
                compiled.Serialize(ms, prod);
#if COREFX
                if (!ms.TryGetBuffer(out var tmp))
                    throw new Exception("oops");
                byte[] buffer = tmp.Array;
#else
                byte[] buffer = ms.GetBuffer();
#endif
                int len = (int)ms.Length;
                Log.WriteLine("protobuf-net v2: {0} bytes", len);
                var sb = new StringBuilder();
                for (int i = 0; i < len; i++)
                {
                    sb.Append(buffer[i].ToString("x2"));
                }
                Log.WriteLine(sb.ToString());
                Log.WriteLine("");
            }
            watch = Stopwatch.StartNew();
            for (int i = 0; i < loop; i++)
            {
                reuseDump.SetLength(0);
                compiled.Serialize(reuseDump, prod);
            }
            watch.Stop();
            Log.WriteLine("protobuf-net v2 serialize: {0} ms", watch.ElapsedMilliseconds);

            watch = Stopwatch.StartNew();
            for (int i = 0; i < loop; i++)
            {
                reuseDump.Position = 0;
#pragma warning disable CS0618
                compiled.Deserialize(reuseDump, null, type);
#pragma warning restore CS0618
            }
            watch.Stop();

            if (loop > 0)
            {
                Log.WriteLine("protobuf-net v2 deserialize: {0} ms", watch.ElapsedMilliseconds);
                {
                    reuseDump.Position = 0;
#pragma warning disable CS0618
                    Product p1 = (Product)compiled.Deserialize(reuseDump, null, type);
#pragma warning restore CS0618
                    DumpObject("protobuf-net v2", props, p1);
                }
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

            if (loop > 0)
            {
                Product clone = (Product)compiled.DeepClone(prod);
                Log.WriteLine(clone.CategoryID?.ToString());
                Log.WriteLine(clone.ProductName);
                Log.WriteLine(clone.CreationDate?.ToString());
                Log.WriteLine(clone.ProductID.ToString());
                Log.WriteLine(clone.UnitPrice?.ToString());
            }

#endif
                model = RuntimeTypeModel.Create();
            model.Add(typeof(Customer), false)
               .Add(1, "Id")
               .Add(3, "Name")
#if !FX11
               .Add(5, "HowMuch")
               .Add(6, "HasValue")
#endif
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

#pragma warning disable RCS1213, IDE0051
        private static Product Read(ref ProtoReader.State state, Product product1)
#pragma warning restore RCS1213, IDE0051
        {
            int num;
            while ((num = state.ReadFieldHeader()) > 0)
            {
                switch (num)
                {
                    case 1:
                        if (product1 == null)
                        {
                            product1 = new Product();
                        }
                        product1.ProductID = state.ReadInt32();
                        continue;
                    case 2:
                        if (product1 == null)
                        {
                            product1 = new Product();
                        }
                        product1.ProductName = state.ReadString();
                        continue;
                    case 3:
                        if (product1 == null)
                        {
                            product1 = new Product();
                        }
                        product1.SupplierID = new int?(state.ReadInt32());
                        continue;
                }
            }
            return product1;
        }
        private void PurgeWithGusto(string path)
        {
            if (File.Exists(path))
            {
                try { File.Delete(path); }
                catch
                {
                    Log.WriteLine("Oops, " + path + " is locked; try looking in process explorer (sysinternals)");
                    for (int i = 30; i != 0; i--)
                    {
                        Log.WriteLine(i.ToString());
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }
        }
        [Fact]
        public void Execute()
        {
            WriteHeading(".NET version");
#if !COREFX
            Log.WriteLine(Environment.Version.ToString());
#endif
            RuntimeTypeModel orderModel = RuntimeTypeModel.Create();
            orderModel.Add(typeof(OrderHeader), true);
            orderModel.Add(typeof(OrderDetail), true);

            PurgeWithGusto("OrderSerializer.dll");
            orderModel.Compile("OrderSerializer", "OrderSerializer.dll");
            PEVerify.Verify("OrderSerializer.dll");
            RuntimeTypeModel model = BuildMeta();
            Customer cust1 = new Customer();
            CustomerStruct cust2 = new CustomerStruct
            {
                Id = cust1.Id = 123,
                Name = cust1.Name = "Fred"
            };
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

#if !COREFX
            PurgeWithGusto("CustomerModel.dll");
            TypeModel compiled = model.Compile("CustomerModel", "CustomerModel.dll");
            PEVerify.Verify("CustomerModel.dll");
            WriteCustomer(compiled, "Compiled - class", cust2);
            WriteCustomer(compiled, "Compiled - struct", cust2);
#endif
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
                Log.WriteLine(clone.Id);
                Log.WriteLine(clone.Name);
            }
             */
#endif
        }
        private void WriteHeading(string caption)
        {
            Log.WriteLine("");
            Log.WriteLine(caption);
            Log.WriteLine(new string('=', caption.Length));
            Log.WriteLine("");
        }

        private void WriteCustomer(TypeModel model, string caption, object obj)
        {
            WriteHeading(caption);
            byte[] blob;
            using (MemoryStream ms = new MemoryStream())
            {
#pragma warning disable CS0618
                model.Serialize(ms, obj);
#pragma warning restore CS0618
                blob = ms.ToArray();
            }
            var sb = new StringBuilder();
            foreach (byte b in blob)
            {
                sb.Append(b.ToString("x2"));
            }
            Log.WriteLine(sb.ToString());
            Log.WriteLine("");

            using (MemoryStream ms = new MemoryStream(blob))
            {
#pragma warning disable CS0618
                object clone = model.Deserialize(ms, null, obj.GetType());
#pragma warning restore CS0618
                string oldS = Convert.ToString(obj), newS = Convert.ToString(clone);
                Log.WriteLine(oldS == newS ? ("match: " + newS) : ("delta" + oldS + " vs " + newS));
            }
            Log.WriteLine("");
        }
    }
}
