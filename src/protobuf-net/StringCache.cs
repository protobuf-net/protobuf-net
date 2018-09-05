namespace ProtoBuf
{
    public class StringCache : IStringSerializer
    {
        public static int CacheStringLength = 32;


        private static readonly ConcurrentDictionary<ArraySlice, string> _cache = new ConcurrentDictionary<ArraySlice, string>();
        private static readonly ConcurrentDictionary<string, byte[]> _utf8Cache = new ConcurrentDictionary<string, byte[]>();

        public string ReadString(byte[] buffer, int index, int count)
        {
            var slice = new ArraySlice(buffer, index, count);
            string result;
            if (!_cache.TryGetValue(slice, out result))
            {
                result = Encoding.UTF8.GetString(slice.Buffer, slice.Index, slice.Count);
                if (string.IsInterned(result) != null)
                {
                    var copy = slice.DeepCopy();
                    if (_cache.TryAdd(copy, result))
                    {
                        _utf8Cache.TryAdd(result, copy.Buffer);
                    }
                }
            }

            return result;
        }

        public int GetLength(string value)
        {
            return _utf8Cache.TryGetValue(value, out byte[] cached)
                ? cached.Length
                : Encoding.UTF8.GetByteCount(value);
        }

        public int WriteString(string value, byte[] buffer, int offset)
        {
            if(_utf8Cache.TryGetValue(value, out byte[] cached))
            {
                Array.Copy(cached, 0, buffer, offset, cached.Length);
                return cached.Length;
            }

            return Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, offset);
        }
    }

    internal struct ArraySlice : IEquatable<ArraySlice>
    {
        private readonly byte[] _buffer;
        private readonly int _index;
        private readonly int _count;
        private readonly int _hashCode;

        public ArraySlice(byte[] buffer, int index, int count)
        {
            _buffer = buffer;
            _index = index;
            _count = count;
            _hashCode = CalculateHashCode(buffer, index, count);
        }

        public byte[] Buffer => _buffer;

        public int Index => _index;

        public int Count => _count;

        public unsafe bool Equals(ArraySlice other)
        {
            if (_count != other._count)
            {
                return false;
            }

            fixed (byte* p1 = &_buffer[_index], p2 = &other._buffer[other._index])
            {
                byte* thisP = p1, otherP = p2;
                int l = _count;
                for (int i = 0; i < l / 8; i++, thisP += 8, otherP += 8)
                {
                    if (*((long*)thisP) != *((long*)otherP))
                    {
                        return false;
                    }
                }

                if ((l & 4) != 0)
                {
                    if (*((int*)thisP) != *((int*)otherP))
                    {
                        return false;
                    }

                    thisP += 4;
                    otherP += 4;
                }

                if ((l & 2) != 0)
                {
                    if (*((short*)thisP) != *((short*)otherP))
                    {
                        return false;
                    }

                    thisP += 2;
                    otherP += 2;
                }

                if ((l & 1) != 0)
                {
                    if (*thisP != *otherP)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            return obj is ArraySlice slice && Equals(slice);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        private static unsafe int CalculateHashCode(byte[] buffer, int index, int count)
        {
            unchecked
            {
                fixed (byte* p = &buffer[index])
                {
                    byte* current = p;
                    uint hash = 5381;
                    var left = count;

                    while (left > 3)
                    {
                        hash = (hash << 5) + hash + (*(uint*)current);
                        left -= sizeof(uint);
                        current += sizeof(uint);
                    }

                    if (left >= 2)
                    {
                        hash = (hash << 5) + hash + (*(ushort*)current);
                        left -= sizeof(ushort);
                        current += sizeof(ushort);
                    }

                    if (left != 0)
                    {
                        hash = (hash << 5) + hash + *current;
                    }

                    return (int)hash;
                }
            }
        }

        internal ArraySlice DeepCopy()
        {
            var buffer = new byte[Count];
            Array.Copy(_buffer, _index, buffer, 0, _count);
            return new ArraySlice(buffer, 0, _count);
        }
    }
}