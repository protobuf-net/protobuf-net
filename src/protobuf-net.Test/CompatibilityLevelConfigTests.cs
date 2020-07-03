using ProtoBuf.Meta;
using System;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test
{
    // explore different ways of manually configuring the compatibility level
    public class CompatibilityLevelConfigTests
    {
        private readonly ITestOutputHelper _log;
        public CompatibilityLevelConfigTests(ITestOutputHelper log)
            => _log = log;

        private string Log(string message)
        {
            _log?.WriteLine(message);
            return message;
        }

        private static TypeModel CreateModelVanilla()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            var mt = model.Add<SomeRandomType>();
            Assert.Equal(CompatibilityLevel.Level200, mt.CompatibilityLevel);
            return model;
        }

        private static TypeModel CreateModelDefaulted()
        {
            var model = RuntimeTypeModel.Create();
            model.DefaultCompatibilityLevel = CompatibilityLevel.Level300;
            model.AutoCompile = false;
            var mt = model.Add<SomeRandomType>();
            Assert.Equal(CompatibilityLevel.Level300, mt.CompatibilityLevel);
            return model;
        }

        private static TypeModel CreateModelCallback()
        {
            var model = RuntimeTypeModel.Create();
            model.BeforeApplyDefaultBehaviour += (s, e) => e.MetaType.CompatibilityLevel = CompatibilityLevel.Level300;
            model.AutoCompile = false;
            var mt = model.Add<SomeRandomType>();
            Assert.Equal(CompatibilityLevel.Level300, mt.CompatibilityLevel);
            return model;
        }

        [Fact]
        public void VanillaSchema()
            => Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types

message SomeRandomType {
   .bcl.Guid Id = 1; // default value could not be applied: 00000000-0000-0000-0000-000000000000
   .bcl.DateTime When = 2;
}
", Log(CreateModelVanilla().GetSchema(typeof(SomeRandomType), ProtoSyntax.Proto3)), ignoreLineEndingDifferences: true);

        [Fact]
        public void ModelDefaultedSchema()
    => Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;
import ""google/protobuf/timestamp.proto"";

message SomeRandomType {
   string Id = 1; // default value could not be applied: 00000000-0000-0000-0000-000000000000
   .google.protobuf.Timestamp When = 2;
}
", Log(CreateModelDefaulted().GetSchema(typeof(SomeRandomType), ProtoSyntax.Proto3)), ignoreLineEndingDifferences: true);

        [Fact]
        public void CallbackHookSchema()
    => Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;
import ""google/protobuf/timestamp.proto"";

message SomeRandomType {
   string Id = 1; // default value could not be applied: 00000000-0000-0000-0000-000000000000
   .google.protobuf.Timestamp When = 2;
}
", Log(CreateModelCallback().GetSchema(typeof(SomeRandomType), ProtoSyntax.Proto3)), ignoreLineEndingDifferences: true);

        [ProtoContract]
        public class SomeRandomType
        {
            [ProtoMember(1)]
            public Guid Id { get; set; }
            [ProtoMember(2)]
            public DateTime When { get; set; }
        }
    }

    
}
