using NUnit.Framework.SyntaxHelpers;
using System;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace TechnologyEvaluation.Protobuf.ArrayOfBaseClassTest
{
    
    [ProtoContract]
    class BaseClassArrayContainerClass
    {
        [ProtoMember(1, DynamicType = true)]
        public Base[] BaseArray { get; set; }
    }

    [ProtoContract]
    class ObjectArrayContainerClass
    {
        [ProtoMember(1, DynamicType = true)]
        public object[] ObjectArray { get; set; }

    }
    [ProtoContract]
    class Base
    {
        [ProtoMember(1)]
        public string BaseClassText { get; set; }
    }

    [ProtoContract]
    class Derived : Base
    {
        [ProtoMember(1)]
        public string DerivedClassText { get; set; }
    }

    [TestFixture]
    public class ArrayOfBaseClassTests : AssertionHelper
    {
        [Test] // needs dynamic handling of list itself
        public void TestObjectArrayContainerClass()
        {
            var model = CreateModel();
            var container = new ObjectArrayContainerClass();
            container.ObjectArray = this.CreateArray();
            var cloned = (ObjectArrayContainerClass)model.DeepClone(container);
            Expect(cloned.ObjectArray, Is.Not.Null);

            foreach (var obj in cloned.ObjectArray)
            {
                Expect(obj as Base, Is.Not.Null);
            }

            Expect(cloned.ObjectArray[1] as Derived, Is.Not.Null);
            Expect(cloned.ObjectArray.GetType(), Is.EqualTo(typeof(Base[])));

        }

        [Test]// needs dynamic handling of list itself
        public void TestBaseClassArrayContainerClass()
        {
            var model = CreateModel();
            var container = new BaseClassArrayContainerClass();
            container.BaseArray = this.CreateArray();
            var cloned = (BaseClassArrayContainerClass)model.DeepClone(container);
            Expect(cloned.BaseArray, Is.Not.Null);

            foreach (var obj in cloned.BaseArray)
            {
                Expect(obj as Base, Is.Not.Null);
            }
            Expect(cloned.BaseArray[1] as Derived, Is.Not.Null);
            Expect(cloned.BaseArray.GetType(), Is.EqualTo(typeof(Base[])), "array type");
        }

        RuntimeTypeModel CreateModel()
        {
            RuntimeTypeModel model = TypeModel.Create();

            model.Add(typeof(ObjectArrayContainerClass), true);
            model.Add(typeof(BaseClassArrayContainerClass), true);
            model.Add(typeof(Base), true);
            model[typeof(Base)].AddSubType(100, typeof(Derived));

            return model;
        }

        Base[] CreateArray()
        {
            return new Base[] { new Base() { BaseClassText = "BaseClassText" }, new Derived() { BaseClassText = "BaseClassText", DerivedClassText = "DerivedClassText" } };
        }
    }



}