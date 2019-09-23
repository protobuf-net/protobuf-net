using BenchmarkDotNet.Attributes;
using ProtoBuf;
using ProtoBuf.Meta;
using protogen;
using System;
using System.IO;

namespace Benchmark
{
    [ClrJob, CoreJob, MemoryDiagnoser]
    public partial class SerializeBenchmarks
    {
        private byte[] _data;
        private RuntimeTypeModel _cip;
#pragma warning disable IDE0044, IDE0051, IDE0052, CS0169
        private TypeModel _c, _dll, _auto;
#pragma warning restore IDE0044, IDE0051, IDE0052, CS0169
        private Database _database;

        [GlobalSetup]
        public void Setup()
        {
            _data = File.ReadAllBytes("test.bin");
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Database), true);
            model.Add(typeof(Order), true);
            model.Add(typeof(OrderLine), true);
            model.CompileInPlace();
            _cip = model;
            _c = model.Compile();
#if WRITE_DLL
            _dll = model.Compile("MySerializer", "DalSerializer.dll");
#endif
#if NEW_API
            _auto = RuntimeTypeModel.CreateForAssembly<Database>();
#endif

#pragma warning disable CS0618
            using var reader = ProtoReader.Create(Exposable(_data), model);
            _database = (Database)model.Deserialize(reader, null, typeof(Database));
#pragma warning restore CS0618
        }

        private static MemoryStream Exposable(byte[] data)
            => new MemoryStream(data, 0, data.Length, false, true);

        const int OperationsPerInvoke = 128;

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void MemoryStream_CIP() => MemoryStream(_cip);
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void MemoryStream_C() => MemoryStream(_c);

        private void MemoryStream(TypeModel model)
        {
            using var buffer = new MemoryStream();
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
#if NEW_API
                var state = ProtoWriter.State.Create(buffer, model);
                try
                {
                    state.Serialize(_database);
                    state.Close();
                }
                finally
                {
                    state.Dispose();
                }
#else
                using (var writer = ProtoWriter.Create(buffer, model))
                {
                    model.Serialize(writer, _database);
                    writer.Close();
                }
#endif
                AssertLength(buffer.Length);
                buffer.Position = 0;
                buffer.SetLength(0);
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
