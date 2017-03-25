using System;
using System.IO;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace Examples.Issues
{
    [TestFixture]
    public class SO6230449
    {
        [ProtoContract]
        class Foo
        {
            [ProtoMember(1)]
            public int Bar { get; set; }
        }

        [Test]
        public void Execute()
        {
            using (var ms = new MemoryStream())
            {
                // write data with a length-prefix but no field number
                Serializer.SerializeWithLengthPrefix(ms, new Foo { Bar = 1 }, PrefixStyle.Base128, 0);
                Serializer.SerializeWithLengthPrefix(ms, new Foo { Bar = 2 }, PrefixStyle.Base128, 0);
                Serializer.SerializeWithLengthPrefix(ms, new Foo { Bar = 3 }, PrefixStyle.Base128, 0);

                ms.Position = 0;
                Assert.AreEqual(9, ms.Length, "3 lengths, 3 headers, 3 values");

                // read the length prefix and use that to limit each call
                TypeModel model = RuntimeTypeModel.Default;
                int len, fieldNumber, bytesRead;
                List<Foo> foos = new List<Foo>();
                do
                {
                    len = ProtoReader.ReadLengthPrefix(ms, false, PrefixStyle.Base128, out fieldNumber, out bytesRead);
                    if (bytesRead <= 0) continue;

                    foos.Add((Foo)model.Deserialize(ms, null, typeof(Foo), len));

                    Assert.IsTrue(foos.Count <= 3, "too much data! (manual)");
                } while (bytesRead > 0);

                Assert.AreEqual(3, foos.Count);
                Assert.AreEqual(1, foos[0].Bar);
                Assert.AreEqual(2, foos[1].Bar);
                Assert.AreEqual(3, foos[2].Bar);

                // do it using DeserializeItems
                ms.Position = 0;

                foos.Clear();
                foreach (var obj in model.DeserializeItems<Foo>(ms, PrefixStyle.Base128, 0))
                {
                    foos.Add(obj);
                    Assert.IsTrue(foos.Count <= 3, "too much data! (foreach)");
                }
                Assert.AreEqual(3, foos.Count);
                Assert.AreEqual(1, foos[0].Bar);
                Assert.AreEqual(2, foos[1].Bar);
                Assert.AreEqual(3, foos[2].Bar);
            }
        }
    }
}
