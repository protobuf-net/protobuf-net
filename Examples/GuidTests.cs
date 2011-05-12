using System;
using System.IO;
using NUnit.Framework;
using ProtoBuf;
using System.Data.Linq.Mapping;

namespace Examples
{
    [ProtoContract]
    class GuidData
    {
        [ProtoMember(1)]
        public Guid Bar { get; set; }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    [ProtoPartialMember(25, "GUID")]
    public partial class User { }

    public partial class User
    {
        [global::System.Data.Linq.Mapping.ColumnAttribute(Storage = "_GUID", DbType = "UniqueIdentifier", UpdateCheck = UpdateCheck.Never)]
        [global::System.Runtime.Serialization.DataMemberAttribute(Order = 26)]
        public System.Guid GUID
        {
            get;
            set;
        }
    }

    [TestFixture]
    public class GuidTests
    {
        [Test]
        public void TestPartialWithGuid()
        {
            User user = new User();
            var clone = Serializer.DeepClone(user);
            Assert.AreEqual(user.GUID, clone.GUID);

            user = new User { GUID = Guid.NewGuid() };
            clone = Serializer.DeepClone(user);
            Assert.AreEqual(user.GUID, clone.GUID);
        }

        [Test]
        public void TestDeserializeEmptyWide()
        {
            GuidData data = Program.Build<GuidData>(
                0x0A, 0x12, // prop 1, string:18
                0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //1:fixed64:0
                0x11, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 //2:fixed64:0
                );
            Assert.AreEqual(Guid.Empty, data.Bar);
        }
        [Test]
        public void TestDeserializeEmptyShort()
        {
            GuidData data = Program.Build<GuidData>(
                0x0A, 0x00 // prop 1, string:0
                );
            Assert.AreEqual(Guid.Empty, data.Bar);
        }
        [Test]
        public void TestEmptyGuid() {
            GuidData foo = new GuidData { Bar = Guid.Empty };
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, foo);
                Assert.AreEqual(0, ms.Length); // 1 tag, 1 length (0)
                ms.Position = 0;
                GuidData clone = Serializer.Deserialize<GuidData>(ms);
                Assert.AreEqual(foo.Bar, clone.Bar);
            }
        }


        [Test]
        public void TestNonEmptyGuid()
        {
            GuidData foo = new GuidData { Bar = Guid.NewGuid() };
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, foo);
                Assert.AreEqual(20, ms.Length); 
                ms.Position = 0;
                GuidData clone = Serializer.Deserialize<GuidData>(ms);
                Assert.AreEqual(foo.Bar, clone.Bar);
            }
        }
    }
}
