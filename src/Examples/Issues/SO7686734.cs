using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class SO7686734
    {
        [DataContract]
        public abstract class GatewayPageEvent
        {
            protected GatewayPageEvent()
            {
                On = DateTime.Now;
            }

            [DataMember(Order = 1)]
            public Guid GatewayPageId { get; set; }

            [DataMember(Order = 2)]
            public DateTime On { get; set; }
        }

        [DataContract]
        public class GatewayPageAddedToSite : GatewayPageEvent
        {
            [DataMember(Order = 3)]
            public string Url { get; set; }

            [DataMember(Order = 4)]
            public string SiteCode { get; set; }
        }

        public string Serialize(object t)
        {
            var memoryStream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(memoryStream, t);
            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public object Deserialize(string value, Type targetType)
        {
            var bytes = Convert.FromBase64String(value);
            var stream = new MemoryStream(bytes);
            return ProtoBuf.Serializer.NonGeneric.Deserialize(targetType, stream);
        }

        [Test]
        public void protobuf_serialization_can_deserialized_guids()
        {
            RuntimeTypeModel.Default[typeof(GatewayPageEvent)].AddSubType(3, typeof(GatewayPageAddedToSite));
            var originalMessage = new GatewayPageAddedToSite
                                      {GatewayPageId = Guid.NewGuid(), SiteCode = "dls", Url = "test"};
            var serializedMessage = Serialize(originalMessage);
            var @event = (GatewayPageAddedToSite) Deserialize(serializedMessage, typeof (GatewayPageAddedToSite));
            Assert.AreEqual(originalMessage.GatewayPageId, @event.GatewayPageId);
        }


        [Test]
        public void guids_work_fine()
        {
            var original = Guid.NewGuid();
            var serialized = Serialize(original);
            var deserialized = (Guid) Deserialize(serialized, typeof (Guid));
            Assert.AreEqual(original, deserialized);
        }
    }
}
