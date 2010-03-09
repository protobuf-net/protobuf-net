using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using NUnit.Framework;
using ProtoBuf;
using ProtoSharp.Core;
using Serializer = ProtoBuf.Serializer;
using Examples;
using System.Diagnostics;
using ProtoBuf.Meta;

/*namespace ProtoBuf.Meta
{
    public class TypeModel {
        public static RuntimeTypeModel Create() { return null; }
        public object Deserialize(Stream source, object obj, Type type) { return null; }
    }
    public class RuntimeTypeModel : TypeModel
    {
        public static TypeModel Default;
        public void CompileInPlace() { }
        public TypeModel Compile() { return null; }
        public TypeModel Compile(string s, string t) { return null; }

        
    }
}*/
namespace DAL
{
    [ProtoContract, DataContract, Serializable]
    public class Database
    {
        public const DataFormat SubObjectFormat = DataFormat.Default;

        [ProtoMember(1, DataFormat=Database.SubObjectFormat), Tag(1), DataMember(Order=1)]
        public List<Order> Orders { get; private set; }

        public Database()
        {
            Orders = new List<Order>();
        }
    }
    [TestFixture]
    public class NWindTests
    {
        public static T LoadDatabaseFromFile<T>(TypeModel model)
            where T : class,new()
        {
            // otherwise...
            using (Stream fs = File.OpenRead(@"NWind\nwind.proto.bin"))
            {
                return (T)model.Deserialize(fs, null, typeof(T));
            }
        }
        
        [Test]
        public void LoadTestDefaultModel()
        {
            Database db = LoadDatabaseFromFile<Database>(RuntimeTypeModel.Default);
            DbMetrics("Database", db);

        }

        [Test]
        public void LoadTestCustomModel()
        {
            var model = TypeModel.Create();
            Database db;
            
            db = LoadDatabaseFromFile<Database>(model);
            DbMetrics("Database", db);

            model.CompileInPlace();
            db = LoadDatabaseFromFile<Database>(model);
            DbMetrics("Database", db);

            
            db = LoadDatabaseFromFile<Database>(model.Compile());
            DbMetrics("Database", db);

            db = LoadDatabaseFromFile<Database>(model.Compile("NWindModel", "NWindModel.dll"));
            PEVerify.AssertValid("NWindModel.dll");
            DbMetrics("Database", db);
        }

        [Test]
        public void PerfTestDb()
        {
            byte[] blob = File.ReadAllBytes(@"NWind\nwind.proto.bin");
            using (MemoryStream ms = new MemoryStream(blob))
            {
                var model = TypeModel.Create();
                Type type = typeof(Database);
                model.Deserialize(ms, null, type);
                var compiled = model.Compile();
                /*erializer.PrepareSerializer<Database>();
                Serializer.Deserialize<Database>(ms);*/
                Stopwatch watch = Stopwatch.StartNew();
                for (int i = 0; i < 1000; i++)
                {
                    ms.Position = 0;
                    //Serializer.Deserialize<Database>(ms);
                    compiled.Deserialize(ms, null, type);
                }
                watch.Stop();
                Console.WriteLine(watch.ElapsedMilliseconds);
            }
        }

        [Test, Ignore("GetProto not implemented")]
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
