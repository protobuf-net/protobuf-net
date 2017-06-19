using System;
using System.Runtime.Serialization;
using Xunit;
using System.IO;
using ProtoBuf;

namespace Examples.Issues
{
    
    public class SO7120856
    {
        [Fact]
        public void RoundTripImmutableTypeAsTuple()
        {
            using(var ms = new MemoryStream())
            {
                var val = new MyValueTypeAsTuple(123, 456);
                Serializer.Serialize(ms, val);
                ms.Position = 0;
                var clone = Serializer.Deserialize<MyValueTypeAsTuple>(ms);
                Assert.Equal(123, clone.X);
                Assert.Equal(456, clone.Z);
            }
        }
        [Fact]
        public void RoundTripImmutableTypeViaFields()
        {
            using (var ms = new MemoryStream())
            {
                var val = new MyValueTypeViaFields(123, 456);
                Serializer.Serialize(ms, val);
                ms.Position = 0;
                var clone = Serializer.Deserialize<MyValueTypeViaFields>(ms);
                Assert.Equal(123, clone.X);
                Assert.Equal(456, clone.Z);
            }
        }
        [Serializable]
        [DataContract]
        public struct MyValueTypeViaFields
#if !COREFX
            : ISerializable
#endif
        {
            [DataMember(Order = 1)]
            private readonly int _x;
            [DataMember(Order = 2)]
            private readonly int _z;

            public MyValueTypeViaFields(int x, int z)
                : this()
            {
                _x = x;
                _z = z;
            }
#if !COREFX
            // this constructor is used for deserialization
            public MyValueTypeViaFields(SerializationInfo info, StreamingContext text)
                : this()
            {
                _x = info.GetInt32("X");
                _z = info.GetInt32("Z");
            }
#endif

            public int X
            {
                get { return _x; }
            }

            public int Z
            {
                get { return _z; }
            }

            public static bool operator ==(MyValueTypeViaFields a, MyValueTypeViaFields b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(MyValueTypeViaFields a, MyValueTypeViaFields b)
            {
                return !(a == b);
            }

            public override bool Equals(object other)
            {
                if (!(other is MyValueTypeViaFields))
                {
                    return false;
                }

                return Equals((MyValueTypeViaFields)other);
            }

            public bool Equals(MyValueTypeViaFields other)
            {
                return X == other.X && Z == other.Z;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (X * 397) ^ Z;
                }
            }

#if !COREFX
            // this method is called during serialization
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("X", X);
                info.AddValue("Z", Z);
            }
#endif
            public override string ToString()
            {
                return string.Format("[{0}, {1}]", X, Z);
            }
        }

        [Serializable]
        public struct MyValueTypeAsTuple
#if !COREFX
            : ISerializable
#endif
        {
            private readonly int _x;
            private readonly int _z;

            public MyValueTypeAsTuple(int x, int z)
                : this()
            {
                _x = x;
                _z = z;
            }
#if !COREFX
            // this constructor is used for deserialization
            public MyValueTypeAsTuple(SerializationInfo info, StreamingContext text)
                : this()
            {
                _x = info.GetInt32("X");
                _z = info.GetInt32("Z");
            }
#endif
            [DataMember(Order = 1)]
            public int X
            {
                get { return _x; }
            }

            [DataMember(Order = 2)]
            public int Z
            {
                get { return _z; }
            }

            public static bool operator ==(MyValueTypeAsTuple a, MyValueTypeAsTuple b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(MyValueTypeAsTuple a, MyValueTypeAsTuple b)
            {
                return !(a == b);
            }

            public override bool Equals(object other)
            {
                if (!(other is MyValueTypeAsTuple))
                {
                    return false;
                }

                return Equals((MyValueTypeAsTuple)other);
            }

            public bool Equals(MyValueTypeAsTuple other)
            {
                return X == other.X && Z == other.Z;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (X * 397) ^ Z;
                }
            }

#if !COREFX
            // this method is called during serialization
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("X", X);
                info.AddValue("Z", Z);
            }
#endif
            public override string ToString()
            {
                return string.Format("[{0}, {1}]", X, Z);
            }
        }


    }
}
