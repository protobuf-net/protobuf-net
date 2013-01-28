using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Examples.Issues
{
    [TestFixture]
    public class SO14436606
    {
        [Serializable]
        [ProtoContract]
        public class A
        {
        }

        [Serializable]
        [ProtoContract]
        public class B
        {
            [ProtoMember(1, AsReference = true)]
            public A A { get; set; }

            [ProtoMember(2, AsReference = true)]
            public Dictionary<int, A> Items { get; set; }

            public B()
            {
                Items = new Dictionary<int, A>();
            }
        }
        [ProtoContract(AsReferenceDefault=true)]
        public class A_WithDefaultRef
        {
        }

        [ProtoContract]
        public class B_WithDefaultRef
        {
            [ProtoMember(1)]
            public A_WithDefaultRef A { get; set; }

            [ProtoMember(2)]
            public Dictionary<int, A_WithDefaultRef> Items { get; set; }

            public B_WithDefaultRef()
            {
                Items = new Dictionary<int, A_WithDefaultRef>();
            }
        }

        [ProtoContract]
        public struct RefPair<TKey,TValue> {
            [ProtoMember(1)]
            public TKey Key {get; private set;}
            [ProtoMember(2, AsReference = true)]
            public TValue Value {get; private set;}
            public RefPair(TKey key, TValue value) : this() {
                Key = key;
                Value = value;
            }
            public static implicit operator KeyValuePair<TKey,TValue> (RefPair<TKey,TValue> val) {
                return new KeyValuePair<TKey,TValue>(val.Key, val.Value);
            }
            public static implicit operator RefPair<TKey,TValue> (KeyValuePair<TKey,TValue> val) {
                return new RefPair<TKey,TValue>(val.Key, val.Value);
            }
        }

        [Test]
        public void VerifyModelViaDefaultRef()
        {
            var model = CreateDefaultRefModel(true);
            Assert.IsTrue(model[typeof(A_WithDefaultRef)].AsReferenceDefault, "A:AsReferenceDefault - A first");
            Assert.IsTrue(model[typeof(B_WithDefaultRef)][1].AsReference, "B:AsReference  - A first");

            model = CreateDefaultRefModel(true);
            Assert.IsTrue(model[typeof(A_WithDefaultRef)].AsReferenceDefault, "A:AsReferenceDefault - B first");
            Assert.IsTrue(model[typeof(B_WithDefaultRef)][1].AsReference, "B:AsReference  - B first");
        }

        [Test]
        public void ThreeApproachesAreCompatible()
        {
            string surrogate, fields, defaultRef_AFirst, defaultRef_BFirst;
            using (var ms = new MemoryStream())
            {
                CreateDefaultRefModel(true).Serialize(ms, CreateB_WithDefaultRef());
                defaultRef_AFirst = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            }
            using (var ms = new MemoryStream())
            {
                CreateDefaultRefModel(false).Serialize(ms, CreateB_WithDefaultRef());
                defaultRef_BFirst = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            }
            using (var ms = new MemoryStream())
            {
                CreateSurrogateModel().Serialize(ms, CreateB());
                surrogate = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            }

            using (var ms = new MemoryStream())
            {
                CreateFieldsModel().Serialize(ms, CreateB());
                fields = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            }

            Assert.AreEqual(surrogate, fields, "fields vs surrogate");
            Assert.AreEqual(surrogate, defaultRef_AFirst, "default-ref (A-first) vs surrogate");
            Assert.AreEqual(surrogate, defaultRef_BFirst, "default-ref (B-first) vs surrogate");
        }

        [Test]
        public void ExecuteHackedViaDefaultRef()
        {
            ExecuteAllModes_WithDefaultRef(CreateDefaultRefModel(true), "ExecuteHackedViaDefaultRef - A first");
            ExecuteAllModes_WithDefaultRef(CreateDefaultRefModel(false), "ExecuteHackedViaDefaultRef - B first");
        }

        [Test]
        public void ExecuteHackedViaFields()
        {
            ExecuteAllModes(CreateFieldsModel(), standalone: false);
        }

        static RuntimeTypeModel CreateDefaultRefModel(bool aFirst)
        {
            var model = TypeModel.Create();
            if (aFirst)
            {
                model.Add(typeof(A_WithDefaultRef), true);
                model.Add(typeof(B_WithDefaultRef), true);
            }
            else
            {
                model.Add(typeof(B_WithDefaultRef), true);
                model.Add(typeof(A_WithDefaultRef), true);
            }
            model.AutoCompile = false;

            return model;
        }
        static RuntimeTypeModel CreateFieldsModel()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            var type = model.Add(typeof(KeyValuePair<int, A>), false);
            type.Add(1, "key");
            type.AddField(2, "value").AsReference = true;

            model[typeof(B)][2].AsReference = false; // or just remove AsReference on Items
            return model;
        }
        static RuntimeTypeModel CreateSurrogateModel()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            model[typeof(B)][2].AsReference = false; // or just remove AsReference on Items

            // this is the evil bit:
            model[typeof(KeyValuePair<int, A>)].SetSurrogate(typeof(RefPair<int, A>));
            return model;
        }

        [Test]
        public void ExecuteHackedViaSurrogate()
        {
            ExecuteAllModes(CreateSurrogateModel());
        }

        void ExecuteAllModes_WithDefaultRef(RuntimeTypeModel model, [CallerMemberName] string caller = null, bool standalone = false)
        {
            Execute_WithDefaultRef(model, "Runtime");
            Execute_WithDefaultRef(model, "CompileInPlace");
            if (standalone)
            {
                Execute_WithDefaultRef(model.Compile(), "Compile");
                model.Compile(caller, caller + ".dll");
                PEVerify.AssertValid(caller + ".dll");
            }
        }
        void ExecuteAllModes(RuntimeTypeModel model, [CallerMemberName] string caller = null, bool standalone = false)
        {
            Execute(model, "Runtime");
            Execute(model, "CompileInPlace");
            if (standalone)
            {
                Execute(model.Compile(), "Compile");
                model.Compile(caller, caller + ".dll");
                PEVerify.AssertValid(caller + ".dll");
            }
        }

        [Serializable]
        [ProtoContract]
        public class C
        {
            [ProtoMember(2, AsReference = true)]
            public List<Tuple<int, A>> Items { get; set; }

            public C()
            {
                Items = new List<Tuple<int, A>>();
            }
        }

        [Test]
        public void TuplesAsReference()
        {
            var obj = new C();
            var t = Tuple.Create(1, new A {});
            obj.Items.Add(t);
            obj.Items.Add(t);
            var clone = Serializer.DeepClone(obj);
            Assert.AreSame(clone.Items[0], clone.Items[1]);
        }

        [Test, ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "AsReference cannot be used with value-types; please see http://stackoverflow.com/q/14436606/")]
        public void AreObjectReferencesSameAfterDeserialization()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            ExecuteAllModes(model);
        }

        static B CreateB()
        {
            A a = new A();
            B b = new B();

            b.A = a;

            b.Items.Add(1, a);
            return b;
        }
        private void Execute(TypeModel model, string caption)
        {
            var b = CreateB();

            Assert.AreSame(b.A, b.Items[1], caption + ":Original");

            B deserializedB = (B)model.DeepClone(b);

            Assert.AreSame(deserializedB.A, deserializedB.Items[1], caption + ":Clone");
        }
        static B_WithDefaultRef CreateB_WithDefaultRef()
        {
            A_WithDefaultRef a = new A_WithDefaultRef();
            B_WithDefaultRef b = new B_WithDefaultRef();

            b.A = a;

            b.Items.Add(1, a);
            return b;
        }
        private void Execute_WithDefaultRef(TypeModel model, string caption)
        {
            var b = CreateB_WithDefaultRef();

            Assert.AreSame(b.A, b.Items[1], caption + ":Original");

            B_WithDefaultRef deserializedB = (B_WithDefaultRef)model.DeepClone(b);

            Assert.AreSame(deserializedB.A, deserializedB.Items[1], caption + ":Clone");
        }
    }
}
