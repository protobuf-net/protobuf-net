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
            var model = TypeModel.Create();
            model.Add(typeof(MyEnum), true);

            var obj1 = new Test<MyEnum> { Value = MyEnum.Default };
            var obj2 = new Test<MyEnum> { Value = MyEnum.Foo };
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, obj1);
                ms.Position = 0;
                var clone1 = (Test<int>)model.Deserialize(ms, null, typeof(Test<int>));
                ms.SetLength(0);
                model.Serialize(ms, obj2);
                ms.Position = 0;
                var clone2 = (Test<int>)model.Deserialize(ms, null, typeof(Test<int>));

                Assert.Equal(2, clone1.Value);
                Assert.Equal(3, clone2.Value);
            }
        }
        [Fact]
        public void ConfigureExplicitEnumValuesAtRuntime()
        {
            var model = TypeModel.Create();
            model.Add(typeof(MyEnum), false).Add(1, "Default").Add(10, "Foo");

            var obj1 = new Test<MyEnum> { Value = MyEnum.Default };
            var obj2 = new Test<MyEnum> { Value = MyEnum.Foo };
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, obj1);
                ms.Position = 0;
                var clone1 = (Test<int>)model.Deserialize(ms, null, typeof(Test<int>));
                ms.SetLength(0);
                model.Serialize(ms, obj2);
                ms.Position = 0;
                var clone2 = (Test<int>)model.Deserialize(ms, null, typeof(Test<int>));

                Assert.Equal(1, clone1.Value);
                Assert.Equal(10, clone2.Value);
            }
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
