using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Nullables.Abstractions
{
    public abstract class CollectionsWithNullsTestsBase
    {
        private readonly ITestOutputHelper _log;

        public CollectionsWithNullsTestsBase(ITestOutputHelper log)
        {
            _log = log;
            Setup();
        }

        protected virtual void Setup() { }

        protected T SerializeAndDeserialize<T>(T instance)
        {
            var ms = new MemoryStream();
            Serializer.Serialize(ms, instance);
            ms.Position = 0;
            return Serializer.Deserialize<T>(ms);
        }

        protected string GetSerializationOutputHex<T>(T instance)
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
        protected void AssertSchemaSections<T>(params string[] protoModelDefinitions)
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

        [ProtoContract]
        public class Bar
        {
            public int Id { get; set; }
        }
    }
}
