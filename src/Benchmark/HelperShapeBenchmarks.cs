// Marc's two proposals, measured rather than reasoned about:
//
//   generic     - Helper<T> is LOCAL (nested in the generated model, so the slot is per-model rather
//                 than process-global, which was the objection to the int version) and holds an
//                 INTERFACE the services type implements for every T, so registration is
//                 Helper<Foo>.Instance = Instance; Helper<Bar>.Instance = Instance; and the call
//                 site gets a usable ISerializer<T> with no lookup, no int and no switch.
//
//   non-generic - harder, because there is no T to key on. "Maybe a delegate tuple?" - so: does a
//                 map straight to a delegate (or to a handler object) beat a map to an int followed
//                 by a switch? The first pass only measured reaching the int, which is half a job.
//
// The size axis is the same small/medium/huge ladder (8/64/512), because "how many interfaces does
// one services type implement" is a real cost question for interface dispatch, not just for chains.
#if NET8_0_OR_GREATER
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Benchmark
{
    /// <summary>Stands in for ISerializer&lt;T&gt;: one interface per contract, on one services type.</summary>
    public interface IThing<T>
    {
        int Do(T value);
    }

    /// <summary>
    /// The GENERIC proposal: a per-model <c>Helper&lt;T&gt;</c> holding an interface, against the
    /// int-plus-switch it replaces.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The interface form does strictly less work: there is no index to switch on, because the
    /// static already holds the thing you wanted. What it costs instead is an INTERFACE dispatch,
    /// and that is the part worth measuring at 512 - a services type implementing 512 interfaces
    /// puts real pressure on the runtime's interface dispatch cache, which no amount of reasoning
    /// will settle.
    /// </para>
    /// <para>
    /// <b>Read these numbers as an upper bound, not a measurement.</b> The first run of this file
    /// reported 0.0003 ns for every arm, which says the benchmark folded away rather than that
    /// dispatch is free; IThing.Do now reads a field off its argument so it cannot be constant-
    /// folded. Even so, a static field read plus a devirtualised interface call is close enough to
    /// nothing that the honest claim is "below what this harness can separate" - including at 512
    /// interfaces, which is the part that was actually in doubt.
    /// </para>
    /// </remarks>
    [SimpleJob(RuntimeMoniker.Net80), MemoryDiagnoser]
    public class HelperShapeBenchmarks
    {
        private readonly Services8 _s8 = new();
        private readonly Services64 _s64 = new();
        private readonly Services512 _s512 = new();
        private readonly TypeDispatch.C7 _c7 = new();
        private readonly TypeDispatch.C63 _c63 = new();
        private readonly TypeDispatch.C511 _c511 = new();

        [GlobalSetup]
        public void Setup()
        {
            _s8.Register();
            _s64.Register();
            _s512.Register();
            TypeDispatch.RegisterHelpers();
        }

        // the floor: a direct call on a known concrete type, nothing resolved at all
        [Benchmark(Baseline = true, Description = "direct call, no dispatch")]
        public int Direct() => ((IThing<TypeDispatch.C7>)_s8).Do(_c7);

        [Benchmark(Description = "Helper<T>.Instance, model of 8")]
        public int Interface_8() => ModelOf8.Helper<TypeDispatch.C7>.Instance.Do(_c7);

        [Benchmark(Description = "Helper<T>.Instance, model of 64")]
        public int Interface_64() => ModelOf64.Helper<TypeDispatch.C63>.Instance.Do(_c63);

        [Benchmark(Description = "Helper<T>.Instance, model of 512")]
        public int Interface_512() => ModelOf512.Helper<TypeDispatch.C511>.Instance.Do(_c511);

        // what it replaces: the int has to be turned back into a call, and the switch is that step
        [Benchmark(Description = "Helper<T>.Index then switch, model of 8")]
        public int IndexThenSwitch_8() => Helper<TypeDispatch.C7>.Index switch
        {
            0 => 0, 1 => 1, 2 => 2, 3 => 3, 4 => 4, 5 => 5, 6 => 6,
            7 => ((IThing<TypeDispatch.C7>)_s8).Do(_c7),
            _ => -1,
        };
    }

    /// <summary>
    /// The NON-GENERIC proposal: having paid for a lookup, what is the cheapest way to turn its
    /// answer into a call?
    /// </summary>
    /// <remarks>
    /// <para>
    /// The earlier pass measured reaching an <c>int</c> and stopped there, which is half a job - an
    /// int is not a call. Three ways to finish it, all keyed on the type handle since that was
    /// already shown to be worth ~2x over keying on <c>Type</c>:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>int then switch</b> - a dense jump table, but it needs a switch arm per
    /// contract in the generated source, so it is the option whose SOURCE grows with the model;</description></item>
    /// <item><description><b>delegate</b> - the map holds the call directly. One indirection, no
    /// generated switch;</description></item>
    /// <item><description><b>delegate tuple</b> - a struct of the several delegates a contract needs
    /// (write / read / measure), so one lookup serves all of them - measured both COPIED out of the
    /// map and reached by <c>ref</c> through <c>CollectionsMarshal.GetValueRefOrNullRef</c>, which
    /// removes the copy entirely;</description></item>
    /// <item><description><b>handler object</b> - an abstract class with the same three as virtual
    /// methods. Same indirection count as a delegate, one allocation per contract instead of
    /// three, and it can carry state.</description></item>
    /// </list>
    /// </remarks>
    [SimpleJob(RuntimeMoniker.Net80), MemoryDiagnoser]
    public class NonGenericShapeBenchmarks
    {
        public abstract class Handler
        {
            public abstract int Write(object value);
            public abstract int Read(object value);
            public abstract int Measure(object value);
        }

        private sealed class RealHandler : Handler
        {
            private readonly int _index;
            public RealHandler(int index) => _index = index;
            public override int Write(object value) => _index;
            public override int Read(object value) => _index;
            public override int Measure(object value) => _index;
        }

        public readonly struct Trio
        {
            public Trio(Func<object, int> write, Func<object, int> read, Func<object, int> measure)
            {
                Write = write; Read = read; Measure = measure;
            }
            public readonly Func<object, int> Write, Read, Measure;
        }

        [Params(8, 64, 512)] public int Size { get; set; }

        private Dictionary<IntPtr, int> _toIndex;
        private Dictionary<IntPtr, Func<object, int>> _toDelegate;
        private Dictionary<IntPtr, Trio> _toTrio;
        private Dictionary<IntPtr, Handler> _toHandler;
        private object[] _objects;
        private int _cursor;

        [GlobalSetup]
        public void Setup()
        {
            _toIndex = new Dictionary<IntPtr, int>(Size);
            _toDelegate = new Dictionary<IntPtr, Func<object, int>>(Size);
            _toTrio = new Dictionary<IntPtr, Trio>(Size);
            _toHandler = new Dictionary<IntPtr, Handler>(Size);
            _objects = new object[Size];
            for (var i = 0; i < Size; i++)
            {
                var index = i;
                var handle = TypeDispatch.Types[i].TypeHandle.Value;
                _toIndex[handle] = index;
                Func<object, int> one = _ => index;
                _toDelegate[handle] = one;
                _toTrio[handle] = new Trio(one, _ => index, _ => index);
                _toHandler[handle] = new RealHandler(index);
                _objects[i] = TypeDispatch.Instances[i];
            }
            _cursor = 0;
        }

        private object NextObject()
        {
            var value = _objects[_cursor];
            if (++_cursor == _objects.Length) _cursor = 0;
            return value;
        }

        // the lookup alone, for reference: the "half a job" number from the earlier pass
        [Benchmark(Baseline = true, Description = "lookup to int only (no call)")]
        public int LookupOnly()
            => _toIndex.TryGetValue(NextObject().GetType().TypeHandle.Value, out var i) ? i : -1;

        [Benchmark(Description = "lookup to delegate, then invoke")]
        public int Delegate()
        {
            var value = NextObject();
            return _toDelegate.TryGetValue(value.GetType().TypeHandle.Value, out var d) ? d(value) : -1;
        }

        [Benchmark(Description = "lookup to delegate TUPLE, then invoke one")]
        public int DelegateTuple()
        {
            var value = NextObject();
            return _toTrio.TryGetValue(value.GetType().TypeHandle.Value, out var t) ? t.Write(value) : -1;
        }

        [Benchmark(Description = "lookup to handler object, then virtual call")]
        public int HandlerObject()
        {
            var value = NextObject();
            return _toHandler.TryGetValue(value.GetType().TypeHandle.Value, out var h) ? h.Write(value) : -1;
        }

        /// <summary>
        /// The tuple again, but reached by <c>ref</c> instead of copied out (Marc):
        /// <c>CollectionsMarshal.GetValueRefOrNullRef</c> hands back a reference INTO the entry, so
        /// a three-delegate struct costs no more to reach than a single one.
        /// </summary>
        /// <remarks>
        /// The returned ref is invalidated by any mutation of the dictionary, which is exactly why
        /// it fits here: a model's table is built at construction and never written again. The
        /// absence test is <c>Unsafe.IsNullRef</c> rather than a bool out-parameter - the sibling
        /// <c>GetValueRefOrAddDefault</c> is the "add it if missing" form, and is not what this
        /// wants.
        /// </remarks>
        [Benchmark(Description = "delegate tuple by REF (GetValueRefOrNullRef)")]
        public int DelegateTupleByRef()
        {
            var value = NextObject();
            ref var trio = ref CollectionsMarshal.GetValueRefOrNullRef(
                _toTrio, value.GetType().TypeHandle.Value);
            return Unsafe.IsNullRef(ref trio) ? -1 : trio.Write(value);
        }
    }
}
#endif
