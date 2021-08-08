using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace ProtoBuf.Issues
{
	public class Issue820
	{
        [ProtoContract]
        public record SomeClass
        {
            [ProtoMember(1)]
            public IReadOnlyCollection<string> Collection { get; set; } = Array.Empty<string>();

            [ProtoMember(2)]
            public IReadOnlyList<string> List { get; set; } = Array.Empty<string>();

            [ProtoMember(3)]
            public IReadOnlyDictionary<string, string> Map { get; set; } = ImmutableDictionary.Create<string, string>();
        }

        [Fact]
        public void CanNotDeserializeIReadonlyCollection()
        {
            var orig = new SomeClass
            {
                Collection = new string[] { "a", "b", "c" },
                List = new string[] { "a", "b", "c" }.ToImmutableList(),
                Map = new Dictionary<string, string>
                {
                    ["a"] = "a",
                    ["b"] = "b",
                    ["c"] = "c"
                }
            };

            static void Equal(SomeClass expected, SomeClass actual)
            {
                Assert.Equal(expected.Collection, actual.Collection);
                Assert.Equal(expected.List, actual.List);
                Assert.Equal(expected.Map, actual.Map);
            }

            var model = RuntimeTypeModel.Create();

            // runtime
            Equal(orig, model.DeepClone(orig));

            // compiled
            Equal(orig, model.Compile().DeepClone(orig)); 
        }

    }
}
