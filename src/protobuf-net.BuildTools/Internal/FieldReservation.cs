using System;

namespace ProtoBuf.BuildTools.Internal
{
    internal readonly struct FieldReservation : IEquatable<FieldReservation>
    {
        private readonly string? _name;
        private readonly int _from, _to;
        public FieldReservation(int from) : this(from, from) { }

        public FieldReservation(int from, int to)
        {
            _from = from;
            _to = to;
            _name = null;
        }

        public FieldReservation(string name)
        {
            _from = _to = default;
            _name = name;
        }

        public override string ToString()
        {
            if (_name is not null) return $"'{_name}'";
            return _from == _to ? _from.ToString() : $"[{_from}-{_to}]";
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + _name?.GetHashCode() ?? 0;
                hash = hash * 23 + _from;
                hash = hash * 23 + _to;
                return hash;
            }
        }
        public bool Includes(string name)
            => _name is not null && _name == name;
        public bool Includes(int number)
            => _name is null && _from <= number && _to >= number;

        bool IEquatable<FieldReservation>.Equals(FieldReservation other)
            => Equals(in other);
        public bool Equals(in FieldReservation other)
            => _name == other._name && _from == other._from && _to == other._to;
        public override bool Equals(object obj)
            => obj is FieldReservation other && Equals(in other);

        internal bool Overlaps(in FieldReservation other)
        {
            if (_name is not null || other._name is not null) return _name == other._name;

            // do we include either end of the other? (or both)
            if (Includes(other._from) || Includes(other._to)) return true;

            // does the other totally include us?
            return other._from < _from && other._to > _to;
        }
    }
}
