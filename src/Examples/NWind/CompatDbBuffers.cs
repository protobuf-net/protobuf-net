#if PLAT_SPANS

using DAL;
using Examples;
using Pipelines.Sockets.Unofficial.Buffers;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.NWind
{
    public class CompatDbBuffers
    {
        private readonly ITestOutputHelper Log;
        public CompatDbBuffers(ITestOutputHelper log)
        {
            Log = log;
            Serializer.PrepareSerializer<DatabaseCompat>();
        }

        [Fact]
        public void GenerateModel()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(DatabaseCompat), true);
            model.Compile("CompatDbBuffers", "CompatDbBuffers.dll");
            Log?.WriteLine(Path.Combine(Environment.CurrentDirectory, "CompatDbBuffers.dll"));
            PEVerify.AssertValid("CompatDbBuffers.dll");
        }

        private DatabaseCompat LoadFromDisk(out ReadOnlySpan<byte> contents)
        {
            var path = NWindTests.GetNWindBinPath();
            byte[] arr = File.ReadAllBytes(path);
            Log?.WriteLine($"{arr.Length} bytes loaded from {path}");
            contents = arr;
            using var readState = ProtoReader.State.Create(arr, RuntimeTypeModel.Default);
            var watch = Stopwatch.StartNew();
            var obj = (DatabaseCompat)readState.DeserializeRoot<DatabaseCompat>(null);
            watch.Stop();
            Log?.WriteLine($"Deserialized: {watch.ElapsedMilliseconds}ms");
            return obj;
        }

        void Write(DatabaseCompat db, ref ProtoWriter.State state)
        {
            try
            {
                var watch = Stopwatch.StartNew();
                state.SerializeRoot(db);
                state.Close();
                watch.Stop();
                Log?.WriteLine($"Serialized: {watch.ElapsedMilliseconds}ms");
            }
            catch
            {
                state.Abandon();
                throw;
            }
        }

        [Fact]
        public void RoundTripViaStream()
        {
            var db = LoadFromDisk(out var contents);
            Assert.Equal(830, db.Orders.Count);

            using var ms = new MemoryStream();
            var writeState = ProtoWriter.State.Create(ms, RuntimeTypeModel.Default);
            try
            {
                Write(db, ref writeState);
            }
            finally
            {
                writeState.Dispose();
            }
            Assert.Equal(contents.Length, ms.Length);

            Assert.True(ms.TryGetBuffer(out var buffer));
            var span = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);
            Assert.True(contents.SequenceEqual(span));
            Log?.WriteLine($"{span.Length} bytes verified");
        }

        [Fact]
        public void RoundTripViaBuffers()
        {
            var db = LoadFromDisk(out var contents);
            Assert.Equal(830, db.Orders.Count);

            using var bw = BufferWriter<byte>.Create();
            var writeState = ProtoWriter.State.Create(bw.Writer, RuntimeTypeModel.Default);
            try
            {
                Write(db, ref writeState);
            }
            finally
            {
                writeState.Dispose();
            }

            using var buffer = bw.Flush();
            var b = buffer.Value;
            int len = checked((int)b.Length);
            var arr = ArrayPool<byte>.Shared.Rent(len);
            try
            {
                var span = new Span<byte>(arr, 0, len);
                b.CopyTo(span);
                Assert.True(contents.SequenceEqual(span));
                Log?.WriteLine($"{span.Length} bytes verified");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(arr);
            }
        }
    }
}
#endif