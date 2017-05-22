using System;
using System.Collections.Generic;
using System.IO;

namespace ProtoBuf
{
    internal sealed class Peekable<T> : IDisposable
    {
        private readonly IEnumerator<T> _iter;
        private T _peek, _prev;
        private bool _havePeek, _eof;
        public Peekable(IEnumerable<T> sequence)
        {
            _iter = sequence.GetEnumerator();
        }
        public T Previous => _prev;
        public bool Consume()
        {
            bool haveData = _havePeek || Peek(out T val);
            _havePeek = false;
            return haveData;
        }
        public bool Peek(out T next)
        {
            if (!_havePeek)
            {
                if (_iter.MoveNext())
                {
                    _prev = _peek;
                    _peek = _iter.Current;
                    _havePeek = true;
                }
                else
                {
                    _eof = true;
                    _havePeek = false;
                }
            }
            if (_eof)
            {
                next = default(T);
                return false;
            }
            next = _peek;
            return true;
        }
        public bool Is(Func<T, bool> predicate) => Peek(out T val) && predicate(val);
        public void Dispose() => _iter?.Dispose();
    }
}
