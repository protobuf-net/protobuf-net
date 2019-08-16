using BenchmarkDotNet.Attributes;
using Pipelines.Sockets.Unofficial.Buffers;
using ProtoBuf.Meta;
using protogen;
using System;
using System.IO;

namespace ProtoBuf
{
    [ClrJob, CoreJob, MemoryDiagnoser]
    public class SerializeBenchmarks
    {
        private byte[] _data;
        private RuntimeTypeModel _cip;
#pragma warning disable IDE0044, IDE0051, IDE0052
        private TypeModel _c, _dll, _auto;
#pragma warning restore IDE0044, IDE0051, IDE0052
        private Database _database;

        [GlobalSetup]
        public void Setup()
        {
            _data = File.ReadAllBytes("test.bin");
            var model = TypeModel.Create();
            model.Add(typeof(protogen.Database), true);
            model.Add(typeof(protogen.Order), true);
            model.Add(typeof(protogen.OrderLine), true);
            model.CompileInPlace();
            _cip = model;
            _c = model.Compile();
#if !(NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP3_0)
            _dll = model.Compile("MySerializer", "DalSerializer.dll");
#endif
            _auto = TypeModel.CreateForAssembly<protogen.Database>();

            using (var reader = ProtoReader.Create(out var state, Exposable(_data), model))
            {
                _database = (protogen.Database)model.Deserialize(reader, ref state, null, typeof(protogen.Database));
            }
        }

        private static MemoryStream Exposable(byte[] data)
            => new MemoryStream(data, 0, data.Length, false, true);

        const int OperationsPerInvoke = 128;

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void MemoryStream_CIP() => MemoryStream(_cip);
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void MemoryStream_C() => MemoryStream(_c);
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void MemoryStream_Auto() => MemoryStream(_auto);

        private void MemoryStream(TypeModel model)
        {
            using (var buffer = new MemoryStream())
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    using (var writer = ProtoWriter.Create(out var state, buffer, model))
                    {
                        model.Serialize(writer, ref state, _database);
                        writer.Close(ref state);
                    }
                    AssertLength(buffer.Length);
                    buffer.Position = 0;
                    buffer.SetLength(0);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void BufferWriter_CIP() => BufferWriter(_cip);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void BufferWriter_C() => BufferWriter(_c);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void BufferWriter_Auto() => BufferWriter(_auto);

        private void BufferWriter(TypeModel model)
        {
            using (var buffer = BufferWriter<byte>.Create(64 * 1024))
            {
                for (int i = 0; i < OperationsPerInvoke; i++)
                {
                    using (var writer = ProtoWriter.Create(out var state, buffer, model))
                    {
                        model.Serialize(writer, ref state, _database);
                        writer.Close(ref state);
                    }
                    AssertLength(buffer.Length);
                    buffer.Flush().Dispose();
                }
            }
        }

        private void AssertLength(long actual)
        {
            var expected = _data.Length;
            if (expected != actual)
                throw new InvalidOperationException($"expected {expected}, actual {actual}");
        }


    }
}
