using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System.Collections;
using ProtoBuf.Meta;
using System.Diagnostics;
using System.IO;
using System;

namespace ProtoBuf.unittest.Meta
{
    [TestFixture]
    public class PocoListTests
    {
        public class TypeWithLists
        {
            public List<string> ListString { get; set; }
            public IList<string> IListStringTyped { get; set; }

            public ArrayList ArrayListString { get; set; }
            public IList IListStringUntyped { get; set; }

            public List<int> ListInt32 { get; set; }
            public IList<int> IListInt32Typed { get; set; }

            public ArrayList ArrayListInt32 { get; set; }
            public IList IListInt32Untyped { get; set; }

        }

        static void VerifyPE(string path)
        {
            // note; PEVerify can be found %ProgramFiles%\Microsoft SDKs\Windows\v6.0A\bin
            const string exePath = "PEVerify.exe";
            using (Process proc = Process.Start(exePath, path))
            {
                if (proc.WaitForExit(10000))
                {
                    Assert.AreEqual(0, proc.ExitCode);
                }
                else
                {
                    proc.Kill();
                    throw new TimeoutException();
                }
            }
        }

        [Test]
        public void EmitModelWithEverything()
        {
            var model = TypeModel.Create();
            MetaType meta = model.Add(typeof(TypeWithLists), false);
            meta.Add(1, "ListString");
            meta.Add(2, "ListInt32");
            meta.Add(3, "IListStringTyped");
            meta.Add(4, "IListInt32Typed");

            meta.Add(5, "ArrayListString", typeof(string), null);
            meta.Add(6, "ArrayListInt32", typeof(int), null);
            meta.Add(7, "IListStringUntyped", typeof(string), null);
            meta.Add(8, "IListInt32Untyped", typeof(int), null);

            model.CompileInPlace();
            model.Compile("EmitModelWithEverything", "EmitModelWithEverything.dll");

            VerifyPE("EmitModelWithEverything.dll");
            
        }

        [Test]
        public void AddOnTypedListShouldResolveCorrectly_String()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(1, "ListString");
            Assert.AreEqual(typeof(TypeWithLists), model[typeof(TypeWithLists)][1].ParentType, "ParentType");
            Assert.AreEqual(typeof(string), model[typeof(TypeWithLists)][1].ItemType, "ItemType");
            Assert.AreEqual(typeof(List<string>), model[typeof(TypeWithLists)][1].MemberType, "MemberType");
            Assert.AreEqual(typeof(List<string>), model[typeof(TypeWithLists)][1].DefaultType, "DefaultType");
        }

