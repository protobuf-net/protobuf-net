using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace LongDataTests
{
    public class LongDataTests
    {
        private ITestOutputHelper Log { get; }
        public LongDataTests(ITestOutputHelper _log) => Log = _log;

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
                return (hash * -17) + (SomeString?.GetHashCode() ?? 0);
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
                const int hash = -12323424;
                return (hash * -17) + (Group?.GetHashCode() ?? 0);
            }
            [ProtoMember(2, DataFormat = DataFormat.Group)]
            public MyModelOuter Group { get; set; }
        }
        private static MyModelOuter CreateOuterModel(int count)
        {
            var obj = new MyModelOuter();
            for (int i = 0; i < count; i++)
                obj.Items.Add(new MyModelInner { Id = i, SomeString = "a long string that will be repeated lots and lots of times in the output data" });
            return obj;
        }
#pragma warning disable xUnit1004 // Test methods should not be skipped
        [Fact(Skip="long running")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
        public void CanSerializeLongData()
        {
            Log.WriteLine($"PID: {Process.GetCurrentProcess().Id}");
            const string path = "large.data";
            var watch = Stopwatch.StartNew();
            const int COUNT = 50000000;

            Log.WriteLine($"Creating model with {COUNT} items...");
            var outer = CreateOuterModel(COUNT);
            watch.Stop();
            Log.WriteLine($"Created in {watch.ElapsedMilliseconds}ms");

            var model = new MyModelWrapper { Group = outer };
            int oldHash = model.GetHashCode();
            using (var file = File.Create(path))
            {
                Log.WriteLine("Serializing...");
                watch = Stopwatch.StartNew();
                Serializer.Serialize(file, model);
                watch.Stop();
                Log.WriteLine($"Wrote: {COUNT} in {file.Length >> 20} MiB ({file.Length / COUNT} each), {watch.ElapsedMilliseconds}ms");
            }

            using (var file = File.OpenRead(path))
            {
                Log.WriteLine($"Verifying {file.Length >> 20} MiB...");
                watch = Stopwatch.StartNew();
                var state = ProtoReader.State.Create(file, null, null);
                try
                {
                    int i = -1;
                    try
                    {
                        Assert.Equal(2, state.ReadFieldHeader());
                        Assert.Equal(WireType.StartGroup, state.WireType);
                        var tok = state.StartSubItem();

                        for (i = 0; i < COUNT; i++)
                        {
                            Assert.Equal(1, state.ReadFieldHeader());
                            Assert.Equal(WireType.String, state.WireType);
                            state.SkipField();
                        }
                        Assert.False(state.ReadFieldHeader() > 0);
                        state.EndSubItem(tok);
                    }
                    catch
                    {
                        Log.WriteLine($"field 2, {i} of {COUNT}, @ {state.GetPosition()}");
                        throw;
                    }
                }
                finally
                {
                    state.Dispose();
                }
                watch.Start();
                Log.WriteLine($"Verified {file.Length >> 20} MiB in {watch.ElapsedMilliseconds}ms");
            }
            using (var file = File.OpenRead(path))
            {
                Log.WriteLine($"Deserializing {file.Length >> 20} MiB");
                watch = Stopwatch.StartNew();
                var clone = Serializer.Deserialize<MyModelWrapper>(file);
                watch.Stop();
                var newHash = clone.GetHashCode();
                Log.WriteLine($"{oldHash} vs {newHash}, {newHash == oldHash}, {watch.ElapsedMilliseconds}ms");
            }
        }
    }
}
