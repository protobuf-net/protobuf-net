#pragma warning disable RCS1213
using System;
using ProtoBuf;
using System.Runtime.Serialization;
using System.IO;

#if !COREFX

using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;

using Xunit;
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
#endif
namespace DAL
{
    [ProtoContract, DataContract, Serializable]
    public class Database
    {
        public const DataFormat SubObjectFormat = DataFormat.Default;
#if !COREFX
        [ProtoMember(1, DataFormat=Database.SubObjectFormat), Tag(1), DataMember(Order=1)]
        public List<Order> Orders { get; private set; }

        public Database()
        {
            Orders = new List<Order>();
        }
#endif
    }


    public class NWindTests
    {
        private static readonly string[] nwindPaths = { @"NWind\nwind.proto.bin", @"Tools\nwind.proto.bin", "nwind.proto.bin" };
        public static string GetNWindBinPath()
        {
            for (int i = 0; i < nwindPaths.Length; i++)
            {
                if (File.Exists(nwindPaths[i])) return nwindPaths[i];
            }
            throw new FileNotFoundException("Unable to locate nwind.proto.bin under " + Directory.GetCurrentDirectory());
        }
#if !COREFX
        public static T LoadDatabaseFromFile<T>(TypeModel model)
            where T : class,new()
        {
            // otherwise...
            using Stream fs = File.OpenRead(NWindTests.GetNWindBinPath());
#pragma warning disable CS0618
            return (T)model.Deserialize(fs, null, typeof(T));
#pragma warning restore CS0618
        }

        [Fact]
        public void LoadTestDefaultModel()
        {
            Database db = LoadDatabaseFromFile<Database>(RuntimeTypeModel.Default);
            DbMetrics("Database", db);
        }

        [Fact]
        public void LoadTestCustomModel()
        {
            var model = RuntimeTypeModel.Create();
            Database db = LoadDatabaseFromFile<Database>(model);
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

        [Fact]
        public void PerfTestDb()
        {
            byte[] blob = File.ReadAllBytes(NWindTests.GetNWindBinPath());
            using MemoryStream ms = new MemoryStream(blob);
            var model = RuntimeTypeModel.Create();
            Type type = typeof(Database);
#pragma warning disable CS0618
            model.Deserialize(ms, null, type);
#pragma warning restore CS0618
            var compiled = model.Compile();
            /*erializer.PrepareSerializer<Database>();
            Serializer.Deserialize<Database>(ms);*/
            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                ms.Position = 0;
                //Serializer.Deserialize<Database>(ms);
#pragma warning disable CS0618
                compiled.Deserialize(ms, null, type);
#pragma warning restore CS0618
            }
            watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds);
            if (Debugger.IsAttached)
            {
                Console.WriteLine("(press any key)");
                Console.ReadKey();
            }
        }

        [Fact]
        public void TestProtoGen()
        {
            // just show it can do *something*!

            _ = Serializer.GetProto<Database>();
        }

        private static void DbMetrics(string caption, Database database)
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

#pragma warning disable IDE0051 // Remove unused private members
        private static Database ReadFromFile(string path)
        {
            Database database;
            using (Stream fs = File.OpenRead(path))
            {
                database = Serializer.Deserialize<Database>(fs);
                fs.Close();
            }
            return database;
        }
        private static void WriteToFile(string path, Database database)
        {
            using Stream fs = File.Create(path);
            Serializer.Serialize(fs, database);
            fs.Close();
        }
        private static Database ReadFromDatabase(NorthwindDataContext ctx)
        {
            Database db = new Database();

            DataLoadOptions opt = new DataLoadOptions();
            opt.AssociateWith<Order>(order => order.Lines);
            ctx.LoadOptions = opt;
            db.Orders.AddRange(ctx.Orders);

            return db;
        }
#pragma warning restore IDE0051 // Remove unused private members
#endif
    }

}

