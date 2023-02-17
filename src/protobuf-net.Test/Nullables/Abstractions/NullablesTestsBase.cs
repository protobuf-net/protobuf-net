using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Google.Protobuf.Reflection;
using ProtoBuf.Reflection;
using ProtoBuf.Test.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Nullables.Abstractions
{
    public abstract class NullablesTestsBase
    {
        private readonly ITestOutputHelper _log;
        protected readonly RuntimeTypeModel _runtimeTypeModel;

        public NullablesTestsBase(ITestOutputHelper log)
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

            var expectedTrimmed = expected.RemoveWhitespacesInLineStart();
            var resultTrimmed = proto.RemoveWhitespacesInLineStart();
            
            Assert.Equal(expectedTrimmed.Trim(), resultTrimmed.Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
        }

        protected void AssertCSharpCodeGeneratorTextEquality(
            string protobufSchemaContent,
            string generatedCode,
            string fileName = "default.proto",
            Dictionary<string, string> options = null)
        {
            var resultText = GetGenerateCodeResult(protobufSchemaContent, fileName, options);
            generatedCode = generatedCode.RemoveEmptyLines().RemoveWhitespacesInLineStart();
            Assert.Equal(generatedCode.Trim(), resultText.Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
        }

        protected void AssertCSharpCodeGeneratorTextDoesNotContain(
            string protobufSchemaContent,
            string expectedContent,
            string fileName = "default.proto",
            Dictionary<string, string> options = null)
        {
            var resultText = GetGenerateCodeResult(protobufSchemaContent, fileName, options);
            Assert.DoesNotContain(expectedContent.Trim(), resultText.Trim());
        }

        string GetGenerateCodeResult(string protobufSchemaContent, string fileName = "default.proto", Dictionary<string, string> options = null)
        {
            using var reader = new StringReader(protobufSchemaContent);
            var set = new FileDescriptorSet();
            set.Add(fileName, true, reader);
            set.Process();

            // act
            var result = CSharpCodeGenerator.Default.Generate(set, NameNormalizer.Default, options)?.ToArray();

            if (result is null || !result.Any()) Assert.Fail("No generation output found");
            if (result.Length > 1) Assert.Fail("Generated more than 1 file, however single file output was expected");

            var resultText = result.First().Text;
            _log.WriteLine("Generated such csharp code:");
            _log.WriteLine(resultText);
            _log.WriteLine("-----------------------------");

            // remove any whitespaces between '\r\n' and any valuable symbol to not struggle with tabs in tests
            return resultText.RemoveEmptyLines().RemoveWhitespacesInLineStart();
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

            public override int GetHashCode() => Id;
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

        protected void MarkTypeFieldsAsSupportNull<T>()
        {
            var metaType = this._runtimeTypeModel[typeof(T)];
            var propertiesAmount = typeof(T).GetProperties().Length;
            for (var i = 1; i < propertiesAmount + 1; i++)
            {
                metaType[i].SupportNull = true;
            }
        }
    }
}
