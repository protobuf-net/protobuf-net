using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ProtoBuf;

namespace Examples
{
    [TestFixture]
    public class ComparisonToNDCS
    {
        static List<BasicDto> GetTestData()
        {
            // just make up some gibberish
            var rand = new Random(12345);
            List<BasicDto> list = new List<BasicDto>(300000);
            for(int i = 0 ; i < 300000 ; i++)
            {
                var basicDto = new BasicDto();
                basicDto.Foo = new DateTime(rand.Next(1980, 2020), rand.Next(1, 13),
                                            rand.Next(1, 29), rand.Next(0, 24),
                                            rand.Next(0, 60), rand.Next(0, 60));
                basicDto.Bar = (float)rand.NextDouble();
                list.Add(basicDto);
            }
            return list;
        }
        [Test]
        public void CompareBasicTypeForBandwidth()
        {
            var list = GetTestData();
            long pb, ndcs;
            using(var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, list);
                pb = ms.Length;
                //Debug.WriteLine(pb);
            }
            using (var ms = new MemoryStream())
            {
                new NetDataContractSerializer().Serialize(ms, list);
                ndcs = ms.Length;
                //Debug.WriteLine(ndcs);
            }
            Assert.That(0, Is.LessThan(1)); // double check! (at least one test API has this reversed)
            Assert.That(pb, Is.LessThan(ndcs / 5));
        }
        [DataContract]
        public class BasicDto
        {
            [DataMember(Order = 1)]
            public DateTime Foo; //{ get;set;}
            [DataMember(Order = 2)]
            public float Bar; //{get;set;}
        }
    }
}
