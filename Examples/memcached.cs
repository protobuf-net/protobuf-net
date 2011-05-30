using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using Enyim.Caching;
using Enyim.Caching.Memcached;

namespace Examples
{
    /// <summary>
    /// Note that these are more integration tests than unit tests...
    /// </summary>
    [TestFixture]
    public class memcached
    {
        [ProtoContract]
        public class ContractType
        {
            [ProtoMember(1)]
            public string Name { get; set; }
            [ProtoMember(2)]
            public int Id { get; set; }
        }
        [Serializable]
        public class BasicType
        {
            public string Name { get; set; }
            public int Id { get; set; }
        }

        [Test, Ignore("Depends on memcached")]
        public void ShouldBeAbleToCacheBasicTypes()
        {
            BasicType original = new BasicType { Id = 123, Name = "abc" }, clone;
            using (var client = new MemcachedClient())
            {
                client.Store(StoreMode.Set, "ShouldBeAbleToCacheBasicTypes", original);
            }
            using (var client = new MemcachedClient())
            {
                clone = client.Get<BasicType>("ShouldBeAbleToCacheBasicTypes");
            }
            Assert.IsNotNull(clone);
            Assert.AreNotSame(original, clone);
            Assert.AreEqual(original.Id, clone.Id);
            Assert.AreEqual(original.Name, clone.Name);
        }

        [Test, Ignore("Depends on memcached")]
        public void ShouldBeAbleToCacheContractTypes()
        {
            ContractType original = new ContractType { Id = 123, Name = "abc" }, clone;
            using (var client = new MemcachedClient())
            {
                client.Store(StoreMode.Set, "ShouldBeAbleToCacheBasicTypes", original);
            }
            using (var client = new MemcachedClient())
            {
                clone = client.Get<ContractType>("ShouldBeAbleToCacheBasicTypes");
            }
            Assert.IsNotNull(clone);
            Assert.AreNotSame(original, clone);
            Assert.AreEqual(original.Id, clone.Id);
            Assert.AreEqual(original.Name, clone.Name);
        }
    }
}
