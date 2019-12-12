using System;
using System.IO;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace Examples.Issues
{
    
    public class SO6230449
    {
        [ProtoContract]
        class Foo
        {
            [ProtoMember(1)]
            public int Bar { get; set; }
        }

        [Fact]
        public void Execute()
        {
            using var ms = new MemoryStream();
            // write data with a length-prefix but no field number
            Serializer.SerializeWithLengthPrefix(ms, new Foo { Bar = 1 }, PrefixStyle.Base128, 0);
            Serializer.SerializeWithLengthPrefix(ms, new Foo { Bar = 2 }, PrefixStyle.Base128, 0);
            Serializer.SerializeWithLengthPrefix(ms, new Foo { Bar = 3 }, PrefixStyle.Base128, 0);

            ms.Position = 0;
            Assert.Equal(9, ms.Length); //, "3 lengths, 3 headers, 3 values");

            // read the length prefix and use that to limit each call
            TypeModel model = RuntimeTypeModel.Default;
            int bytesRead;
            List<Foo> foos = new List<Foo>();
            do
            {
                var len = ProtoReader.ReadLengthPrefix(ms, false, PrefixStyle.Base128, out _, out bytesRead);
                if (bytesRead <= 0) continue;

#pragma warning disable CS0618
                foos.Add((Foo)model.Deserialize(ms, null, typeof(Foo), len));
#pragma warning restore CS0618

                Assert.True(foos.Count <= 3, "too much data! (manual)");
            } while (bytesRead > 0);

            Assert.Equal(3, foos.Count);
            Assert.Equal(1, foos[0].Bar);
            Assert.Equal(2, foos[1].Bar);
            Assert.Equal(3, foos[2].Bar);

            // do it using DeserializeItems
            ms.Position = 0;

            foos.Clear();
            foreach (var obj in model.DeserializeItems<Foo>(ms, PrefixStyle.Base128, 0))
            {
                foos.Add(obj);
                Assert.True(foos.Count <= 3, "too much data! (foreach)");
            }
            Assert.Equal(3, foos.Count);
            Assert.Equal(1, foos[0].Bar);
            Assert.Equal(2, foos[1].Bar);
            Assert.Equal(3, foos[2].Bar);
        }
    }
}
