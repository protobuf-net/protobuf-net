using AotFixtures.TrivialGetter;
using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.AotConformance
{
    /// <summary>
    /// Getter-only members backed by a field the generator could name exactly.
    /// </summary>
    /// <remarks>
    /// These have no differential coverage on purpose: <see cref="RuntimeTypeModel"/> throws on
    /// them rather than producing bytes to compare against, so this is the one place the generator
    /// is strictly more capable than ref-emit rather than merely different. That makes a direct
    /// round-trip the only available proof — and worth having, since <c>[UnsafeAccessor]</c>
    /// writing through a <c>ref</c> to a private (and, for <c>_numbers</c>, <c>readonly</c>) field
    /// is exactly the kind of thing that compiles and then fails at runtime.
    /// </remarks>
    public class TrivialGetterTests
    {
        private static readonly TypeModel Model = TrivialGetterModel.Instance;

        [Fact]
        public void ExpressionBodiedAndBlockGettersRoundTrip()
        {
            var value = RoundTrip(new Backed(42, "hi"));

            Assert.Equal(42, value.Value);
            Assert.Equal("hi", value.Text);
        }

        [Fact]
        public void DefaultsAreNotWritten()
        {
            using var ms = new MemoryStream();
            Model.Serialize(ms, new Backed());
            Assert.Empty(ms.ToArray());
        }

        [Fact]
        public void GetterOnlyCollectionIsPopulated()
        {
            var source = new Backed();
            source.Numbers.Add(1);
            source.Numbers.Add(2);

            Assert.Equal([1, 2], RoundTrip(source).Numbers);
        }

        [Fact]
        public void NonTrivialGetterIsReadAndDiscarded()
        {
            // `Doubled => _value * 2` names no field we can trust, so it writes but never comes back
            var source = new Computed();
            using var ms = new MemoryStream();
            Model.Serialize(ms, source);

            Assert.Equal(0, RoundTrip(source).Doubled);
        }

        [Fact]
        public void RuntimeModelCannotDoThisAtAll()
        {
            // pins the reason there is no differential case: this is not a discard, it is a throw
            var runtime = RuntimeTypeModel.Create();
            runtime.Add(typeof(Backed), applyDefaultBehaviour: true);

            using var ms = new MemoryStream();
            Assert.Throws<InvalidOperationException>(() => runtime.Serialize(ms, new Backed(42, "hi")));
        }

        private static T RoundTrip<T>(T value)
        {
            using var ms = new MemoryStream();
            Model.Serialize(ms, value);
            ms.Position = 0;
            return (T)Model.Deserialize(ms, null, typeof(T));
        }
    }
}
