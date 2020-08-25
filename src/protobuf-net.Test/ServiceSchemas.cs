using ProtoBuf.Meta;
using ProtoBuf.WellKnownTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;
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
            var service = new Service
            {
                Name = "myService",
                Methods =
                {
                    new ServiceMethod { Name = "unary", InputType = typeof(Empty), OutputType = typeof(Timestamp) },
                    new ServiceMethod { Name = "clientStreaming", InputType = typeof(Foo), OutputType =typeof(Foo), ClientStreaming = true },
                    new ServiceMethod { Name = "serverStreaming", InputType = typeof(Duration), OutputType =typeof(Foo), ServerStreaming = true },
                    new ServiceMethod { Name = "fullDuplex", InputType = typeof(Foo), OutputType = typeof(Foo), ClientStreaming = true, ServerStreaming = true },
                }
            };

            var schema = RuntimeTypeModel.Default.GetSchema(
                new SchemaGenerationOptions
                {
                    Syntax = ProtoSyntax.Proto3,
                    Flags = SchemaGenerationFlags.PreserveSubType,
                    Package = "mypackage",
                    Services = { service }
                }
            );
            Log(schema);
            Assert.Equal(@"syntax = ""proto3"";
package mypackage;
import ""google/protobuf/duration.proto"";
import ""google/protobuf/empty.proto"";
import ""google/protobuf/timestamp.proto"";
import ""protobuf-net/protogen.proto""; // custom protobuf-net options

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
   rpc unary (.google.protobuf.Empty) returns (.google.protobuf.Timestamp);
   rpc clientStreaming (stream Foo) returns (Foo);
   rpc serverStreaming (.google.protobuf.Duration) returns (stream Foo);
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


        [Fact]
        public void IEnumerableServiceSchema()
        {
            var service = new Service
            {
                Name = "ConferencesService",
                Methods =
                {
                    new ServiceMethod { Name = "ListConferencesEnumerable", InputType = typeof(Empty), OutputType = typeof(IEnumerable<ConferenceOverview>) },
                }
            };

            var schema = RuntimeTypeModel.Default.GetSchema(
                new SchemaGenerationOptions
                {
                    Syntax = ProtoSyntax.Proto3,
                    Package = "protobuf_net.Grpc.Reflection.Test",
                    Services = { service }
                }
            );
            Log(schema);
            Assert.Equal(@"syntax = ""proto3"";
package protobuf_net.Grpc.Reflection.Test;
import ""google/protobuf/empty.proto"";

message ConferenceOverview {
   string ID = 1; // default value could not be applied: 00000000-0000-0000-0000-000000000000
   string Title = 2;
}
message IEnumerable_ConferenceOverview {
   repeated ConferenceOverview items = 1;
}
service ConferencesService {
   rpc ListConferencesEnumerable (.google.protobuf.Empty) returns (IEnumerable_ConferenceOverview);
}
", schema, ignoreLineEndingDifferences: true);
        }

        [DataContract, CompatibilityLevel(CompatibilityLevel.Level300)]
        public class ConferenceOverview
        {
            [DataMember(Order = 1)]
            public Guid ID { get; set; }

            [DataMember(Order = 2)]
            public string Title { get; set; }
        }
    }
}
