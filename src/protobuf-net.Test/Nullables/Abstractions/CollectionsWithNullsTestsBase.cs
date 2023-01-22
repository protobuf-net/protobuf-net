using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
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
            var hexOutput = BitConverter.ToString(segment.Array, segment.Offset, segment.Count);
            _log.WriteLine($"Serialization Hex-Output: '{hexOutput}'");
            return hexOutput;
        }

        /// <summary>
        /// Validates that sections exist inside of Serializer.Proto model definition
        /// </summary>
        /// <typeparam name="T">C# type to serialize into protobuf</typeparam>
        /// <param name="protoModelDefinitions">sections of protobuf, that exist in serialized protobuf. I.e. could be 'message Msg { }'</param>
        /// <remarks>each definition has to be contained only a single time</remarks>
        protected void AssertSchemaSections<T>(string expected)
        {
            var proto = _runtimeTypeModel.GetSchema(typeof(T), ProtoSyntax.Default);
            _log.WriteLine("Protobuf definition of model:");
            _log.WriteLine(proto);
            _log.WriteLine("-----------------------------");

            Assert.Equal(expected.Trim(), proto.Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
        }

        [ProtoContract]
        public class Bar
        {
            [ProtoMember(1)]
            public int Id { get; set; }

            public override bool Equals(object obj)
            {               
                if (obj is Bar)
                {
                    var that = obj as Bar;
                    return this.Id == that.Id;
                }

                return false;
            }
        }

        protected void AssertCollectionEquality<T>(List<T> one, List<T> another)
        {
            Assert.NotNull(one);
            Assert.NotNull(another);
            Assert.Equal(one.Count, another.Count);

            for (var i = 0; i < 0; i++)
            {
                Assert.Equal(one[i], another[i]);
            }
        }
    }
}
