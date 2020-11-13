using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Benchmark
{
    [SimpleJob(RuntimeMoniker.Net472), SimpleJob(RuntimeMoniker.NetCoreApp31), SimpleJob(RuntimeMoniker.NetCoreApp50), MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public partial class DeserializeBenchmarks
    {
        [BenchmarkCategory("Google")]
        [Benchmark(Description = "Array")]
        public protoc.Database Google_Array()
        {
            return protoc.Database.Parser.ParseFrom(_data);
        }

        [BenchmarkCategory("Google")]
        [Benchmark(Description = "MemoryStream")]
        public protoc.Database Google_MemoryStream()
        {
            return protoc.Database.Parser.ParseFrom(ExposableData());
        }

        private byte[] _data;

        internal void Verify(byte[] data, int length)
        {
            if(_data.Length != length)
                throw new InvalidOperationException($"Length mismatch; {_data.Length} vs {length}");
            for (int i = 0; i < _data.Length;i++)
            {
                if (_data[i] != data[i])
                    throw new InvalidOperationException($"Data mismatch at offset {i}; {Convert.ToString(_data[i], 16)} vs {Convert.ToString(data[i], 16)}");
            }
            Console.WriteLine($"verified: {length} bytes");
        }


        [BenchmarkCategory("MS_Legacy")]
        [Benchmark(Description = "Default")]
        public protogen.Database MemoryStream_Legacy_Default() => MemoryStream_Legacy(RuntimeTypeModel.Default);

        [BenchmarkCategory("MS_Legacy")]
        [Benchmark(Description = "CIP")]
        public protogen.Database MemoryStream_Legacy_CIP() => MemoryStream_Legacy(_cip);

        [BenchmarkCategory("MS_Legacy")]
        [Benchmark(Description = "C")]
        public protogen.Database MemoryStream_Legacy_C() => MemoryStream_Legacy(_c);

#if NEW_API
        [BenchmarkCategory("MS_Legacy")]
        [Benchmark(Description = "Auto")]
        public protogen.Database MemoryStream_Legacy_Auto() => MemoryStream_Legacy(_auto);
#endif

        [BenchmarkCategory("MS_Legacy")]
        [Benchmark(Description = "Dll")]
        public protogen.Database MemoryStream_Legacy_Dll() => MemoryStream_Legacy(_dll);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static TypeModel Throw() => throw new NotSupportedException();

        private protogen.Database MemoryStream_Legacy(TypeModel model)
        {
#pragma warning disable CS0618
            using var reader = ProtoReader.Create(ExposableData(), model ?? Throw());
            return (protogen.Database)model.Deserialize(reader, null, typeof(protogen.Database));
#pragma warning restore CS0618
        }

        private MemoryStream _exposable;
        private Stream ExposableData()
        {
            if (_exposable == null) _exposable = new MemoryStream(_data, 0, _data.Length, false, true);
            _exposable.Position = 0;
            return _exposable;
        }

        [GlobalSetup]
        public void Setup()
        {
            _data = File.ReadAllBytes("test.bin");
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(protogen.Database), true);
            model.Add(typeof(protogen.Order), true);
            model.Add(typeof(protogen.OrderLine), true);
//#if NEW_API
//            model.Add(typeof(protogen.pooled.Database));
//            model.Add(typeof(protogen.pooled.Order));
//            model.Add(typeof(protogen.pooled.OrderLine));
//#endif
            model.CompileInPlace();
            _cip = model;
            _c = model.Compile();
#if WRITE_DLL
            _dll = model.Compile("MySerializer", "DalSerializer.dll");
#endif
#if NEW_API
            _auto = RuntimeTypeModel.CreateForAssembly<protogen.Database>();
#endif
        }

#pragma warning disable CS0649, CS0169, IDE0044, IDE0051
        TypeModel _cip, _c, _auto, _dll;
#pragma warning restore CS0649, CS0169, IDE0044, IDE0051
    }
}
