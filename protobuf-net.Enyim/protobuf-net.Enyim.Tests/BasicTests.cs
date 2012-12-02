using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using NUnit.Framework;
using System;
using System.Runtime.Serialization;

namespace protobuf_net.Enyim.Tests
{
    [TestFixture]
    public class BasicTests
    {
        [Test]
        public void CanConnectToMemcached()
        {
            var config = new MemcachedClientConfiguration();
            config.AddServer("127.0.0.1", 11211);
            string s1 = Guid.NewGuid().ToString();
            string s2 = Guid.NewGuid().ToString();
            Assert.AreNotEqual(s1, s2);
            using (var client1 = new MemcachedClient(config))
            using (var client2 = new MemcachedClient(config))
            {
                client1.Store(StoreMode.Set, "key1", s1);
                client2.Store(StoreMode.Set, "key2", s2);

                Assert.AreEqual(s2, client1.Get("key2"), "client1.key2");
                Assert.AreEqual(s1, client2.Get("key1"), "client2.key1");
            }
        }

        [Test]
        public void StoreWithDefaultTranscoder()
        {
            var config = new MemcachedClientConfiguration();
            config.AddServer("127.0.0.1", 11211);
            SomeType obj = new SomeType { Id = 1, Name = "abc" }, clone;
            using (var client = new MemcachedClient(config))
            {
                client.Store(StoreMode.Set, "raw1", obj);
            }
            using (var client = new MemcachedClient(config))
            {
                clone = (SomeType) client.Get("raw1");
            }
            Assert.AreEqual(1, clone.Id);
            Assert.AreEqual("abc", clone.Name);
        }
        [Test]
        public void StoreWithProtoTranscoder()
        {
            var config = new MemcachedClientConfiguration();
            config.AddServer("127.0.0.1", 11211);
            var transcoder = new ProtoBuf.Caching.Enyim.NetTranscoder();
            config.Transcoder =  transcoder;
            SomeType obj = new SomeType { Id = 1, Name = "abc" }, clone;
            string cloneString;
            Assert.AreEqual(0, transcoder.Deserialized);
            Assert.AreEqual(0, transcoder.Serialized);
            using (var client = new MemcachedClient(config))
            {
                client.Store(StoreMode.Set, "raw1", obj);
                client.Store(StoreMode.Set, "raw2", "def");
            }
            Assert.AreEqual(0, transcoder.Deserialized);
            Assert.AreEqual(1, transcoder.Serialized);
            using (var client = new MemcachedClient(config))
            {
                clone = (SomeType)client.Get("raw1");
                cloneString = (string)client.Get("raw2");
            }
            Assert.AreEqual(1, transcoder.Deserialized);
            Assert.AreEqual(1, transcoder.Serialized);

            Assert.AreEqual(1, clone.Id);
            Assert.AreEqual("abc", clone.Name);
            Assert.AreEqual("def", cloneString);
        }
    }
}

[Serializable, DataContract]
public class SomeType
{
    [DataMember(Order = 1)]
    public string Name { get; set; }

    [DataMember(Order = 2)]
    public int Id { get; set; }
}
