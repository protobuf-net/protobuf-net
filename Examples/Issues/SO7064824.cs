using System.IO;
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
            
            // this would be nice...
            //Expect(cloned.ObjectArray.GetType(), Is.EqualTo(typeof(Base[])));

            // but this is what we currently **expect**
            Expect(cloned.ObjectArray.GetType(), Is.EqualTo(typeof(object[])));
        }

        [Test]
        public void WrittenDataShouldBeConstant()
        {
            var container = new ObjectArrayContainerClass();
            container.ObjectArray = this.CreateArray();
            var ms = new MemoryStream();
            var model = CreateModel();
            model.DynamicTypeFormatting += new TypeFormatEventHandler(model_DynamicTypeFormatting);
            model.Serialize(ms, container);

            string s = Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
            // written with r480
            Assert.AreEqual("ChkgAUIEQmFzZVIPCg1CYXNlQ2xhc3NUZXh0CjEgAkIHRGVyaXZlZFIkogYSChBEZXJpdmVkQ2xhc3NUZXh0Cg1CYXNlQ2xhc3NUZXh0", s);
        }
        void model_DynamicTypeFormatting(object sender, TypeFormatEventArgs args)
        {

            if (args.Type != null)
            {
                if (args.Type == typeof(Derived)) { args.FormattedName = "Derived"; return; }
                if (args.Type == typeof(Base)) { args.FormattedName = "Base"; return; }
                throw new NotSupportedException(args.Type.Name);
            }
            else
            {
                switch (args.FormattedName)
                {
                    case "Derived": args.Type = typeof(Derived); break;
                    case "Base": args.Type = typeof(Base); break;
                    default: throw new NotSupportedException(args.FormattedName);
                }
            }
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

            // this would be nice...
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