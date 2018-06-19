using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ProtoBuf.Issues
{
    public class Issue403
    {
        [Fact]
        public void MultiLevelInheritance()
        {
            Serializer.PrepareSerializer<PublishableMessage>();

            var publishableMessage = new AnotherDerivedPublishableMessage() { Name = "HELLO", Location = "HERE" };

            var serialised = SerialiseMessage(publishableMessage);

            var obj = DeserialiseMessage(serialised);
            var deserialised = Assert.IsType<AnotherDerivedPublishableMessage>(obj);

            Assert.Equal(publishableMessage.Name, deserialised.Name);
            Assert.Equal(publishableMessage.Location, deserialised.Location);
        }

        private static byte[] SerialiseMessage(PublishableMessage message)
        {
            using (var memoryStream = new MemoryStream())
            {
                Serializer.Serialize(memoryStream, message);
                return memoryStream.ToArray();
            }
        }

        private static PublishableMessage DeserialiseMessage(byte[] serialisedMessage)
        {
            using (var stream = new MemoryStream(serialisedMessage))
            {
                return Serializer.Deserialize<PublishableMessage>(stream);
            }
        }

        [ProtoContract]
        [ProtoInclude(1, typeof(DerivedPublishableMessage))]
        public class PublishableMessage
        {

        }

        [ProtoContract]
        [ProtoInclude(100, typeof(AnotherDerivedPublishableMessage))]
        public class DerivedPublishableMessage : PublishableMessage
        {
            [ProtoMember(1)]
            public string Name { get; set; }
        }


        [ProtoContract]
        public class AnotherDerivedPublishableMessage : DerivedPublishableMessage
        {
            [ProtoMember(1)]
            public string Location { get; set; }
        }
    }
}
