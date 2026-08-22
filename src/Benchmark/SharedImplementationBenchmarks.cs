// Marc: "in both generic and non-generic cases, can we pass the helper/delegate IN to a single
// shared implementation to avoid all the duplication?"
//
// The shape being asked about is that the generated override collapses to one expression -
//
//     public override long Serialize<T>(Stream dest, T value, object userState)
//         => SerializeCore(dest, value, Helper<T>.Instance, userState);
//
// - with the whole writer setup living once in the base rather than being emitted per model. The
// question is whether handing the resolved thing across a call boundary costs anything the baked-in
// version would not pay, which is a JIT question and therefore a measurement rather than an opinion.
//
// Every arm is [MethodImpl(NoInlining)] on the core, so all three pay exactly one real call; what
// differs is only what the callee knows about its argument.
#if NET8_0_OR_GREATER
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Runtime.CompilerServices;

namespace Benchmark
{
    /// <summary>
    /// Three ways to reach the same work: baked into the caller, passed as an interface, or passed
    /// as a generic type parameter constrained to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The third is not available everywhere, and that is the structural finding rather than the
    /// timing.</b> <c>Core&lt;T, TThing&gt;(value, thing) where TThing : IThing&lt;T&gt;</c> needs
    /// the caller to know the concrete services type <i>and</i> the contract type. A generic
    /// override cannot: inside <c>Serialize&lt;T&gt;</c> the <c>T</c> is open, and the services
    /// type implements <c>IThing&lt;T&gt;</c> only for its own contracts, which the compiler cannot
    /// prove for an open <c>T</c>. So the generic path can only use the interface form; the
    /// generic-parameter form is available to per-contract typed overloads, where both are known.
    /// </para>
    /// <para>
    /// Note also that for REFERENCE type arguments the generic form is shared code anyway
    /// (<c>__Canon</c>), so it does not specialise and the constraint buys no devirtualisation -
    /// unlike a struct serializer, which protobuf-net does not use here.
    /// </para>
    /// </remarks>
    [SimpleJob(RuntimeMoniker.Net80), MemoryDiagnoser]
    public class SharedImplementationBenchmarks
    {
        private readonly Services64 _services = new();
        private readonly TypeDispatch.C63 _value = new();
        private readonly NonGenericShapeBenchmarks.Handler _handler = new Passthrough();

        private sealed class Passthrough : NonGenericShapeBenchmarks.Handler
        {
            public override int Write(object value) => ((TypeDispatch.C63)value).Value + 1;
            public override int Read(object value) => 0;
            public override int Measure(object value) => 0;
        }

        [GlobalSetup]
        public void Setup() => _services.Register();

        // ---------------------------------------------------------------- generic

        /// <summary>Baked: the caller knows the concrete services type. What duplication buys.</summary>
        [Benchmark(Baseline = true, Description = "generic: baked into the caller")]
        public int Generic_Baked() => Baked(_value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int Baked(TypeDispatch.C63 value) => ((IThing<TypeDispatch.C63>)_services).Do(value);

        /// <summary>Passed as an interface - the only form a generic override can actually use.</summary>
        [Benchmark(Description = "generic: passed in as IThing<T>")]
        public int Generic_PassedInterface()
            => CoreInterface(_value, ModelOf64.Helper<TypeDispatch.C63>.Instance);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CoreInterface<T>(T value, IThing<T> thing) => thing.Do(value);

        /// <summary>Passed as a constrained type parameter - only usable where both types are known.</summary>
        [Benchmark(Description = "generic: passed in as TThing : IThing<T>")]
        public int Generic_PassedGeneric()
            => CoreGeneric<TypeDispatch.C63, Services64>(_value, _services);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CoreGeneric<T, TThing>(T value, TThing thing) where TThing : IThing<T>
            => thing.Do(value);

        // ---------------------------------------------------------------- non-generic

        /// <summary>Baked: the caller knows the concrete handler.</summary>
        [Benchmark(Description = "non-generic: baked into the caller")]
        public int NonGeneric_Baked() => BakedHandler(_value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int BakedHandler(object value) => ((Passthrough)_handler).Write(value);

        /// <summary>Passed as the abstract handler - what a shared core would take.</summary>
        [Benchmark(Description = "non-generic: passed in as Handler")]
        public int NonGeneric_PassedHandler() => CoreHandler(_value, _handler);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CoreHandler(object value, NonGenericShapeBenchmarks.Handler handler)
            => handler.Write(value);
    }
}
#endif
