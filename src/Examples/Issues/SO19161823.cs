using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System.IO;

namespace Examples.Issues
{
    [TestFixture]
    public class SO19161823
    {
        [Test]
        public void Execute()
        {
            RuntimeTypeModel.Default.Add(typeof(IDummy), false)
                .SetSurrogate(typeof(DummySurrogate));

            var container = new Container { Data = new Dummy { Positive = 3 } };

            using (var file = File.Create("test.bin"))
            {
                Serializer.Serialize(file, container);
            }

            using (var file = File.OpenRead("test.bin"))
            {
                container = Serializer.Deserialize<Container>(file);
                Assert.AreEqual(3, container.Data.Positive);
            }
        }
        // Outside of the project, cannot be changed
        public interface IDummy
        {
            int Positive { get; set; }
        }


        [ProtoContract]
        public class Container
        {
            [ProtoMember(1)]
            public IDummy Data { get; set; }
        }

        public class Dummy : IDummy
        {
            public int Positive { get; set; }
        }

        [ProtoContract]
        class DummySurrogate
        {
            [ProtoMember(1)]
            public int Negative { get; set; }

            [ProtoConverter]
            public static IDummy From(DummySurrogate value)
            {
                return value == null ? null : new Dummy { Positive = -value.Negative };
            }

            [ProtoConverter]
            public static DummySurrogate To(IDummy value)
            {
                return value == null ? null : new DummySurrogate { Negative = -value.Positive };
            }
        }
    }
}
