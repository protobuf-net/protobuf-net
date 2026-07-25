using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace ProtoBuf.AotConformance
{
    /// <summary>
    /// Asserts that the generated serializers agree with ref-emit, byte for byte.
    /// </summary>
    /// <remarks>
    /// The golden-file tests prove the generator's output is <em>stable</em>; these prove it is
    /// <em>correct</em>. Note the cross-deserialization: a serializer that consistently writes the
    /// wrong field number round-trips against itself perfectly, and only a cross-check catches it.
    /// </remarks>
    public class DifferentialTests
    {
        private static readonly Assembly Fixtures = typeof(DifferentialTests).Assembly;

        // the trigger attributes are generated into this assembly, so match them by name
        private const string ProtoModelAttribute = "ProtoBuf.ProtoModelAttribute";
        private const string ProtoSerializableAttribute = "ProtoBuf.ProtoSerializableAttribute";

        public static IEnumerable<object[]> GetCases()
            => from model in DiscoverModels()
               let count = GetSamples(model).Length
               from index in Enumerable.Range(0, count)
               select new object[] { model.FullName!, index };

        [Theory, MemberData(nameof(GetCases))]
        public void GeneratedSerializerMatchesRuntimeModel(string modelTypeName, int index)
        {
            var modelType = Fixtures.GetType(modelTypeName);
            Assert.NotNull(modelType);

            var generated = Assert.IsAssignableFrom<TypeModel>(Activator.CreateInstance(modelType));
            var value = GetSamples(modelType)[index];
            var contractType = value.GetType();

            var runtime = RuntimeTypeModel.Create();
            runtime.Add(contractType, applyDefaultBehaviour: true);

            var generatedBytes = Serialize(generated, value);
            var runtimeBytes = Serialize(runtime, value);
            Assert.Equal(Hex(runtimeBytes), Hex(generatedBytes));

            // each model must be able to read what the other wrote
            var viaGenerated = Deserialize(generated, runtimeBytes, contractType);
            var viaRuntime = Deserialize(runtime, generatedBytes, contractType);

            // compare by re-serializing with the reference model, rather than hand-writing a deep
            // comparer that would need extending for every future member shape
            Assert.Equal(Hex(runtimeBytes), Hex(Serialize(runtime, viaGenerated)));
            Assert.Equal(Hex(runtimeBytes), Hex(Serialize(runtime, viaRuntime)));
        }

        public static IEnumerable<object[]> GetModels()
            => from model in DiscoverModels() select new object[] { model.FullName! };

        /// <summary>
        /// Repeated occurrences of the same field must merge the same way in both models.
        /// </summary>
        /// <remarks>
        /// Round-tripping cannot reach this: serialization never emits a duplicated field, so the
        /// merge paths (<c>AppendBytes</c> concatenating byte arrays, <c>ReadMessage</c> merging into
        /// an existing sub-message) are invisible to it. Concatenating two payloads is itself a valid
        /// protobuf message, and is the cheapest way to produce duplicates for any contract.
        /// </remarks>
        [Theory, MemberData(nameof(GetModels))]
        public void RepeatedFieldOccurrencesMergeIdentically(string modelTypeName)
        {
            var modelType = Fixtures.GetType(modelTypeName);
            Assert.NotNull(modelType);

            var generated = Assert.IsAssignableFrom<TypeModel>(Activator.CreateInstance(modelType));
            var compared = 0;

            foreach (var group in GetSamples(modelType).GroupBy(static x => x.GetType()))
            {
                if (group.Count() < 2) continue;

                var contractType = group.Key;
                var runtime = RuntimeTypeModel.Create();
                runtime.Add(contractType, applyDefaultBehaviour: true);

                // every sample, not just two: samples that are entirely at their defaults serialize
                // to nothing, so a fixed pair can easily produce an empty payload
                var payload = group.SelectMany(x => Serialize(runtime, x)).ToArray();
                if (payload.Length == 0) continue; // nothing duplicated, nothing to compare

                var viaGenerated = Deserialize(generated, payload, contractType);
                var viaRuntime = Deserialize(runtime, payload, contractType);

                Assert.Equal(Hex(Serialize(runtime, viaRuntime)), Hex(Serialize(runtime, viaGenerated)));
                compared++;
            }

            Assert.True(compared > 0, $"no contract in {modelTypeName} had two samples to concatenate");
        }

        [Fact]
        public void AtLeastOneModelWasGenerated()
        {
            // guards against the whole suite silently passing because the generator produced nothing
            Assert.NotEmpty(DiscoverModels());
        }

        private static List<Type> DiscoverModels()
            => (from type in Fixtures.GetTypes()
                where type.GetCustomAttributes().Any(
                    static a => a.GetType().FullName == ProtoModelAttribute)
                orderby type.FullName
                select type).ToList();

        /// <summary>
        /// Sample values for a model: whatever the fixture declares in its <c>*Samples.Values</c>,
        /// plus a default instance of every seed type.
        /// </summary>
        private static object[] GetSamples(Type modelType)
        {
            var stem = modelType.Name.EndsWith("Model", StringComparison.Ordinal)
                ? modelType.Name.Substring(0, modelType.Name.Length - "Model".Length)
                : modelType.Name;

            var declared = Fixtures
                .GetType($"{modelType.Namespace}.{stem}Samples")
                ?.GetProperty("Values", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null) as object[];

            // the all-defaults case matters: it is the one that should write no bytes at all
            var defaults = from attribute in modelType.GetCustomAttributes()
                           where attribute.GetType().FullName == ProtoSerializableAttribute
                           let seed = (Type?)attribute.GetType().GetProperty("Type")?.GetValue(attribute)
                           where seed is not null
                           select Activator.CreateInstance(seed!)!;

            return (declared ?? Array.Empty<object>()).Concat(defaults).ToArray();
        }

        private static byte[] Serialize(TypeModel model, object value)
        {
            using var ms = new MemoryStream();
            model.Serialize(ms, value);
            return ms.ToArray();
        }

        private static object Deserialize(TypeModel model, byte[] payload, Type type)
        {
            using var ms = new MemoryStream(payload);
            return model.Deserialize(type, ms);
        }

        private static string Hex(byte[] value)
            => value.Length == 0 ? "(empty)" : BitConverter.ToString(value);
    }
}
