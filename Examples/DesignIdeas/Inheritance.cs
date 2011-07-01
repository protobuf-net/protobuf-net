using System.Collections.Generic;
using ProtoBuf;
using NUnit.Framework;
using System;
using System.Reflection;

namespace Examples.DesignIdeas
{
    /// <summary>
    /// would like to be able to support something similar to [XmlInclude]/[KnownType];
    /// not supported by .proto spec, though, so "NetExtensions".
    /// 
    /// List/entity serializers would need to be aware; perhaps with a set of sub-serializers?
    /// No need to support on primative serializers.
    /// 
    /// During single entity deserialization, merge message (to merge) would need to check existing type
    /// against perceived type:
    /// * if same type merge directly
    /// * if new message is for a base-class of the current value then merge directly
    /// * if new message is for a sub-class of the current value, then:
    ///     * serialize the current value into a buffer
    ///     * deserialize the current value into a new instance of the new type
    ///     * merge the stream into the new instance
    ///         (see new ChangeType method)
    /// </summary>
    [ProtoContract]
    class Message {
        [ProtoMember(1)]
        public List<SomeBase> Data { get; private set; }
    }
    /* 
     * repeated somebase data = 1
     * repeated sub1 data_sub1 = 2
     * repeated sub2 data_sub2 = 3
     */ 
    [ProtoContract]
    [ProtoInclude(2, typeof(Sub1))]
    [ProtoInclude(3, typeof(Sub2))]
    class SomeBase
    {
        [ProtoMember(10)]
        public int Test { get; set; }
    }
    [ProtoContract] class Sub1 : SomeBase {
        [ProtoMember(11)]
        public string Foo { get; set; }
    }
    [ProtoContract] class Sub2 : SomeBase {
        [ProtoMember(11)]
        public float Bar { get; set; }
    }

    [TestFixture]
    public class InheritanceTests
    {
        [Test]
        public void InheritanceBaseType()
        {
            SomeBase sb = new SomeBase {Test = 12345};
            SomeBase clone = Serializer.DeepClone<SomeBase>(sb);
            Assert.IsInstanceOfType(typeof(SomeBase), clone, "Type");
            Assert.AreEqual(sb.Test, clone.Test, "Value");
        }
        [Test]
        public void InheritanceSub1()
        {
            SomeBase sb = new Sub1 { Test = 12345, Foo = "abc" };
            SomeBase clone = Serializer.DeepClone<SomeBase>(sb);
            Assert.IsInstanceOfType(typeof(Sub1), clone, "Type");
            Assert.AreEqual(sb.Test, clone.Test, "Value");
            Assert.AreEqual(((Sub1)sb).Foo, ((Sub1)clone).Foo, "Foo");
        }
        [Test]
        public void InheritanceSub2()
        {
            SomeBase sb = new Sub2 { Test = 12345, Bar = 123.45F};
            SomeBase clone = Serializer.DeepClone<SomeBase>(sb);
            Assert.IsInstanceOfType(typeof(Sub2), clone, "Type");
            Assert.AreEqual(sb.Test, clone.Test, "Value");
            Assert.AreEqual(((Sub2)sb).Bar, ((Sub2)clone).Bar, "Foo");
        }


        [Test]
        public void InheritanceCheckBytesCorrectOrder()
        {
            // the purpose of this test is to validate the byte stream so that when
            // we turn around the order we know what we are expecting
            SomeBase sb = new Sub1 { Test = 12345, Foo = "abc" };
            byte[] raw = { 0x12, 0x05, 0x5A, 0x03, 0x61, 0x62, 0x63, 0x50, 0xB9, 0x60 };
            // 0x12 = 10 010 = field 2, string (Sub1)
            // 0x05 = 5 bytes
            // 0x5A = 1011 010 = field 11, string (Foo)
            // 0x03 = 3 bytes
            // 0x61 0x62 0x63 = "abc"
            // 0x50 = 1010 000 = field 10, variant (Test)
            // 0xB9 0x60 = [0]1100000[1]0111001 = 12345            

            Assert.IsTrue(Program.CheckBytes(sb, raw), "raw bytes");
            SomeBase clone = Program.Build<SomeBase>(raw);
            Assert.IsInstanceOfType(typeof(Sub1), clone);
            Assert.AreEqual(sb.Test, clone.Test);
            Assert.AreEqual(((Sub1)sb).Foo, ((Sub1)clone).Foo);
        }

        [Test]
#if DEBUG
        [ExpectedException(typeof(TargetException))]
#else
        [ExpectedException(typeof(InvalidCastException))]
#endif
        public void InheritanceCheckBytesWrongOrder()
        {   // breaking change: not supported in v2; frankly, this is moot - the entire
            // inheritance chain is protobuf-net specific, and that always writes data in
            // the same order; the only edge case is message concatenation.
            // note sure this is a realistic concern
            byte[] raw = { 0x50, 0xB9, 0x60, 0x12, 0x05, 0x5A, 0x03, 0x61, 0x62, 0x63};
            SomeBase clone = Program.Build<SomeBase>(raw);
            Assert.IsInstanceOfType(typeof(Sub1), clone);
            Assert.AreEqual(12345, clone.Test);
            Assert.AreEqual("abc", ((Sub1)clone).Foo);
        }
    }
}
