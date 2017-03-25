using System.Collections.Generic;
using NUnit.Framework;
using ProtoBuf;

namespace Examples
{
    [TestFixture]
    public class ListsWithInheritance
    {
        [Test]
        public void TestBasicRoundtripViaDataClass()
        {
            Data data = new Data();
            data.Parties.Add(new Debtor());
            data.Parties.Add(new Party());
            data.Parties.Add(new Creditor());
            var clone = Serializer.DeepClone(data);

            Assert.AreEqual(3, clone.Parties.Count);
            Assert.AreEqual(typeof(Debtor), clone.Parties[0].GetType());
            Assert.AreEqual(typeof(Party), clone.Parties[1].GetType());
            Assert.AreEqual(typeof(Creditor), clone.Parties[2].GetType());
        }

        [Test]
        public void TestBasicRoundtripOfNakedList()
        {
            var list = new List<Party>();
            list.Add(new Debtor());
            list.Add(new Party());
            list.Add(new Creditor());
            var clone = Serializer.DeepClone(list);

            Assert.AreEqual(3, clone.Count);
            Assert.AreEqual(typeof(Debtor), clone[0].GetType());
            Assert.AreEqual(typeof(Party), clone[1].GetType());
            Assert.AreEqual(typeof(Creditor), clone[2].GetType());
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
