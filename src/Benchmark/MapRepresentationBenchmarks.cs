// The follow-up TypeDispatchBenchmarks raises: for the NON-generic shapes there is no Helper<T>
// trick available, so a lookup is unavoidable - which makes "how good can the lookup be" the real
// question rather than "map or chain". Dictionary<Type,int> is the obvious spelling; it is not
// obviously the best one, because Type's equality and hashing go through virtual members, and the
// thing actually being compared is a type handle - a pointer.
//
// Same ladder as its parent (8/64/512) and the rotating case only, that being the realistic one for
// a shared model; the ends of the chain are a chain-specific concern and there are no chains here.
#if NET8_0_OR_GREATER
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;

namespace Benchmark
{
    [SimpleJob(RuntimeMoniker.Net80), MemoryDiagnoser]
    public class MapRepresentationBenchmarks
    {
        [Params(8, 64, 512)] public int Size { get; set; }

        private Dictionary<Type, int> _byType;
        private Dictionary<IntPtr, int> _byHandle;
        private IntPtr[] _sortedHandles;
        private int[] _sortedIndexes;
        private object[] _objects;
        private Type[] _types;
        private int _cursor;

        [GlobalSetup]
        public void Setup()
        {
            _byType = new Dictionary<Type, int>(Size);
            _byHandle = new Dictionary<IntPtr, int>(Size);
            var handles = new List<KeyValuePair<IntPtr, int>>(Size);
            for (var i = 0; i < Size; i++)
            {
                var type = TypeDispatch.Types[i];
                _byType[type] = i;
                var handle = type.TypeHandle.Value;
                _byHandle[handle] = i;
                handles.Add(new KeyValuePair<IntPtr, int>(handle, i));
            }
            handles.Sort(static (x, y) => x.Key.CompareTo(y.Key));
            _sortedHandles = new IntPtr[Size];
            _sortedIndexes = new int[Size];
            for (var i = 0; i < Size; i++)
            {
                _sortedHandles[i] = handles[i].Key;
                _sortedIndexes[i] = handles[i].Value;
            }

            _objects = new object[Size];
            _types = new Type[Size];
            for (var i = 0; i < Size; i++)
            {
                _objects[i] = TypeDispatch.Instances[i];
                _types[i] = TypeDispatch.Types[i];
            }
            _cursor = 0;
        }

        private Type NextType()
        {
            var value = _types[_cursor];
            if (++_cursor == _types.Length) _cursor = 0;
            return value;
        }

        private object NextObject()
        {
            var value = _objects[_cursor];
            if (++_cursor == _objects.Length) _cursor = 0;
            return value;
        }

        /// <summary>Binary search over type handles - O(log n), and no hashing at all.</summary>
        private int Search(IntPtr handle)
        {
            int lo = 0, hi = _sortedHandles.Length - 1;
            while (lo <= hi)
            {
                var mid = (int)(((uint)lo + (uint)hi) >> 1);
                var cmp = _sortedHandles[mid].CompareTo(handle);
                if (cmp == 0) return _sortedIndexes[mid];
                if (cmp < 0) lo = mid + 1; else hi = mid - 1;
            }
            return -1;
        }

        [Benchmark(Baseline = true, Description = "Type: Dictionary<Type,int>")]
        public int Type_ByType() => _byType.TryGetValue(NextType(), out var i) ? i : -1;

        [Benchmark(Description = "Type: Dictionary<IntPtr,int> on the handle")]
        public int Type_ByHandle()
            => _byHandle.TryGetValue(NextType().TypeHandle.Value, out var i) ? i : -1;

        [Benchmark(Description = "Type: binary search over handles")]
        public int Type_Search() => Search(NextType().TypeHandle.Value);

        [Benchmark(Description = "object: Dictionary<Type,int>")]
        public int Object_ByType() => _byType.TryGetValue(NextObject().GetType(), out var i) ? i : -1;

        [Benchmark(Description = "object: Dictionary<IntPtr,int> on the handle")]
        public int Object_ByHandle()
            => _byHandle.TryGetValue(NextObject().GetType().TypeHandle.Value, out var i) ? i : -1;
    }
}
#endif
