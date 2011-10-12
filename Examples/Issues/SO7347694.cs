using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;

namespace Examples.Issues
{
    [TestFixture]
    public class SO7347694
    {
        [ProtoContract]
        public class Thing
        {
            [ProtoMember(1)] private readonly string _name;

            public string Name
            {
                get { return _name; }
            }

            public Thing()
            {}

            public Thing(string name)
            {
                _name = name;
            }
        }

        [Test]
        public void SerializeTheEasyWay()
        {
            var list = GetListOfThings();

            using (var fs = File.Create(@"things.bin"))
            {
                ProtoBuf.Serializer.Serialize(fs, list);

                fs.Close();
            }

            using (var fs = File.OpenRead(@"things.bin"))
            {
                list = ProtoBuf.Serializer.Deserialize<MyDto>(fs);

                Assert.AreEqual(3, list.Things.Count);
                Assert.AreNotSame(list.Things[0], list.Things[1]);
                Assert.AreSame(list.Things[0], list.Things[2]);

                fs.Close();
            }
        }

        [ProtoContract]
        public class MyDto
        {
            [ProtoMember(1, AsReference = true)]
            public List<Thing> Things { get; set; }
        }

        private MyDto GetListOfThings()
        {
            var thing1 = new Thing("thing1");
            var thing2 = new Thing("thing2");

            var list = new List<Thing>();
            list.Add(thing1);
            list.Add(thing2);
            list.Add(thing1);

            return new MyDto {Things = list};
        }
    }
}
