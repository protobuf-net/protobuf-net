#if !COREFX
using System;
using System.IO;
using Xunit;
using ProtoBuf;
using System.Data.Linq.Mapping;
using System.ComponentModel;
using System.Diagnostics;

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
    [ProtoContract]
    public class UserWithCrazyDefault
    {
        public UserWithCrazyDefault()
        {
            GUID = new Guid("01020304050607080102030405060708");
        }
        [ProtoMember(25), DefaultValue("01020304050607080102030405060708")]
        public System.Guid GUID { get; set; }
    }

    
    public class GuidTests
    {
        public static int Measure<T>(T value)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize<T>(ms, value);
                return (int)ms.Length;
            }
        }
        [Fact]
        public void TestPartialWithGuid()
        {
            var user = new User();
            var clone = Serializer.DeepClone(user);
            Assert.Equal(user.GUID, clone.GUID);
            Assert.Equal(0, Measure(user));

            user = new User { GUID = new Guid("00112233445566778899AABBCCDDEEFF") };
            clone = Serializer.DeepClone(user);
            Assert.Equal(user.GUID, clone.GUID);
            Assert.Equal(21, Measure(user));

            Serializer.PrepareSerializer<User>();

            user = new User();
            clone = Serializer.DeepClone(user);
            Assert.Equal(user.GUID, clone.GUID);
            Assert.Equal(0, Measure(user));

            user = new User { GUID = new Guid("00112233445566778899AABBCCDDEEFF") };
            clone = Serializer.DeepClone(user);
            Assert.Equal(user.GUID, clone.GUID);
            Assert.Equal(21, Measure(user));


        }

        [Fact]
        public void TestGuidWithCrazyDefault()
        {
            var user = new UserWithCrazyDefault();
            var clone = Serializer.DeepClone(user);
            Assert.Equal(user.GUID, clone.GUID);
            Assert.Equal(0, Measure(user));

            user = new UserWithCrazyDefault { GUID = new Guid("00112233445566778899AABBCCDDEEFF") };
            clone = Serializer.DeepClone(user);
            Assert.Equal(user.GUID, clone.GUID);
            Assert.Equal(21, Measure(user));

            user = new UserWithCrazyDefault { GUID = Guid.Empty };
            clone = Serializer.DeepClone(user);
            Assert.Equal(user.GUID, clone.GUID);
            Assert.Equal(3, Measure(user));

            Serializer.PrepareSerializer<UserWithCrazyDefault>();

            user = new UserWithCrazyDefault();
            clone = Serializer.DeepClone(user);
            Assert.Equal(user.GUID, clone.GUID);
            Assert.Equal(0, Measure(user));

            user = new UserWithCrazyDefault { GUID = new Guid("00112233445566778899AABBCCDDEEFF") };
            clone = Serializer.DeepClone(user);
            Assert.Equal(user.GUID, clone.GUID);
            Assert.Equal(21, Measure(user));

            user = new UserWithCrazyDefault { GUID = Guid.Empty };
            clone = Serializer.DeepClone(user);
            Assert.Equal(user.GUID, clone.GUID);
            Assert.Equal(3, Measure(user));
        }

        [Fact]
        public void TestGuidLayout()
        {
            var guid = new Guid("00112233445566778899AABBCCDDEEFF");
            var msBlob = guid.ToByteArray();
            var msHex = BitConverter.ToString(msBlob);
            Assert.Equal("33-22-11-00-55-44-77-66-88-99-AA-BB-CC-DD-EE-FF", msHex);

            var obj = new GuidLayout { Value = guid };
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, obj);
                string hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
                // 0A = 1010 = field 1, length delimited (sub-object)
                // 12 = length 18
                  // 09 = field 1, fixed-length 64 bit
                  // 33-22-11-00-55-44-77-66 = payload
                  // 11 = field 2, fixed-length 64 bit
                  // 88-99-AA-BB-CC-DD-EE-FF

                Assert.Equal(
                    "0A-12-09-33-22-11-00-55-44-77-66-11-88-99-AA-BB-CC-DD-EE-FF",
                    hex
                    );
            }
        }

        [ProtoContract]
        public class GuidLayout
        {
            [ProtoMember(1)]
            public Guid Value { get; set; }
        }

        [Fact]
        public void TestDeserializeEmptyWide()
        {
            GuidData data = Program.Build<GuidData>(
                0x0A, 0x12, // prop 1, string:18
                0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //1:fixed64:0
                0x11, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 //2:fixed64:0
                );
            Assert.Equal(Guid.Empty, data.Bar);
        }
        [Fact]
        public void TestDeserializeEmptyShort()
        {
            GuidData data = Program.Build<GuidData>(
                0x0A, 0x00 // prop 1, string:0
                );
            Assert.Equal(Guid.Empty, data.Bar);
        }
        [Fact]
        public void TestEmptyGuid() {
            GuidData foo = new GuidData { Bar = Guid.Empty };
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, foo);
                Assert.Equal(0, ms.Length); // 1 tag, 1 length (0)
                ms.Position = 0;
                GuidData clone = Serializer.Deserialize<GuidData>(ms);
                Assert.Equal(foo.Bar, clone.Bar);
            }
        }


        [Fact]
        public void TestNonEmptyGuid()
        {
            GuidData foo = new GuidData { Bar = Guid.NewGuid() };
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, foo);
                Assert.Equal(20, ms.Length); 
                ms.Position = 0;
                GuidData clone = Serializer.Deserialize<GuidData>(ms);
                Assert.Equal(foo.Bar, clone.Bar);
            }
        }
    }
}
#endif