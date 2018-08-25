#if !COREFX
using Xunit;
using ProtoBuf;
using System.Collections.Generic;

namespace Examples.Issues
{
    
    public class Issue318
    {
        [Fact]
        public void Execute()
        {
            Serializer.PrepareSerializer<DictWithImmutableStructValueType>(); // this used to throw.
        }

        [ProtoContract]
        class DictWithImmutableStructValueType
        {
            [ProtoMember(1)]
            public Dictionary<string, ImmutableValueType> Dict { get; set; } = new Dictionary<string, ImmutableValueType>();
        }

        [ProtoContract]
        struct ImmutableValueType
        {
            [ProtoMember(1)]
            private readonly int _value;

            public ImmutableValueType(int value)
            {
                _value = value;
            }

            public int Value => _value;

            public override int GetHashCode()
            {
                return _value;
            }

            public override bool Equals(object obj)
            {
                if (obj is ImmutableValueType x)
                    return x._value == _value;
                return false;
            }
        }
    }
}
#endif