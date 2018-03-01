using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;

namespace Examples
{
    [ProtoContract]
    public class FieldData
    {
        public FieldData() {}
        public FieldData(object value) {
            Value = value;}
        public object Value { get; set; }

        private bool Is<T>() {
            return Value != null && Value is T;
        }
        private T Get<T>() {
            return Is<T>() ? (T) Value : default(T);
        }
        [ProtoMember(1)]
        private int ValueInt32
        {
            get { return Get<int>(); }
            set { Value = value; }
        }
        private bool ValueInt32Specified { get { return Is<int>(); } }

        [ProtoMember(2)]
        private float ValueSingle
        {
            get { return Get<float>();}
            set { Value = value; }
        }
        private bool ValueSingleSpecified { get { return Is<float>(); } }

        [ProtoMember(3)]
        private double ValueDouble
        {
            get { return Get<double>(); ; }
            set { Value = value; }
        }
        private bool ValueDoubleSpecified { get { return Is<double>(); } }

        // etc for expected types
    }

    [ProtoContract]
    public class FieldDataViaNullable
    {
        public FieldDataViaNullable() { }
        public FieldDataViaNullable(object value)
        {
            Value = value;
        }
        public object Value { get; set; }

        private T? Get<T>() where T : struct
        {
            return (Value != null && Value is T) ? (T?)Value : (T?)null;
        }
        [ProtoMember(1)]
        private int? ValueInt32
        {
            get { return Get<int>(); }
            set { Value = value; }
        }
        [ProtoMember(2)]
        private float? ValueSingle
        {
            get { return Get<float>(); }
            set { Value = value; }
        }
        [ProtoMember(3)]
        private double? ValueDouble
        {
            get { return Get<double>(); ; }
            set { Value = value; }
        }
        // etc for expected types
    }

    [ProtoContract]
    class Int32Simple
    {
        [ProtoMember(1)]
        public int Value { get; set; }
    }
    [ProtoContract]
    class SingleSimple
    {
        [ProtoMember(2)]
        public float Value { get; set; }
    }
    [ProtoContract]
    class DoubleSimple
    {
        [ProtoMember(3)]
        public double Value { get; set; }
    }

    
    public class ValueWrapperTests
    {
        static byte[] GetBytes<T>(T item)
        {
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, item);
            return ms.ToArray();
        }
        [Fact]
        public void TestRaw()
        {
            Assert.True(Program.CheckBytes(new FieldData()), "Empty");
            Assert.Null(Serializer.DeepClone(new FieldData()).Value);

        }
        [Fact]
        public void TestInt32()
        {

            Assert.True(Program.CheckBytes(new FieldData {Value = 123},
                                             GetBytes(new Int32Simple {Value = 123})), "Int32");
            Assert.Equal(123, Serializer.DeepClone(new FieldData(123)).Value);
        }
        [Fact]
        public void TestSingle()
        {
            Assert.True(Program.CheckBytes(new FieldData {Value = 123.45F},
                                             GetBytes(new SingleSimple {Value = 123.45F})), "Single");
            Assert.Equal(123.45F, Serializer.DeepClone(new FieldData(123.45F)).Value);

        }
        [Fact]
        public void TestDouble()
        {
            Assert.True(Program.CheckBytes(new FieldData { Value = 123.45 },
                GetBytes(new DoubleSimple { Value = 123.45 })), "Double");
            Assert.Equal(123.45, Serializer.DeepClone(new FieldData(123.45)).Value);
        }

    }

    
    public class ValueWrapperTestsViaNullable
    {
        static byte[] GetBytes<T>(T item)
        {
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, item);
            return ms.ToArray();
        }
        [Fact]
        public void TestRaw()
        {
            Assert.True(Program.CheckBytes(new FieldDataViaNullable()), "Empty");
            Assert.Null(Serializer.DeepClone(new FieldDataViaNullable()).Value);

        }

        [Fact]
        public void TestInt32()
        {

            Assert.True(Program.CheckBytes(new FieldDataViaNullable {Value = 123},
                                             GetBytes(new Int32Simple {Value = 123})), "Int32");
            Assert.Equal(123, Serializer.DeepClone(new FieldDataViaNullable(123)).Value);
        }
        [Fact]
        public void TestSingle()
        {
            Assert.True(Program.CheckBytes(new FieldDataViaNullable {Value = 123.45F},
                                             GetBytes(new SingleSimple {Value = 123.45F})), "Single");
            Assert.Equal(123.45F, Serializer.DeepClone(new FieldDataViaNullable(123.45F)).Value);

        }
        [Fact]
        public void TestDouble()
        {
            Assert.True(Program.CheckBytes(new FieldDataViaNullable { Value = 123.45 },
                GetBytes(new DoubleSimple { Value = 123.45 })), "Double");
            Assert.Equal(123.45, Serializer.DeepClone(new FieldDataViaNullable(123.45)).Value);
        }

    }
}
