using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Examples.Issues
{
    
    public class Issue312
    {
        [ProtoContract]
        public class RootClass
        {
            [ProtoMember(1)]
            public int Prop1 { get; set; }
            [ProtoMember(2)]
            public string Prop2 { get; set; }
            [ProtoMember(3)]
            public IEnumerableClass Prop3 { get; set; }
        }

        [ProtoContract(IgnoreListHandling = true)]
        public class IEnumerableClass : ICollection<char>
        {
            [ProtoMember(1)]
            public int Prop1 { get; set; }
            [ProtoMember(2)]
            public string Prop2 { get; set; }

            int ICollection<char>.Count => throw new NotImplementedException();

            bool ICollection<char>.IsReadOnly => throw new NotImplementedException();

            public IEnumerator<char> GetEnumerator()
            {
                throw new NotImplementedException(nameof(IEnumerableClass) + "." + nameof(GetEnumerator));
            }

            // Comment out this indexed property to prevent the crash
            public char this[int i] { get { return Prop2[i]; } }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException(nameof(IEnumerableClass) + "." + nameof(GetEnumerator));
            }

            void ICollection<char>.Add(char item)
            {
                throw new NotImplementedException();
            }

            void ICollection<char>.Clear()
            {
                throw new NotImplementedException();
            }

            bool ICollection<char>.Contains(char item)
            {
                throw new NotImplementedException();
            }

            void ICollection<char>.CopyTo(char[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            bool ICollection<char>.Remove(char item)
            {
                throw new NotImplementedException();
            }

            IEnumerator<char> IEnumerable<char>.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void ExecuteIEnumerableClass()
        {
            var rt = RuntimeTypeModel.Create();
            rt.Add(typeof(IEnumerableClass), true);
            rt.Add(typeof(RootClass), true);
            rt.Compile();

            rt.Compile("ExecuteIEnumerableClass", "ExecuteIEnumerableClass.dll");
            PEVerify.AssertValid("ExecuteIEnumerableClass.dll");

            var c1 = new IEnumerableClass() { Prop1 = 1, Prop2 = "a" };
            var i1 = new RootClass() { Prop1 = 1, Prop2 = "blabla", Prop3 = c1 };
            var cloned = rt.DeepClone(i1) as RootClass;
            Assert.Equal(1, cloned.Prop3.Prop1);
        }
    }
}
