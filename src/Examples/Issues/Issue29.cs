using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ProtoBuf;
using NUnit.Framework;
using System.IO;

namespace Examples.Issue29
{
    
    [TestFixture]
    public class TestIssue29
    {
        [Test]
        public void TestDeepClone()
        {
            Person person = Person.Create();
            Person clone = Serializer.DeepClone(person);
            CheckEqual(person, clone);
        }

        [Test]
        public void TestMemoryStream()
        {
            Person person = Person.Create(), clone;
            using(MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, person);
                ms.Position = 0;
                EchoStream("TestMemoryStream", ms);
                ms.Position = 0;
                clone = Serializer.Deserialize<Person>(ms);
            }
            CheckEqual(person,clone);
        }

        static void EchoStream(string caption, Stream stream)
        {
            StringBuilder sb = new StringBuilder();
            int i;
            while((i=stream.ReadByte()) >=0)
            {
                sb.Append(i.ToString("X2")).Append('.');
            }
            Debug.WriteLine(sb, caption);
        }

        [Test]
        public void TestToDisk()
        {
            Person person = Person.Create(), clone;
            using(FileStream fs = File.Create("issue29.dat"))
            {
                Serializer.Serialize(fs, person);
                fs.Close();
            }
            using (FileStream fs = File.OpenRead("issue29.dat"))
            {
                EchoStream("SaveToDisk", fs);
            }

            using(FileStream fs = File.OpenRead("issue29.dat"))
            {
                clone = Serializer.Deserialize<Person>(fs);
            }
            CheckEqual(person, clone);
        }

        static void CheckEqual(Person person, Person clone) {
            Assert.AreNotSame(person,clone);
            Assert.AreEqual(person.FirstName, clone.FirstName, "FirstName");
            Assert.AreEqual(person.LastName, clone.LastName, "LastName");
            Assert.AreEqual(person.Aliases.Count, clone.Aliases.Count, "Aliases.Count");
            for(int i = 0 ; i < person.Aliases.Count ; i++)
            {
                Assert.AreEqual(person.Aliases[i].GetType(), clone.Aliases[i].GetType(), "Type: " + i);
                Assert.AreEqual(person.Aliases[i].AliasName, clone.Aliases[i].AliasName, "AliasName: " + i);
            }
        }
    }

    public class Alias :AliasBase
    {
    }

    [ProtoContract]
    [ProtoInclude(5, typeof(Alias))]
    public class AliasBase
    {
        [ProtoMember(6)]
        public string AliasName { get; set; }
    }

    public class AliasCollection : Collection<AliasBase>
    {
    }

    [ProtoContract]
    public class Person
    {
        [ProtoMember(1)]
        public string FirstName { get; set; }

        [ProtoMember(2)]
        public string LastName { get; set; }

        [ProtoMember(3)]
        public AliasCollection Aliases { get; set; }

        public Person()
        {
            Aliases = new AliasCollection();
        }

        public static Person Create()
        {
            Person person = new Person
                                {
                                    FirstName = "Bill", 
                                    LastName = "Jones",
                                    Aliases = new AliasCollection
                                                  {
                                                      new Alias{ AliasName = "Billy"},
                                                      new Alias{ AliasName = "Wild Bill"},
                                                      new Alias{ AliasName = "William"},
                                                      new Alias{ AliasName = "Willie"}
                                                  }
                                };

            return person;
            
        }
    }
}
