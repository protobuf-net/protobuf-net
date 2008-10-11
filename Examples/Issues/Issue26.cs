using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;

namespace Examples.Issues
{

    /// <summary>
    /// To investigate: http://code.google.com/p/protobuf-net/issues/detail?id=26
    /// (cannot reproduce)
    /// </summary>
    [TestFixture]
    public class Issue26
    {
        [Test]
        public void RoundTripCStation()
        {
            CStation cs = new CStation { number = 1, ticket = 2 },
                clone = Serializer.DeepClone(cs);
            Assert.AreNotSame(cs, clone);
            Assert.IsNotNull(clone);
            Assert.AreEqual(cs.number, clone.number);
            Assert.AreEqual(cs.ticket, clone.ticket);
        }

        [Test]
        public void RoundTripCListStations()
        {
            CListStations list = new CListStations
            {
                liststation =
                {
                    new CStation { number = 1, ticket = 2},
                    new CStation { number = 3, ticket = 4}
                }
            }, clone = Serializer.DeepClone(list);
            Assert.AreNotSame(list, clone);
            Assert.IsNotNull(clone);
            Assert.AreEqual(2, clone.liststation.Count, "Count");
            Assert.AreEqual(1, clone.liststation[0].number);
            Assert.AreEqual(2, clone.liststation[0].ticket);
            Assert.AreEqual(3, clone.liststation[1].number);
            Assert.AreEqual(4, clone.liststation[1].ticket);
        }
    }

    [ProtoContract]
    public class CStation
    {
        [ProtoMember(1)]
        public int number { get; set; }

        [ProtoMember(8)]
        public int ticket { get; set; }

    }



    [ProtoContract]
    public class CListStations
    {
        [ProtoMember(1)]
        private List<CStation> _liststations = new List<CStation>();
        public List<CStation> liststation { get { return _liststations; } }

    }
}
