using ProtoBuf.Meta;
using ProtoBuf.WellKnownTypes;
using System;
using System.Collections.Immutable;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test
{
    public class ServiceSchemas
    {
        public ServiceSchemas(ITestOutputHelper log) => _log = log;
        private readonly ITestOutputHelper _log;
        private void Log(string message) => _log?.WriteLine(message);
        [Fact]
        public void GenerateServiceSchema()
        {
            var methodsBuilder = ImmutableArray<ServiceMethod>.Empty.ToBuilder();
            methodsBuilder.Add(new ServiceMethod("unary", typeof(Empty), typeof(Timestamp), MethodType.Unary));
            methodsBuilder.Add(new ServiceMethod("clientStreaming", typeof(Foo), typeof(Foo), MethodType.ClientStreaming));
            methodsBuilder.Add(new ServiceMethod("serverStreaming", typeof(Duration), typeof(Foo), MethodType.ServerStreaming));
            methodsBuilder.Add(new ServiceMethod("fullDuplex", typeof(Foo), typeof(Foo), MethodType.DuplexStreaming));

            var servicesBuilder = ImmutableArray<Service>.Empty.ToBuilder();
            servicesBuilder.Add(new Service("myService", methodsBuilder.ToImmutable()));

            var schema = RuntimeTypeModel.Default.GetSchema(null,
                new SchemaGenerationOptions(ProtoSyntax.Proto3, flags: SchemaGenerationFlags.PreserveSubType,
                package: "mypackage", services: servicesBuilder.ToImmutable()));
            Log(schema);
            Assert.Equal(@"syntax = ""proto3"";
package mypackage;
import ""protobuf-net/protogen.proto""; // custom protobuf-net options
import ""google/protobuf/timestamp.proto"";
import ""google/protobuf/duration.proto"";
import ""google/protobuf/empty.proto"";

message Bar {
   string Value = 1;
}
message Foo {
   .google.protobuf.Timestamp When = 1;
   string Id = 2; // default value could not be applied: 00000000-0000-0000-0000-000000000000
   oneof subtype {
      option (.protobuf_net.oneofopt).isSubType = true;
      Bar Bar = 42;
   }
}
service myService {
   rpc unary (.google.protobuf.Timestamp) returns (.google.protobuf.Empty);
   rpc clientStreaming (Foo) returns (stream Foo);
   rpc serverStreaming (stream Foo) returns (.google.protobuf.Duration);
   rpc fullDuplex (stream Foo) returns (stream Foo);
}
", schema, ignoreLineEndingDifferences: true);
        }

        [ProtoContract]
        [ProtoInclude(42, typeof(Bar))]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public class Foo
        {
            [ProtoMember(1)]
            public DateTime When { get; set; }
            [ProtoMember(2)]
            public Guid Id { get; set; }
        }

        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public class Bar : Foo
        {
            [ProtoMember(1)]
            public decimal Value { get; set; }
        }
    }
}
