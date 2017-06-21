using System.Collections.Generic;
using Xunit;
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
    
    public class Issue26
    {
        [Fact]
        public void RoundTripCStation()
        {
            Station cs = new Station(1,2),
                clone = Serializer.DeepClone(cs);
            Assert.NotSame(cs, clone);
            Assert.NotNull(clone);
            Assert.Equal(cs.Number, clone.Number);
            Assert.Equal(cs.Ticket, clone.Ticket);
        }

        [Fact]
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
            Assert.NotSame(list, clone);
            Assert.NotNull(clone);
            Assert.Equal(2, clone.Stations.Count); //, "Count");
            Assert.Equal(1, clone.Stations[0].Number);
            Assert.Equal(2, clone.Stations[0].Ticket);
            Assert.Equal(3, clone.Stations[1].Number);
            Assert.Equal(4, clone.Stations[1].Ticket);
        }

        [Fact]
        public void CheckMeaningfulErrorIfNoParameterlessCtor()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                WithoutParameterlessCtor obj = new WithoutParameterlessCtor(123);
                Serializer.DeepClone(obj);
            }, "No parameterless constructor found for Examples.Issues.WithoutParameterlessCtor");
        }

        [Fact]
        public void CheckMeaningfulErrorIfNoParameterlessCtor_Interface()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                HazGenericInterface obj = new HazGenericInterface { Data = new Foo(12) };
                Serializer.DeepClone(obj);
            }, "No parameterless constructor found for Examples.Issues.Issue26+Foo");
        }

        [ProtoContract]
        public class HazGenericInterface
        {
            [ProtoMember(1)]
            public IFoo<int> Data { get;set;}
        }
        [ProtoContract]
        [ProtoInclude(2, typeof(Foo))]
        public interface IFoo<T>
        {
            [ProtoMember(1)]
            T Item { get; set; }
        }
        public class Foo : IFoo<int>
        {
            public Foo(int value) { Item = value; }
            public int Item { get; set; }
        }

        [Fact]
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
            Assert.Equal(obj.Foo, clone.Foo);
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
        public Station() { }

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
