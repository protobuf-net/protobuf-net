#if REMOTING
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using DAL;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Xml.Serialization;

namespace Examples.Remoting
{

    public sealed class DbServer : MarshalByRefObject
    {
        public void LoadFrom(string path)
        {
            Assembly.LoadFrom(path);
        }

        public DatabaseCompat Roundtrip(DatabaseCompat db)
        {
            return db;
        }
        public DatabaseCompatRem Roundtrip(DatabaseCompatRem db)
        {
            return db;
        }

    }

    [TestFixture]
    public class DbRemoting
    {
        [Test]
        public void LargePayload()
        {
            DAL.Database db = DAL.NWindTests.LoadDatabaseFromFile<DAL.Database>(RuntimeTypeModel.Default);
            DatabaseCompat compat = Serializer.ChangeType<Database, DatabaseCompat>(db);
            DatabaseCompatRem rem = Serializer.ChangeType<Database, DatabaseCompatRem>(db);

            AppDomain app = AppDomain.CreateDomain("Isolated", null,
                AppDomain.CurrentDomain.BaseDirectory,
                AppDomain.CurrentDomain.RelativeSearchPath,false);
            try
            {
                DbServer server = (DbServer)app.CreateInstanceAndUnwrap(
                    typeof(DbServer).Assembly.FullName, typeof(DbServer).FullName);

                const int LOOP = 5;
                Stopwatch dbTimer = Stopwatch.StartNew();
                for (int i = 0; i < LOOP; i++)
                {
                    compat = server.Roundtrip(compat);
                }
                dbTimer.Stop();
                Stopwatch remTimer = Stopwatch.StartNew();
                for (int i = 0; i < LOOP; i++)
                {
                    rem = server.Roundtrip(rem);
                }
                remTimer.Stop();
                // want to aim for twice the speed
                decimal factor = 0.50M;
#if DEBUG
                factor = 1.2M; // be realistic in debug...
#endif
                long target = (long) (dbTimer.ElapsedTicks * factor);
                Assert.LessOrEqual(3, 5, "args wrong way around!");
                Assert.LessOrEqual(remTimer.ElapsedTicks, target);
            }
            finally
            {
                AppDomain.Unload(app);
            }
        }
    }

    [Serializable, DataContract]
    public sealed class RegularFragment
    {
        [DataMember]
        public int Foo { get; set; }
        [DataMember]
        public float Bar { get; set; }
    }
    [Serializable, ProtoContract, XmlType]
    public sealed class ProtoFragment : ISerializable
    {
        [ProtoMember(1, DataFormat=DataFormat.TwosComplement), XmlAttribute("foo")]
        public int Foo { get; set; }
        [ProtoMember(2), XmlAttribute("bar")]
        public float Bar { get; set; }

