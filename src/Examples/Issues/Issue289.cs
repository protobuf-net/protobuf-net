using System.Collections.Generic;
using Xunit;

namespace ProtoBuf.Issues
{
    public class Issue289
    {
        [Fact]
        public void Run()
        {
            var obj = new MyPrincipal("abc", 123);
            var clone = Serializer.DeepClone(obj);
            Assert.Equal("abc", clone.Id);
            Assert.Equal(123, clone.MyId);
        }

        [ProtoContract(SkipConstructor = true)]
        public class MyPrincipal
        {

            public MyPrincipal(string Id, int myId)
            {
                this.Id = Id;
                this.MyId = myId;
                this.Principals = new Dictionary<MyPrincipalType, User>();
            }

            [ProtoMember(1)]
            public string Id { get; set; }


            [ProtoMember(2)]
            public int MyId { get; set; }

            [ProtoMember(3)]
            //[ProtoMap(DisableMap = true)]
            public Dictionary<MyPrincipalType, User> Principals { get; set; }


            public MyPrincipal AddPrincipal(MyPrincipalType principalType, User principalValue)
            {
                this.Principals.Add(principalType, principalValue);
                return this;
            }
        }
        [ProtoContract]
        public class User { }

        public enum MyPrincipalType
        {
            Test = 0
        }
    }
}
