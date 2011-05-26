using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using System.Windows.Media;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue124
    {
        // note this is a simplified example that 
        [ProtoContract]
        public struct MyColor
        {
            [ProtoMember(1, DataFormat=DataFormat.FixedSize)]
            public uint ARGB { get; set; }

            public static explicit operator MyColor  (Color color)
            {
                return new MyColor { ARGB = ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | (uint)color.B};
            }
            public static explicit operator Color(MyColor color)
            {
                return new Color { A = (byte)(color.ARGB >> 24), R = (byte)(color.ARGB >> 16), G = (byte)(color.ARGB >> 8), B = (byte)color.ARGB };
            }
        }
        [ProtoContract]
        public class TypeWithColor
        {
            [ProtoMember(1)]
            public Color Color { get; set; }
        }

        [Test]
        public void TestMediaColorDirect()
        {
            var model = TypeModel.Create();
            model.Add(typeof(Color), false).Add("A","R","G","B");

            RoundtripTypeWithColor(model);
        }

        [Test]
        public void TestMediaColorSurrogate()
        {
            var model = TypeModel.Create();
            model.Add(typeof(Color), false).SetSurrogate(typeof(MyColor));

            RoundtripTypeWithColor(model);
        }

        private void RoundtripTypeWithColor(RuntimeTypeModel model)
        {
            var orig = new TypeWithColor
            {
                Color = new Color { A = 1, R = 2, G = 3, B = 4 }
            };
            var clone = (TypeWithColor)model.DeepClone(orig);
            Assert.AreEqual(1, clone.Color.A);
            Assert.AreEqual(2, clone.Color.R);
            Assert.AreEqual(3, clone.Color.G);
            Assert.AreEqual(4, clone.Color.B);
        }
    }
}
