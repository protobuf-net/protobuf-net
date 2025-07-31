﻿using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace ProtoBuf.unittest.Serializers
{
    public class DiscriminatedUnionSerializable
    {
        [Fact]
        public void CanSerializeTypeThatUsesNakedDiscriminatedUnions()
        {
            BinaryFormatter bf;
            using var ms = new MemoryStream();
            try
            {
                bf = new BinaryFormatter();
                bf.Serialize(ms, new TypeThatUsesNakedDiscriminatedUnions());
            }
            catch (PlatformNotSupportedException)
            {
                return; // not much we can do, then 
            }

            ms.Position = 0;
            var obj = bf.Deserialize(ms);
            Assert.IsType<TypeThatUsesNakedDiscriminatedUnions>(obj);
        }

        [Serializable]
        public class TypeThatUsesNakedDiscriminatedUnions
        {
            public DiscriminatedUnionObject DUO { get; set; }
            public DiscriminatedUnion32 DU32 { get; set; }
            public DiscriminatedUnion32Object DU32O { get; set; }
            public DiscriminatedUnion64 DU64 { get; set; }
            public DiscriminatedUnion64Object DU64O { get; set; }
            public DiscriminatedUnion128 DU128 { get; set; }
            public DiscriminatedUnion128Object DU128O { get; set; }
        }
    }
}