using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NUnit.Framework;
using ProtoBuf;

namespace Examples.Issues
{
    [TestFixture]
    public class SO3261310
    {

        public enum UrlStatus { A,B }
        public enum TrafficEntry { A }
        [ProtoContract]
        public class SerializableException { }
    
        [Test]
        public void TestBasicRoundTrip()
        {
            var item = new ProtoDictionary<string>();
            item.Add("abc", "def");
            item.Add("ghi", new List<UrlStatus> {UrlStatus.A, UrlStatus.B});

            var clone = Serializer.DeepClone(item);
            Assert.AreEqual(2, clone.Keys.Count);
            object o = clone["abc"];
            Assert.AreEqual("def", clone["abc"].Value);
            var list = (IList<UrlStatus>)clone["ghi"].Value;
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(UrlStatus.A, list[0]);
            Assert.AreEqual(UrlStatus.B, list[1]);
        }



        public class ProtoDictionary<TKey> : Dictionary<TKey, ProtoObject>
        {
            public void Add(TKey key, string value)
            {
                base.Add(key, new ProtoObject<string>(value));
            }

            public void Add(TKey key, List<string> value)
            {
                base.Add(key, new ProtoObject<List<string>>(value));
            }

            public void Add(TKey key, List<UrlStatus> value)
            {
                base.Add(key, new ProtoObject<List<UrlStatus>>(value));
            }

            public void Add(TKey key, Dictionary<string, string> value)
            {
                base.Add(key, new ProtoObject<Dictionary<string, string>>(value));
            }

            public void Add(TKey key, Dictionary<string, int> value)
            {
                base.Add(key, new ProtoObject<Dictionary<string, int>>(value));
            }

            public void Add(TKey key, List<TrafficEntry> value)
            {
                base.Add(key, new ProtoObject<List<TrafficEntry>>(value));
            }

            public ProtoDictionary()
            {
                // Do nothing
            }

            // NOTE: For whatever reason, this class will not correctly deserialize without this method, even though
            // the base class, Dictionary, has the SerializableAttribute. It's protected so only the framework can access it.
            protected ProtoDictionary(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }
        }
        [ProtoContract]
        [ProtoInclude(1, typeof(ProtoObject<string>))]
        [ProtoInclude(2, typeof(ProtoObject<int>))]
        [ProtoInclude(3, typeof(ProtoObject<List<string>>))]
        [ProtoInclude(4, typeof(ProtoObject<Dictionary<string, string>>))]
        [ProtoInclude(5, typeof(ProtoObject<List<TrafficEntry>>))]
        [ProtoInclude(6, typeof(ProtoObject<Dictionary<string, int>>))]
        [ProtoInclude(7, typeof(ProtoObject<bool>))]
        [ProtoInclude(8, typeof(ProtoObject<double>))]
        [ProtoInclude(9, typeof(ProtoObject<decimal>))]
        [ProtoInclude(10, typeof(ProtoObject<float>))]
        [ProtoInclude(11, typeof(ProtoObject<long>))]
        [ProtoInclude(12, typeof(ProtoObject<SerializableException>))]
        [ProtoInclude(13, typeof(ProtoObject<List<UrlStatus>>))]
        [Serializable]
        public abstract class ProtoObject
        {
            public static ProtoObject<T> Create<T>(T value)
            {
                return new ProtoObject<T>(value);
            }

            public object Value
            {
                get { return ValueImpl; }
                set { ValueImpl = value; }
            }

            protected abstract object ValueImpl { get; set; }
            
            protected ProtoObject()
            {

            }
        }

        [ProtoContract]
        [Serializable]
        public sealed class ProtoObject<T> : ProtoObject
        {
            public ProtoObject()
            {

            }

            public ProtoObject(T value)
            {
                Value = value;
            }

            [ProtoMember(1)]
            public new T Value { get; set; }

            protected override object ValueImpl
            {
                get { return Value; }
                set { Value = (T)value; }
            }

            public override string ToString()
            {
                return Value.ToString();
            }
        }
    }
}
