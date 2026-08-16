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
        private const string ProtoSurrogateAttribute = "ProtoBuf.ProtoSurrogateAttribute";
        private const string ProtoSerializerAttribute = "ProtoBuf.ProtoSerializerAttribute";

        public static IEnumerable<object[]> GetCases()
            => from model in DiscoverModels()
               let count = GetSamples(model).Length
               from index in Enumerable.Range(0, count)
               select new object[] { model.FullName!, index };

        /// <summary>
        /// Resolves one case to a fresh generated model and its sample, so that a sibling fixture
        /// can drive the same corpus without duplicating the reflection.
        /// </summary>
        internal static (TypeModel Model, object Value) CreateGeneratedCase(string modelTypeName, int index)
        {
            var modelType = Fixtures.GetType(modelTypeName);
            Assert.NotNull(modelType);
            var generated = Assert.IsAssignableFrom<TypeModel>(Activator.CreateInstance(modelType, nonPublic: true));
            return (generated, GetSamples(modelType)[index]);
        }

        [Theory, MemberData(nameof(GetCases))]
        public void GeneratedSerializerMatchesRuntimeModel(string modelTypeName, int index)
        {
            var modelType = Fixtures.GetType(modelTypeName);
            Assert.NotNull(modelType);

            var generated = Assert.IsAssignableFrom<TypeModel>(Activator.CreateInstance(modelType, nonPublic: true));
            var value = GetSamples(modelType)[index];
            var contractType = value.GetType();

            var runtime = CreateReference(modelType, contractType);

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

            AssertSimplePropertiesMatch(contractType, viaRuntime, viaGenerated);
        }

        /// <summary>
        /// Compare state that a byte comparison cannot see.
        /// </summary>
        /// <remarks>
        /// Some behaviour is only observable in members the payload never touches — SkipConstructor
        /// is exactly that: whether the constructor ran shows up in a non-serialized member, and
        /// re-serializing would compare equal either way. Only simply-typed properties are compared;
        /// message-typed ones are distinct instances by construction and would never match.
        /// </remarks>
        private static void AssertSimplePropertiesMatch(Type contractType, object expected, object actual)
        {
            foreach (var property in contractType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || property.GetIndexParameters().Length != 0) continue;

                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (!type.IsPrimitive && !type.IsEnum && type != typeof(string) && type != typeof(decimal)) continue;

                var expectedValue = property.GetValue(expected);
                var actualValue = property.GetValue(actual);
                Assert.True(Equals(expectedValue, actualValue),
                    $"{contractType.Name}.{property.Name}: runtime={expectedValue ?? "null"}, generated={actualValue ?? "null"}");
            }
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

            var generated = Assert.IsAssignableFrom<TypeModel>(Activator.CreateInstance(modelType, nonPublic: true));
            var compared = 0;

            foreach (var group in GetSamples(modelType).GroupBy(static x => x.GetType()))
            {
                if (group.Count() < 2) continue;

                var contractType = group.Key;
                var runtime = CreateReference(modelType, contractType);

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

        /// <summary>
        /// A getter-only member round-trips, agreeing with <see cref="RuntimeTypeModel"/>.
        /// </summary>
        /// <remarks>
        /// This cannot be a differential case, and the reason is worth stating: a sample can only
        /// ever hold the value its constructor gave it, so "discard the incoming value" and "store
        /// it" agree on every sample we are able to build. It only shows up against a payload that
        /// disagrees with the constructor — hence the hand-built bytes.
        /// <para>
        /// This is the case that separates ref-emit's two paths. The persisted-dll path discards the
        /// value (it has no verifiable way to assign the field); the runtime path assigns it by
        /// reflection. <c>[UnsafeAccessor]</c> lets generated code match the <em>runtime</em> path,
        /// so the fixture's <c>.reference.cs</c> snapshot is the outlier here, not us.
        /// </para>
        /// </remarks>
        [Fact]
        public void GetterOnlyMemberRoundTrips()
        {
            var contractType = Fixtures.GetType("AotFixtures.Getter.Getters")!;
            var generated = (TypeModel)Activator.CreateInstance(
                Fixtures.GetType("AotFixtures.Getter.GetterModel")!, nonPublic: true)!;

            // field 4 (the getter-only `Value`), varint, 7 — a value no constructor here produces
            byte[] payload = [0x20, 0x07];

            var runtime = RuntimeTypeModel.Create();
            runtime.Add(contractType, applyDefaultBehaviour: true);
            var value = contractType.GetProperty("Value")!;

            Assert.Equal(7, value.GetValue(Deserialize(generated, payload, contractType)));
            Assert.Equal(7, value.GetValue(Deserialize(runtime, payload, contractType)));
        }

        [Fact]
        public void AtLeastOneModelWasGenerated()
        {
            // guards against the whole suite silently passing because the generator produced nothing
            Assert.NotEmpty(DiscoverModels());
        }

        /// <summary>
        /// Models with no reference behaviour to differ from, because <see cref="RuntimeTypeModel"/>
        /// throws on their contracts rather than producing bytes to compare against.
        /// </summary>
        /// <remarks>
        /// Kept as an explicit list rather than "skip whatever ref-emit rejects", so that a contract
        /// ref-emit starts rejecting shows up as a failure instead of quietly leaving the suite.
        /// </remarks>
        private static readonly HashSet<string> NotDifferentiable =
        [
            "AotFixtures.TrivialGetter.TrivialGetterModel", // see TrivialGetterTests
        ];

        /// <summary>
        /// The reference model for a fixture, carrying whatever model-level options its
        /// <c>[ProtoModel]</c> declares.
        /// </summary>
        /// <remarks>
        /// Without this the comparison is against a *differently configured* model rather than
        /// against ref-emit: <c>AllowParseableTypes</c> is off by default and changes the wire form
        /// of every qualifying member, so the reference would be the unparsed shape and every
        /// parseable fixture would look like a generator bug.
        /// </remarks>
        private static RuntimeTypeModel CreateReference(Type modelType, Type contractType)
        {
            var runtime = RuntimeTypeModel.Create();

            // the attribute is generated into this assembly, so it is matched by name
            foreach (var attribute in modelType.GetCustomAttributes())
            {
                if (attribute.GetType().FullName != ProtoModelAttribute) continue;
                if (attribute.GetType().GetProperty("AllowParseableTypes")?.GetValue(attribute) is true)
                {
                    runtime.AllowParseableTypes = true;
                }
            }

            ApplySurrogates(runtime, modelType);
            ApplySerializers(runtime, modelType, contractType);
            runtime.Add(contractType, applyDefaultBehaviour: true);
            return runtime;
        }

        /// <summary>
        /// Replay the model's <c>[ProtoSurrogate]</c> declarations, which are the compile-time
        /// equivalent of <see cref="RuntimeTypeModel.SetSurrogate{TUnderlying, TSurrogate}"/>.
        /// </summary>
        /// <remarks>
        /// Without this the reference model has never heard of the surrogate, so the comparison
        /// would be against a differently-configured model rather than against ref-emit. The
        /// attribute is generated into this assembly, so everything is matched by name.
        /// </remarks>
        private static void ApplySurrogates(RuntimeTypeModel runtime, Type modelType)
        {
            var declarations = modelType.Assembly.GetCustomAttributes()
                .Concat(modelType.GetCustomAttributes())
                .Where(static x => x.GetType().FullName == ProtoSurrogateAttribute);

            foreach (var declaration in declarations)
            {
                var type = declaration.GetType();
                var underlying = (Type)type.GetProperty("Type")!.GetValue(declaration)!;
                var surrogate = (Type)type.GetProperty("Surrogate")!.GetValue(declaration)!;
                var converter = (Type)type.GetProperty("Converter")!.GetValue(declaration);

                if (converter is null)
                {
                    runtime.Add(underlying, applyDefaultBehaviour: false).SetSurrogate(surrogate);
                    continue;
                }

                var toSurrogate = MakeConverter(converter,
                    (string)type.GetProperty("ToSurrogate")!.GetValue(declaration)!, underlying, surrogate);
                var toUnderlying = MakeConverter(converter,
                    (string)type.GetProperty("ToType")!.GetValue(declaration)!, surrogate, underlying);

                typeof(RuntimeTypeModel).GetMethod(nameof(RuntimeTypeModel.SetSurrogate))!
                    .MakeGenericMethod(underlying, surrogate)
                    .Invoke(runtime, [toSurrogate, toUnderlying, DataFormat.Default, CompatibilityLevel.NotSpecified]);
            }
        }

        /// <summary>
        /// Replay the model's <c>[ProtoSerializer]</c> declarations, the compile-time equivalent of
        /// <see cref="MetaType.SerializerType"/>. Open declarations are closed over every matching
        /// instantiation reachable from the contract's member graph, which is exactly the set the
        /// generator closes over.
        /// </summary>
        private static void ApplySerializers(RuntimeTypeModel runtime, Type modelType, Type contractType)
        {
            var declarations = modelType.Assembly.GetCustomAttributes()
                .Concat(modelType.GetCustomAttributes())
                .Where(static x => x.GetType().FullName == ProtoSerializerAttribute)
                .ToList();
            if (declarations.Count == 0) return;

            var closed = new List<(Type Type, Type Serializer)>();
            var open = new List<(Type Definition, Type Serializer)>();
            foreach (var declaration in declarations)
            {
                var type = declaration.GetType();
                var underlying = (Type)type.GetProperty("Type")!.GetValue(declaration)!;
                var serializer = (Type)type.GetProperty("Serializer")!.GetValue(declaration)!;
                if (underlying.IsGenericTypeDefinition) open.Add((underlying, serializer));
                else closed.Add((underlying, serializer));
            }

            foreach (var reached in ReachableTypes(contractType))
            {
                if (!reached.IsConstructedGenericType) continue;
                foreach (var (definition, serializer) in open)
                {
                    if (reached.GetGenericTypeDefinition() != definition) continue;
                    if (closed.Any(x => x.Type == reached)) continue; // a closed declaration wins
                    closed.Add((reached, serializer.MakeGenericType(reached.GenericTypeArguments)));
                }
            }

            foreach (var (underlying, serializer) in closed)
            {
                runtime.Add(underlying, applyDefaultBehaviour: false).SerializerType = serializer;
            }
        }

        /// <summary>
        /// Every type reachable from a contract's public members. Member recursion stays inside the
        /// fixture assembly; generic arguments and element types are always followed.
        /// </summary>
        private static IEnumerable<Type> ReachableTypes(Type root)
        {
            var seen = new HashSet<Type>();
            var stack = new Stack<Type>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!seen.Add(current)) continue;
                yield return current;
                if (Nullable.GetUnderlyingType(current) is { } wrapped) stack.Push(wrapped);
                if (current.IsConstructedGenericType)
                {
                    foreach (var argument in current.GenericTypeArguments) stack.Push(argument);
                }
                if (current.IsArray) stack.Push(current.GetElementType()!);
                if (current.Assembly != root.Assembly) continue;
                foreach (var property in current.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    stack.Push(property.PropertyType);
                }
                foreach (var field in current.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    stack.Push(field.FieldType);
                }
            }
        }

        private static Delegate MakeConverter(Type converter, string methodName, Type from, Type to)
        {
            var method = converter.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static,
                null, [from], null)
                ?? throw new InvalidOperationException($"{converter.Name}.{methodName}({from.Name}) not found");

            return Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(from, to), method);
        }

        private static List<Type> DiscoverModels()
            => (from type in Fixtures.GetTypes()
                where type.GetCustomAttributes().Any(
                    static a => a.GetType().FullName == ProtoModelAttribute)
                where !NotDifferentiable.Contains(type.FullName!)
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

            // the all-defaults case matters: it is the one that should write no bytes at all. Tuples
            // have no parameterless constructor, so they can only be covered by declared samples.
            var defaults = from attribute in modelType.GetCustomAttributes()
                           where attribute.GetType().FullName == ProtoSerializableAttribute
                           let seed = (Type)attribute.GetType().GetProperty("Type")?.GetValue(attribute)
                           where seed is not null
                              && (seed.IsValueType || seed.GetConstructor(Type.EmptyTypes) is not null)
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
