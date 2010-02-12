using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf.Serializers;
namespace ProtoBuf.unittest.Serializers
{
    [TestFixture]
    public class Tag
    {
        [Test]
        public void TestBasicTags()
        {

            Util.Test("abc", nil => new TagDecorator(1, WireType.String, nil), "0A");
        }
    }
}