        public ProtoFragment() { }
        private ProtoFragment(SerializationInfo info, StreamingContext context)
        {
            Serializer.Merge(info, this);
        }
        void  ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Serializer.Serialize(info, this);
        }
}

    public sealed class Server : MarshalByRefObject
    {
        public RegularFragment SomeMethod1(RegularFragment value)
        {
            return new RegularFragment {
                Foo = value.Foo * 2,
                Bar = value.Bar * 2
            };
        }
        public ProtoFragment SomeMethod2(ProtoFragment value)
        {
            return new ProtoFragment
            {
                Foo = value.Foo * 2,
                Bar = value.Bar * 2
            };
        }
    }

    [TestFixture]
    public class RemotingDemo
    {
        [Test]
        [Ignore("small messages known to be slower")]
        public void SmallPayload()
        {
            AppDomain app = AppDomain.CreateDomain("Isolated", null,
                AppDomain.CurrentDomain.BaseDirectory,
                AppDomain.CurrentDomain.RelativeSearchPath, false);
            
            try
            {
                // create a server and two identical messages
                Server local = new Server(),
                    remote = (Server)app.CreateInstanceAndUnwrap(typeof(Server).Assembly.FullName, typeof(Server).FullName);
                RegularFragment frag1 = new RegularFragment { Foo = 27, Bar = 123.45F };
                Serializer.PrepareSerializer<ProtoFragment>();
                ProtoFragment frag2 = new ProtoFragment { Foo = frag1.Foo, Bar = frag1.Bar };
                // verify basic transport
                RegularFragment localFrag1 = local.SomeMethod1(frag1),
                    remoteFrag1 = remote.SomeMethod1(frag1);
                ProtoFragment localFrag2 = local.SomeMethod2(frag2),
                    remoteFrag2 = remote.SomeMethod2(frag2);

                Assert.AreNotSame(localFrag1, remoteFrag1);
                Assert.AreNotSame(localFrag2, remoteFrag2);
                Assert.AreEqual(localFrag1.Foo, remoteFrag1.Foo);
                Assert.AreEqual(localFrag1.Bar, remoteFrag1.Bar);
                Assert.AreEqual(localFrag2.Foo, remoteFrag2.Foo);
                Assert.AreEqual(localFrag2.Bar, remoteFrag2.Bar);
                Assert.AreEqual(localFrag1.Foo, localFrag2.Foo);
                Assert.AreEqual(localFrag1.Bar, localFrag2.Bar);

                const int LOOP = 5000;

                Stopwatch regWatch = Stopwatch.StartNew();
                for (int i = 0; i < LOOP; i++)
                {
                    remoteFrag1 = remote.SomeMethod1(remoteFrag1);
                }
                regWatch.Stop();
                Stopwatch protoWatch = Stopwatch.StartNew();
                for (int i = 0; i < LOOP; i++)
                {
                    remoteFrag2 = remote.SomeMethod2(remoteFrag2);
                }
                protoWatch.Stop();

                Assert.LessOrEqual(3, 5, "just checking...");

                Assert.LessOrEqual(protoWatch.ElapsedMilliseconds, regWatch.ElapsedMilliseconds);
            }
            finally
            {                
                AppDomain.Unload(app);
            }
        }
        [Test]
        public void TestRawSerializerPerformance()
        {
            RegularFragment frag1 = new RegularFragment { Foo = 27, Bar = 123.45F };
            
            ProtoFragment frag2 = new ProtoFragment { Foo = frag1.Foo, Bar = frag1.Bar };
            Type regular = typeof(RegularFragment), proto = typeof(ProtoFragment);
            var model = TypeModel.Create();
            model.AutoAddMissingTypes = false;
            model.Add(typeof(ProtoFragment), true);
            
            const int LOOP = 10000;
            Roundtrip(() => new BinaryFormatter(), LOOP,
                (ser, dest) => ser.Serialize(dest, frag1), (ser, src) => ser.Deserialize(src));

            Roundtrip("BinaryFormatter via pb-net", () => new BinaryFormatter(), LOOP,
                (ser, dest) => ser.Serialize(dest, frag2), (ser, src) => ser.Deserialize(src));

            Roundtrip(() => model, LOOP,
                (ser, dest) => ser.Serialize(dest, frag2), (ser, src) => ser.Deserialize(src, null, proto));

            Roundtrip(() => new XmlSerializer(proto), LOOP,
                (ser, dest) => ser.Serialize(dest, frag2), (ser, src) => ser.Deserialize(src));

            Roundtrip(() => new DataContractSerializer(regular), LOOP,
                (ser, dest) => ser.WriteObject(dest, frag1), (ser, src) => ser.ReadObject(src));

            Roundtrip(() => new NetDataContractSerializer(), LOOP,
                (ser, dest) => ser.Serialize(dest, frag1), (ser, src) => ser.Deserialize(src));

            model.CompileInPlace();
            Roundtrip("CompileInPlace", () => model, LOOP * 50,
                (ser, dest) => ser.Serialize(dest, frag2), (ser, src) => ser.Deserialize(src, null, proto));

            Roundtrip("Compile", () => model.Compile(), LOOP * 50,
                (ser, dest) => ser.Serialize(dest, frag2), (ser, src) => ser.Deserialize(src, null, proto));
        }
        static void Roundtrip<T>(Func<T> ctor, int loop, Action<T, Stream> write, Func<T, Stream, object> read)
            where T : class
        {
            Roundtrip(typeof(T).Name, ctor, loop, write, read);
        }
        static void Roundtrip<T>(string caption, Func<T> ctor, int loop, Action<T, Stream> write, Func<T, Stream, object> read)
            where T : class
        {
            T ser = ctor();

            using (MemoryStream ms = new MemoryStream())
            {
                write(ser, ms);
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
                Stopwatch writeWatch = Stopwatch.StartNew();
                for (int i = 0; i < loop; i++)
                {
                    ms.SetLength(0);
                    write(ser, ms);
                }
                writeWatch.Stop();

                ms.Position = 0;
                read(ser, ms);
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
                Stopwatch readWatch = Stopwatch.StartNew();
                for (int i = 0; i < loop; i++)
                {
                    ms.Position = 0;
                    read(ser, ms);
                }
                readWatch.Stop();

                Console.WriteLine("{0}:\tread: {1}μs/item; write: {2}μs/item; bytes: {3})", caption,
                    (readWatch.ElapsedMilliseconds * 1000.0) / loop,
                    (writeWatch.ElapsedMilliseconds * 1000.0) / loop, ms.Length);
            }
        }
    }
}
#endif