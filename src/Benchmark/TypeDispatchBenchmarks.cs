// Isolates ONE question: if TypeModel's core read/write entry points became virtual so a generated
// model could steer them, what does the steering itself cost? No serialization here at all - every
// benchmark resolves a contract to an int index and nothing else, so what is compared is the
// dispatch and only the dispatch.
//
// net8-only deliberately: the question is about the modern runtime, and the net472 leg of this
// project has nothing to say about it. The companion TypeDispatch.generated.cs holds sixty-four
// empty contract types and three sizes of dispatch chain; see notes/gaps.md B40.
#if NET8_0_OR_GREATER
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;

namespace Benchmark
{
    /// <summary>Per-T registration slot: 0 means "no helper", so absence is free to test.</summary>
    public static class Helper<T>
    {
        public static int Index;
    }

    public static partial class TypeDispatch
    {
        public static Dictionary<Type, int> Map = new();

        public static void BuildMap(int size)
        {
            Map = new Dictionary<Type, int>(size);
            for (var i = 0; i < size; i++) Map[Types[i]] = i;
        }
    }

    /// <summary>
    /// The four strategies, across the three input shapes the real APIs actually have.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The input shape is the axis that matters most, and it is not a free choice</b> - it is
    /// fixed by whichever signature is being made virtual:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Generic</b> - Serialize&lt;T&gt; / Deserialize&lt;T&gt;. T is known to
    /// the JIT, but for a REFERENCE type the code is shared across instantiations (__Canon), so
    /// typeof(T) == typeof(Foo) is a real runtime comparison rather than a folded constant. That is
    /// the whole reason the if-chain is worth measuring rather than assuming away.</description></item>
    /// <item><description><b>Object</b> - the non-generic Serialize(Stream, object). The only shape
    /// where a C# type-pattern switch is even expressible.</description></item>
    /// <item><description><b>Type</b> - the non-generic Deserialize(Stream, object, Type), where the
    /// contract arrives as a Type value and there is no instance to test. A pattern switch cannot
    /// express this at all, which is worth knowing before designing around one.</description></item>
    /// </list>
    /// <para>
    /// <b>Where the value sits in the chain is the second axis</b>, which is why the chain arms run
    /// three ways: First (trivially favourable), Last (worst), and Rotating (every type in turn -
    /// realistic for a shared model, and the case that defeats branch prediction). One "average"
    /// number would hide all of that.
    /// </para>
    /// <para>
    /// VirtualOnly is the floor: an abstract method overridden once, returning a constant. Every
    /// other number should be read as THAT PLUS the dispatch, since making the API virtual costs the
    /// call whether or not anything is steered.
    /// </para>
    /// </remarks>
    [SimpleJob(RuntimeMoniker.Net80), MemoryDiagnoser]
    public class TypeDispatchBenchmarks
    {
        public enum Position { First, Last, Rotating }

        [Params(4, 16, 64)] public int Size { get; set; }
        [Params(Position.First, Position.Last, Position.Rotating)] public Position Where { get; set; }

        private object[] _objects = Array.Empty<object>();
        private Type[] _types = Array.Empty<Type>();
        private Floor _floor;
        private int _cursor;

        private abstract class Floor { public abstract int Index(object value); }
        private sealed class RealFloor : Floor { public override int Index(object value) => 0; }

        [GlobalSetup]
        public void Setup()
        {
            TypeDispatch.BuildMap(Size);
            TypeDispatch.RegisterHelpers();
            _floor = new RealFloor();

            var count = Where == Position.Rotating ? Size : 1;
            _objects = new object[count];
            _types = new Type[count];
            for (var i = 0; i < count; i++)
            {
                var index = Where switch
                {
                    Position.First => 0,
                    Position.Last => Size - 1,
                    _ => i,
                };
                _objects[i] = TypeDispatch.Instances[index];
                _types[i] = TypeDispatch.Types[index];
            }
            _cursor = 0;
        }

        private object NextObject()
        {
            var value = _objects[_cursor];
            if (++_cursor == _objects.Length) _cursor = 0;
            return value;
        }

        private Type NextType()
        {
            var value = _types[_cursor];
            if (++_cursor == _types.Length) _cursor = 0;
            return value;
        }

        [Benchmark(Baseline = true, Description = "virtual call only")]
        public int VirtualOnly() => _floor.Index(NextObject());

        [Benchmark(Description = "object: dictionary")]
        public int Object_Dictionary()
            => TypeDispatch.Map.TryGetValue(NextObject().GetType(), out var index) ? index : -1;

        [Benchmark(Description = "object: if-chain")]
        public int Object_IfChain() => Size switch
        {
            4 => TypeDispatch.IfChainObject4(NextObject()),
            16 => TypeDispatch.IfChainObject16(NextObject()),
            _ => TypeDispatch.IfChainObject64(NextObject()),
        };

        [Benchmark(Description = "object: type-pattern switch")]
        public int Object_TypeSwitch() => Size switch
        {
            4 => TypeDispatch.TypeSwitch4(NextObject()),
            16 => TypeDispatch.TypeSwitch16(NextObject()),
            _ => TypeDispatch.TypeSwitch64(NextObject()),
        };

        [Benchmark(Description = "Type: dictionary")]
        public int Type_Dictionary()
            => TypeDispatch.Map.TryGetValue(NextType(), out var index) ? index : -1;

        [Benchmark(Description = "Type: if-chain")]
        public int Type_IfChain() => Size switch
        {
            4 => TypeDispatch.IfChainType4(NextType()),
            16 => TypeDispatch.IfChainType16(NextType()),
            _ => TypeDispatch.IfChainType64(NextType()),
        };
    }

    /// <summary>
    /// The generic shape, split out because T is fixed at the call site - there is no "rotating" to
    /// model, and position in the chain is a property of which T the caller named.
    /// </summary>
    /// <remarks>
    /// Helper&lt;T&gt;.Index is the interesting one: a static field on a per-T generic type, so
    /// there is no lookup at all - the JIT resolves the field address per instantiation. The
    /// registration cost is paid once at model construction, and 0 means "not mine", so a model that
    /// does not know a type answers without branching on a lookup result.
    /// </remarks>
    [SimpleJob(RuntimeMoniker.Net80), MemoryDiagnoser]
    public class GenericDispatchBenchmarks
    {
        [GlobalSetup]
        public void Setup()
        {
            TypeDispatch.BuildMap(64);
            TypeDispatch.RegisterHelpers();
        }

        [Benchmark(Baseline = true, Description = "Helper<T>.Index (first)")]
        public int Helper_First() => Helper<TypeDispatch.C0>.Index;

        [Benchmark(Description = "Helper<T>.Index (last)")]
        public int Helper_Last() => Helper<TypeDispatch.C63>.Index;

        [Benchmark(Description = "dictionary (first)")]
        public int Dictionary_First()
            => TypeDispatch.Map.TryGetValue(typeof(TypeDispatch.C0), out var i) ? i : -1;

        [Benchmark(Description = "dictionary (last)")]
        public int Dictionary_Last()
            => TypeDispatch.Map.TryGetValue(typeof(TypeDispatch.C63), out var i) ? i : -1;

        [Benchmark(Description = "if-chain over typeof(T), 64 arms (first)")]
        public int IfChain_First() => TypeDispatch.IfChainGeneric64<TypeDispatch.C0>();

        [Benchmark(Description = "if-chain over typeof(T), 64 arms (last)")]
        public int IfChain_Last() => TypeDispatch.IfChainGeneric64<TypeDispatch.C63>();
    }
}
#endif
