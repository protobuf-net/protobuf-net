using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Examples.Issues
{

    public class AssortedGoLiveRegressions
    {
        [Fact]
        public void TestStringFromEmpty()
        {
            using (var ms = new MemoryStream())
            {
                Assert.NotNull(Serializer.Deserialize<Foo>(ms)); //, "Foo");
                Assert.Equal("", Serializer.Deserialize<string>(ms)); //, "string");
                Assert.Null(Serializer.Deserialize<DateTime?>(ms)); //, "DateTime?");
                Assert.Null(Serializer.Deserialize<int?>(ms)); //, "int?");

                Assert.Equal(default(DateTime), Serializer.Deserialize<DateTime>(ms)); //, "DateTime");
                Assert.Equal(0, Serializer.Deserialize<int>(ms)); //, "int");
            }
        }

        [Fact]
        public void LegacyListTimeSpan()
        {
            var list = new List<TimeSpan>
        {
            new TimeSpan(2010, 2, 1),
            new TimeSpan(9, 3, 10, 30, 32),
        };
            var ms = new MemoryStream();
            Serializer.Serialize(ms, list);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal("0A-07-08-B2-A8-F3-06-10-03-0A-08-08-A0-92-BD-F0-05-10-04", hex);
            ms.Position = 0;
            var clone = Serializer.Deserialize<List<TimeSpan>>(ms);
            Assert.Equal(2, clone.Count);
            Assert.Equal(list[0], clone[0]);
            Assert.Equal(list[1], clone[1]);
        }

        [Fact]
        public void LegacyListDateTime()
        {
            var list = new List<DateTime>
            {
                new DateTime(2010, 2, 1),
                new DateTime(2019, 9, 3, 10, 30, 32),
            };
            var ms = new MemoryStream();
            Serializer.Serialize(ms, list);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            ms.Position = 0;
            Assert.Equal("0A-04-08-E2-E4-01-0A-08-08-90-83-F2-D6-0B-10-03", hex);
            var clone = Serializer.Deserialize<List<DateTime>>(ms);
            Assert.Equal(2, clone.Count);
            Assert.Equal(list[0], clone[0]);
            Assert.Equal(list[1], clone[1]);
        }


        [Fact]
        public void LegacyTimeSpanBehaviors()
        {
            var model = RuntimeTypeModel.Create();
            model.IncludeDateTimeKind = true;
            Verify(model, TimeSpan.MinValue, "0A-04-08-01-10-0F");
            Verify(model, default(TimeSpan), "0A-00", "");
            Verify(model, TimeSpan.MaxValue, "0A-04-08-02-10-0F");

            Verify(model, new TimeSpan(32, 10, 25, 32, 123), "0A-08-08-B6-C7-C1-F0-14-10-04");

            model = RuntimeTypeModel.Create();
            model.IncludeDateTimeKind = false;
            Verify(model, TimeSpan.MinValue, "0A-04-08-01-10-0F");
            Verify(model, default(TimeSpan), "0A-00", "");
            Verify(model, TimeSpan.MaxValue, "0A-04-08-02-10-0F");
            Verify(model, new TimeSpan(32, 10, 25, 32, 123), "0A-08-08-B6-C7-C1-F0-14-10-04");


            static void Verify(TypeModel model, TimeSpan value, string expected, string expectedHaz = null)
            {
                expectedHaz ??= expected;
                var ms = new MemoryStream();
                string Hex(bool reset = true)
                {
                    var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
                    if (reset)
                    {
                        ms.Position = 0;
                        ms.SetLength(0);
                    }
                    return hex;
                }

                model.Serialize<TimeSpan>(ms, value);
                Assert.Equal(expected, Hex());

                model.Serialize<TimeSpan?>(ms, value);
                Assert.Equal(expected, Hex());

                model.Serialize(ms, (object)value);
                Assert.Equal(expected, Hex(reset: false));

                ms.Position = 0;
                Assert.Equal(value, model.Deserialize<TimeSpan>(ms));

                ms.Position = 0;
                Assert.Equal(value, model.Deserialize<TimeSpan?>(ms));

                ms.Position = 0;
                Assert.Equal(value, model.Deserialize(ms, null, typeof(TimeSpan)));

                ms.Position = 0;
                Assert.Equal(value, model.Deserialize(ms, null, typeof(TimeSpan?)));

                ms.Position = 0;
                Assert.Equal(value, model.Deserialize<HazTime>(ms).Val);

                ms.Position = 0;
                Assert.Equal(value, ((HazTime)model.Deserialize(ms, null, typeof(HazTime))).Val);

                Hex(); // reset
                var haz = new HazTime { Val = value };
                model.Serialize<HazTime>(ms, haz);
                Assert.Equal(expectedHaz, Hex());

                model.Serialize(ms, (object)haz);
                Assert.Equal(expectedHaz, Hex());
            }
        }

        [Fact]
        public void LegacyDateTimeBehaviors()
        {
            var model = RuntimeTypeModel.Create();
            model.IncludeDateTimeKind = false;
            Verify(model, DateTime.MinValue, "0A-04-08-01-10-0F");
            Verify(model, default(DateTime), "0A-04-08-01-10-0F");
            Verify(model, DateTime.MaxValue, "0A-04-08-02-10-0F");

            Verify(model, new DateTime(2019, 9, 27, 10, 25, 32, 123, DateTimeKind.Utc), "0A-09-08-B6-E7-88-A4-AE-5B-10-04");
            Verify(model, new DateTime(2019, 9, 27, 10, 25, 32, 123, DateTimeKind.Local), "0A-09-08-B6-E7-88-A4-AE-5B-10-04");
            Verify(model, new DateTime(2019, 9, 27, 10, 25, 32, 123, DateTimeKind.Unspecified), "0A-09-08-B6-E7-88-A4-AE-5B-10-04");

            model = RuntimeTypeModel.Create();
            model.IncludeDateTimeKind = true;
            Verify(model, DateTime.MinValue, "0A-04-08-01-10-0F");
            Verify(model, default(DateTime), "0A-04-08-01-10-0F");
            Verify(model, DateTime.MaxValue, "0A-04-08-02-10-0F");

            Verify(model, new DateTime(2019, 9, 27, 10, 25, 32, 123, DateTimeKind.Utc), "0A-0B-08-B6-E7-88-A4-AE-5B-10-04-18-01");
            Verify(model, new DateTime(2019, 9, 27, 10, 25, 32, 123, DateTimeKind.Local), "0A-0B-08-B6-E7-88-A4-AE-5B-10-04-18-02");
            Verify(model, new DateTime(2019, 9, 27, 10, 25, 32, 123, DateTimeKind.Unspecified), "0A-09-08-B6-E7-88-A4-AE-5B-10-04");

            static void Verify(TypeModel model, DateTime value, string expected)
            {
                var ms = new MemoryStream();
                string Hex(bool reset = true)
                {
                    var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
                    if (reset)
                    {
                        ms.Position = 0;
                        ms.SetLength(0);
                    }
                    return hex;
                }

                model.Serialize<DateTime>(ms, value);
                Assert.Equal(expected, Hex());

                model.Serialize<DateTime?>(ms, value);
                Assert.Equal(expected, Hex());

                model.Serialize(ms, (object)value);
                Assert.Equal(expected, Hex());

                var haz = new HazDate { Val = value };
                model.Serialize<HazDate>(ms, haz);
                Assert.Equal(expected, Hex());

                model.Serialize(ms, (object)haz);
                Assert.Equal(expected, Hex(reset: false));

                ms.Position = 0;
                Assert.Equal(value, model.Deserialize<DateTime>(ms));

                ms.Position = 0;
                Assert.Equal(value, model.Deserialize<DateTime?>(ms));

                ms.Position = 0;
                Assert.Equal(value, model.Deserialize(ms, null, typeof(DateTime)));

                ms.Position = 0;
                Assert.Equal(value, model.Deserialize(ms, null, typeof(DateTime?)));
            }
        }

        [ProtoContract]
        public class HazDate

        {
            [ProtoMember(1)]
            public DateTime Val { get; set; }
        }

        [ProtoContract]
        public class HazTime

        {
            [ProtoMember(1)]
            public TimeSpan Val { get; set; }
        }

        [Fact]
        public void TestStringArray()
        {
            var orig = new[] { "abc", "def" };
            Assert.True(Serializer.DeepClone(orig).SequenceEqual(orig));
        }

        [Fact]
        public void TestInt32Array()
        {
            var orig = new[] { 1, 2 };
            Assert.True(Serializer.DeepClone(orig).SequenceEqual(orig));
        }

        [Fact]
        public void TestByteArray()
        {
            // byte[] is a special case that compares most closely to 1:data
            // (rather than 1:item0 1:item1 1:item2 etc)
            var orig = new byte[] { 0, 1, 2, 4, 5 };
            var clone = Serializer.ChangeType<byte[], HasBytes>(orig).Blob;
            Assert.True(orig.SequenceEqual(clone));
        }

        [ProtoContract]
        public class HasBytes
        {
            [ProtoMember(1)]
            public byte[] Blob { get; set; }
        }

        [Fact]
        public void TestStringDictionary()
        {
            var orig = new Dictionary<string,string> { {"abc","def" }};
            var clone = Serializer.DeepClone(orig).Single();
            _ = RuntimeTypeModel.Default.GetTypes().Cast<MetaType>().ToArray();
            Assert.Equal(orig.Single().Key, clone.Key);
            Assert.Equal(orig.Single().Value, clone.Value);
        }

        [Fact]
        public void TestFooList()
        {
            var orig = new List<Foo> { new Foo() { Count = 12, Name = "abc" } };

            var clone = Serializer.DeepClone(orig).Single();
            Assert.Equal(orig.Single().Count, clone.Count);
            Assert.Equal(orig.Single().Name, clone.Name);
        }



        [Fact]
        public void TestEmptyStringDictionary()
        {
            var orig = new Dictionary<string, string> { };
            Assert.Empty(orig);

            var clone = Serializer.DeepClone(orig);
            Assert.NotNull(clone);
            Assert.Empty(clone);
        }

        [ProtoContract]
        class Foo
        {
            [ProtoMember(1)]
            public string Name { get; set; }
            [ProtoMember(2)]
            public int Count { get; set; }
        }
    }
}
