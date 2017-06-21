using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf.Meta;
using System.IO;
using ProtoBuf;

namespace Examples.Issues
{
    
    public class SO6651451
    {
        public class SerializeClass
        {
            public Dictionary<string, SerializeDictionaryItem> MyDictionary { get; set; }
            public List<SerializeDictionaryItem> MyList { get; set; }
        }

        [ProtoContract]
        public class SerializeDictionaryItem
        {
            [ProtoMember(1)]
            public string MyField { get; set; }
        }

        [ProtoContract(SkipConstructor = true)]
        public class SerializeClassSurrogate
        {
            [ProtoMember(1000, AsReference = true)]
            public Dictionary<string, SerializeDictionaryItem> MyDictionary { get; set; }

            [ProtoMember(1001, AsReference = true)]
            public List<SerializeDictionaryItem> MyList { get; set; }

            public static implicit operator SerializeClass(SerializeClassSurrogate surrogate)
            {
                if (surrogate == null)
                    return null;

                var serializeClass = new SerializeClass();
                serializeClass.MyDictionary = surrogate.MyDictionary;
                serializeClass.MyList = surrogate.MyList;

                return serializeClass;
            }

            public static implicit operator SerializeClassSurrogate(SerializeClass serializeClass)
            {
                if (serializeClass == null)
                    return null;

                var surrogate = new SerializeClassSurrogate();
                surrogate.MyDictionary = serializeClass.MyDictionary;
                surrogate.MyList = serializeClass.MyList;

                return surrogate;
            }
        }
        [Fact]
        public void RunTest()
        {
            //Serialization Logic:
            RuntimeTypeModel.Default[typeof(SerializeClass)].SetSurrogate(typeof(SerializeClassSurrogate));

            var myDictionaryItem = new SerializeDictionaryItem();
            myDictionaryItem.MyField = "ABC";

            SerializeClass m = new SerializeClass() { MyDictionary = new Dictionary<string, SerializeDictionaryItem>() };
            m.MyDictionary.Add("def", myDictionaryItem);
            m.MyDictionary.Add("abc", myDictionaryItem);

            m.MyList = new List<SerializeDictionaryItem>();
            m.MyList.Add(myDictionaryItem);
            m.MyList.Add(myDictionaryItem);

            Assert.Same(m.MyDictionary["def"], m.MyDictionary["abc"]);
            Assert.Same(m.MyList[0], m.MyList[1]);

            byte[] buffer;
            using (var writer = new MemoryStream())
            {
                Serializer.Serialize(writer, m);
                buffer = writer.ToArray();
            }

            using (var reader = new MemoryStream(buffer))
            {
                var deserialized = Serializer.Deserialize<SerializeClass>(reader);
                Assert.Same(deserialized.MyDictionary["def"], deserialized.MyDictionary["abc"]);
                Assert.Same(deserialized.MyList[0], deserialized.MyList[1]);
            }
        }

    }
}
