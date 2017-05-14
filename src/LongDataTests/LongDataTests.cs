using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace LongDataTests
{
    public class LongDataTests
    {
        [ProtoContract]
        public class MyModelInner
        {
            [ProtoMember(1)]
            public int Id { get; set; }

            [ProtoMember(2)]
            public string SomeString { get; set; }
            public override int GetHashCode()
            {
                int hash = -12323424;
                hash = (hash * -17) + Id.GetHashCode();
                hash = (hash * -17) + (SomeString?.GetHashCode() ?? 0);
                return hash;
            }
        }

        [ProtoContract]
        public class MyModelOuter
        {
            [ProtoMember(1)]
            public List<MyModelInner> Items { get; } = new List<MyModelInner>();

            public override int GetHashCode()
            {
                int hash = -12323424;
                if (Items != null)
                {
                    hash = (hash * -17) + Items.Count.GetHashCode();
                    foreach (var item in Items)
                    {
                        hash = (hash * -17) + (item?.GetHashCode() ?? 0);
                    }
                }
                return hash;
            }
        }

        [ProtoContract]
        public class MyModelWrapper
        {
            public override int GetHashCode()
            {
                int hash = -12323424;
                hash = (hash * -17) + (LengthPrefix?.GetHashCode() ?? 0);
                hash = (hash * -17) + (Group?.GetHashCode() ?? 0);
                return hash;
            }
            [ProtoMember(1, DataFormat = DataFormat.Default)]
            public MyModelOuter LengthPrefix { get; set; }

            [ProtoMember(2, DataFormat = DataFormat.Group)]
            public MyModelOuter Group { get; set; }
        }
        static MyModelOuter CreateOuterModel(int count)
        {
            var obj = new MyModelOuter();
            for (int i = 0; i < count; i++)
                obj.Items.Add(new MyModelInner { Id = i, SomeString = "a long string that will be repeated lots and lots of times in the output data" });
            return obj;
        }
        [Fact]
        public void CanSerializeLongData()
        {
            Console.WriteLine($"PID: {Process.GetCurrentProcess().Id}");
            const string path = "large.data";
            Console.WriteLine("Creating model...");
            const int APPROX_COUNT = 20000000, CHUNKS = 10, CHUNKSIZE = APPROX_COUNT / CHUNKS, COUNT = CHUNKS * CHUNKSIZE;
            var outer = CreateOuterModel(COUNT);
            var model = new MyModelWrapper { Group = outer, LengthPrefix = outer };
            int oldHash = model.GetHashCode();
            using (var file = File.Create(path))
            {
                Console.Write("Serializing in pieces");
                var watch = Stopwatch.StartNew();
                for (int i = 0; i < CHUNKS; i++)
                {
                    var x = new MyModelOuter();
                    x.Items.AddRange(outer.Items.Skip(i * CHUNKS).Take(CHUNKSIZE));
                    Serializer.Serialize(file, new MyModelWrapper { LengthPrefix = x });
                    Console.Write('.');
                }
                for (int i = 0; i < CHUNKS; i++)
                {
                    var x = new MyModelOuter();
                    x.Items.AddRange(outer.Items.Skip(i * CHUNKS).Take(CHUNKSIZE));
                    Serializer.Serialize(file, new MyModelWrapper { Group = x });
                    Console.Write('.');
                }
                watch.Stop();
                Console.WriteLine();
                Console.WriteLine($"Wrote: {COUNT} in {file.Length >> 20} MiB ({file.Length / COUNT} each), {watch.ElapsedMilliseconds}ms");
            }

            using (var file = File.OpenRead(path))
            {
                Console.WriteLine($"Verifying {file.Length >> 20} MiB...");
                var watch = Stopwatch.StartNew();
                using (var reader = new ProtoReader(file, null, null))
                {
                    int i = -1;
                    try
                    {
                        for (int c = 0; c < CHUNKS; c++)
                        {
                            Assert.Equal(1, reader.ReadFieldHeader());
                            Assert.Equal(WireType.String, reader.WireType);
                            var tok = ProtoReader.StartSubItem(reader);

                            for (i = 0; i < CHUNKSIZE; i++)
                            {
                                Assert.Equal(1, reader.ReadFieldHeader());
                                Assert.Equal(WireType.String, reader.WireType);
                                reader.SkipField();
                            }
                            Assert.False(reader.ReadFieldHeader() > 0);
                            ProtoReader.EndSubItem(tok, reader);
                        }
                    }
                    catch
                    {
                        Console.WriteLine($"field 1, {i} of {COUNT}, @ {reader.LongPosition}");
                        throw;
                    }
                    try
                    {
                        for (int c = 0; c < CHUNKS; c++)
                        {
                            Assert.Equal(2, reader.ReadFieldHeader());
                            Assert.Equal(WireType.StartGroup, reader.WireType);
                            var tok = ProtoReader.StartSubItem(reader);

                            for (i = 0; i < CHUNKSIZE; i++)
                            {
                                Assert.Equal(1, reader.ReadFieldHeader());
                                Assert.Equal(WireType.String, reader.WireType);
                                reader.SkipField();
                            }
                            Assert.False(reader.ReadFieldHeader() > 0);
                            ProtoReader.EndSubItem(tok, reader);
                        }
                    }
                    catch
                    {
                        Console.WriteLine($"field 2, {i} of {COUNT}, @ {reader.LongPosition}");
                        throw;
                    }
                }
                watch.Start();
                Console.WriteLine($"Verified {file.Length >> 20} MiB in {watch.ElapsedMilliseconds}ms");
            }
            using (var file = File.OpenRead(path))
            {
                Console.WriteLine($"Deserializing {file.Length >> 20} MiB");
                var watch = Stopwatch.StartNew();
                var clone = Serializer.Deserialize<MyModelWrapper>(file);
                watch.Stop();
                var newHash = clone.GetHashCode();
                Console.WriteLine($"{oldHash} vs {newHash}, {newHash == oldHash}, {watch.ElapsedMilliseconds}ms");
            }
        }
    }
}