        [Test]
        public void AddOnTypedListShouldResolveCorrectly_Int32()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(1, "ListInt32");
            Assert.AreEqual(typeof(TypeWithLists), model[typeof(TypeWithLists)][1].ParentType, "ParentType");
            Assert.AreEqual(typeof(int), model[typeof(TypeWithLists)][1].ItemType, "ItemType");
            Assert.AreEqual(typeof(List<int>), model[typeof(TypeWithLists)][1].MemberType, "MemberType");
            Assert.AreEqual(typeof(List<int>), model[typeof(TypeWithLists)][1].DefaultType, "DefaultType");
        }

        [Test]
        public void AddOnTypedIListShouldResolveCorrectly_String()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(2, "IListStringTyped");
            Assert.AreEqual(typeof(TypeWithLists), model[typeof(TypeWithLists)][2].ParentType, "ParentType");
            Assert.AreEqual(typeof(string), model[typeof(TypeWithLists)][2].ItemType, "ItemType");
            Assert.AreEqual(typeof(IList<string>), model[typeof(TypeWithLists)][2].MemberType, "MemberType");
            Assert.AreEqual(typeof(List<string>), model[typeof(TypeWithLists)][2].DefaultType, "DefaultType");
        }

        [Test]
        public void AddOnTypedIListShouldResolveCorrectly_Int32()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(2, "IListInt32Typed");
            Assert.AreEqual(typeof(TypeWithLists), model[typeof(TypeWithLists)][2].ParentType, "ParentType");
            Assert.AreEqual(typeof(int), model[typeof(TypeWithLists)][2].ItemType, "ItemType");
            Assert.AreEqual(typeof(IList<int>), model[typeof(TypeWithLists)][2].MemberType, "MemberType");
            Assert.AreEqual(typeof(List<int>), model[typeof(TypeWithLists)][2].DefaultType, "DefaultType");
        }

        [Test]
        public void RoundTripTypedList_String()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(1, "ListString");
            TypeWithLists obj = new TypeWithLists();
            obj.ListString = new List<string>();
            obj.ListString.Add("abc");
            obj.ListString.Add("def");

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ListString);
            Assert.IsTrue(obj.ListString.SequenceEqual(clone.ListString));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ListString);
            Assert.IsTrue(obj.ListString.SequenceEqual(clone.ListString));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ListString);
            Assert.IsTrue(obj.ListString.SequenceEqual(clone.ListString));
        }

        [Test]
        public void RoundTripTypedIList_String()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(2, "IListStringTyped");
            TypeWithLists obj = new TypeWithLists();
            obj.IListStringTyped = new List<string>();
            obj.IListStringTyped.Add("abc");
            obj.IListStringTyped.Add("def");

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListStringTyped);
            Assert.IsTrue(obj.IListStringTyped.SequenceEqual(clone.IListStringTyped));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListStringTyped);
            Assert.IsTrue(obj.IListStringTyped.SequenceEqual(clone.IListStringTyped));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListStringTyped);
            Assert.IsTrue(obj.IListStringTyped.SequenceEqual(clone.IListStringTyped));
        }


        [Test]
        public void RoundTripArrayList_String()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(3, "ArrayListString", typeof(string), null);
            TypeWithLists obj = new TypeWithLists();
            obj.ArrayListString = new ArrayList();
            obj.ArrayListString.Add("abc");
            obj.ArrayListString.Add("def");

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ArrayListString);
            Assert.IsTrue(obj.ArrayListString.Cast<string>().SequenceEqual(clone.ArrayListString.Cast<string>()));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ArrayListString);
            Assert.IsTrue(obj.ArrayListString.Cast<string>().SequenceEqual(clone.ArrayListString.Cast<string>()));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ArrayListString);
            Assert.IsTrue(obj.ArrayListString.Cast<string>().SequenceEqual(clone.ArrayListString.Cast<string>()));
        }

        [Test]
        public void RoundTripIList_String()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(4, "IListStringUntyped", typeof(string), null);
            TypeWithLists obj = new TypeWithLists();
            obj.IListStringUntyped = new ArrayList();
            obj.IListStringUntyped.Add("abc");
            obj.IListStringUntyped.Add("def");

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListStringUntyped);
            Assert.IsTrue(obj.IListStringUntyped.Cast<string>().SequenceEqual(clone.IListStringUntyped.Cast<string>()));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListStringUntyped);
            Assert.IsTrue(obj.IListStringUntyped.Cast<string>().SequenceEqual(clone.IListStringUntyped.Cast<string>()));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListStringUntyped);
            Assert.IsTrue(obj.IListStringUntyped.Cast<string>().SequenceEqual(clone.IListStringUntyped.Cast<string>()));
        }

        [Test]
        public void RoundTripTypedList_Int32()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(1, "ListInt32");
            TypeWithLists obj = new TypeWithLists();
            obj.ListInt32 = new List<int>();
            obj.ListInt32.Add(123);
            obj.ListInt32.Add(456);

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ListInt32);
            Assert.IsTrue(obj.ListInt32.SequenceEqual(clone.ListInt32));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ListInt32);
            Assert.IsTrue(obj.ListInt32.SequenceEqual(clone.ListInt32));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ListInt32);
            Assert.IsTrue(obj.ListInt32.SequenceEqual(clone.ListInt32));
        }

        [Test]
        public void RoundTripTypedIList_Int32()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(2, "IListInt32Typed");
            TypeWithLists obj = new TypeWithLists();
            obj.IListInt32Typed = new List<int>();
            obj.IListInt32Typed.Add(123);
            obj.IListInt32Typed.Add(456);

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListInt32Typed);
            Assert.IsTrue(obj.IListInt32Typed.SequenceEqual(clone.IListInt32Typed));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListInt32Typed);
            Assert.IsTrue(obj.IListInt32Typed.SequenceEqual(clone.IListInt32Typed));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListInt32Typed);
            Assert.IsTrue(obj.IListInt32Typed.SequenceEqual(clone.IListInt32Typed));
        }


        [Test]
        public void RoundTripArrayList_Int32()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(3, "ArrayListInt32", typeof(int), null);
            TypeWithLists obj = new TypeWithLists();
            obj.ArrayListInt32 = new ArrayList();
            obj.ArrayListInt32.Add(123);
            obj.ArrayListInt32.Add(456);

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ArrayListInt32);
            Assert.IsTrue(obj.ArrayListInt32.Cast<int>().SequenceEqual(clone.ArrayListInt32.Cast<int>()));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ArrayListInt32);
            Assert.IsTrue(obj.ArrayListInt32.Cast<int>().SequenceEqual(clone.ArrayListInt32.Cast<int>()));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.ArrayListInt32);
            Assert.IsTrue(obj.ArrayListInt32.Cast<int>().SequenceEqual(clone.ArrayListInt32.Cast<int>()));
        }

        [Test]
        public void RoundTripIList_Int32()
        {
            var model = TypeModel.Create();
            model.Add(typeof(TypeWithLists), false).Add(4, "IListInt32Untyped", typeof(int), null);
            TypeWithLists obj = new TypeWithLists();
            obj.IListInt32Untyped = new ArrayList();
            obj.IListInt32Untyped.Add(123);
            obj.IListInt32Untyped.Add(456);

            TypeWithLists clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListInt32Untyped);
            Assert.IsTrue(obj.IListInt32Untyped.Cast<int>().SequenceEqual(clone.IListInt32Untyped.Cast<int>()));

            model.CompileInPlace();
            clone = (TypeWithLists)model.DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListInt32Untyped);
            Assert.IsTrue(obj.IListInt32Untyped.Cast<int>().SequenceEqual(clone.IListInt32Untyped.Cast<int>()));

            clone = (TypeWithLists)model.Compile().DeepClone(obj);
            Assert.IsNotNull(clone);
            Assert.IsNotNull(clone.IListInt32Untyped);
            Assert.IsTrue(obj.IListInt32Untyped.Cast<int>().SequenceEqual(clone.IListInt32Untyped.Cast<int>()));
        }
    }
}
