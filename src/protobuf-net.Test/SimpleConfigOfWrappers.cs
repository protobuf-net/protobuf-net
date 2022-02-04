using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Xunit;

namespace ProtoBuf.Test
{
    public class SimpleConfigOfWrappers
    {

        [Fact]
        public void ExistingListsBehaviour()
        {
            using var ms = new MemoryStream(new byte[] { 0x08, 0x00, 0x08, 0x01, 0x08, 0x02 });
            var clone = Serializer.Deserialize<Foo>(ms);
            if (!ms.TryGetBuffer(out var buffer)) buffer = new ArraySegment<byte>(ms.ToArray());
            Assert.Equal("08-00-08-01-08-02", BitConverter.ToString(buffer.Array, buffer.Offset, buffer.Count));
        }

        [DataContract]
        public class Foo
        {
            [DataMember(Order = 1)]
            public List<int?> Items { get; } = new List<int?>();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanApplySimpleConfiguration(bool configured)
        {
            Assert.Equal(0, Internal.TypeHelper<int?>.Default);
            Assert.Equal(0, Internal.TypeHelper<int>.Default);
            Assert.Equal(0L, Internal.TypeHelper<long?>.Default);
            Assert.Equal(0L, Internal.TypeHelper<long>.Default);
            //var model = RuntimeTypeModel.Create();
            //if (configured)
            //{
            //    model.AfterApplyDefaultBehaviour += (sender, e) =>
            //    {
            //        foreach (var field in e.MetaType.GetFields())
            //        {
            //            if (Nullable.GetUnderlyingType(field.MemberType) is object)
            //            {
            //                field.Wrap = WrapOptions.ValueWrapped;
            //            }
            //        }
            //    };
            //}
            //var mt = model[typeof(SomeTypeWithNullables)];
            //Assert.Equal(WrapOptions.None, mt[1].Wrap);
            //Assert.Equal(configured ? WrapOptions.ValueWrapped : WrapOptions.None, mt[2].Wrap);
            //Assert.Equal(WrapOptions.None, mt[3].Wrap);
            //Assert.Equal(configured ? WrapOptions.ValueWrapped : WrapOptions.None, mt[4].Wrap);
            //Assert.Equal(WrapOptions.None, mt[5].Wrap);
        }

        [DataContract]
        public class SomeTypeWithNullables
        {
            [DataMember(Order = 1)]
            public int Int32 { get; set; }

            [DataMember(Order = 2)]
            public int? WrappedInt32 { get; set; }

            [DataMember(Order = 3)]
            public float Single { get; set; }

            [DataMember(Order = 4)]
            public float? WrappedSingle { get; set; }

            [DataMember(Order = 5)]
            public string String { get; set; }
        }
    }
}
