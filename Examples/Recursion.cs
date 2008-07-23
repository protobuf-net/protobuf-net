using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using NUnit.Framework;
using System.IO;
using System.Runtime.Serialization;

namespace Examples
{
    [ProtoContract]
    public class RecursiveObject
    {
        [ProtoMember(1)]
        public RecursiveObject Yeuch { get; set; }
    }
    [TestFixture]
    public class Recursion
    {
        [Test, ExpectedException(typeof(ProtoException))]
        public void BlowUp()
        {
            RecursiveObject obj = new RecursiveObject();
            obj.Yeuch = obj;
            Serializer.Serialize(Stream.Null, obj);
        }
    }
}
