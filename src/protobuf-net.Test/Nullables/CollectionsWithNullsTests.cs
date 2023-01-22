using ProtoBuf.Test.Nullables.Abstractions;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Nullables
{
    public class CollectionsWithNullsTests : CollectionsWithNullsTestsBase
    {
        public CollectionsWithNullsTests(ITestOutputHelper log) 
            : base(log)
        {
        }

        [Fact]
        public void DuplicateFieldTypeModel_DoesNotGenerateMultipleWrappedSchemaDefinitions() 
            => AssertSchemaSections<DuplicateFieldTypesModel>(
@"syntax = ""proto3"";

message Bar {
    int32 Id = 1;
}
// warning: duplicate message name; you can use [ProtoContract(Name = ""..."")] to supply an alternative schema name
message WrappedBar {
    optional Bar value = 1;
}
message DuplicateFieldTypesModel {
    repeated WrappedBar Items1 = 1;
    repeated WrappedBar Items2 = 2;
}");

        [ProtoContract]
        class DuplicateFieldTypesModel
        {
            [ProtoMember(1), NullWrappedValue]
            public List<Bar?> Items1 { get; set; } = new();

            [ProtoMember(2), NullWrappedValue]
            public List<Bar?> Items2 { get; set; } = new();
        }
    }
}