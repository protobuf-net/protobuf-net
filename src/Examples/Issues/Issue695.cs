using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Issues
{
    public class Issue695
    {
        [ProtoContract]
        [ProtoInclude(1, typeof(DeclaredDerived_Derived))]
        public class DeclaredDerived_Base
        {
        }

        [ProtoContract]
        public class DeclaredDerived_Derived : DeclaredDerived_Base
        {
            [ProtoAfterDeserialization]
            public void PostDeserialization()
            {
                Value = 42;
            }

            public int Value { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(1, typeof(DeclaredRoot_Derived))]
        public class DeclaredRoot_Base
        {
            [ProtoAfterDeserialization]
            public virtual void PostDeserialization()
            {
            }
        }

        [ProtoContract]
        public class DeclaredRoot_Derived : DeclaredRoot_Base
        {
            public override void PostDeserialization()
            {
                base.PostDeserialization();
                Value = 42;
            }

            public int Value { get; set; }
        }

        [Fact]
        public void ExecuteDeclaredDerived()
        {
            var req = new DeclaredDerived_Derived();
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, req);
                bytes = ms.ToArray();
            }
            using (var ms = new MemoryStream(bytes))
            {
                var dRequest = Serializer.Deserialize<DeclaredDerived_Derived>(ms);
                Assert.IsType<DeclaredDerived_Derived>(dRequest);
                Assert.Equal(0, dRequest.Value);
            }
        }

        [Fact]
        public void ExecuteDeclaredRoot()
        {
            var req = new DeclaredRoot_Derived();
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, req);
                bytes = ms.ToArray();
            }
            using (var ms = new MemoryStream(bytes))
            {
                var dRequest = Serializer.Deserialize<DeclaredRoot_Derived>(ms);
                Assert.IsType<DeclaredRoot_Derived>(dRequest);
                Assert.Equal(42, dRequest.Value);
            }
        }
    }
}
