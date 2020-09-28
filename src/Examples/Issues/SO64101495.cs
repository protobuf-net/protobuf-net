using ProtoBuf.Meta;
using System;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Issues
{
    public class SO64101495
    {
        public SO64101495(ITestOutputHelper log)
            => Log = log;
        private readonly ITestOutputHelper Log;
        [Fact]
        public void RoundTripIPAddess()
        {
            var model = RuntimeTypeModel.Create();
            model.AllowParseableTypes = true;

            var schema = model.GetSchema(typeof(Example));
            Log?.WriteLine(schema);
            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Issues;
import ""google/protobuf/timestamp.proto"";

message Example {
   .google.protobuf.Timestamp Date = 1;
   string Ip = 2;
   string ExampleDescription = 3;
}
", schema);
        }

        

        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)] // use timestamp.proto for Date
        public class Example
        {
            [ProtoMember(1)]
            public DateTime Date { get; set; }
            [ProtoMember(2)]
            public IPAddress Ip { get; set; }
            [ProtoMember(3)]
            public string ExampleDescription { get; set; }
        }
    }
}
