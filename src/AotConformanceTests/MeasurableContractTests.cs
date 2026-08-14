using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Xunit;

namespace ProtoBuf.AotConformance
{
    /// <summary>
    /// A contract that can be measured by arithmetic must not carry a before-serialization
    /// callback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a CORRECTNESS property, not a policy one - which is worth stating, because
    /// callbacks otherwise have no business influencing whether a message is measured or
    /// back-filled (that is chosen on its own merits). A generated <c>Measure_</c> static is pure
    /// arithmetic with no serializer body, so a before-callback cannot run during the measure but
    /// does run during the write: the length prefix would be computed from pre-callback state and
    /// the body written from post-callback state. Wrong bytes.
    /// </para>
    /// <para>
    /// The generator excludes such contracts (the measure-eligibility rules), and this asserts
    /// that the exclusion actually holds over the whole fixture corpus rather than being trusted.
    /// It keys off the <c>Measure_</c> statics' first parameter, so it does not depend on how
    /// type names are sanitised into method names.
    /// </para>
    /// </remarks>
    public class MeasurableContractTests
    {
        private static readonly Assembly Fixtures = typeof(MeasurableContractTests).Assembly;

        /// <summary>Every contract type for which a Measure_ static was emitted, anywhere in the
        /// fixture assembly's generated models.</summary>
        public static TheoryData<Type> MeasurableContracts()
        {
            var data = new TheoryData<Type>();
            foreach (var type in DiscoverMeasurableContracts()) data.Add(type);
            return data;
        }

        private static System.Collections.Generic.HashSet<Type> DiscoverMeasurableContracts()
        {
            var seen = new System.Collections.Generic.HashSet<Type>();
            foreach (var type in Fixtures.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (!method.Name.StartsWith("Measure_", StringComparison.Ordinal)) continue;
                    var parameters = method.GetParameters();
                    if (parameters.Length == 0) continue;
                    seen.Add(parameters[0].ParameterType);
                }
            }
            return seen;
        }

        [Theory, MemberData(nameof(MeasurableContracts))]
        public void MeasurableContractHasNoBeforeSerializationCallback(Type contract)
        {
            var offenders = (
                from method in contract.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                where method.GetCustomAttributes().Any(static a =>
                    a is ProtoBeforeSerializationAttribute || a is OnSerializingAttribute)
                select method.Name).ToList();

            Assert.True(offenders.Count == 0,
                $"{contract.Name} has a Measure_ static AND before-serialization callback(s) "
                + $"[{string.Join(", ", offenders)}]: the measure cannot run the callback, so the "
                + "length prefix would be computed before it and the body written after it.");
        }

        /// <summary>
        /// The corpus must actually contain measurable contracts, or the theory above is vacuous.
        /// </summary>
        [Fact]
        public void TheCorpusContainsMeasurableContracts()
            => Assert.NotEmpty(MeasurableContracts());

        /// <summary>
        /// ...and it must contain callback-bearing contracts too, or the theory is vacuous in a
        /// subtler way: it would pass simply because nothing anywhere declares a callback, rather
        /// than because the exclusion works. This asserts the exclusion is doing real work - the
        /// callback contracts exist, and none of them is measurable.
        /// </summary>
        [Fact]
        public void CallbackBearingContractsExistAndAreExcluded()
        {
            var withCallbacks = (
                from type in Fixtures.GetTypes()
                where type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                        | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Any(static m => m.GetCustomAttributes().Any(static a =>
                        a is ProtoBeforeSerializationAttribute || a is OnSerializingAttribute))
                select type).ToList();

            Assert.NotEmpty(withCallbacks); // the corpus really does exercise callbacks

            var measurable = DiscoverMeasurableContracts();
            var both = withCallbacks.Where(measurable.Contains).Select(static t => t.Name).ToList();
            Assert.True(both.Count == 0, $"measurable AND callback-bearing: {string.Join(", ", both)}");
        }
    }
}
