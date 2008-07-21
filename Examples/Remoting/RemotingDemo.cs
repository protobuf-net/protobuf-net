#if REMOTING
using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using ProtoBuf;

namespace Examples.Remoting
{
    [Serializable]
    public sealed class RegularFragment
    {
        public int Foo { get; set; }
        public float Bar { get; set; }
    }
    [Serializable, ProtoContract]
    public sealed class ProtoFragment : ISerializable
    {
        [ProtoMember(1, DataFormat=DataFormat.TwosComplement)]
        public int Foo { get; set; }
        [ProtoMember(2)]
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

    static class RemotingDemo
    {
        public static void Run(int index)
        {
            Console.WriteLine(" (note; currently slower than regular remoting)");
            Console.WriteLine();

            AppDomain app = AppDomain.CreateDomain("Isolated");
            try
            {
                // create a server and two identical messages
                Server local = new Server(),
                    remote = (Server)app.CreateInstanceAndUnwrap(typeof(Server).Assembly.FullName, typeof(Server).FullName);
                RegularFragment frag1 = new RegularFragment { Foo = 27, Bar = 123.45F };
                ProtoFragment frag2 = new ProtoFragment { Foo = frag1.Foo, Bar = frag1.Bar };
                // verify basic transport
                RegularFragment localFrag1 = local.SomeMethod1(frag1),
                    remoteFrag1 = remote.SomeMethod1(frag1);
                ProtoFragment localFrag2 = local.SomeMethod2(frag2),
                    remoteFrag2 = remote.SomeMethod2(frag2);

                if (localFrag1.Foo == remoteFrag1.Foo && localFrag1.Bar == remoteFrag1.Bar
                    && localFrag2.Foo == remoteFrag2.Foo && localFrag2.Bar == remoteFrag2.Bar
                    && localFrag1.Foo == localFrag2.Foo && localFrag1.Bar == localFrag2.Bar)
                {
                    Console.WriteLine("\tMessages passed successfully; identical replies received");
                }
                else
                {
                    Console.WriteLine("\t*** Remoting messages did not match!!!");
                }
                const int LOOP = 10000;

                Stopwatch watch = Stopwatch.StartNew();
                for (int i = 0; i < LOOP; i++)
                {
                    localFrag1 = local.SomeMethod1(frag1);
                }
                watch.Stop();
                Console.WriteLine("\tLocal, Regular x{0}: {1:###,###,###} ticks", LOOP, watch.ElapsedTicks);

                watch = Stopwatch.StartNew();
                for (int i = 0; i < LOOP; i++)
                {
                    localFrag2 = local.SomeMethod2(frag2);
                }
                watch.Stop();
                Console.WriteLine("\tLocal, Proto x{0}: {1:###,###,###} ticks", LOOP, watch.ElapsedTicks);

                watch = Stopwatch.StartNew();
                for (int i = 0; i < LOOP; i++)
                {
                    remoteFrag1 = remote.SomeMethod1(frag1);
                }
                watch.Stop();
                Console.WriteLine("\tRemote, Regular x{0}: {1:###,###,###} ticks", LOOP, watch.ElapsedTicks);

                watch = Stopwatch.StartNew();
                for (int i = 0; i < LOOP; i++)
                {
                    remoteFrag2 = remote.SomeMethod2(frag2);
                }
                watch.Stop();
                Console.WriteLine("\tRemote, Proto x{0}: {1:###,###,###} ticks", LOOP, watch.ElapsedTicks);

                watch.Stop();

            }
            finally
            {
                AppDomain.Unload(app);
            }
        }
    }
}
#endif