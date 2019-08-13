#if PLAT_SPANS

using DAL;
using Pipelines.Sockets.Unofficial.Buffers;
using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.NWind
{
    public class CompatDbBuffers
    {
        [Fact]
        public void RoundTripViaBuffers()
        {
            var path = NWindTests.GetNWindBinPath();
            var contents = File.ReadAllBytes(path);

            DatabaseCompat db;
            using (var reader = ProtoReader.Create(out var readState, contents, RuntimeTypeModel.Default))
            {
                db = (DatabaseCompat)reader.Model.Deserialize(reader, ref readState, null, typeof(DatabaseCompat));
            }

            int count = db.Orders.Count;

            Assert.Equal(830, count);

            using (var bw = BufferWriter<byte>.Create())
            {
                using (var writer = ProtoWriter.Create(out var writeState, bw.Writer, RuntimeTypeModel.Default))
                {
                    writer.Model.Serialize(writer, ref writeState, db);
                }
                Assert.Equal(contents.Length, bw.Length);
            }
        }
    }
}
#endif