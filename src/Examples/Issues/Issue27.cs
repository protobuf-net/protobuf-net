using System.Runtime.Serialization;
using Xunit;
using ProtoBuf;
using System.IO;

namespace Examples.Issues
{
    /// <summary>
    /// Relates to wanting to serialize structs; issue here is that:
    /// a: structs are generally immutable, and protobuf-net wants to mutate
    /// b: would require lots of "ref" additions
    /// 
    /// This is a workaround, simply intended to show an alternative construction, i.e.
    /// wrapping the structs with a class during serialization.
    /// </summary>
    
    public class Issue27
    {
        [Fact]
        public void Roundtrip()
        {
            KeyPair<int, string> pair = new KeyPair<int, string>(1, "abc");

            KeyPair<int,string> clone = Serializer.DeepClone<KeyPairProxy<int,string>>(pair);
            Assert.Equal(pair.Key1, clone.Key1);
            Assert.Equal(pair.Key2, clone.Key2);
        }

        [Fact]
        public void TestWrapped()
        {
            Foo foo = new Foo { Pair = new KeyPair<int, string>(1, "abc") };
            Assert.Equal(1, foo.Pair.Key1); //, "Key1 - orig");
            Assert.Equal("abc", foo.Pair.Key2); //, "Key2 - orig");

            var clone = Serializer.DeepClone(foo);
            Assert.Equal(1, clone.Pair.Key1); //, "Key1 - clone");
            Assert.Equal("abc", clone.Pair.Key2); //, "Key2 - clone");
        }
    }
    [DataContract]
    class Foo
    {
        public KeyPair<int, string> Pair { get; set; }

        [DataMember(Name="Pair", Order = 1)]
        private KeyPairProxy<int, string> PairProxy {
            get { return Pair; }
            set { Pair = value; }
        }

    }

    [DataContract]
    sealed class KeyPairProxy<TKey1, TKey2>
    {
        [DataMember(Order = 1)]
        public TKey1 Key1 { get; set; }
        [DataMember(Order = 2)]
        public TKey2 Key2 { get; set; }

        public static implicit operator KeyPair<TKey1, TKey2> (KeyPairProxy<TKey1, TKey2> pair)
        {
            return new KeyPair<TKey1, TKey2>(pair.Key1, pair.Key2);
        }
        public static implicit operator KeyPairProxy<TKey1, TKey2>(KeyPair<TKey1, TKey2> pair)
        {
            return new KeyPairProxy<TKey1, TKey2> { Key1 = pair.Key1, Key2 = pair.Key2 };
        }
    }
   [DataContract(Namespace = "foo")]
   public struct KeyPair<TKey1, TKey2>
   {
       public KeyPair(TKey1 k1, TKey2 k2)
           : this() {
           Key1 = k1;
           Key2 = k2;
       }
       // Stupid tuple class for datacontract
       [DataMember(Order = 1)]
       public TKey1 Key1 { get;  internal set; }
       [DataMember(Order = 2)]
       public TKey2 Key2 { get;  internal set; }

       public override string ToString() {
           return Key1.ToString() + ", " + Key2.ToString();
       }
   }

}

