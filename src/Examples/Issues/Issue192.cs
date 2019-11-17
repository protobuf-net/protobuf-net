using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;
using System.IO;

namespace Examples.Issues
{
    
    public class Issue192
    {
        [ProtoContract]
        class SomeType { }
        [ProtoContract]
        class Wrapper
        {
            [ProtoMember(1)]
            public List<SomeType>[] List { get; set; }
        }
        // the important thing is that this error is identical to the one from SerializeWrappedDeepList
        [Fact]
        public void SerializeDeepList()
        {
            Program.ExpectFailure<NotSupportedException>(() =>
            {
                var list = new List<SomeType>[] { new List<SomeType> { new SomeType() }, new List<SomeType> { new SomeType() } };
                Serializer.Serialize(Stream.Null, list);
            }, "Nested or jagged lists, arrays and maps are not supported: System.Collections.Generic.List`1[Examples.Issues.Issue192+SomeType][]");
        }
        

        [Fact]
        public void DeserializeDeepList()
        {
            Program.ExpectFailure<NotSupportedException>(() =>
            {
                Serializer.DeepClone<List<SomeType>[]>(new List<SomeType>[0]);
            }, "Nested or jagged lists, arrays and maps are not supported: System.Collections.Generic.List`1[Examples.Issues.Issue192+SomeType][]");
        }
        [Fact]
        public void SerializeWrappedDeepList()
        {
            Program.ExpectFailure<NotSupportedException>(() =>
            {
                var wrapped = new Wrapper { List = new List<SomeType>[0] };
                var clone = Serializer.DeepClone(wrapped);
            }, "Nested or jagged lists, arrays and maps are not supported: System.Collections.Generic.List`1[Examples.Issues.Issue192+SomeType][]");
        }

    }
}
