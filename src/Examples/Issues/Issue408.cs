using System;
using System.IO;
using Examples;
using ProtoBuf;
using ProtoBuf.Meta;
using Xunit;

namespace Examples.Issues
{
    public class Issue408
    {
        public struct Point
        {
            public double X { get; set; }

            public double Y { get; set; }
        }

        [ProtoContract]
        public class Container
        {
            [ProtoMember(1)]
            public object Value;
        }

        [Fact]
        public void ShouldSerializeValueTypeSubTypes()
        {
            var model = TypeModel.Create();

            ConfigureTypeModel(model);
            model.Compile("ShouldSerializeValueTypeSubTypes", "ShouldSerializeValueTypeSubTypes.dll");
            PEVerify.AssertValid("ShouldSerializeValueTypeSubTypes.dll");
            TestValueType(model);
            // This bug only occured in compiled type models
            TestValueType(model.Compile());
        }

        [Fact]
        public void ShouldSerializeBoxedValueType()
        {
            var model = TypeModel.Create();

            ConfigureTypeModel(model);
            model.Add(typeof(Container), true);

            TestBoxedValueType(model);
            TestValueType(model.Compile());
        }

        private static void ConfigureTypeModel(RuntimeTypeModel model)
        {
            var objectMetaType = model.Add(typeof(object), false);

            var metaType = model.Add(typeof(Point), false);
            metaType.AddField(2, "X");
            metaType.AddField(3, "Y");

            objectMetaType.AddSubType(1, typeof(Point));
        }

        private static void TestBoxedValueType(TypeModel model)
        {
            var obj = new Container
            {
                Value = new Point {X = 42, Y = 9001}
            };

            var clone = model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.IsType<Container>(clone);

            var container = (Container) clone;
            Assert.NotNull(container.Value);
            Assert.IsType<Point>(container.Value);

            var point = (Point) container.Value;
            Assert.Equal(42, point.X);
            Assert.Equal(9001, point.Y);
        }

        private static void TestValueType(TypeModel model)
        {
            TestPoint(model);
            TestNull(model);
        }

        private static void TestPoint(TypeModel model)
        {
            var obj = new Point {X = 1, Y = 2};
            var clone = model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.IsType<Point>(clone);

            var point = (Point) clone;
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        private static void TestNull(TypeModel model)
        {
            using (var stream = new MemoryStream())
            {
                Assert.Throws<ArgumentNullException>(() => model.Serialize(stream, null));
            }
        }
    }
}
