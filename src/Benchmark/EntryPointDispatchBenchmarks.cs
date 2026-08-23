// gap B40, the in-situ half. The isolated dispatch benchmarks (TypeDispatch, HelperShape,
// SharedImplementation) measured the MECHANISMS; this measures what a consumer actually pays on the
// three ways they can reach a generated model, on the same payload:
//
//   1. the typed overload the generator emits and PBN3010's fixer steers people onto;
//   2. the GENERIC TypeModel API, which already resolves through SerializerCache<TProvider, T> -
//      i.e. it is already the Helper<T> shape, so the interesting question is how much that leaves;
//   3. the NON-GENERIC TypeModel API, which goes SerializeRootFallback -> DynamicStub -> a
//      MakeGenericType. That one is both the slowest AND the AOT-hostile route, which is why it is
//      the target rather than the generic path.
//
// The receiver types are deliberately different: (1) needs the model's own static type, (2) and (3)
// are what anyone holding a TypeModel reference gets, and are exactly the call sites no analyzer
// can reach.
#if NET8_0_OR_GREATER
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Buffers;

namespace Benchmark
{
    [SimpleJob(RuntimeMoniker.Net80), MemoryDiagnoser]
    public class EntryPointDispatchBenchmarks
    {
        private DispatchPayload _value;
        private DispatchModel _typed;
        private TypeModel _model;
        private object _boxed;
        private InheritanceDepthBenchmarks.ReusableWriter _buffer;

        [GlobalSetup]
        public void Setup()
        {
            _value = new DispatchPayload { Id = 42, Name = "dispatch", Extra = 7 };
            _boxed = _value;
            _typed = (DispatchModel)Activator.CreateInstance(typeof(DispatchModel), nonPublic: true);
            _model = _typed;
            _buffer = new InheritanceDepthBenchmarks.ReusableWriter();

            // all three routes must agree on the bytes, or the comparison is meaningless
            static byte[] Bytes(Action<IBufferWriter<byte>> write)
            {
                var w = new InheritanceDepthBenchmarks.ReusableWriter();
                write(w);
                return w.Written.ToArray();
            }
            var a = Bytes(w => _typed.Serialize(w, _value));
            var b = Bytes(w => _model.Serialize<DispatchPayload>(w, _value));
            var c = Bytes(w => _model.Serialize(w, _boxed));
            if (BitConverter.ToString(a) != BitConverter.ToString(b)
                || BitConverter.ToString(a) != BitConverter.ToString(c))
            {
                throw new InvalidOperationException("the three entry points disagree on the bytes:"
                    + Environment.NewLine + "typed       " + BitConverter.ToString(a)
                    + Environment.NewLine + "generic     " + BitConverter.ToString(b)
                    + Environment.NewLine + "non-generic " + BitConverter.ToString(c));
            }
        }

        [Benchmark(Baseline = true, Description = "typed overload (model-typed receiver)")]
        public long Typed()
        {
            _buffer.Reset();
            return _typed.Serialize(_buffer, _value);
        }

        [Benchmark(Description = "generic TypeModel.Serialize<T>")]
        public long Generic()
        {
            _buffer.Reset();
            return _model.Serialize<DispatchPayload>(_buffer, _value);
        }

        [Benchmark(Description = "non-generic TypeModel.Serialize(object)")]
        public long NonGeneric()
        {
            _buffer.Reset();
            _model.Serialize(_buffer, _boxed);
            return 0;
        }
    }

    [ProtoContract]
    public class DispatchPayload
    {
        [ProtoMember(1)] public int Id { get; set; }
        [ProtoMember(2)] public string Name { get; set; }
        [ProtoMember(3)] public int Extra { get; set; }
    }

    [ProtoModel]
    [ProtoSerializable(typeof(DispatchPayload))]
    public partial class DispatchModel : TypeModel { }
}
#endif
