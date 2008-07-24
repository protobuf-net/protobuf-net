using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;
using System.Diagnostics;
using System.Data.Linq;
using System.Linq;

namespace DAL
{
    [ProtoContract]
    class Database
    {
        [ProtoMember(1)]
        public List<Order> Orders { get; private set; }

        public Database()
        {
            Orders = new List<Order>();
        }
    }

    class Program
    {
        static void Main()
        {
            const string path = @"d:\nwind.proto.bin";
            const int COUNT = 500;

            ReadFromDatabase(path);
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
        static void ReadFromDatabase(string path) {
            Database db = new Database();
            using (var ctx = new NorthwindDataContext())
            {
                DataLoadOptions opt = new DataLoadOptions();
                opt.AssociateWith<Order>(order => order.Lines);
                ctx.LoadOptions = opt;
                db.Orders.AddRange(ctx.Orders);

                Console.WriteLine("Orders: {0}", db.Orders.Count);
                WriteToFile(path, db);
                
            }
            
        }
    }
}
