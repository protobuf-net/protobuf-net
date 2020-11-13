using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Google.Protobuf;
using ProtoBuf;
using ProtoBuf.Meta;
using protogen;
using System;
using System.IO;

namespace Benchmark
{
    [SimpleJob(RuntimeMoniker.Net472), SimpleJob(RuntimeMoniker.NetCoreApp31), SimpleJob(RuntimeMoniker.NetCoreApp50), MemoryDiagnoser]
    public partial class SerializeBenchmarks
    {

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void MemoryStream_Google()
        {
            using var ms = new MemoryStream();

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                ms.Position = 0;
                ms.SetLength(0);
                using var cos = new CodedOutputStream(ms, leaveOpen: true);
                _googleModel.WriteTo(cos);
            }
        }

        protoc.Database _googleModel;

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
            Console.WriteLine("Serializer written to " + Directory.GetCurrentDirectory());
#endif
#if NEW_API
            _auto = RuntimeTypeModel.CreateForAssembly<Database>();
#endif

#pragma warning disable CS0618
            using var reader = ProtoReader.Create(Exposable(_data), model);
            _database = (Database)model.Deserialize(reader, null, typeof(Database));
            _googleModel = protoc.Database.Parser.ParseFrom(_data);
#pragma warning restore CS0618
        }

        private static MemoryStream Exposable(byte[] data)
            => new MemoryStream(data, 0, data.Length, false, true);

        const int OperationsPerInvoke = 128;

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void MemoryStream_CIP() => MemoryStream_ViaWriter(_cip);
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void MemoryStream_C() => MemoryStream_ViaWriter(_c);

        private void MemoryStream_ViaWriter(TypeModel model)
        {
            using var buffer = new MemoryStream();
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
#pragma warning disable CS0618
                using (var writer = ProtoWriter.Create(buffer, model))
                {
                    model.Serialize(writer, _database);
                    writer.Close();
                }
#pragma warning restore CS0618
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
