using System;
using System.Data.Linq;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using ProtoBuf;
using Serializer = ProtoBuf.Serializer;
namespace DAL
{
    static class Program
    {
        static void Main()
        {
        
            Database db;


            Console.WriteLine("Using groups: {0}", Database.SubObjectFormat== DataFormat.Group);
            // if have a Northwind handy...
            using(var ctx = new NorthwindDataContext())
            {
                db = ctx.ReadFromDatabase("nwind.proto.bin");
                DbMetrics("Database", db);
            }
            

            string proto = Serializer.GetProto<Database>();
            File.WriteAllText("nwind.proto", proto);
            Console.WriteLine(proto);
            
            // otherwise...

            using(MemoryStream ms = new MemoryStream(File.ReadAllBytes("nwind.proto.bin")))
            {
                db = Serializer.Deserialize<Database>(ms);
                for (int i = 0; i < 3; i++)
                {
                    Serializer.Serialize(Stream.Null, db);
                }
            }
            /*
            for (int i = 0; i < 1; i++)
            {
                using (Stream ms = new MemoryStream())
                {
                    Serializer.Serialize(ms, db);
                    //ms.Position = 0;
                    //db = Serializer.Deserialize<Database>(ms);
                }
            }
            */

            using (MemoryStream ms = new MemoryStream())
            {
                new DataContractSerializer(db.GetType()).WriteObject(ms, db);
                Console.WriteLine("DataContractSerializer length: {0:###,###,000}", ms.Length);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    new DataContractSerializer(db.GetType()).WriteObject(zip, db);
                    zip.Close();
                }
                Console.WriteLine("GZip/DataContractSerializer length: {0:###,###,000}", ms.Length);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    Serializer.Serialize(zip, db);
                    zip.Close();
                }
                Console.WriteLine("GZip/proto length: {0:###,###,000}", ms.Length);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, db);
                ms.Position = 0;
                Console.WriteLine("proto length: {0:###,###,000}", ms.Length);

                Database pbnet = Serializer.Deserialize<Database>(ms);
                DbMetrics("protobuf-net", pbnet);

                //Database psharp = MessageReader.Read<Database>(ms.ToArray());
                //DbMetrics("proto#", psharp);
            }

            Console.WriteLine();
            Console.WriteLine("[press any key]");
            Console.ReadKey();
            
            
            
            /*
            Console.WriteLine("{0}={1} bytes", path, new FileInfo(path).Length);
            
            Database db = null;
            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < COUNT; i++)
            {
                db = ReadFromFile(path);
            }
            watch.Stop();
            Console.WriteLine("Load x{0}: {1}ms", COUNT, watch.ElapsedMilliseconds);
            watch = Stopwatch.StartNew();
            for (int i = 0; i < COUNT; i++)
            {
                WriteToFile(path, db);
            }
            watch.Stop();
            Console.WriteLine("Save x{0}: {1}ms", COUNT, watch.ElapsedMilliseconds);
             */ 
             
        }
        static void DbMetrics(string caption, Database database)
        {
            int orders = database.Orders.Count;
            int lines = database.Orders.SelectMany(ord => ord.Lines).Count();
            int totalQty = database.Orders.SelectMany(ord => ord.Lines)
                    .Sum(line => line.Quantity);
            decimal totalValue = database.Orders.SelectMany(ord => ord.Lines)
                    .Sum(line => line.Quantity * line.UnitPrice);

            Console.WriteLine("{0}\torders {1}; lines {2}; units {3}; value {4:C}",
                caption, orders, lines, totalQty, totalValue);

        }
        static Database ReadFromFile(string path)
        {
            Database database;
            using (Stream fs = File.OpenRead(path))
            {
                database = Serializer.Deserialize<Database>(fs);
                fs.Close();
            }
            return database;
        }
        static void WriteToFile(string path, Database database)
        {
            using (Stream fs = File.Create(path))
            {
                Serializer.Serialize(fs, database);
                fs.Close();
            }
        }
        static Database ReadFromDatabase(this NorthwindDataContext ctx, string path) {
            Database db = new Database();
        
            DataLoadOptions opt = new DataLoadOptions();
            opt.AssociateWith<Order>(order => order.Lines);
            ctx.LoadOptions = opt;
            db.Orders.AddRange(ctx.Orders);

            using (FileStream fs = File.Create(path))
            {
                Serializer.Serialize(fs, db);
            }
            return db;            
        }
    }
}
