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
    }
}
