using ProtoBuf.Meta;
using ProtoBuf.Serializers;
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
        public void RoundTripIPAddessViaParseable()
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

            var obj = new Example { Date = DateTime.Today, Ip = IPAddress.Parse("114.43.32.145"), ExampleDescription = "foo" };
            var clone = model.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.Equal(obj.Date, clone.Date);
            Assert.Equal(obj.Ip, clone.Ip);
            Assert.Equal(obj.ExampleDescription, clone.ExampleDescription);
        }

        [Fact]
        public void RoundTripIPAddessViaSurrogate()
        {
            var model = RuntimeTypeModel.Create();
            model.SetSurrogate<IPAddress, string>(IPAddressFormat, IPAddressParse);

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

            var obj = new Example { Date = DateTime.Today, Ip = IPAddress.Parse("114.43.32.145"), ExampleDescription = "foo" };
            var clone = model.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.Equal(obj.Date, clone.Date);
            Assert.Equal(obj.Ip, clone.Ip);
            Assert.Equal(obj.ExampleDescription, clone.ExampleDescription);
        }

        public static string IPAddressFormat(IPAddress value) => value?.ToString();
        public static IPAddress IPAddressParse(string value) => value is null ? null : IPAddress.Parse(value);

        [Fact]
        public void RoundTripIPAddessViaCustomSerializer()
        {
            var model = RuntimeTypeModel.Create();
            var mt = model.Add<IPAddress>(false);
            mt.SerializerType = typeof(IPAddressSerializer);
            mt.Name = "string";

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

            var obj = new Example { Date = DateTime.Today, Ip = IPAddress.Parse("114.43.32.145"), ExampleDescription = "foo" };
            var clone = model.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.Equal(obj.Date, clone.Date);
            Assert.Equal(obj.Ip, clone.Ip);
            Assert.Equal(obj.ExampleDescription, clone.ExampleDescription);
        }

        public sealed class IPAddressSerializer : ISerializer<IPAddress>
        {
            SerializerFeatures ISerializer<IPAddress>.Features => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeString;

            IPAddress ISerializer<IPAddress>.Read(ref ProtoReader.State state, IPAddress value)
                => IPAddressParse(state.ReadString());

            void ISerializer<IPAddress>.Write(ref ProtoWriter.State state, IPAddress value)
                => state.WriteString(IPAddressFormat(value));
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
