using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Threading;

namespace Examples.Issues
{
    [TestFixture]
    public class SO14532116
    {
        [Test]
        public void Execute()
        {
            
            var model = TypeModel.Create();
            model.AutoCompile = false;
            model.SetDefaultFactory(typeof(SO14532116).GetMethod("ObjectMaker"));

            int oldCount = Count;
            Test(model, "Runtime");
            model.CompileInPlace();
            Test(model, "CompileInPlace");
            Test(model.Compile(), "CompileInPlace");
            model.Compile("SO14532116", "SO14532116.dll");
            PEVerify.AssertValid("SO14532116.dll");

            int newCount = Count;
            Assert.AreEqual(oldCount + 3, newCount);
        }

        [ProtoContract]
        public class Foo
        {
            [ProtoMember(1)]
            public int X {get;set;}

            public static Foo Create(int x = 0)
            {
                return new Foo(x);
            }

            public Foo(int x) { X = x; }
        }


        private void Test(TypeModel model, string p)
        {
            var obj = Foo.Create(123);

            int oldCount = Count;
            var clone = (Foo)model.DeepClone(obj);
            int newCount = Count;
            Assert.AreEqual(oldCount + 1, newCount);
            Assert.AreEqual(123, clone.X);
        }

        private static int count;
        public static int Count
        {
            get { return Interlocked.CompareExchange(ref count, 0, 0); } 
        }
        public static object ObjectMaker(Type type)
        {
            object obj;
            if(type == typeof(Foo))
            {
                obj = Foo.Create();
            } else {
                obj = Activator.CreateInstance(type);
            }
            Interlocked.Increment(ref count);
            return obj;
            
        }
    }
}
