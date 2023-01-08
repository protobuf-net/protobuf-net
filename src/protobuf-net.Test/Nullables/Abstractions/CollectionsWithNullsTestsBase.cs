using ProtoBuf.Meta;
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
        protected readonly RuntimeTypeModel _runtimeTypeModel;

        public CollectionsWithNullsTestsBase(ITestOutputHelper log)
        {
            _log = log;

            _runtimeTypeModel = RuntimeTypeModel.Create();
            SetupRuntimeTypeModel(_runtimeTypeModel);
        }

        protected virtual void SetupRuntimeTypeModel(RuntimeTypeModel runtimeTypeModel) { }

        protected T DeepClone<T>(T instance) => _runtimeTypeModel.DeepClone(instance);

        protected string GetSerializationOutputHex<T>(T instance)
        {
            var ms = new MemoryStream();
            _runtimeTypeModel.Serialize(ms, instance);
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

            var proto = _runtimeTypeModel.GetSchema(typeof(T), ProtoSyntax.Default);
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
