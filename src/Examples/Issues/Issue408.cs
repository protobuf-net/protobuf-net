using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Issues
{
    public class Issue408
    {
        public struct Point
        {
            public double X { get; set; }

            public double Y { get; set; }
        }

        [Fact]
        public void CanRoundtripValueTypeSubTypes()
        {
            var model = TypeModel.Create();

            var objectMetaType = model.Add(typeof(object), false);

            var metaType = model.Add(typeof(Point), false);
            metaType.AddField(2, "X");
            metaType.AddField(3, "Y");

            objectMetaType.AddSubType(1, typeof(Point));

            var obj = new Point {X = 1, Y = 2};
            var clone = model.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.IsType<Point>(clone);

            var point = (Point) clone;
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public void CanRoundtripValueTypeSubTypesCompiled()
        {
            // https://github.com/mgravell/protobuf-net/issues/408

            var model = TypeModel.Create();

            var objectMetaType = model.Add(typeof(object), false);

            var metaType = model.Add(typeof(Point), false);
            metaType.AddField(2, "X");
            metaType.AddField(3, "Y");

            objectMetaType.AddSubType(1, typeof(Point));

            // This bug only occured in compiled type models
            var compiledTypeModel = model.Compile();

            var obj = new Point {X = 1, Y = 2};
            var clone = compiledTypeModel.DeepClone(obj);
            Assert.NotNull(clone);
            Assert.IsType<Point>(clone);

            var point = (Point) clone;
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }
    }
}
