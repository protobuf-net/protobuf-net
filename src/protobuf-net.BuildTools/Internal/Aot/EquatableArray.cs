#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;

namespace ProtoBuf.BuildTools.Internal.Aot
{
    /// <summary>
    /// A small immutable array with structural equality, so that incremental generator models
    /// compare by value and the driver's caching actually holds between edits.
    /// </summary>
    internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
        where T : IEquatable<T>
    {
        private readonly T[]? _items;

        public EquatableArray(T[]? items) => _items = items;

        public int Count => _items?.Length ?? 0;

        public T this[int index] => _items![index];

        public bool Equals(EquatableArray<T> other)
        {
            var mine = _items;
            var theirs = other._items;
            if (ReferenceEquals(mine, theirs)) return true;
            if (mine is null || theirs is null || mine.Length != theirs.Length) return false;

            // EqualityComparer<T>.Default rather than mine[i].Equals(...): null-safe for reference
            // types, and still dispatches through IEquatable<T> without boxing for structs
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < mine.Length; i++)
            {
                if (!comparer.Equals(mine[i], theirs[i])) return false;
            }
            return true;
        }

        public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

        public override int GetHashCode()
        {
            if (_items is null) return 0;
            var hash = 17;
            var comparer = EqualityComparer<T>.Default;
            foreach (var item in _items)
            {
                hash = (hash * 31) + comparer.GetHashCode(item);
            }
            return hash;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var items = _items;
            if (items is null) yield break;
            foreach (var item in items) yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
