using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.IO;

namespace Benchmark
{
    public class Program
    {
        [ProtoContract]
        class Foo
        {
            [ProtoMember(1)]
            public Bar Bar { get; set; }
        }
        [ProtoContract]
        class Bar
        {
            [ProtoMember(1)]
            public int Id { get; set; }
        }

        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Benchmarks>();
            Console.WriteLine(summary);

            //var ms = new MemoryStream();
            //Serializer.Serialize(ms, new Foo { Bar = new Bar { Id = 42 } });
            //var reader = ProtoReader.Create(
            //    new ReadOnlySequence<byte>(ms.GetBuffer(), 0, (int)ms.Length),
            //    RuntimeTypeModel.Default);

            //while(reader.ReadFieldHeader() > 0)
            //{
            //    Console.WriteLine($"pos {reader.Position}, field {reader.FieldNumber}, type {reader.WireType}");
            //    switch(reader.FieldNumber)
            //    {
            //        case 1:
            //            var tok = ProtoReader.StartSubItem(reader);
            //            Console.WriteLine(tok);
            //            while(reader.ReadFieldHeader() > 0)
            //            {
            //                Console.WriteLine($"\tpos {reader.Position}, field {reader.FieldNumber}, type {reader.WireType}");
            //                switch(reader.FieldNumber)
            //                {
            //                    case 1:
            //                        Console.WriteLine($"\tId={reader.ReadInt32()}");
            //                        break;
            //                    default:
            //                        throw new NotImplementedException();
            //                }
            //            }
            //            ProtoReader.EndSubItem(tok, reader);
            //            break;
            //        default:
            //            throw new NotImplementedException();
            //    }
            //}

            //var foo = (Foo)RuntimeTypeModel.Default.Deserialize(reader, null, typeof(Foo));
            //Console.WriteLine(foo.Bar.Id);
        }
    }

    [ClrJob, CoreJob, MemoryDiagnoser]
    public class Benchmarks
    {
        private MemoryStream _ms;
        private ReadOnlySequence<byte> _ros;

        [GlobalSetup]
        public void Setup()
        {
            var data = File.ReadAllBytes("nwind.proto.bin");
            _ms = new MemoryStream(data);
            _ros = new ReadOnlySequence<byte>(data);
        }

        [Benchmark(Baseline = true)]
        public void MemoryStream()
        {
            _ms.Position = 0;
            var model = RuntimeTypeModel.Default;
            using (var reader = ProtoReader.Create(_ms, model))
            {
                var dal = model.Deserialize(reader, null, typeof(DAL.Database));
            }
        }
        [Benchmark(Description = "ReadOnlySequence<byte>")]
        public void ReadOnlySequence()
        {
            var model = RuntimeTypeModel.Default;
            using (var reader = ProtoReader.Create(_ros, model))
            {
                var dal = model.Deserialize(reader, null, typeof(DAL.Database));
            }
        }
    }
}
