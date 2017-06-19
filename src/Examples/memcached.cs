#if !NO_ENYIM
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;
using Enyim.Caching;
using Enyim.Caching.Memcached;

namespace Examples
{
    /// <summary>
    /// Note that these are more integration tests than unit tests...
    /// </summary>
    
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

        [Fact(Skip = "Depends on memcached")]
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
            Assert.NotNull(clone);
            Assert.NotSame(original, clone);
            Assert.Equal(original.Id, clone.Id);
            Assert.Equal(original.Name, clone.Name);
        }

        [Fact, Ignore("Depends on memcached")]
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
            Assert.NotNull(clone);
            Assert.NotSame(original, clone);
            Assert.Equal(original.Id, clone.Id);
            Assert.Equal(original.Name, clone.Name);
        }
    }
}
#endif