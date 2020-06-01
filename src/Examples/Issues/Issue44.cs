using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Examples.Issues
{
    
    public class Issue44
    {
        [Fact]
        public void DateTimeKind_NotSerializedByDefault()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            Assert.False(model.IncludeDateTimeKind);
        }

        [Fact]
        public void  DateTimeKind_SerializedElectively_Enabled()
        {
            DateTimeKind_SerializedElectively(true, "DateTimeKind_SerializedElectively_Enabled");
        }
        [Fact]
        public void DateTimeKind_SerializedElectively_Disabled()
        {
            DateTimeKind_SerializedElectively(false, "DateTimeKind_SerializedElectively_Disabled");
        }

        private static void DateTimeKind_SerializedElectively(bool includeDateTimeKind, string name)
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.IncludeDateTimeKind = includeDateTimeKind;
            var original = HazTime.Create();

            var clone = (HazTime)model.DeepClone(original);
            CompareDates(original, clone, includeDateTimeKind, "runtime");

            model.CompileInPlace();
            clone = (HazTime)model.DeepClone(original);
            CompareDates(original, clone, includeDateTimeKind, "CompileInPlace");

            TypeModel compiled = model.Compile();
            Assert.Equal(includeDateTimeKind, SerializeDateTimeKind(compiled));

            clone = (HazTime)compiled.DeepClone(original);
            CompareDates(original, clone, includeDateTimeKind, "Compile");

            compiled = model.Compile(name, name + ".dll");
            Assert.Equal(includeDateTimeKind, SerializeDateTimeKind(compiled));
            clone = (HazTime)compiled.DeepClone(original);
            CompareDates(original, clone, includeDateTimeKind, "Compile-dll");
            PEVerify.AssertValid(name + ".dll");
        }
        static bool SerializeDateTimeKind(TypeModel model)
            => (model.Options & TypeModel.TypeModelOptions.IncludeDateTimeKind) != 0;

        static void CompareDates(HazTime expected, HazTime actual, bool withKind, string model)
        {
            CompareDates(expected.Utc, actual.Utc, withKind ? DateTimeKind.Utc : DateTimeKind.Unspecified, model + ":Utc");
            CompareDates(expected.Local, actual.Local, withKind ? DateTimeKind.Local : DateTimeKind.Unspecified, model + ":Local");
            CompareDates(expected.Unspecified, actual.Unspecified, DateTimeKind.Unspecified, model + ":Unspecified");
        }

        static void CompareDates(DateTime expected, DateTime actual, DateTimeKind expectedKind, string caption)
        {
            Assert.Equal(expectedKind, actual.Kind); //, caption);
            Assert.Equal(expected.Date, actual.Date); //, caption);
            Assert.Equal(expected.TimeOfDay, actual.TimeOfDay); //, caption);
        }

        [ProtoContract]
        public class HazTime
        {
            public static HazTime  Create()
            {
                var now = DateTime.UtcNow;
                return new HazTime
                {
                    Utc = new DateTime(now.Ticks, DateTimeKind.Utc),
                    Local = new DateTime(now.Ticks, DateTimeKind.Local),
                    Unspecified = new DateTime(now.Ticks, DateTimeKind.Unspecified),
                };
            }
            [ProtoMember(1)]
            public DateTime Utc { get; set; }

            [ProtoMember(2)]
            public DateTime Local { get; set; }

            [ProtoMember(3)]
            public DateTime Unspecified { get; set; }
        }
    }
}
