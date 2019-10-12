using System.Text;
using System.Xml;

namespace test
{
    using System;
    using System.IO;
    using System.Xml.Serialization;
    using Xunit;
    using ProtoBuf;
    using Examples;
    using Xunit.Abstractions;

    [XmlType]
    public class SimpleObject : IEquatable<SimpleObject>
    {
        private double _value;
        private String _name;

        [XmlElement(Order = 1)]
        public double Value { get { return _value; } set { _value = value; } }
        [XmlElement(Order = 2)]
        public String Name { get { return _name; } set { _name = value; } }

        /// <summary>
        /// Revert to the default settings
        /// </summary>
        public void ToDefaults()
        {
            _value = 10.0;
            _name = "Default name";
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj as SimpleObject);
        }
        public override int GetHashCode()
        {
            return _value.GetHashCode() * 17 + (_name == null ? 0 : _name.GetHashCode());
        }
        public bool Equals(SimpleObject other)
        {
            if (null == other) return false;
            if ((object)this == (object)other) return true; // same instance

            return (Math.Abs(Value - other.Value) < 1e-12 &&
                    Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase));
        }
    }

    
    public class TestIncorrectStream
    {
        private ITestOutputHelper Log { get; }
        public TestIncorrectStream(ITestOutputHelper _log) => Log = _log;

        [Fact]
        public void TestDeserializationFromXml()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                SimpleObject original = new SimpleObject();
                original.ToDefaults();

                // assert that Equals routine works
                Assert.Equal(original, original);

                // serialize to XML text
                StringBuilder sb = new StringBuilder();
                XmlWriter writer = XmlWriter.Create(sb);
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(SimpleObject));
                xmlSerializer.Serialize(writer, original);
                Log.WriteLine(sb.ToString());

                // use XML text as input stream for XML deserialization
                byte[] bytes = Encoding.Unicode.GetBytes(sb.ToString());
                Assert.True(bytes.Length > 0);
                MemoryStream ms = new MemoryStream(bytes);
                SimpleObject fromXml = (SimpleObject)xmlSerializer.Deserialize(ms);
                Assert.Equal(original, fromXml);

                // rewind the stream and deserialize using Protobuf
                ms.Seek(0L, SeekOrigin.Begin);
                SimpleObject fromProtobuf = Serializer.Deserialize<SimpleObject>(ms);

                // either deserialization from XML works or
                // it should not give an object instance (either return null or throw Exception)
                Assert.True(fromProtobuf == null || original.Equals(fromProtobuf), "equiv objects");
            }, "Unexpected end-group in source data; this usually means the source data is corrupt");
        }
    }
}