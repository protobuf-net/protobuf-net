using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using NUnit.Framework;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace protobuf_net.Enyim.Tests
{
    [TestFixture]
    public class BasicTests
    {
        static MemcachedClientConfiguration GetConfig()
        {
            const string server = "192.168.0.8";
            const int port = 11211;

            var config = new MemcachedClientConfiguration();
            config.AddServer(server, port);
            return config;
        }
        [Test]
        public void CanConnectToMemcached()
        {
            var config = GetConfig();
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
            var config = GetConfig();
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
            var config = GetConfig();
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

        [Test]
        public void RoundTripList()
        {
            List<MyClass> myList = new List<MyClass> {
                new MyClass { a = 1, b = 2},
                new MyClass { a = 3, b = 4},
                new MyClass { a = 5, b = 6}
            }, cloneList;
            var config = GetConfig();
            var transcoder = new ProtoBuf.Caching.Enyim.NetTranscoder();
            config.Transcoder = transcoder;

            using (var client = new MemcachedClient(config))
            {
                client.Store(StoreMode.Set, "list1", myList);
            }

            using (var client = new MemcachedClient(config))
            {
                cloneList = (List<MyClass>)client.Get("list1");
            }
            Assert.AreEqual(3, cloneList.Count);
            Assert.AreEqual(1, cloneList[0].a);
            Assert.AreEqual(2, cloneList[0].b);
            Assert.AreEqual(3, cloneList[1].a);
            Assert.AreEqual(4, cloneList[1].b);
            Assert.AreEqual(5, cloneList[2].a);
            Assert.AreEqual(6, cloneList[2].b);
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


[ProtoContract]
public class MyClass
{
    [ProtoMember(1)]
    public int a { get; set; }
    [ProtoMember(2)]
    public int b { get; set; }
}