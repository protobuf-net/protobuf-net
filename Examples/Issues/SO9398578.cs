using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;

namespace Examples.Issues
{
    [TestFixture]
    public class SO9398578
    {
        [Test]
        public void TestRandomDataWithString()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                var bytes = new byte[1024];
                new Random(123456).NextBytes(bytes);
                var stream = new MemoryStream(bytes);
                stream.Seek(0, SeekOrigin.Begin);
                Assert.Greater(3, 0); // I always double-check the param order
                Assert.Greater(stream.Length, 0);
                Serializer.Deserialize<string>(stream);
            });
        }
        [Test]
        public void TestRandomDataWithContractType()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                var bytes = new byte[1024];
                new Random(123456).NextBytes(bytes);
                var stream = new MemoryStream(bytes);
                stream.Seek(0, SeekOrigin.Begin);
                Assert.Greater(3, 0); // I always double-check the param order
                Assert.Greater(stream.Length, 0);
                Serializer.Deserialize<Foo>(stream);
            });
        }
        [Test]
        public void TestRandomDataWithReader()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                var bytes = new byte[1024];
                new Random(123456).NextBytes(bytes);
                var stream = new MemoryStream(bytes);                
                stream.Seek(0, SeekOrigin.Begin);
                Assert.Greater(3, 0); // I always double-check the param order
                Assert.Greater(stream.Length, 0);

                using (var reader = new ProtoReader(stream, null, null))
                {
                    while (reader.ReadFieldHeader() > 0)
                    {
                        reader.SkipField();
                    }
                }
            });
        }

        [ProtoContract]
        public class Foo
        {
        }
    }
}
