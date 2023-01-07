using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test
{
    public partial class CollectionsWithNullsTests
    {
        private readonly ITestOutputHelper _log;
        public CollectionsWithNullsTests(ITestOutputHelper log)
        {
            _log = log;
            AdjustRuntimeTypeModel();
        }

        /*
5: "wrappers"; consider schema: import "google/protobuf/wrappers.proto";
syntax = "proto3"; message WrappedTest {
  .google.protobuf.DoubleValue optionalValue = 42;
} What we get today: [global::ProtoBuf.ProtoContract()]
public partial class WrappedTest
{
    [global::ProtoBuf.ProtoMember(42)]
    public global::Google.Protobuf.WellKnownTypes.DoubleValue optionalValue { get; set; }
} unusable, because global::Google.Protobuf.WellKnownTypes.DoubleValue doesn't exist [global::ProtoBuf.ProtoContract()]
public partial class WrappedTest
{
    [global::ProtoBuf.ProtoMember(42), NullWrappedValue]
    public double? optionalValue { get; set; }
} and want that to work in both directions, i.e. GetSchema with NullWrappedValue on a supported wrappers.proto type should emit ^^^

    */

        #region LegacyBehaviour

        [Fact]
        public void ExistingListsBehaviour()
        {
            using var ms = new MemoryStream(new byte[] { 0x08, 0x00, 0x08, 0x01, 0x08, 0x02 });
            var clone = Serializer.Deserialize<LegacyBehaviourPoco>(ms);
            if (!ms.TryGetBuffer(out var buffer)) buffer = new ArraySegment<byte>(ms.ToArray());
            Assert.Equal("08-00-08-01-08-02", BitConverter.ToString(buffer.Array, buffer.Offset, buffer.Count));
        }

        [DataContract]
        public class LegacyBehaviourPoco
        {
            [DataMember(Order = 1)]
            public List<int?> Items { get; } = new List<int?>();
        }

        #endregion

        [ProtoContract]
        public class Bar
        {
            public int Id { get; set; }
        }

        string GetSerializationOutputHex<T>(T instance)
        {
            var ms = new MemoryStream();
            Serializer.Serialize(ms, instance);
            if (!ms.TryGetBuffer(out var segment))
                segment = new ArraySegment<byte>(ms.ToArray());
            return BitConverter.ToString(segment.Array, segment.Offset, segment.Count);
        }

        /// <summary>
        /// Validates that sections exist inside of Serializer.Proto model definition
        /// </summary>
        /// <typeparam name="T">C# type to serialize into protobuf</typeparam>
        /// <param name="protoModelDefinitions">sections of protobuf, that exist in serialized protobuf. I.e. could be 'message Msg { }'</param>
        void AssertSchemaSections<T>(params string[] protoModelDefinitions)
        {
            if (protoModelDefinitions.Length == 0) Assert.Fail(nameof(protoModelDefinitions));

            var proto = Serializer.GetProto<T>();
            _log.WriteLine("Protobuf definition of model:");
            _log.WriteLine(proto);
            _log.WriteLine("-----------------------------");

            var actualProtoDefinition = Regex.Replace(proto, @"\s", "");
            foreach (var protoModelDefinition in protoModelDefinitions)
            {
                var expectedProtoDefinition = Regex.Replace(protoModelDefinition, @"\s", "");
                Assert.Contains(expectedProtoDefinition, actualProtoDefinition);
            }
        }
    }
}
