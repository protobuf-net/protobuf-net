using System.Collections.Generic;
using Xunit;
using ProtoBuf;

namespace Examples
{
    
    public class ListsWithInheritance
    {
        [Fact]
        public void TestBasicRoundtripViaDataClass()
        {
            Data data = new Data();
            data.Parties.Add(new Debtor());
            data.Parties.Add(new Party());
            data.Parties.Add(new Creditor());
            var clone = Serializer.DeepClone(data);

            Assert.Equal(3, clone.Parties.Count);
            Assert.Equal(typeof(Debtor), clone.Parties[0].GetType());
            Assert.Equal(typeof(Party), clone.Parties[1].GetType());
            Assert.Equal(typeof(Creditor), clone.Parties[2].GetType());
        }

        [Fact]
        public void TestBasicRoundtripOfNakedList()
        {
            var list = new List<Party>();
            list.Add(new Debtor());
            list.Add(new Party());
            list.Add(new Creditor());
            var clone = Serializer.DeepClone(list);

            Assert.Equal(3, clone.Count);
            Assert.Equal(typeof(Debtor), clone[0].GetType());
            Assert.Equal(typeof(Party), clone[1].GetType());
            Assert.Equal(typeof(Creditor), clone[2].GetType());
        }

        [ProtoContract]
        public class Data
        {
            [ProtoMember(1)]
            public List<Party> Parties { get { return parties; } }

            private readonly List<Party> parties = new List<Party>();
        }

        [ProtoContract]
        [ProtoInclude(1, typeof(Party))]
        public class BaseClass
        {
        }
        [ProtoContract]
        [ProtoInclude(1, typeof(Creditor))]
        [ProtoInclude(2, typeof(Debtor))]
        public class Party : BaseClass
        {
        }
        [ProtoContract]
        public class Creditor : Party
        {
        }
        [ProtoContract]
        public class Debtor : Party
        {
        }
    }
}
