using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Issues
{
    public class Issue697
    {
        [Fact]
        public void SkipFailsWhenNoStream()
        {
            var bytes = new byte[] {
                18, 209, 180, 9, 164, 51, 235, 15, 208, 245, 70, 233, 227, 170, 79, 135, 203, 158, 107, 30, 244, 111, 35, 0, 60, 73, 117, 227, 122, 147, 19, 38
            };
            using var ms = new MemoryStream(bytes);
            using var reader = ProtoReader.State.Create(ms, RuntimeTypeModel.Default);

            Assert.True(reader.ReadFieldHeader() > 0);
            try
            {   // this was throwing NullReferenceException
                reader.SkipField();
                Assert.True(false); // force fail, this should not have worked
            }
            catch (EndOfStreamException) { } // success (note: can't use Assert.Throws here because of ref-struct)
        }
    }
}
