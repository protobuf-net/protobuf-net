//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using NUnit.Framework;
//using ProtoBuf;
//using ProtoBuf.Meta;

//namespace Examples.Issues
//{
//    [TestFixture]
//    public class SO3101816
//    {
//        [ProtoContract]
//        public class A
//        {
//            [ProtoMember(1)]
//            public IB B { get; set; }
//        }

//        public interface IB
//        {
//        }

//        [ProtoContract]
//        public class B : IB
//        {
//            [ProtoMember(1)]
//            public int SomeProperty { get; set; }
//        }


//        [Test]
//        public void Test()
//        {
//            var a = new A { B = new B() };
//            var model = TypeModel.Create();
//            model.Add(typeof(B), )
//            using (var m = new MemoryStream())
//            {
//                Serializer.NonGeneric.Serialize(, a);
//            }
//        }
//    }
//}
