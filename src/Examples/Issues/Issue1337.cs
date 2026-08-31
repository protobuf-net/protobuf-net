using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    /// <summary>
    /// https://github.com/protobuf-net/protobuf-net/issues/1337 - a dictionary whose value is a
    /// collection is legal (RepeatedSerializerStub.TestIfNestedNotSupported exempts maps), and the
    /// element being a *message* rather than a scalar changes nothing about how it goes on the wire.
    /// The AOT generator dropped every such contract; these shapes are here as much to put them in
    /// the AotDifferential corpus - which carried only scalar elements - as to pin the runtime.
    /// </summary>
    public class Issue1337
    {
        [ProtoContract]
        public class Leg
        {
            [ProtoMember(1)]
            public double Rate { get; set; }

            [ProtoMember(2)]
            public string Label { get; set; }
        }

        [ProtoContract]
        public class MyMapping
        {
            // the shape as reported, and its value-tuple spelling: an auto-tuple is a message here
            [ProtoMember(1)]
            public Dictionary<double, List<Tuple<double, string>>> TheMapping { get; set; } = new Dictionary<double, List<Tuple<double, string>>>();

            [ProtoMember(2)]
            public Dictionary<double, List<(double, string)>> ValueTuples { get; set; } = new Dictionary<double, List<(double, string)>>();

            // ... and an ordinary contract element, which is the same case
            [ProtoMember(3)]
            public Dictionary<double, List<Leg>> Messages { get; set; } = new Dictionary<double, List<Leg>>();

            // a message reached only through a nested *map* value
            [ProtoMember(4)]
            public Dictionary<double, Dictionary<int, Leg>> Mapped { get; set; } = new Dictionary<double, Dictionary<int, Leg>>();
        }

        [Fact]
        public void NestedCollectionsOfMessagesRoundTrip()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            DoIt(model);

            model.CompileInPlace();
            DoIt(model);

            static void DoIt(TypeModel model)
            {
                var obj = new MyMapping
                {
                    TheMapping = { [1.5] = new List<Tuple<double, string>> { Tuple.Create(2.5, "a"), Tuple.Create(3.5, "b") } },
                    ValueTuples = { [4.5] = new List<(double, string)> { (5.5, "c") } },
                    Messages = { [6.5] = new List<Leg> { new Leg { Rate = 7.5, Label = "d" }, new Leg { Rate = 8.5, Label = "e" } } },
                    Mapped = { [9.5] = new Dictionary<int, Leg> { [10] = new Leg { Rate = 11.5, Label = "f" } } },
                };

                var clone = model.DeepClone(obj);
                Assert.NotSame(obj, clone);

                Assert.Equal(
                    obj.TheMapping[1.5].Select(x => $"{x.Item1}/{x.Item2}"),
                    clone.TheMapping[1.5].Select(x => $"{x.Item1}/{x.Item2}"));
                Assert.Equal(obj.ValueTuples[4.5], clone.ValueTuples[4.5]);
                Assert.Equal(
                    obj.Messages[6.5].Select(x => $"{x.Rate}/{x.Label}"),
                    clone.Messages[6.5].Select(x => $"{x.Rate}/{x.Label}"));
                Assert.Equal(11.5, clone.Mapped[9.5][10].Rate);
                Assert.Equal("f", clone.Mapped[9.5][10].Label);
            }
        }

        /// <summary>
        /// The <em>fully compiled</em> path does not manage this, and that is worth pinning rather
        /// than leaving as a surprise: the emitted code hands over
        /// <c>this as ISerializer&lt;List&lt;Tuple&lt;double, string&gt;&gt;&gt;</c>, which is null
        /// because the services type implements <c>ISerializer&lt;KeyValuePair&lt;..&gt;&gt;</c>,
        /// and resolution then falls back to a model with no entry for it.
        /// </summary>
        /// <remarks>
        /// Note Issue54's <c>Dictionary&lt;float, List&lt;int&gt;&gt;</c> survives the same route,
        /// because a <c>List&lt;int&gt;</c> serializer can be built with no model entry at all -
        /// so this is specifically about a *message* element. The reflection path above handles it,
        /// and so does the AOT generator, which resolves the collection through an
        /// <c>ISerializerProxy&lt;List&lt;T&gt;&gt;</c> on its services type.
        /// </remarks>
        [Fact]
        public void FullyCompiledModelCannotServeAMessageElementUnderAMap()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(MyMapping), true);

            var compiled = model.Compile();
            var obj = new MyMapping { Messages = { [6.5] = new List<Leg> { new Leg { Rate = 7.5 } } } };

            var ex = Assert.Throws<InvalidOperationException>(() => compiled.DeepClone(obj));
            Assert.StartsWith("No serializer for type ", ex.Message);
        }
    }
}
