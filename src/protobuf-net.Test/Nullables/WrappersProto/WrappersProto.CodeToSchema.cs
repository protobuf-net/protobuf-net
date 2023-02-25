using ProtoBuf.Meta;
using ProtoBuf.Test.Nullables.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Nullables.WrappersProto
{
    public class WrappersProtoGetSchema : NullablesTestsBase
    {
        public WrappersProtoGetSchema(ITestOutputHelper log) : base(log)
        {
        }

        protected override void SetupRuntimeTypeModel(RuntimeTypeModel runtimeTypeModel)
        {
            MarkTypeFieldsAsSupportNull<FieldsMarkedWithSupportsNullPoco>();
        }

        [Fact]
        public void NullWrappedAsGroup_Transform_ToGoogleProtobufWellKnownType()
            => AssertSchemaSections<FieldsMarkedWithNullWrappedAsGroupPoco>(@"
                syntax = ""proto3"";
                package ProtoBuf.Test.Nullables.WrappersProto;
                import ""google/protobuf/wrappers.proto"";

                message FieldsMarkedWithNullWrappedAsGroupPoco {
                    group .google.protobuf.DoubleValue Item1 = 1;
                    group .google.protobuf.FloatValue Item2 = 2;
                    group .google.protobuf.Int64Value Item3 = 3;
                    group .google.protobuf.UInt64Value Item4 = 4;
                    group .google.protobuf.Int32Value Item5 = 5;
                    group .google.protobuf.UInt32Value Item6 = 6;
                    group .google.protobuf.BoolValue Item7 = 7;
                    group .google.protobuf.StringValue Item8 = 8;
                    group .google.protobuf.BytesValue Item9 = 9;
                }");
        
        [Fact]
        public void NullWrappedValue_Transform_ToGoogleProtobufWellKnownType()
            => AssertSchemaSections<FieldsMarkedWithNullWrappedValuePoco>(@"
                syntax = ""proto3"";
                package ProtoBuf.Test.Nullables.WrappersProto;
                import ""google/protobuf/wrappers.proto"";

                message FieldsMarkedWithNullWrappedValuePoco {
                    .google.protobuf.DoubleValue Item1 = 1;
                    .google.protobuf.FloatValue Item2 = 2;
                    .google.protobuf.Int64Value Item3 = 3;
                    .google.protobuf.UInt64Value Item4 = 4;
                    .google.protobuf.Int32Value Item5 = 5;
                    .google.protobuf.UInt32Value Item6 = 6;
                    .google.protobuf.BoolValue Item7 = 7;
                    .google.protobuf.StringValue Item8 = 8;
                    .google.protobuf.BytesValue Item9 = 9;
                }");
        
        [Fact]
        public void SupportsNullFields_Transform_ToGoogleProtobufWellKnownType()
            => AssertSchemaSections<FieldsMarkedWithSupportsNullPoco>(@"
                syntax = ""proto3"";
                package ProtoBuf.Test.Nullables.WrappersProto;
                import ""google/protobuf/wrappers.proto"";

                message FieldsMarkedWithSupportsNullPoco {
                    group .google.protobuf.DoubleValue Item1 = 1;
                    group .google.protobuf.FloatValue Item2 = 2;
                    group .google.protobuf.Int64Value Item3 = 3;
                    group .google.protobuf.UInt64Value Item4 = 4;
                    group .google.protobuf.Int32Value Item5 = 5;
                    group .google.protobuf.UInt32Value Item6 = 6;
                    group .google.protobuf.BoolValue Item7 = 7;
                    group .google.protobuf.StringValue Item8 = 8;
                    group .google.protobuf.BytesValue Item9 = 9;
                }");
        
        [Fact]
        public void BasicPoco_DoesNotTransformAnyFields()
            => AssertSchemaSections<BasicPoco>(@"
                syntax = ""proto3"";
                package ProtoBuf.Test.Nullables.WrappersProto;

                message BasicPoco {
                   double Item1 = 1;
                   float Item2 = 2;
                   int64 Item3 = 3;
                   uint64 Item4 = 4;
                   int32 Item5 = 5;
                   uint32 Item6 = 6;
                   bool Item7 = 7;
                   string Item8 = 8;
                   bytes Item9 = 9;
                }");

        [ProtoContract]
        private class FieldsMarkedWithSupportsNullPoco
        {
            [ProtoMember(1)]
            public double? Item1 { get; set; }
            
            [ProtoMember(2)]
            public float? Item2 { get; set; }
            
            [ProtoMember(3)]
            public long? Item3 { get; set; }
            
            [ProtoMember(4)]
            public ulong? Item4 { get; set; }
            
            [ProtoMember(5)]
            public int? Item5 { get; set; }
            
            [ProtoMember(6)]
            public uint? Item6 { get; set; }
            
            [ProtoMember(7)]
            public bool? Item7 { get; set; }
            
            [ProtoMember(8)]
            public string Item8 { get; set; }
            
            [ProtoMember(9)]
            public byte[] Item9 { get; set; }
        }
        
        [ProtoContract]
        private class FieldsMarkedWithNullWrappedAsGroupPoco
        {
            [ProtoMember(1), NullWrappedValue(AsGroup = true)]
            public double? Item1 { get; set; }
            
            [ProtoMember(2), NullWrappedValue(AsGroup = true)]
            public float? Item2 { get; set; }
            
            [ProtoMember(3), NullWrappedValue(AsGroup = true)]
            public long? Item3 { get; set; }
            
            [ProtoMember(4), NullWrappedValue(AsGroup = true)]
            public ulong? Item4 { get; set; }
            
            [ProtoMember(5), NullWrappedValue(AsGroup = true)]
            public int? Item5 { get; set; }
            
            [ProtoMember(6), NullWrappedValue(AsGroup = true)]
            public uint? Item6 { get; set; }
            
            [ProtoMember(7), NullWrappedValue(AsGroup = true)]
            public bool? Item7 { get; set; }
            
            [ProtoMember(8), NullWrappedValue(AsGroup = true)]
            public string Item8 { get; set; }
            
            [ProtoMember(9), NullWrappedValue(AsGroup = true)]
            public byte[] Item9 { get; set; }
        }
        
        [ProtoContract]
        private class FieldsMarkedWithNullWrappedValuePoco
        {
            [ProtoMember(1), NullWrappedValue]
            public double? Item1 { get; set; }
            
            [ProtoMember(2), NullWrappedValue]
            public float? Item2 { get; set; }
            
            [ProtoMember(3), NullWrappedValue]
            public long? Item3 { get; set; }
            
            [ProtoMember(4), NullWrappedValue]
            public ulong? Item4 { get; set; }
            
            [ProtoMember(5), NullWrappedValue]
            public int? Item5 { get; set; }
            
            [ProtoMember(6), NullWrappedValue]
            public uint? Item6 { get; set; }
            
            [ProtoMember(7), NullWrappedValue]
            public bool? Item7 { get; set; }
            
            [ProtoMember(8), NullWrappedValue]
            public string Item8 { get; set; }
            
            [ProtoMember(9), NullWrappedValue]
            public byte[] Item9 { get; set; }
        }
        
        [ProtoContract]
        private class BasicPoco
        {
            [ProtoMember(1)]
            public double? Item1 { get; set; }
            
            [ProtoMember(2)]
            public float? Item2 { get; set; }
            
            [ProtoMember(3)]
            public long? Item3 { get; set; }
            
            [ProtoMember(4)]
            public ulong? Item4 { get; set; }
            
            [ProtoMember(5)]
            public int? Item5 { get; set; }
            
            [ProtoMember(6)]
            public uint? Item6 { get; set; }
            
            [ProtoMember(7)]
            public bool? Item7 { get; set; }
            
            [ProtoMember(8)]
            public string Item8 { get; set; }
            
            [ProtoMember(9)]
            public byte[] Item9 { get; set; }
        }
    }
}