using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Issues
{
    [TestFixture]
    public class SO16797650
    {
        [ProtoContract]
        public abstract class MessageBase
        {
            [ProtoMember(1)]
            public string ErrorMessage { get; set; }

            public abstract int Type { get; }
        }

        [ProtoContract]
        public class Echo : MessageBase
        {
            public const int ID = 1;



            public override int Type
            {
                get { return ID; }
            }

            [ProtoMember(1)]
            public string Message { get; set; }
        }
        [ProtoContract]
        public class Foo : MessageBase { public override int Type { get { return 42; } } }
        [ProtoContract]
        public class Bar : MessageBase { public override int Type { get { return 43; } } }
        [Test]
        public void AddSubtypeAtRuntime()
        {
            var messageBase = RuntimeTypeModel.Default[typeof(MessageBase)];
            // this could be explicit in code, or via some external config file
            // that you process at startup
            messageBase.AddSubType(10, typeof(Echo)); // would need to **reliably** be 10
            messageBase.AddSubType(11, typeof(Foo));
            messageBase.AddSubType(12, typeof(Bar)); // etc

            // test it...
            Echo echo = new Echo { Message = "Some message", ErrorMessage = "XXXXX" };
            MessageBase echo1;
            using (var ms = new MemoryStream())
            {
                Serializer.NonGeneric.Serialize(ms, echo);
                ms.Position = 0;
                echo1 = (MessageBase)Serializer.NonGeneric.Deserialize(typeof(MessageBase), ms);
            }
            Assert.AreSame(echo.GetType(), echo1.GetType());
            Assert.AreEqual(echo.ErrorMessage, echo1.ErrorMessage);
            Assert.AreEqual(echo.Message, ((Echo)echo1).Message);
        }
    }
}
