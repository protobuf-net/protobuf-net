using ProtoBuf.Meta;
using ProtoBuf.Test.Nullables.Abstractions;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Nullables
{
    public class CollectionsWithNullsTests_DuplicateMessageNames : NullablesTestsBase
    {
        public CollectionsWithNullsTests_DuplicateMessageNames(ITestOutputHelper log) 
            : base(log)
        {
        }

        protected override void SetupRuntimeTypeModel(RuntimeTypeModel runtimeTypeModel)
        {
            runtimeTypeModel[typeof(DuplicateFieldTypesWithDifferentNullWrappingModel)][3].SupportNull = true;
        }

        [Fact]
        public void DuplicateFieldTypeModel_DoesNotGenerateMultipleWrappedSchemaDefinitions() 
            => AssertSchemaSections<DuplicateFieldTypesModel>(
@"syntax = ""proto3"";

message Bar {
   int32 Id = 1;
}
message WrappedBar {
   optional Bar value = 1;
}
message DuplicateFieldTypesModel {
   repeated WrappedBar Items1 = 1;
   repeated WrappedBar Items2 = 2;
}");

        [Fact]
        public void DuplicateFieldTypeWithAltNameModel_ReusesSameWrappedModel()
            => AssertSchemaSections<DuplicateFieldTypesWithAltNameModel>(
@"syntax = ""proto3"";

message Bar {
   int32 Id = 1;
}
message WrappedBar {
   optional Bar value = 1;
}
message DuplicateFieldTypesWithAltNameModel {
   repeated WrappedBar Items1 = 1;
   repeated WrappedBar AlternativeItems = 2;
}");

        [Fact]
        public void DuplicateFieldTypeWithAltNameModel_AndDifferentNamespace_EmitsWarningForMessageDuplication()
    => AssertSchemaSections<DuplicateFieldTypesWithUniqueNamespaceModel>( // it is expected to generate double 'BarNamespace'
@"syntax = ""proto3"";

message BarNamespace {
   int32 Id = 1;
}
message BarNamespace {
   int32 Id = 1;
}
// warning: duplicate message name; you can use [ProtoContract(Name = ""..."")] to supply an alternative schema name
message WrappedBarNamespace {
   optional BarNamespace value = 1;
}
message DuplicateFieldTypesWithUniqueNamespaceModel {
   repeated WrappedBarNamespace Items1 = 1;
   repeated WrappedBarNamespace Items2 = 2;
}");

        [Fact]
        public void DuplicateFieldTypeWithAltNameModel_AndDifferentNamespace_AndAlternativeNameSpecified_GeneratesUniqueSchemaDefinitionsWithoutWarning()
            => AssertSchemaSections<DuplicateFieldTypesWithUniqueNamespaceWithAlternativeNameModel>( // it is expected to generate double 'BarNamespace'
@"syntax = ""proto3"";

message BarNamespace {
   int32 Id = 1;
}
message BarNamespace {
   int32 Id = 1;
}
message WrappedBarNamespace {
   optional BarNamespace value = 1;
}
message WrappedAlternativeBarNamespace {
   optional BarNamespace value = 1;
}
message DuplicateFieldTypesWithUniqueNamespaceWithAlternativeNameModel {
   repeated WrappedBarNamespace Items1 = 1;
   repeated WrappedAlternativeBarNamespace AlternativeBarNamespace = 2;
}");

        [Fact]
        public void DuplicateFieldTypesWithNullWrappingModel_GeneratesUniqueSchemaDefinitionsWithoutWarning() 
            => AssertSchemaSections<DuplicateFieldTypesWithDifferentNullWrappingModel>(
@"syntax = ""proto3"";

message Bar {
   int32 Id = 1;
}
message WrappedBar {
   optional Bar value = 1;
}
message WrappedAsGroupBar {
   optional Bar value = 1;
}
message WrappedAsSupportNullBar {
   optional Bar value = 1;
}
message DuplicateFieldTypesWithDifferentNullWrappingModel {
   repeated WrappedBar Items1 = 1;
   repeated group WrappedAsGroupBar Items2 = 2;
   repeated group WrappedAsSupportNullBar Items3 = 3;
}");

        [ProtoContract]
        class DuplicateFieldTypesModel
        {
            [ProtoMember(1), NullWrappedValue]
            public List<Bar> Items1 { get; set; } = new();

            [ProtoMember(2), NullWrappedValue]
            public List<Bar> Items2 { get; set; } = new();
        }

        [ProtoContract]
        class DuplicateFieldTypesWithAltNameModel
        {
            [ProtoMember(1), NullWrappedValue]
            public List<Bar> Items1 { get; set; } = new();

            [ProtoMember(2, Name = "AlternativeItems"), NullWrappedValue]
            public List<Bar> Items2 { get; set; } = new();
        }

        [ProtoContract]
        class DuplicateFieldTypesWithUniqueNamespaceModel
        {
            [ProtoMember(1), NullWrappedValue]
            public List<Test1.BarNamespace> Items1 { get; set; } = new();

            [ProtoMember(2), NullWrappedValue]
            public List<Test2.BarNamespace> Items2 { get; set; } = new();
        }

        [ProtoContract]
        class DuplicateFieldTypesWithUniqueNamespaceWithAlternativeNameModel
        {
            [ProtoMember(1), NullWrappedValue]
            public List<Test1.BarNamespace> Items1 { get; set; } = new();

            [ProtoMember(2, Name = "AlternativeBarNamespace"), NullWrappedValue]
            public List<Test2.BarNamespace> Items2 { get; set; } = new();
        }

        [ProtoContract]
        class DuplicateFieldTypesWithDifferentNullWrappingModel
        {
            [ProtoMember(1), NullWrappedValue]
            public List<Bar> Items1 { get; set; } = new();

            [ProtoMember(2), NullWrappedValue(AsGroup = true)]
            public List<Bar> Items2 { get; set; } = new();

            [ProtoMember(3)] // [SupportNull] defined in test
            public List<Bar> Items3 { get; set; } = new();
        }
    }
}

namespace ProtoBuf.Test.Nullables.Test1
{
    [ProtoContract]
    public class BarNamespace
    {
        [ProtoMember(1)]
        public int Id { get; set; }
    }
}

namespace ProtoBuf.Test.Nullables.Test2
{
    [ProtoContract]
    public class BarNamespace
    {
        [ProtoMember(1)]
        public int Id { get; set; }
    }
}