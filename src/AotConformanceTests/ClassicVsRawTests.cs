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
    /// Compares a <c>ClassicEmit</c> model against its raw-writer twin <b>directly</b>, over the
    /// same contracts in the same build — gap B18.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Marc's observation: nothing precludes two type models over the same domain, one classic and
    /// one not, so the cross-check needs no second build and no second process.
    /// </para>
    /// <para>
    /// This is <b>sharper than comparing each against <c>RuntimeTypeModel</c> separately</b>, which
    /// is what <c>DifferentialTests</c> does for the twins. Two models that diverged from ref-emit
    /// in the same way would pass that and fail this; and this states the property we actually
    /// promise — <i>classic emit is functionally equivalent, if slower</i> — rather than inferring
    /// it from two other equivalences.
    /// </para>
    /// <para>
    /// The twins are proven to be a real second code path rather than a silently-ignored flag:
    /// <c>GroupedElementsModel</c> emits 3 <c>RawWrite_</c> and 3 <c>Measure_</c> bodies, its
    /// classic twin emits none of either.
    /// </para>
    /// </remarks>
    public class ClassicVsRawTests
    {
        private const string ProtoModelAttribute = "ProtoBuf.ProtoModelAttribute";
        private static readonly Assembly Fixtures = typeof(ClassicVsRawTests).Assembly;

        /// <summary>Every `<c>XClassicModel</c>` paired with the `<c>XModel</c>` it shadows.</summary>
        public static IEnumerable<object[]> Pairs()
        {
            foreach (var classic in Fixtures.GetTypes()
                .Where(t => t.Name.EndsWith("ClassicModel", StringComparison.Ordinal))
                .Where(t => t.GetCustomAttributes().Any(a => a.GetType().FullName == ProtoModelAttribute))
                .OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                var rawName = classic.FullName!.Replace("ClassicModel", "Model");
                var raw = Fixtures.GetType(rawName);
                if (raw is not null) yield return [classic.FullName!, rawName];
            }
        }

        [Theory, MemberData(nameof(Pairs))]
        public void ClassicEmitAgreesWithTheRawWriter(string classicName, string rawName)
        {
            var classic = Instantiate(classicName);
            var raw = Instantiate(rawName);

            var samples = DifferentialTests.SamplesFor(Fixtures.GetType(rawName)!);
            Assert.NotEmpty(samples);

            foreach (var sample in samples)
            {
                var fromClassic = Serialize(classic, sample);
                var fromRaw = Serialize(raw, sample);
                Assert.Equal(Hex(fromRaw), Hex(fromClassic));

                // ...and each must read what the other wrote, which is what catches a shape that
                // is self-consistently wrong in both directions
                var viaClassic = classic.Deserialize(sample.GetType(), new MemoryStream(fromRaw));
                var viaRaw = raw.Deserialize(sample.GetType(), new MemoryStream(fromClassic));
                Assert.Equal(Hex(Serialize(raw, viaClassic)), Hex(Serialize(raw, viaRaw)));
            }
        }

        private static TypeModel Instantiate(string typeName)
            => Assert.IsAssignableFrom<TypeModel>(
                Activator.CreateInstance(Fixtures.GetType(typeName)!, nonPublic: true));

        private static byte[] Serialize(TypeModel model, object value)
        {
            using var ms = new MemoryStream();
            model.Serialize(ms, value);
            return ms.ToArray();
        }

        private static string Hex(byte[] value) => BitConverter.ToString(value);
    }
}
