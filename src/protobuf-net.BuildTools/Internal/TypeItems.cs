#nullable enable
using Microsoft.CodeAnalysis;

namespace ProtoBuf.BuildTools.Internal
{
    internal interface IBlame
    {
        Location? Blame { get; }
    }
    internal readonly struct Member : IBlame
    {
        public Member (Location? blame, int fieldNumber, string memberName, string name)
        {
            Blame = blame;
            FieldNumber = fieldNumber;
            MemberName = memberName;
            Name  = name;
        }
        public Location? Blame { get; }
        public int FieldNumber { get; }
        public string MemberName { get; }
        public string Name { get; }
    }

    internal readonly struct Ignore : IBlame
    {
        public Ignore(Location? blame, string memberName)
        {
            Blame = blame;
            MemberName = memberName;
        }

        public Location? Blame { get; }
        public string MemberName { get; }
    }

    internal readonly struct Include : IBlame
    {
        public Include(Location? blame, int fieldNumber, ITypeSymbol type)
        {
            Blame = blame;
            FieldNumber = fieldNumber;
            Type = type;
        }
        public Location? Blame { get; }
        public int FieldNumber { get; }
        public ITypeSymbol Type { get; }
    }

    internal readonly struct Reservation : IBlame
    {
        public readonly Location? Blame { get; }
        public string? Name { get; }
        public int From { get; }
        public int To { get; }

        public Reservation(Location? blame, int from, int to)
        {
            Blame = blame;
            From = from;
            To = to;
            Name = null;
        }

        public Reservation(Location? blame, string name)
        {
            Blame = blame;
            From = To = default;
            Name = name;
        }

        public override string ToString()
        {
            if (Name is not null) return $"'{Name}'";
            return From == To ? From.ToString() : $"[{From}-{To}]";
        }

        public bool Includes(string name)
            => Name is not null && Name == name;
        public bool Includes(int number)
            => Name is null && From <= number && To >= number;

        internal bool Overlaps(Reservation other)
        {
            if (Name is not null || other.Name is not null) return Name == other.Name;

            // do we include either end of the other? (or both)
            if (From <= other.From && To >= other.From) return true;
            if (From <= other.To && To >= other.To) return true;

            // does the other totally include us?
            return other.From < From && other.To > To;
        }
    }


}
