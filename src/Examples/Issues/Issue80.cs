using System;
using System.IO;
using NUnit.Framework;
using ProtoBuf;


namespace Examples.Issues
{
    [TestFixture]
   public class Issue80
   {

/*===============================================================================================*/
        [ProtoContract]
        public class OmsMessage {
            public enum MessageType
            {
                None = 0,
                MSG_TYPE_CONFIRMATION = 1
            }
            [ProtoMember(1)]
            public MessageType message_type;
            [ProtoMember(2)]
            public string application_id;
            [ProtoMember(3)]
            public string symbol;
            [ProtoMember(4)]
            public string initial_qty;
            [ProtoMember(5)]
            public string limit_price;
            [ProtoMember(6)]
            public string last_fill_qty;
            [ProtoMember(7)]
            public string last_fill_price;
            [ProtoMember(8)]
            public string trader_id;

        }

       [Test]
       public void Execute()
       {
           int len32_1, len32_2, len128_1, len128_2;

           //CreateParams a proto message.
           OmsMessage omsMessage = new OmsMessage();

           omsMessage.message_type = OmsMessage.MessageType.MSG_TYPE_CONFIRMATION;
           omsMessage.application_id = "application_id";
           omsMessage.symbol = "symbol";
           omsMessage.initial_qty = "initial_qty";
           omsMessage.limit_price = "limit_price";
           omsMessage.last_fill_qty = "last_fill_qty";
           omsMessage.last_fill_price = "last_fill_price";
           omsMessage.trader_id = "trader_hid";

           MemoryStream textStream = new MemoryStream();

           ProtoBuf.Serializer.SerializeWithLengthPrefix<OmsMessage>(textStream,
                omsMessage, ProtoBuf.PrefixStyle.Fixed32);

           textStream.Position = 0;
           Assert.IsTrue(ProtoBuf.Serializer.TryReadLengthPrefix(textStream.GetBuffer(), 0, 5, ProtoBuf.PrefixStyle.Fixed32, out len32_1), "len32 - buffer");
           Assert.IsTrue(ProtoBuf.Serializer.TryReadLengthPrefix(textStream, ProtoBuf.PrefixStyle.Fixed32, out len32_2), "len32 - stream");

           textStream = new MemoryStream();

           ProtoBuf.Serializer.SerializeWithLengthPrefix<OmsMessage>(textStream,
omsMessage, ProtoBuf.PrefixStyle.Base128,0);

           textStream.Position = 0;
           Assert.IsTrue(ProtoBuf.Serializer.TryReadLengthPrefix(textStream.GetBuffer(), 0, 5, ProtoBuf.PrefixStyle.Base128, out len128_1), "len128 - buffer");
           Assert.IsTrue(ProtoBuf.Serializer.TryReadLengthPrefix(textStream, ProtoBuf.PrefixStyle.Base128, out len128_2), "len128 - stream");
           

           Assert.AreEqual(len32_1, len32_2, "len32 - stream vs buffer");
           Assert.AreEqual(len128_1, len128_2, "len128 - stream vs buffer");
           Assert.AreEqual(len128_1, len32_1, "len32 vs len128");
       }

   }
}