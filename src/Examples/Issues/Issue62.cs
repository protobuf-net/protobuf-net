using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue62
    {
        [ProtoContract]
        class CacheItem
        {
            [ProtoMember(1)]
            public int Id { get; set; }
            [ProtoMember(2)]
            public int AnotherNumber { get; set; }
            private readonly Dictionary<string, CacheItemValue> data
                = new Dictionary<string, CacheItemValue>();
            [ProtoMember(3)]
            public Dictionary<string, CacheItemValue> Data { get { return data; } }

            [ProtoMember(4)] // commented out while I investigate...
            public ListNode Nodes { get; set; }
        }
        [ProtoContract]
        class ListNode // I'd probably expose this as a simple list, though
        {
            [ProtoMember(1)]
            public double Head { get; set; }
            [ProtoMember(2)]
            public ListNode Tail { get; set; }
        }
        [ProtoContract]
        class CacheItemValue
        {
            [ProtoMember(1)]
            public string Key { get; set; }
            [ProtoMember(2)]
            public float Value { get; set; }
        }
        [Test]
        public void RunTest()
        {
            // invent CacheItemValue records
            Dictionary<string, CacheItem> htCacheItems = new Dictionary<string, CacheItem>();
            Random rand = new Random(123456);
            for (int i = 0; i < 40; i++)
            {
                string key;
                CacheItem ci = new CacheItem
                {
                    Id = rand.Next(10000),
                    AnotherNumber = rand.Next(10000)
                };
                while (htCacheItems.ContainsKey(key = NextString(rand))) { }
                htCacheItems.Add(key, ci);
                for (int j = 0; j < 100; j++)
                {
                    while (ci.Data.ContainsKey(key = NextString(rand))) { }
                    ci.Data.Add(key,
                        new CacheItemValue
                        {
                            Key = key,
                            Value = (float)rand.NextDouble()
                        });
                    int tail = rand.Next(1, 50);
                    ListNode node = null;
                    while (tail-- > 0)
                    {
                        node = new ListNode
                        {
                            Tail = node,
                            Head = rand.NextDouble()
                        };
                    }
                    ci.Nodes = node;
                }
            }
            int expected = GetChecksum(htCacheItems);
            var clone = Serializer.DeepClone(htCacheItems);
            int actual = GetChecksum(clone);
            Assert.AreEqual(expected, actual);
        }
        static int GetChecksum(Dictionary<string, CacheItem> data)
        {
            int chk = data.Count;
            foreach (var item in data)
            {
                chk += item.Key.GetHashCode()
                    + item.Value.AnotherNumber + item.Value.Id;
                foreach (var subItem in item.Value.Data.Values)
                {
                    chk += subItem.Key.GetHashCode()
                        + subItem.Value.GetHashCode();
                    ListNode node = item.Value.Nodes;
                    while(node != null) {
                        chk += node.Head.GetHashCode();
                        node = node.Tail;
                    }
                }
                
            }
            return chk;
        }
        static string NextString(Random random)
        {
            const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789 ";
            int len = random.Next(4, 10);
            char[] buffer = new char[len];
            for (int i = 0; i < len; i++)
            {
                buffer[i] = alphabet[random.Next(0, alphabet.Length)];
            }
            return new string(buffer);
        }
    }
}


