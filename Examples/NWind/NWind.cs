using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ProtoBuf;
using System.Linq;
using ProtoSharp.Core;
using System.Runtime.Serialization;
using System.IO.Compression;
using NUnit.Framework;
using System.Data.Linq;
using Serializer = ProtoBuf.Serializer;
namespace DAL
{
    [ProtoContract, DataContract, Serializable]
    public class Database
    {
        [ProtoMember(1), Tag(1), DataMember(Order=1)]
        public List<Order> Orders { get; private set; }

        public Database()
        {
            Orders = new List<Order>();
        }
    }

    [TestFixture]
    public class NWindTests
    {
        public static Database LoadDatabaseFromFile()
        {
            // otherwise...
            using (Stream fs = File.OpenRead(@"NWind\nwind.proto.bin"))
            {
                return Serializer.Deserialize<Database>(fs);
            }
        }
        
        [Test]
        public void LoadTest()
        {

            Database db = LoadDatabaseFromFile();
            DbMetrics("Database", db);

        }

        [Test]
        public void TestProtoGen() {
            string proto = Serializer.GetProto<Database>();
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
        static Database ReadFromDatabase(NorthwindDataContext ctx) {
            Database db = new Database();
        
            DataLoadOptions opt = new DataLoadOptions();
            opt.AssociateWith<Order>(order => order.Lines);
            ctx.LoadOptions = opt;
            db.Orders.AddRange(ctx.Orders);

            return db;            
        }
    }
}
