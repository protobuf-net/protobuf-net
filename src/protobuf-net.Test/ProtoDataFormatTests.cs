using ProtoBuf;
using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    public class ProtoDataFormatTests
    {
        private readonly ITestOutputHelper _log;
        public ProtoDataFormatTests(ITestOutputHelper log) => _log = log;
        private string Log(string message)
        {
            _log?.WriteLine(message);
            return message;
        }

        void AssertPayload<T>(T message, string expectedHex)
        {
            var model = RuntimeTypeModel.Create(typeof(T).Name);
            model.AutoCompile = false;
            AssertImpl(model);
            model.CompileInPlace();
            AssertImpl(model);
            AssertImpl(model.Compile());

            void AssertImpl(TypeModel serializer)
            {
                using var ms = new MemoryStream();
                serializer.Serialize(ms, message);
                var actualHex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
                Log(actualHex);
                Assert.Equal(expectedHex, actualHex, ignoreCase: true);
            }
        }

        [ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
        [ProtoDataFormat(typeof(int), DataFormat.ZigZag)]
        public class Declaring { }

        public class Derived : Declaring { }

        [ProtoDataFormat(typeof(int), DataFormat.FixedSize)]
        public class Overriding : Declaring { }

        public class Plain { }

        [Fact]
        public void DeclaredTypeIsMatched()
            => Assert.Equal(DataFormat.FixedSize,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Declaring), typeof(Guid)));

        [Fact]
        public void MultipleDeclarationsAreKeyedByType()
            => Assert.Equal(DataFormat.ZigZag,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Declaring), typeof(int)));

        [Fact]
        public void UndeclaredTypeGetsDefault()
            => Assert.Equal(DataFormat.Default,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Declaring), typeof(long)));

        [Fact]
        public void BaseTypeDeclarationIsInherited()
            => Assert.Equal(DataFormat.FixedSize,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Derived), typeof(Guid)));

        [Fact]
        public void DerivedDeclarationWinsOverBase()
            => Assert.Equal(DataFormat.FixedSize,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Overriding), typeof(int)));

        [Fact]
        public void UndecoratedTypeGetsDefault()
            => Assert.Equal(DataFormat.Default,
                TypeDataFormatHelper.GetTypeDataFormat(typeof(Plain), typeof(Guid)));

        private static readonly Guid s_KnownGuid = Guid.Parse("c416e4af-455e-414c-948c-f27873263547");

        [ProtoContract, CompatibilityLevel(CompatibilityLevel.Level300)]
        [ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
        public class FixedGuidHolder
        {
            [ProtoMember(1)] public Guid Id { get; set; }
        }

        [Fact]
        public void TypeScopedDefaultMakesBareGuidFixed() => AssertPayload(
            new FixedGuidHolder { Id = s_KnownGuid },
            "0A-10-C4-16-E4-AF-45-5E-41-4C-94-8C-F2-78-73-26-35-47");
        /*
        0A = field 1, type String
        10 = length 16
        payload = the guid's 16 bytes; without the default this is the 36-char string form
        */

        [ProtoContract, CompatibilityLevel(CompatibilityLevel.Level300)]
        [ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
        public class NullableAndRepeatedGuids
        {
            [ProtoMember(1)] public Guid? MaybeId { get; set; }
            [ProtoMember(2)] public List<Guid> Batch { get; } = new List<Guid>();
        }

        [Fact]
        public void NullableUnwrapsToTheDeclaredType() => AssertPayload(
            new NullableAndRepeatedGuids { MaybeId = s_KnownGuid },
            "0A-10-C4-16-E4-AF-45-5E-41-4C-94-8C-F2-78-73-26-35-47");

        [Fact]
        public void RepeatedMembersKeyOnTheElementType() => AssertPayload(
            new NullableAndRepeatedGuids { Batch = { s_KnownGuid } },
            "12-10-C4-16-E4-AF-45-5E-41-4C-94-8C-F2-78-73-26-35-47");

        [ProtoContract]
        [ProtoDataFormat(typeof(int), DataFormat.ZigZag)]
        public class ExplicitWins
        {
            [ProtoMember(1)] public int Defaulted { get; set; }
            [ProtoMember(2, DataFormat = DataFormat.FixedSize)] public int Stated { get; set; }
        }

        [Fact]
        public void ExplicitMemberFormatBeatsTheDefault() => AssertPayload(
            new ExplicitWins { Defaulted = -1, Stated = -1 },
            "08-01-15-FF-FF-FF-FF");
        /*
        08-01       = field 1 varint, -1 zigzag-encoded as 1 (the default applied)
        15-FF^4     = field 2 fixed32, -1 (the explicit format won)
        */

        [ProtoContract, CompatibilityLevel(CompatibilityLevel.Level300)]
        [ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
        public class MapUntouched
        {
            [ProtoMember(1)] public Dictionary<int, Guid> ById { get; } = new Dictionary<int, Guid>();
        }

        [Fact]
        public void MapValuesDoNotTakeTheDefault() => AssertPayload(
            new MapUntouched { ById = { { 1, s_KnownGuid } } },
            "0A-28-08-01-12-24-63-34-31-36-65-34-61-66-2D-34-35-35-65-2D-34-31-34-63-2D-39-34-38-63-2D-66-32-37-38-37-33-32-36-33-35-34-37");
        /*
        0A-28 = field 1 (the map entry), length 40: key (08-01) + value tag/len (12-24) + 36 bytes.
        The value is the 36-char *string* form — [ProtoMap(ValueFormat)] is the tool for maps;
        the cross-cutting default deliberately does not reach them
        */

        [ProtoContract] // level 200: FixedSize on a Guid is simply ignored, like the explicit form
        [ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
        public class Level200Ignores
        {
            [ProtoMember(1)] public Guid Id { get; set; }
        }

        [Fact]
        public void Level200IgnoresAFixedGuidDefault() => AssertPayload(
            new Level200Ignores { Id = s_KnownGuid },
            "0A-12-09-AF-E4-16-C4-5E-45-4C-41-11-94-8C-F2-78-73-26-35-47");
        /*
        the level-200 BclGuid form regardless of the declared default, matching the explicit
        per-member behaviour ("FixedSize on a Guid below level 300 is simply ignored"):
        0A-12 = field 1, length 18; inside, two Fixed64 halves (09-... / 11-...) — the same
        literal CompatibilityLevelListsMaps.VanillaHazGuidsPayload pins for this guid
        */

        [ProtoContract]
        [ProtoDataFormat(typeof(int), DataFormat.FixedSize)]
        public class NullWrappedExempt
        {
            [ProtoMember(1), NullWrappedValue] public int? Wrapped { get; set; }
        }

        [Fact]
        public void NullWrappedMembersAreExemptFromInjection()
        {
            // would throw while building the model if the default were injected
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(NullWrappedExempt), applyDefaultBehaviour: true);
            _ = model.Serialize<NullWrappedExempt>(new MemoryStream(), new NullWrappedExempt { Wrapped = 0 });
        }

        [ProtoContract]
        [ProtoDataFormat(typeof(DateTime), DataFormat.WellKnown)]
        public class WellKnownPromotes
        {
            [ProtoMember(1)] public DateTime When { get; set; }
        }

        [Fact]
        public void WellKnownDefaultPromotesLevel200To240()
        {
            // identical semantics to the explicit per-member format: WellKnown at level 200 means
            // 240, i.e. Timestamp encoding. Pinned so the promotion is deliberate, not a surprise.
            var model = RuntimeTypeModel.Create();
            var schema = model.GetSchema(typeof(WellKnownPromotes), ProtoSyntax.Proto3);
            Assert.Contains(".google.protobuf.Timestamp", schema);
        }

        [ProtoContract]
        [ProtoDataFormat(typeof(long), DataFormat.FixedSize)]
        [ProtoDataFormat(typeof(int), DataFormat.ZigZag)]
        public class SchemaShapes
        {
            [ProtoMember(1)] public long Fixed { get; set; }
            [ProtoMember(2)] public int Zigged { get; set; }
            [ProtoMember(3)] public long Plain { get; set; }
        }

        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        [ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
        public class SchemaGuidBytes
        {
            [ProtoMember(1)] public Guid Id { get; set; }
        }

        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public class SchemaGuidString
        {
            [ProtoMember(1)] public Guid Id { get; set; }
        }

        /// <summary>
        /// The ambient default rewrites the emitted <c>.proto</c>, which is arguably its most
        /// user-visible consequence and was covered nowhere - gap B30 item 4.
        /// </summary>
        [Fact]
        public void AmbientDefaultReachesTheEmittedSchema()
        {
            var schema = Log(RuntimeTypeModel.Create().GetSchema(typeof(SchemaShapes), ProtoSyntax.Proto3));
            Assert.Contains("sfixed64 Fixed", schema);
            Assert.Contains("sint32 Zigged", schema);
            // field 3 is the SAME CLR type as field 1 and takes the same ambient default, so a
            // schema that varied per member would show it: this pins that the default is per TYPE
            Assert.Contains("sfixed64 Plain", schema);
        }

        /// <summary>
        /// The same, for the one case where the ambient default changes the wire TYPE rather than
        /// its width: a level-300 Guid is a string by default and bytes under FixedSize.
        /// </summary>
        [Fact]
        public void AmbientDefaultSwitchesGuidBetweenStringAndBytesInTheSchema()
        {
            Assert.Contains("string Id", Log(
                RuntimeTypeModel.Create().GetSchema(typeof(SchemaGuidString), ProtoSyntax.Proto3)));
            Assert.Contains("bytes Id", Log(
                RuntimeTypeModel.Create().GetSchema(typeof(SchemaGuidBytes), ProtoSyntax.Proto3)));
        }

        [Fact]
        public void AssemblyScopedGuidDefaultApplies()
            => Assert.Equal(DataFormat.FixedSize, TypeDataFormatHelper.GetTypeDataFormat(
                typeof(ProtoBuf.Test.TestDataFormat.AssemblyScopedFormats), typeof(Guid)));

        [Fact]
        public void ModuleBeatsAssembly()
            => Assert.Equal(DataFormat.FixedSize, TypeDataFormatHelper.GetTypeDataFormat(
                typeof(ProtoBuf.Test.TestDataFormat.AssemblyScopedFormats), typeof(int)));

        [Fact]
        public void AssemblyScopedGuidGoesOutFixed() => AssertPayload(
            new ProtoBuf.Test.TestDataFormat.AssemblyScopedFormats { Guid = s_KnownGuid },
            "0A-10-C4-16-E4-AF-45-5E-41-4C-94-8C-F2-78-73-26-35-47");

        // the precedence is type -> module -> assembly, most specific wins; the tests above prove
        // module beats assembly, and DeclaredTypeIsMatched/DerivedDeclarationWinsOverBase prove type
        // scope resolves in isolation (no module/assembly declaration in play). Neither proves TYPE
        // beats MODULE/ASSEMBLY end to end. TypeOverridesModule carries its own [ProtoDataFormat] for
        // int (ZigZag), in the same module/assembly that declares FixedSize for int - so this is the
        // one place the full three-way precedence is actually exercised.
        [Fact]
        public void TypeLevelBeatsModuleLevel()
            => Assert.Equal(DataFormat.ZigZag, TypeDataFormatHelper.GetTypeDataFormat(
                typeof(ProtoBuf.Test.TestDataFormat.TypeOverridesModule), typeof(int)));

        [Fact]
        public void TypeLevelOverrideGoesOutZigZag() => AssertPayload(
            new ProtoBuf.Test.TestDataFormat.TypeOverridesModule { Int32 = -1 },
            "08-01");
        /*
        08-01 = field 1 varint, -1 zigzag-encoded as 1 - not 0D-FF-FF-FF-FF, which is what the
        module's FixedSize default would have produced
        */
    }
}
