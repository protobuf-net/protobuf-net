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
        public void ExecuteHackedViaFields()
        {
            var model = TypeModel.Create();
            var type = model.Add(typeof(KeyValuePair<int, A>), false);
            type.Add(1, "key");
            type.AddField(2, "value").AsReference = true;

            model[typeof(B)][2].AsReference = false; // or just remove AsReference on Items

            ExecuteAllModes(model, standalone: false);
        }
        [Test]
        public void ExecuteHackedViaSurrogate()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            model[typeof(B)][2].AsReference = false; // or just remove AsReference on Items

            // this is the evil bit:
            model[typeof(KeyValuePair<int, A>)].SetSurrogate(typeof(RefPair<int, A>));

            ExecuteAllModes(model);
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

        private void Execute(TypeModel model, string caption)
        {
            A a = new A();
            B b = new B();

            b.A = a;

            b.Items.Add(1, a);

            Assert.AreSame(a, b.A, caption + ":Trivial");
            Assert.AreSame(b.A, b.Items[1], caption + ":Original");

            B deserializedB = (B)model.DeepClone(b);

            Assert.AreSame(deserializedB.A, deserializedB.Items[1], caption + ":Clone");
        }
    }
}
