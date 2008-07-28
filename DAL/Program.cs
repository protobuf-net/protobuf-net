using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using ProtoBuf;
using System.Linq;
using ProtoSharp.Core;
using System.Runtime.Serialization;
using System.IO.Compression;
using Serializer = ProtoBuf.Serializer;
namespace DAL
{
    [ProtoContract, DataContract]
    public class Database
    {
        [ProtoMember(1), Tag(1), DataMember(Order=1)]
        public List<Order> Orders { get; private set; }

        public Database()
        {
            Orders = new List<Order>();
        }
    }

    static class Program
    {
        static void Main()
        {
        
            Database db;

            /*
            // if have a Northwind handy...
            using(var ctx = new NorthwindDataContext())
            {
                db = ctx.ReadFromDatabase();
                DbMetrics("Database", db);
            }
            */

            string proto = Serializer.GetProto<Database>();
            File.WriteAllText("nwind.proto", proto);
            Console.WriteLine(proto);

            // otherwise...
            using (Stream fs = File.OpenRead("nwind.proto.bin"))
            {
                db = Serializer.Deserialize<Database>(fs);
            }

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
             * */
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
        static Database ReadFromDatabase(this NorthwindDataContext ctx) {
            Database db = new Database();
        
            DataLoadOptions opt = new DataLoadOptions();
            opt.AssociateWith<Order>(order => order.Lines);
            ctx.LoadOptions = opt;
            db.Orders.AddRange(ctx.Orders);

            return db;            
        }
    }
}
