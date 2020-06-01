using System.Diagnostics;
using System.IO;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using Xunit.Abstractions;

namespace Examples.Issues
{
    public class SO7078615
    {
        [ProtoContract] // treat the interface as a contract
        // since protobuf-net *by default* doesn't know about type metadata, need to use some clue
        [ProtoInclude(1, typeof(DogBarkedEvent))]
        // other concrete messages here; note these can also be defined at runtime - nothing *needs*
        // to use attributes
        public interface IMessage { }
        public interface IEvent : IMessage { }

        [ProtoContract] // removed (InferTagFromName = true) - since you are already qualifying your tags
        public class DogBarkedEvent : IEvent
        {
            [ProtoMember(1)] // .proto tags are 1-based; blame google ;p
            public string NameOfDog { get; set; }
            [ProtoMember(2)]
            public int Times { get; set; }
        }

        [ProtoContract]
        class DontAskWrapper
        {
            [ProtoMember(1)]
            public IMessage Message { get; set; }
        }

        [Fact]
        public void RoundTripAnUnknownMessage()
        {
            IMessage msg = new DogBarkedEvent
            {
                  NameOfDog = "Woofy", Times = 5
            }, copy;
            var model = RuntimeTypeModel.Create(); // could also use the default model, but
            using(var ms = new MemoryStream()) // separation makes life easy for my tests
            {
                var tmp = new DontAskWrapper {Message = msg};
                model.Serialize(ms, tmp);
                ms.Position = 0;
                string hex = Program.GetByteString(ms.ToArray());
                Debug.WriteLine(hex);

#pragma warning disable CS0618
                var wrapper = (DontAskWrapper)model.Deserialize(ms, null, typeof(DontAskWrapper));
#pragma warning restore CS0618
                copy = wrapper.Message;
             }
            // check the data is all there
            Assert.IsType<DogBarkedEvent>(copy);
            var typed = (DogBarkedEvent)copy;
            var orig = (DogBarkedEvent)msg;
            Assert.Equal(orig.Times, typed.Times);
            Assert.Equal(orig.NameOfDog, typed.NameOfDog);
        }
    }


    
    public class SO7078615_NoAttribs
    {
        private ITestOutputHelper Log { get; }
        public SO7078615_NoAttribs(ITestOutputHelper _log) => Log = _log;

        public interface IMessage { }
        public interface IEvent : IMessage { }
        public class DogBarkedEvent : IEvent
        {
            public string NameOfDog { get; set; }
            public int Times { get; set; }
        }
        class DontAskWrapper
        {
            public IMessage Message { get; set; }
        }

        [Fact]
        public void RoundTripAnUnknownMessage()
        {
            IMessage msg = new DogBarkedEvent
            {
                NameOfDog = "Woofy",
                Times = 5
            }, copy;
            var model = RuntimeTypeModel.Create(); // could also use the default model, but
            model.Add(typeof (DogBarkedEvent), false).Add("NameOfDog", "Times");
            model.Add(typeof (IMessage), false).AddSubType(1, typeof (DogBarkedEvent));
            model.Add(typeof (DontAskWrapper), false).Add("Message");

            using (var ms = new MemoryStream()) // separation makes life easy for my tests
            {
                var tmp = new DontAskWrapper { Message = msg };
                model.Serialize(ms, tmp);
                ms.Position = 0;
                string hex = Program.GetByteString(ms.ToArray());
                Debug.WriteLine(hex);

#pragma warning disable CS0618
                var wrapper = (DontAskWrapper)model.Deserialize(ms, null, typeof(DontAskWrapper));
#pragma warning restore CS0618
                copy = wrapper.Message;
            }
            // check the data is all there
            Assert.IsType<DogBarkedEvent>(copy);
            var typed = (DogBarkedEvent)copy;
            var orig = (DogBarkedEvent)msg;
            Assert.Equal(orig.Times, typed.Times);
            Assert.Equal(orig.NameOfDog, typed.NameOfDog);
        }

#pragma warning disable xUnit1004 // Test methods should not be skipped
        [Fact(Skip = "Long running")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
        public void TestPerf()
        {
            int[] values = new int[100000000];
            var watch = Stopwatch.StartNew();
            {
                int length = values.Length;
                for (int i = 0; i < length; i++)
                    values[i] = i;
            }
            watch.Stop();
            var hoisted = watch.ElapsedMilliseconds;
            Log.WriteLine(hoisted.ToString());
            watch = Stopwatch.StartNew();
            {
                for (int i = 0; i < values.Length; i++)
                    values[i] = i;
            }
            watch.Stop();
            var direct = watch.ElapsedMilliseconds;
            Log.WriteLine(direct.ToString());
            Assert.True(2 < 3);
            Assert.True(hoisted < direct);
        }
    }
}
