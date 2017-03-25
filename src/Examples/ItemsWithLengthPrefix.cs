using NUnit.Framework;
using System.IO;
using ProtoBuf;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

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
        static int ReadIndividually(Stream source, int tag, PrefixStyle style, params int[] values)
        {
            int count = 0;
            foreach(int value in values)
            {
                if (source.Length == source.Position)
                {
                    Debugger.Break();
                }
                Foo foo = Serializer.DeserializeWithLengthPrefix<Foo>(source, style, tag);
                Assert.AreEqual(value, foo.Value);
                count++;
            }
            return count;
        }

        static int ReadStreaming(Stream source, int tag, PrefixStyle style, params int[] values)
        {
            var list = Serializer.DeserializeItems<int>(source, style, tag).ToList();
            Assert.AreEqual(values.Length, list.Count, "Count");
            for (int i = 0; i < values.Length; i++ )
            {
                Assert.AreEqual(values[i], list[i], "Index " + i + ", value " + values[i]);
            }
            return values.Length;
        }

        private static int CheckIndividually(int tag, PrefixStyle style, params int[] values)
        {
            using(Stream source = WriteData(tag, style, values))
            {
                return ReadIndividually(source, tag, style, values);
            }
        }
        private static int CheckStreaming(int tag, PrefixStyle style, params int[] values)
        {
            using (Stream source = WriteData(tag, style, values))
            {
                return ReadStreaming(source, tag, style, values);
            }
        }

        [Test]
        public void ReadIndividuallyFixedLength()
        {
            Assert.AreEqual(8, CheckIndividually(0, PrefixStyle.Fixed32, -2,-1,0,1,2,3,4,5));
        }

        [Test]
        public void ReadIndividuallyBase128NoTag()
        {
            Assert.AreEqual(8, CheckIndividually(0, PrefixStyle.Base128, -2, -1, 0, 1, 2, 3, 4, 5));
        }

        [Test]
        public void ReadIndividuallyBase128Tag()
        {
            Assert.AreEqual(8, CheckIndividually(2, PrefixStyle.Base128, -2, -1, 0, 1, 2, 3, 4, 5));
        }

        [Test]
        public void ReadStreamingFixedLength()
        {
            Assert.AreEqual(8, CheckStreaming(0, PrefixStyle.Fixed32, -2, -1, 0, 1, 2, 3, 4, 5));
        }

        [Test]
        public void ReadStreamingBase128NoTag()
        {
            Assert.AreEqual(8, CheckStreaming(0, PrefixStyle.Base128, -2, -1, 0, 1, 2, 3, 4, 5));
        }

        [Test]
        public void ReadStreamingBase128Tag()
        {
            Assert.AreEqual(8, CheckStreaming(2, PrefixStyle.Base128, -2, -1, 0, 1, 2, 3, 4, 5));
        }

        [Test]
        public void ReadStreamingParentFixedLength()
        {
            MemoryStream ms = new MemoryStream();
            IMLParent a, b, c;
            Serializer.SerializeWithLengthPrefix<IMLParent>(ms, a = InheritanceMidLevel.CreateChild(123, 456, 789), PrefixStyle.Fixed32);
            Serializer.SerializeWithLengthPrefix<IMLParent>(ms, b = InheritanceMidLevel.CreateChild(100, 200, 300), PrefixStyle.Fixed32);
            Serializer.SerializeWithLengthPrefix<IMLParent>(ms, c = InheritanceMidLevel.CreateChild(400, 500, 600), PrefixStyle.Fixed32);
            ms.Position = 0;
            var list = Serializer.DeserializeItems<IMLParent>(ms, PrefixStyle.Fixed32, 0).ToList();
            Assert.AreEqual(3, list.Count, "Count");
            InheritanceMidLevel.CheckParent(a, list[0]);
            InheritanceMidLevel.CheckParent(b, list[1]);
            InheritanceMidLevel.CheckParent(c, list[2]);
        }
        [Test]
        public void ReadStreamingParentBase128Tag()
        {
            MemoryStream ms = new MemoryStream();
            IMLParent a, b, c;
            Serializer.SerializeWithLengthPrefix<IMLParent>(ms, a = InheritanceMidLevel.CreateChild(123, 456, 789), PrefixStyle.Base128, 3);
            Serializer.SerializeWithLengthPrefix<IMLParent>(ms, b = InheritanceMidLevel.CreateChild(100, 200, 300), PrefixStyle.Base128, 3);
            Serializer.SerializeWithLengthPrefix<IMLParent>(ms, c = InheritanceMidLevel.CreateChild(400, 500, 600), PrefixStyle.Base128, 3);
            ms.Position = 0;
            var list = Serializer.DeserializeItems<IMLParent>(ms, PrefixStyle.Base128, 3).ToList();
            Assert.AreEqual(3, list.Count, "Count");
            InheritanceMidLevel.CheckParent(a, list[0]);
            InheritanceMidLevel.CheckParent(b, list[1]);
            InheritanceMidLevel.CheckParent(c, list[2]);
        }

        [Test]
        public void ReadStreamingParentBase128NoTag()
        {
            MemoryStream ms = new MemoryStream();
            IMLParent a, b, c;
            Serializer.SerializeWithLengthPrefix<IMLParent>(ms, a = InheritanceMidLevel.CreateChild(123, 456, 789), PrefixStyle.Base128, 0);
            Serializer.SerializeWithLengthPrefix<IMLParent>(ms, b = InheritanceMidLevel.CreateChild(100, 200, 300), PrefixStyle.Base128, 0);
            Serializer.SerializeWithLengthPrefix<IMLParent>(ms, c = InheritanceMidLevel.CreateChild(400, 500, 600), PrefixStyle.Base128, 0);
            ms.Position = 0;
            var list = Serializer.DeserializeItems<IMLParent>(ms, PrefixStyle.Base128, 0).ToList();
            Assert.AreEqual(3, list.Count, "Count");
            InheritanceMidLevel.CheckParent(a, list[0]);
            InheritanceMidLevel.CheckParent(b, list[1]);
            InheritanceMidLevel.CheckParent(c, list[2]);
        }


        [Test]
        public void ReadStreamingChildFixedLength()
        {
            MemoryStream ms = new MemoryStream();
            IMLChild a, b, c;
            Serializer.SerializeWithLengthPrefix<IMLChild>(ms, a = InheritanceMidLevel.CreateChild(123, 456, 789), PrefixStyle.Fixed32);
            Serializer.SerializeWithLengthPrefix<IMLChild>(ms, b = InheritanceMidLevel.CreateChild(100, 200, 300), PrefixStyle.Fixed32);
            Serializer.SerializeWithLengthPrefix<IMLChild>(ms, c = InheritanceMidLevel.CreateChild(400, 500, 600), PrefixStyle.Fixed32);
            ms.Position = 0;
            var list = Serializer.DeserializeItems<IMLChild>(ms, PrefixStyle.Fixed32, 0).ToList();
            Assert.AreEqual(3, list.Count, "Count");
            InheritanceMidLevel.CheckChild(a, list[0]);
            InheritanceMidLevel.CheckChild(b, list[1]);
            InheritanceMidLevel.CheckChild(c, list[2]);
        }
        [Test]
        public void ReadStreamingChildBase128Tag()
        {
            MemoryStream ms = new MemoryStream();
            IMLChild a, b, c;
            Serializer.SerializeWithLengthPrefix<IMLChild>(ms, a = InheritanceMidLevel.CreateChild(123, 456, 789), PrefixStyle.Base128, 3);
            Serializer.SerializeWithLengthPrefix<IMLChild>(ms, b = InheritanceMidLevel.CreateChild(100, 200, 300), PrefixStyle.Base128, 3);
            Serializer.SerializeWithLengthPrefix<IMLChild>(ms, c = InheritanceMidLevel.CreateChild(400, 500, 600), PrefixStyle.Base128, 3);
            ms.Position = 0;
            var list = Serializer.DeserializeItems<IMLChild>(ms, PrefixStyle.Base128, 3).ToList();
            Assert.AreEqual(3, list.Count, "Count");
            InheritanceMidLevel.CheckChild(a, list[0]);
            InheritanceMidLevel.CheckChild(b, list[1]);
            InheritanceMidLevel.CheckChild(c, list[2]);
        }

        [Test]
        public void ReadStreamingChildBase128NoTag()
        {
            MemoryStream ms = new MemoryStream();
            IMLChild a, b, c;
            Serializer.SerializeWithLengthPrefix<IMLChild>(ms, a = InheritanceMidLevel.CreateChild(123, 456, 789), PrefixStyle.Base128, 0);
            Serializer.SerializeWithLengthPrefix<IMLChild>(ms, b = InheritanceMidLevel.CreateChild(100, 200, 300), PrefixStyle.Base128, 0);
            Serializer.SerializeWithLengthPrefix<IMLChild>(ms, c = InheritanceMidLevel.CreateChild(400, 500, 600), PrefixStyle.Base128, 0);
            ms.Position = 0;
            var list = Serializer.DeserializeItems<IMLChild>(ms, PrefixStyle.Base128, 0).ToList();
            Assert.AreEqual(3, list.Count, "Count");
            InheritanceMidLevel.CheckChild(a, list[0]);
            InheritanceMidLevel.CheckChild(b, list[1]);
            InheritanceMidLevel.CheckChild(c, list[2]);
        }

        [Test]
        public void TestEmptyStreams()
        {
            TestEmptyStreams<int>();
            TestEmptyStreams<IMLChild>();
            TestEmptyStreams<IMLParent>();
        }

        static void TestEmptyStreams<T>()
        {
            Assert.IsFalse(Serializer.DeserializeItems<T>(Stream.Null, PrefixStyle.Fixed32, 0).Any());
            Assert.IsFalse(Serializer.DeserializeItems<T>(Stream.Null, PrefixStyle.Base128, 0).Any());
            Assert.IsFalse(Serializer.DeserializeItems<T>(Stream.Null, PrefixStyle.Base128, 1).Any());

            Assert.IsFalse(Serializer.DeserializeItems<T>(new MemoryStream(), PrefixStyle.Fixed32, 0).Any());
            Assert.IsFalse(Serializer.DeserializeItems<T>(new MemoryStream(), PrefixStyle.Base128, 0).Any());
            Assert.IsFalse(Serializer.DeserializeItems<T>(new MemoryStream(), PrefixStyle.Base128, 1).Any());
        }
    }
}
