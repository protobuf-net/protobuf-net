using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using ProtoBuf.WellKnownTypes;
using System;
using System.IO;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Issues
{
    public class SO64101495
    {
        /*
         Verify 4 different ways of mapping a type to a primitive

        1. via AllowParseableTypes (let the engine handle the parse/format)
        2. via SetSurrogate (explicitly provide the conversion)
        3. via SerializerType (write our own serializer)
        4. via a separate DTO that matches what we want to serialize (baseline)
        */

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
", schema, ignoreLineEndingDifferences: true);

            var obj = new Example { Date = When, Ip = IPAddress.Parse(TestIPAddress), ExampleDescription = "foo" };
            Assert.Equal(ExpectedHex, GetHex(model, obj));
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
", schema, ignoreLineEndingDifferences: true);

            var obj = new Example { Date = When, Ip = IPAddress.Parse(TestIPAddress), ExampleDescription = "foo" };
            Assert.Equal(ExpectedHex, GetHex(model, obj));
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
", schema, ignoreLineEndingDifferences: true);

            var obj = new Example { Date = When, Ip = IPAddress.Parse(TestIPAddress), ExampleDescription = "foo" };
            Assert.Equal(ExpectedHex, GetHex(model, obj));
            var clone = model.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.Equal(obj.Date, clone.Date);
            Assert.Equal(obj.Ip, clone.Ip);
            Assert.Equal(obj.ExampleDescription, clone.ExampleDescription);
        }

        [Fact]
        public void RoundTripIPAddessEffectiveCompat()
        {
            var model = RuntimeTypeModel.Create();
            var schema = model.GetSchema(typeof(EffectiveExample));
            Log?.WriteLine(schema);
            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Issues;
import ""google/protobuf/timestamp.proto"";

message Example {
   .google.protobuf.Timestamp Date = 1;
   string Ip = 2;
   string ExampleDescription = 3;
}
", schema, ignoreLineEndingDifferences: true);

            var obj = new EffectiveExample { Date = When, Ip = TestIPAddress, ExampleDescription = "foo" };
            var hex = GetHex(model, obj);
            Log?.WriteLine(hex);
            Assert.Equal(ExpectedHex, hex);
            var clone = model.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.Equal(obj.Date, clone.Date);
            Assert.Equal(obj.Ip, clone.Ip);
            Assert.Equal(obj.ExampleDescription, clone.ExampleDescription);
        }

        static string GetHex<T>(TypeModel model, T value)
        {
            using var ms = new MemoryStream();
            model.Serialize(ms, value);
            return BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
        }

        private static readonly DateTime When = new DateTime(2020, 10, 1, 8, 0, 0, DateTimeKind.Utc);
        private const string
            TestIPAddress = "114.43.32.145",
            ExpectedHex = @"0A-06-08-80-99-D6-FB-05-12-0D-31-31-34-2E-34-33-2E-33-32-2E-31-34-35-1A-03-66-6F-6F";

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

        [ProtoContract(Name = "Example")]
        public class EffectiveExample
        {
            [ProtoMember(1)]
            public Timestamp Date { get; set; }
            [ProtoMember(2)]
            public string Ip { get; set; }
            [ProtoMember(3)]
            public string ExampleDescription { get; set; }
        }
    }
}
