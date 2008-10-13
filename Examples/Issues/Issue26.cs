using System.Collections.Generic;
using NUnit.Framework;
using ProtoBuf;
using System.IO;

namespace Examples.Issues
{

    /// <summary>
    /// To investigate: http://code.google.com/p/protobuf-net/issues/detail?id=26
    /// Related to classes requiring a public constructor. This constraint now removed;
    /// a non-public (but parameterless) constructor can be used instead, and a better
    /// error message is returned if you get it wrong.
    /// </summary>
    [TestFixture]
    public class Issue26
    {
        [Test]
        public void RoundTripCStation()
        {
            Station cs = new Station(1,2),
                clone = Serializer.DeepClone(cs);
            Assert.AreNotSame(cs, clone);
            Assert.IsNotNull(clone);
            Assert.AreEqual(cs.Number, clone.Number);
            Assert.AreEqual(cs.Ticket, clone.Ticket);
        }

        [Test]
        public void RoundTripCListStations()
        {
            StationList list = new StationList
            {
                Stations =
                {
                    new Station(1,2),
                    new Station(3,4)
                }
            }, clone = Serializer.DeepClone(list);
            Assert.AreNotSame(list, clone);
            Assert.IsNotNull(clone);
            Assert.AreEqual(2, clone.Stations.Count, "Count");
            Assert.AreEqual(1, clone.Stations[0].Number);
            Assert.AreEqual(2, clone.Stations[0].Ticket);
            Assert.AreEqual(3, clone.Stations[1].Number);
            Assert.AreEqual(4, clone.Stations[1].Ticket);
        }

        [Test, ExpectedException(typeof(ProtoException), ExpectedMessage = "No parameterless constructor found for WithoutParameterlessCtor", MatchType=MessageMatch.Exact)]
        public void CheckMeaningfulErrorIfNoParameterlessCtor()
        {
            WithoutParameterlessCtor obj = new WithoutParameterlessCtor(123);
            Serializer.DeepClone(obj);
        }

        [Test]
        public void TestMergeWithoutParameterlessCtor()
        {
            WithoutParameterlessCtor obj = new WithoutParameterlessCtor(123),
                clone = new WithoutParameterlessCtor(456);
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, obj);
                ms.Position = 0;
                Serializer.Merge(ms, clone);
            }
            Assert.AreEqual(obj.Foo, clone.Foo);
        }
        
    }

    [ProtoContract]
    public class Station
    {
        [ProtoMember(1)]
        public int Number { get; internal set; }

        [ProtoMember(8)]
        public int Ticket { get; internal set; }

        public Station(int number, int ticket)
        {
            this.Number = number;
            this.Ticket = ticket;
        }
        private Station() { }

    }

    /// <remarks>Re the unusual structure here, note that this is related to the code-sample
    /// provided to investigate this issue.</remarks>
    [ProtoContract]
    public class StationList
    {
        [ProtoMember(1)]
        private readonly List<Station> _liststations = new List<Station>();
        public List<Station> Stations { get { return _liststations; } }

    }

    [ProtoContract]
    public class WithoutParameterlessCtor {
        [ProtoMember(1)]
        public int Foo {get; internal set;}

        public WithoutParameterlessCtor(int foo)
        {
            this.Foo = foo;
        }
    }
}
