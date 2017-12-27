using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace ProtoBuf.Issues
{
    public class CustomListSurrogate
    {
        [Fact]
        public void CustomCollectionSurrogateWorks()
        {
            var model = RuntimeTypeModel.Create();

            var modelType = model.Add(typeof(List<string>), false);
            modelType.SetSurrogate(typeof(EmptyListIsNotNullSurrogate<string>));
            modelType.IgnoreListHandling = true;
            modelType.CompileInPlace();
            
            var input = new TestSerialisedObject { Data = new List<string>() { "TEST", "TEST2" } };

            var clonedObject = model.DeepClone(input);

            Assert.Equal(input, clonedObject);
        }

        [ProtoContract]
        private class TestSerialisedObject
        {
            [ProtoMember(1)]
            public List<string> Data { get; set; }

            public override bool Equals(object obj)
            {
                var @object = obj as TestSerialisedObject;
                return @object != null && Enumerable.SequenceEqual(this.Data, @object.Data);
            }

            public override int GetHashCode()
            {
                return 1;
            }
        }
    }

    [ProtoContract]
    public class EmptyListIsNotNullSurrogate<T>
    {
        public EmptyListIsNotNullSurrogate()
        {
            Array = new T[0];
        }

        [ProtoMember(1)]
        public T[] Array { get; set;} 

        public static implicit operator EmptyListIsNotNullSurrogate<T>(List<T> l)
        {
            if (l == null)
            {
                return new EmptyListIsNotNullSurrogate<T>();
            }

            return new EmptyListIsNotNullSurrogate<T>()
            {
                Array = l.ToArray()
            };
        }

        public static implicit operator List<T>(EmptyListIsNotNullSurrogate<T> l)
        {
            if (l == null)
            {
                return new List<T>();
            }

            return new List<T>(l.Array);
        }
    }
}
