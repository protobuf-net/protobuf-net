using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.unittest.Attribs
{
    public class ProtoContractSurrogateTests
    {
        [ProtoContract(Surrogate = typeof(ImmutableTableColumnSurrogate))]
        public class ImmutableTableColumn
        {
            public ImmutableTableColumn(string key)
            {
                Key = key;
                var parts = key.Split('.');
                Table = parts[0];
                Column = parts[1];
            }

            public string Key { get; }
            public string Table { get; }
            public string Column { get; }
        }

        [ProtoContract]
        public class ImmutableTableColumnSurrogate
        {
            [ProtoMember(1)]
            public string Key { get; set; }

            public static implicit operator ImmutableTableColumn(ImmutableTableColumnSurrogate surrogate)
            {
                return surrogate == null ? null : new ImmutableTableColumn(surrogate.Key);
            }

            public static implicit operator ImmutableTableColumnSurrogate(ImmutableTableColumn source)
            {
                return source == null ? null : new ImmutableTableColumnSurrogate
                {
                    Key = source.Key
                };
            }
        }

        [Fact]
        public void ImmutableSerialization()
        {
            var tableColumn = new ImmutableTableColumn("Table.Column");
            var clone = (ImmutableTableColumn)RuntimeTypeModel.Default.DeepClone(tableColumn);

            Assert.Equal(tableColumn.Key, clone.Key);
        }

        [ProtoContract(Surrogate = typeof(ImmutableGenericType1Surrogate<>))]
        public class ImmutableGenericType1<T>
        {
            public ImmutableGenericType1(T value)
            {
                Value = value;
            }

            public T Value { get; }
        }

        [ProtoContract]
        public class ImmutableGenericType1Surrogate<T>
        {
            [ProtoMember(1, IsRequired = true)]
            public T Value { get; set; }

            public static implicit operator ImmutableGenericType1<T>(ImmutableGenericType1Surrogate<T> surrogate)
            {
                return surrogate == null ? null : new ImmutableGenericType1<T>(surrogate.Value);
            }

            public static implicit operator ImmutableGenericType1Surrogate<T>(ImmutableGenericType1<T> source)
            {
                return source == null ? null : new ImmutableGenericType1Surrogate<T> { Value = source.Value };
            }
        }

        [Fact]
        public void ImmutableGenericSerialization()
        {
            var instance = new ImmutableGenericType1<string>("XYZ!");
            var clone = RuntimeTypeModel.Default.DeepClone(instance);

            Assert.Equal(instance.Value, clone.Value);
        }

        [ProtoContract(Surrogate = typeof(ImmutableGenericType2<>.Surrogate))]
        public class ImmutableGenericType2<T>
        {
            public ImmutableGenericType2(T value)
            {
                Value = value;
            }

            public T Value { get; }

            [ProtoContract]
            public class Surrogate
            {
                [ProtoMember(1, IsRequired = true)]
                public T Value { get; set; }

                public static implicit operator ImmutableGenericType2<T>(Surrogate surrogate)
                {
                    return surrogate == null ? null : new ImmutableGenericType2<T>(surrogate.Value);
                }

                public static implicit operator Surrogate(ImmutableGenericType2<T> source)
                {
                    return source == null ? null : new Surrogate { Value = source.Value };
                }
            }
        }

        [Fact]
        public void ImmutableGenericNestedSerialization()
        {
            var instance = new ImmutableGenericType2<string>("XYZ!");
            var clone = RuntimeTypeModel.Default.DeepClone(instance);

            Assert.Equal(instance.Value, clone.Value);
        }
    }
}
