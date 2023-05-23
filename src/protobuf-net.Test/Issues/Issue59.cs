using ProtoBuf.Meta;
using System;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class Issue59
    {
        [ProtoContract]
        public class Data
        {
            [ProtoMember(1)]
            public Uri Value { get; set; }
        }

        [Fact]
        public static void CanGetSchema()
        {
            Assert.NotEmpty(RuntimeTypeModel.Default.GetSchema(typeof(Data)));
        }
    }
}