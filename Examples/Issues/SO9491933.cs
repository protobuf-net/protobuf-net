using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProtoBuf;

namespace Examples.Issues
{
    [TestFixture]
    public class SO9491933
    {
        [ProtoContract]
        [ProtoInclude(6, typeof(B))]
        public class A
        {
            [ProtoMember(1)]
            public int Property1 { get; set; }

            [ProtoMember(2)]
            public int? Property2 { get; set; }

            [ProtoMember(3)]
            public int Property3 { get; set; }

            [ProtoMember(4, DynamicType = true)]
            public object Property4 { get; set; }

            [ProtoMember(5, DynamicType = true)]
            public object Property5 { get; set; }

            public override int GetHashCode()
            {
                return Property1.GetHashCode(); // minimal but sufficient
            }
            public override bool Equals(object obj)
            {
                A a = obj as A;
                if (a == null)
                    return false;

                return a.Property1 == this.Property1
                       && a.Property2 == this.Property2
                       && a.Property3 == this.Property3
                       && Object.Equals(a.Property4, this.Property4)
                       && Object.Equals(a.Property5, this.Property5);
            }
        }

        public class B : A
        {
            [ProtoMember(1)]
            public string Property6 { get; set; }

            public override bool Equals(object obj)
            {
                B b = obj as B;
                if (b == null)
                    return false;

                return b.Property6 == this.Property6 && base.Equals(obj);
            }
            public override int GetHashCode()
            {
                return Property6.GetHashCode(); // minimal but sufficient
            }
        }

        [Test]
        public void TestProtoBuf2()
        {
            IList<A> list = new List<A>
            {
                new A {Property1 = 1, Property2 = 1, Property3 = 200, Property4 = "Test1", Property5 = DateTime.Now},
                new B {Property1 = 2, Property2 = 2, Property3 = 400, Property4 = "Test2", Property5 = DateTime.Now, Property6 = "yyyy"},
                new A {Property1 = 3, Property2 = 3, Property3 = 600, Property4 = "Test3", Property5 = new Decimal(200)},
            };
            using (var file = new FileStream("list.bin", FileMode.Create))
            {
                Serializer.Serialize(file, list);
            }

            IList<A> list2;
            using (var file = File.OpenRead("list.bin"))
            {
                list2 = Serializer.Deserialize<IList<A>>(file);
            }

            Assert.AreEqual(list.Count, list2.Count);

            for (int i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(list[i], list2[i]);
            }
        }

    }
}
