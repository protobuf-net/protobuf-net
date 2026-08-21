using System;
using System.Buffers;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.AotConformance
{
    /// <summary>
    /// A contract that can be measured by arithmetic MAY carry a before-serialization callback,
    /// provided both passes fire it - see gap B42, which replaced the previous exclusion.
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

        /// <summary>
        /// A measurable contract MAY carry a before-serialization callback, and when it does the
        /// callback must fire in BOTH passes - measure and write - so the two observe the same
        /// object and the length prefix matches the bytes.
        /// </summary>
        /// <remarks>
        /// This test used to assert the opposite: that no measurable contract carried such a
        /// callback at all. The refusal was load-bearing only while <c>Measure_</c> had no
        /// <c>ISerializationContext</c> and so could not fire one - firing in the write alone would
        /// let the object change between the passes. Threading a context removed that, and firing
        /// in both is exactly what the classic buffer-writer backend has always done
        /// (<c>IsMeasuring</c> true, then false). See <c>notes/gaps.md</c> B42.
        /// <para>
        /// Asserted on observed behaviour rather than on emitted shape: the fixture records each
        /// invocation, so a callback that stopped firing in either pass shows up here as a count.
        /// </para>
        /// </remarks>
        [Fact]
        public void MeasurableContractFiresBeforeSerializationInBothPasses()
        {
            var modelType = Fixtures.GetType("AotFixtures.Callbacks.CallbacksModel")!;
            var model = Assert.IsAssignableFrom<TypeModel>(
                Activator.CreateInstance(modelType, nonPublic: true));
            var hooked = Fixtures.GetType("AotFixtures.Callbacks.Hooked")!;
            Assert.Contains(hooked, DiscoverMeasurableContracts()); // else this proves nothing

            // NESTED, deliberately. As a root, nothing above it needs a length, so no measure pass
            // runs and the hook fires exactly once - correct, but silent about the two-pass case.
            var holderType = Fixtures.GetType("AotFixtures.Callbacks.Holder")!;
            var holder = Activator.CreateInstance(holderType)!;
            var value = Activator.CreateInstance(hooked)!;
            holderType.GetProperty("Inner")!.SetValue(holder, value);
            var trace = hooked.GetProperty("Trace")!;
            trace.SetValue(value, "");

            // a buffer-writer target, which is the backend that measures first
            var writer = new ArrayBufferWriter<byte>();
            model.Serialize(writer, holder);

            // "bs;" once per pass - measure, then write - which is the documented normal for a
            // buffer-writer target. Anything else means a pass stopped firing it; the after-
            // serialization hook is stripped so this asserts the BEFORE count alone.
            Assert.Equal("bs;bs;", ((string)trace.GetValue(value)!).Replace("as;", ""));
        }

        /// <summary>
        /// ...and the two passes must be TELLABLE APART, which is what makes firing twice
        /// acceptable rather than merely tolerated.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A callback that cannot tell which pass it is in sees its side-effects silently doubled,
        /// which would be a worse bargain than the old behaviour of refusing to measure such a
        /// contract at all. <c>ProtoWriter.IsMeasuring</c> is the answer, and it needs the callback
        /// to declare an <c>ISerializationContext</c> - the only flavour carrying the context
        /// OBJECT rather than a copy of its data.
        /// </para>
        /// <para>
        /// The classic buffer-writer backend answers by BEING a counting writer; the generated
        /// measure has no writer at all, so it wraps the real context. Both must give the same
        /// <c>[true, false]</c> that <c>CallbackMeasurePassTests</c> pins for the classic path -
        /// otherwise moving a contract onto the generator changes what a consumer's callback sees.
        /// </para>
        /// </remarks>
        [Fact]
        public void TheMeasurePassIdentifiesItselfToCallbacks()
        {
            var modelType = Fixtures.GetType("AotFixtures.Callbacks.CallbacksModel")!;
            var model = Assert.IsAssignableFrom<TypeModel>(
                Activator.CreateInstance(modelType, nonPublic: true));
            var watched = Fixtures.GetType("AotFixtures.Callbacks.Watched")!;
            Assert.Contains(watched, DiscoverMeasurableContracts()); // else this proves nothing

            var holderType = Fixtures.GetType("AotFixtures.Callbacks.WatchedHolder")!;
            var holder = Activator.CreateInstance(holderType)!;
            var value = Activator.CreateInstance(watched)!;
            holderType.GetProperty("Inner")!.SetValue(holder, value);
            var trace = watched.GetProperty("Trace")!;
            trace.SetValue(value, "");

            var writer = new ArrayBufferWriter<byte>();
            model.Serialize(writer, holder);

            // the fixture writes "bs*;" when IsMeasuring answers true. Measuring pass FIRST, real
            // write second - the same order, and the same answers, as the classic backend gives.
            Assert.Equal("bs*;as;bs;as;", (string)trace.GetValue(value)!);
        }

        /// <summary>
        /// The corpus must actually contain measurable contracts, or the theory above is vacuous.
        /// </summary>
        [Fact]
        public void TheCorpusContainsMeasurableContracts()
            => Assert.NotEmpty(MeasurableContracts());

        /// <summary>
        /// The corpus must contain callback-bearing contracts, and they must now be MEASURABLE -
        /// the inverse of what this asserted before B42. Without it the test above could pass
        /// vacuously, on a corpus where nothing declares a callback at all.
        /// </summary>
        [Fact]
        public void CallbackBearingContractsExistAndAreMeasurable()
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
            var excluded = withCallbacks.Where(t => !measurable.Contains(t)).Select(static t => t.Name).ToList();
            Assert.True(excluded.Count == 0,
                $"callback-bearing but NOT measurable: {string.Join(", ", excluded)} - a "
                + "before-serialization callback no longer costs a contract its arithmetic measure "
                + "(gap B42), so an exclusion here means something else is blocking it.");
        }
    }
}
