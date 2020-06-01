using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Examples.Issues
{
    
    public class SO13783894
    {
        [Fact]
        public void ConfigureBasicEnum()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(MyEnum), true);

            var obj1 = new Test<MyEnum> { Value = MyEnum.Default };
            var obj2 = new Test<MyEnum> { Value = MyEnum.Foo };
            using var ms = new MemoryStream();
            model.Serialize(ms, obj1);
            ms.Position = 0;
#pragma warning disable CS0618
            var clone1 = (Test<int>)model.Deserialize(ms, null, typeof(Test<int>));
#pragma warning restore CS0618
            ms.SetLength(0);
            model.Serialize(ms, obj2);
            ms.Position = 0;
#pragma warning disable CS0618
            var clone2 = (Test<int>)model.Deserialize(ms, null, typeof(Test<int>));
#pragma warning restore CS0618

            Assert.Equal(2, clone1.Value);
            Assert.Equal(3, clone2.Value);
        }
        [Fact]
        public void ConfigureExplicitEnumValuesAtRuntime()
        {
            var model = RuntimeTypeModel.Create();
            var mt = model.Add(typeof(MyEnum), false);
            mt.SetEnumValues(new[]
            {// these **do not** change the serialized values
                new EnumMember(1, "Default"),
                new EnumMember((MyEnum)10, "Foo"),
            });

            var obj1 = new Test<MyEnum> { Value = MyEnum.Default };
            var obj2 = new Test<MyEnum> { Value = MyEnum.Foo };
            using var ms = new MemoryStream();
            model.Serialize(ms, obj1);
            ms.Position = 0;
#pragma warning disable CS0618
            var clone1 = (Test<int>)model.Deserialize(ms, null, typeof(Test<int>));
#pragma warning restore CS0618
            ms.SetLength(0);
            model.Serialize(ms, obj2);
            ms.Position = 0;
#pragma warning disable CS0618
            var clone2 = (Test<int>)model.Deserialize(ms, null, typeof(Test<int>));
#pragma warning restore CS0618

            Assert.Equal(2, clone1.Value);
            Assert.Equal(3, clone2.Value);
        }
        //[ProtoContract]
        enum MyEnum
        {
            //[ProtoEnum(Value = 1)]
            Default = 2,
            //[ProtoEnum(Value = 10)]
            Foo = 3
        }
        [ProtoContract]
        public class Test<T>
        {
            [ProtoMember(1, IsRequired=true)]
            public T Value { get; set; }
        }
    }
}
