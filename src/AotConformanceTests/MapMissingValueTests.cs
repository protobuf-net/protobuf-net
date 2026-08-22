using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Xunit;

namespace ProtoBuf.AotConformance
{
    /// <summary>
    /// A map entry carrying a key and NO value field: gap B45.
    /// </summary>
    /// <remarks>
    /// <para>
    /// That shape is legal and <b>common</b> - it is exactly what protobuf-net writes when the value
    /// is trivial - so this is not an exotic payload. The runtime model never yields a null map
    /// value: <c>KeyValuePairSerializer.Read</c> tests <c>ValueChecker.IsNull</c> and then calls
    /// <c>CreateDefault</c>, which for a message *reads an empty payload through the serializer*
    /// rather than returning <c>default</c>, "useful in case the type is using a non-trivial
    /// constructor or factory API".
    /// </para>
    /// <para>
    /// <b>Only a READ can see this.</b> Writing is byte-identical either way, which is why the
    /// corpus - which compares bytes - never caught it, and why the payload here is hand-built.
    /// </para>
    /// </remarks>
    public class MapMissingValueTests
    {
        private static readonly Assembly Fixtures = typeof(MapMissingValueTests).Assembly;

        // field 2 (Dictionary<string, Payload>), length 3, entry = { field 1 : "a" } and no field 2.
        private static readonly byte[] KeyOnlyEntry = { 0x12, 0x03, 0x0A, 0x01, 0x61 };

        private static TypeModel Generated() => (TypeModel)Activator.CreateInstance(
            Fixtures.GetType("AotFixtures.Map.MapModel")!, nonPublic: true)!;

        private static TypeModel Reference()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(Fixtures.GetType("AotFixtures.Map.Maps")!, true);
            model.CompileInPlace();
            return model;
        }

        private static object ValueForKeyA(TypeModel model)
        {
            var mapsType = Fixtures.GetType("AotFixtures.Map.Maps")!;
            using var ms = new MemoryStream(KeyOnlyEntry);
            var result = model.Deserialize(ms, null, mapsType);
            var messages = (IDictionary)mapsType.GetProperty("Messages")!.GetValue(result)!;
            Assert.True(messages.Contains("a"), "the entry itself was lost, which is a different bug");
            return messages["a"];
        }

        [Fact]
        public void AMissingMessageValueIsAnEmptyInstanceNotNull()
        {
            // the reference first, so a change in protobuf-net's own behaviour shows up here as a
            // failure of the premise rather than as a mysterious failure of ours
            Assert.NotNull(ValueForKeyA(Reference()));
            Assert.NotNull(ValueForKeyA(Generated()));
        }

        [Fact]
        public void TheTwoModelsAgreeOnWhatAMissingMessageValueReadsBackAs()
        {
            var theirs = ValueForKeyA(Reference());
            var mine = ValueForKeyA(Generated());
            Assert.Equal(theirs is null, mine is null);
            if (theirs is not null) Assert.Equal(theirs.GetType(), mine.GetType());
        }
    }
}
