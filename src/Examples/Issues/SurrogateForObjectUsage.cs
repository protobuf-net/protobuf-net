using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class SurrogateForObjectUsage
    {
        public class Param
        {

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("FloatData", typeof(FloatData))]
            //[System.Xml.Serialization.XmlElementAttribute("StringData", typeof(StringData))]
            //[System.Xml.Serialization.XmlElementAttribute("IntData", typeof(IntData))]
            //[System.Xml.Serialization.XmlElementAttribute("Int64Data", typeof(Int64Data))]
            //[System.Xml.Serialization.XmlElementAttribute("CompositeData", typeof(CompositeData))]
            public object Item;
        }

        public class FloatData
        {

            /// <remarks/>
            [System.Xml.Serialization.XmlArrayItemAttribute("item", IsNullable = false)]
            public float[] Ranges;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public System.Single AdjustValue;


            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public System.Single[] Values;
        }
        [ProtoContract]
        public class ParamSurrogate
        {
            [ProtoMember(1)]
            public FloatData FloatData { get; set; }
            // TODO: other types here

            public static implicit operator ParamSurrogate(Param value)
            {
                if (value == null) return null;
                var surrogate = new ParamSurrogate();
                if(value.Item != null)
                {
                    surrogate.FloatData = value.Item as FloatData; // will be null if not this
                    // TODO: other types here
                }
                return surrogate;
            }
            public static implicit  operator Param(ParamSurrogate value)
            {
                if (value == null) return null;
                var param = new Param();
                if (value.FloatData != null) param.Item = value.FloatData;
                // TODO: other types here
                return param;
            }
        }

        [Fact]
        public void CreateSuggorateModel()
        {
            // configure model (do once at app startup)
            var model = RuntimeTypeModel.Default;
            model.Add(typeof(Param), false).SetSurrogate(typeof(ParamSurrogate));
            model.Add(typeof (FloatData), false).Add("Ranges", "AdjustValue", "Values");
            //TODO: other types here

            // test data
            var param = new Param
            {
                Item = new FloatData
                {
                    AdjustValue = 123.45F,
                    Ranges = new float[] { 1.0F, 2.4F },
                    Values = new float[] { 7.21F, 19.2F }
                }
            };
            // note the fallowing is the same as Serializer.DeepClone, since
            // model === RuntimeTypeModel.Default
            var clone = (Param) model.DeepClone(param);
            Assert.NotSame(clone, param); //, "Different instance");
            Assert.IsType<FloatData>(clone.Item); //, "Data type");
            var data = (FloatData) clone.Item;
            Assert.Equal(123.45F, data.AdjustValue);
            Assert.Equal(2, data.Ranges.Length);
            Assert.Equal(1.0F, data.Ranges[0]);
            Assert.Equal(2.4F, data.Ranges[1]);
            Assert.Equal(2, data.Values.Length);
            Assert.Equal(7.21F, data.Values[0]);
            Assert.Equal(19.2F, data.Values[1]);

        }


    }
}
