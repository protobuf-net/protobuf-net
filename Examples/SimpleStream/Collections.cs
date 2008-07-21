using System.Collections.Generic;
using ProtoBuf;
using System.IO;
using System;

namespace Examples.SimpleStream
{
    class Collections
    {
        [ProtoContract]
        public class Foo
        {
            public Foo() { Bars = new List<Bar>(); }
            [ProtoMember(1)]
            public List<Bar> Bars { get; private set; }
        }
        [ProtoContract]
        public class Bar
        {
            [ProtoMember(1)]
            public int Value { get; set; }
        }
        public static bool RunCollectionTests()
        {
            Foo foo = new Foo(), clone;
            for (int i = Int16.MinValue; i <= Int16.MaxValue; i++)
            {
                foo.Bars.Add(new Bar { Value = i });
            }
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, foo);
                Console.WriteLine("\t{0} items; {1} bytes", foo.Bars.Count, ms.Length);
                ms.Position = 0;
                clone = Serializer.Deserialize<Foo>(ms);
            }

            if (clone.Bars.Count != foo.Bars.Count)
            {
                Console.WriteLine("\t### data count mismatch###");
                return false;
            }
            else
            {
                bool fail = false;
                int count = clone.Bars.Count;
                for (int i = 0; i < count; i++)
                {

                    if (foo.Bars[i].Value != clone.Bars[i].Value)
                    {
                        Console.WriteLine("\t### data mismatch at index {0}: {1} vs {2}",
                            i, foo.Bars[i].Value, clone.Bars[i].Value);
                        fail = true;
                        break;
                    }                    
                }
                Console.WriteLine(fail ? "\tTest failed" : "\tTest passed");
                return !fail;
            }
        }
    }
}
