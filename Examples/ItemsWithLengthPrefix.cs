using NUnit.Framework;
using System.IO;
using ProtoBuf;

namespace Examples
{
    [TestFixture]
    public class ItemsWithLengthPrefix
    {
        static Stream WriteData(int tag, PrefixStyle style, params int[] values)
        {
            MemoryStream ms = new MemoryStream();
            Foo foo = new Foo();
            foreach (int value in values)
            {
                foo.Value = value;
                Serializer.SerializeWithLengthPrefix(ms, foo, style, tag);
            }
            ms.Position = 0;
            return ms;
        }
        static int ReadData(Stream source, int tag, PrefixStyle style, params int[] values)
        {
            int count = 0;
            foreach(int value in values)
            {
                Foo foo = Serializer.DeserializeWithLengthPrefix<Foo>(source, style, tag);
                Assert.AreEqual(value, foo.Value);
                count++;
            }
            return count;
        }
        private static int CheckSimple(int tag, PrefixStyle style, params int[] values)
        {
            using(Stream source = WriteData(tag, style, values))
            {
                return ReadData(source, tag, style, values);
            }
        }

        [Test]
        public void ReadIndividuallyFixedLength()
        {
            Assert.AreEqual(8, CheckSimple(0, PrefixStyle.Fixed32, -2,-1,0,1,2,3,4,5));
        }

        [Test]
        public void ReadIndividuallyBase128NoTag()
        {
            Assert.AreEqual(8, CheckSimple(0, PrefixStyle.Base128, -2, -1, 0, 1, 2, 3, 4, 5));
        }

        [Test]
        public void ReadIndividuallyBase128Tag()
        {
            Assert.AreEqual(8, CheckSimple(2, PrefixStyle.Base128, -2, -1, 0, 1, 2, 3, 4, 5));
        }
    }
}
