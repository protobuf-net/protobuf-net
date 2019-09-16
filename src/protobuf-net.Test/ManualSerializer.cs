using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.Buffers;
using System.IO;
using Xunit;

namespace ProtoBuf
{
    public class ManualSerializer
    {
#if !PLAT_NO_EMITDLL
        [Fact]
        public void EmitManualSerializer()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(A));
            model.Add(typeof(B));
            model.Add(typeof(C));
            model.Add(typeof(D));
            model.Compile("EmitManualSerializer", "EmitManualSerializer.dll");
            PEVerify.Verify("EmitManualSerializer.dll");
        }
#endif

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadWriteAutomated_StreamReaderWriter(bool withState)
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            using (var ms = new MemoryStream())
            {
                var obj = new C { AVal = 123, BVal = 456, CVal = 789 };
                ProtoWriter.State writeState = default;
#pragma warning disable CS0618
                using (var writer = withState ? ProtoWriter.Create(out writeState, ms, null) : ProtoWriter.Create(ms, null))
#pragma warning restore CS0618
                {
                    model.Serialize(writer, ref writeState, obj);
                    Assert.Equal(0, writer.Depth);
                    writer.Close(ref writeState);
                }
                var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
                Assert.Equal("22-08-2A-03-18-95-06-10-C8-03-08-7B", hex);
                // 22 = field 4, type String
                // 08 = length 8
                //      2A = field 5, type String
                //      03 = length 3
                //          18 = field 3, type Variant
                //          95-06 = 789 (raw) or -395 (zigzag)
                //      10 = field 2, type Variant
                //      C8-03 = 456(raw) or 228(zigzag)
                // 08 = field 1, type Variant
                // 7B = 123(raw) or - 62(zigzag)

                ms.Position = 0;
                ProtoReader.State readState = default;
#pragma warning disable CS0618
                using (var reader = withState ? ProtoReader.Create(out readState, ms, null) : ProtoReader.Create(ms, null))
#pragma warning restore CS0618
                {
                    var raw = model.Deserialize(reader, ref readState, null, typeof(A));
                    var clone = Assert.IsType<C>(raw);
                    Assert.NotSame(obj, clone);
                    Assert.Equal(123, clone.AVal);
                    Assert.Equal(456, clone.BVal);
                    Assert.Equal(789, clone.CVal);
                }
            }
        }

        [Fact]
        public void ReadWriteAutomated_Stream()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            using (var ms = new MemoryStream())
            {
                var obj = new C { AVal = 123, BVal = 456, CVal = 789 };
                model.Serialize(ms, obj);

                var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
                Assert.Equal("22-08-2A-03-18-95-06-10-C8-03-08-7B", hex);
                // 22 = field 4, type String
                // 08 = length 8
                //      2A = field 5, type String
                //      03 = length 3
                //          18 = field 3, type Variant
                //          95-06 = 789 (raw) or -395 (zigzag)
                //      10 = field 2, type Variant
                //      C8-03 = 456(raw) or 228(zigzag)
                // 08 = field 1, type Variant
                // 7B = 123(raw) or - 62(zigzag)

                ms.Position = 0;
                var raw = model.Deserialize(ms, null, typeof(A));
                var clone = Assert.IsType<C>(raw);
                Assert.NotSame(obj, clone);
                Assert.Equal(123, clone.AVal);
                Assert.Equal(456, clone.BVal);
                Assert.Equal(789, clone.CVal);
            }
        }

        static string Hex(ReadOnlySequence<byte> sequence)
        {
            var len = checked((int)sequence.Length);
            var rented = ArrayPool<byte>.Shared.Rent(len);
            sequence.CopyTo(rented);
            var hex = BitConverter.ToString(rented, 0, len);
            ArrayPool<byte>.Shared.Return(rented);
            return hex;
        }

        [Fact]
        public void ReadWriteAutomated_PipeReaderWriter()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            using (var pipe = Pipelines.Sockets.Unofficial.Buffers.BufferWriter<byte>.Create())
            {
                var obj = new C { AVal = 123, BVal = 456, CVal = 789 };
                using (var writer = ProtoWriter.Create(out var state, pipe.Writer, null))
                {
                    model.Serialize(writer, ref state, obj);
                    Assert.Equal(0, writer.Depth);
                    writer.Close(ref state);
                }
                using (var result = pipe.Flush())
                {
                    var hex = Hex(result.Value);
                    Assert.Equal("22-08-2A-03-18-95-06-10-C8-03-08-7B", hex);
                    // 22 = field 4, type String
                    // 08 = length 8
                    //      2A = field 5, type String
                    //      03 = length 3
                    //          18 = field 3, type Variant
                    //          95-06 = 789 (raw) or -395 (zigzag)
                    //      10 = field 2, type Variant
                    //      C8-03 = 456(raw) or 228(zigzag)
                    // 08 = field 1, type Variant
                    // 7B = 123(raw) or - 62(zigzag)

                    using (var reader = ProtoReader.Create(out var state, result.Value, null))
                    {
                        var raw = model.Deserialize(reader, ref state, null, typeof(A));
                        var clone = Assert.IsType<C>(raw);
                        Assert.NotSame(obj, clone);
                        Assert.Equal(123, clone.AVal);
                        Assert.Equal(456, clone.BVal);
                        Assert.Equal(789, clone.CVal);
                    }
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadWriteManual_StreamReaderWriter(bool withState)
        {
            using (var ms = new MemoryStream())
            {
                var obj = new C { AVal = 123, BVal = 456, CVal = 789 };
                ProtoWriter.State writeState = default;
#pragma warning disable CS0618
                using (var writer = withState ? ProtoWriter.Create(out writeState, ms, null) : ProtoWriter.Create(ms, null))
#pragma warning restore CS0618
                {
                    var bytes = writer.Serialize<A>(ref writeState, obj, ModelSerializer.Default);
                    Assert.Equal(12, bytes);
                    Assert.Equal(0, writer.Depth);
                    writer.Close(ref writeState);
                }
                Assert.Equal(12, ms.Length);
                var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
                Assert.Equal("22-08-2A-03-18-95-06-10-C8-03-08-7B", hex);

                ms.Position = 0;
                ProtoReader.State readState = default;
#pragma warning disable CS0618
                using (var reader = withState ? ProtoReader.Create(out readState, ms, null) : ProtoReader.Create(ms, null))
#pragma warning restore CS0618
                {
                    var raw = reader.Deserialize<A>(ref readState, null, ModelSerializer.Default);
                    var clone = Assert.IsType<C>(raw);
                    Assert.NotSame(obj, clone);
                    Assert.Equal(123, clone.AVal);
                    Assert.Equal(456, clone.BVal);
                    Assert.Equal(789, clone.CVal);
                }
            }
        }

        [Fact]
        public void ReadWriteManual_PipeReaderWriter()
        {
            using (var pipe = Pipelines.Sockets.Unofficial.Buffers.BufferWriter<byte>.Create())
            {
                var obj = new C { AVal = 123, BVal = 456, CVal = 789 };
                using (var writer = ProtoWriter.Create(out var state, pipe.Writer, null))
                {
                    var bytes = writer.Serialize<A>(ref state, obj, ModelSerializer.Default);
                    Assert.Equal(12, bytes);
                    Assert.Equal(0, writer.Depth);
                    writer.Close(ref state);
                }
                Assert.Equal(12, pipe.Length);

                using (var result = pipe.Flush())
                {
                    var hex = Hex(result.Value);
                    Assert.Equal("22-08-2A-03-18-95-06-10-C8-03-08-7B", hex);

                    using (var reader = ProtoReader.Create(out var state, result.Value, null))
                    {
                        var raw = reader.Deserialize<A>(ref state, null, ModelSerializer.Default);
                        var clone = Assert.IsType<C>(raw);
                        Assert.NotSame(obj, clone);
                        Assert.Equal(123, clone.AVal);
                        Assert.Equal(456, clone.BVal);
                        Assert.Equal(789, clone.CVal);
                    }
                }
            }
        }
    }

    class ModelSerializer :
        IProtoSerializer<A>,
        IProtoDeserializer<A>, IProtoSubTypeSerializer<A>, IProtoFactory<A>,
        IProtoDeserializer<B>, IProtoSubTypeSerializer<B>, IProtoFactory<B>,
        IProtoDeserializer<C>, IProtoSubTypeSerializer<C>, IProtoFactory<C>,
        IProtoSerializer<D>, IProtoDeserializer<D>, IProtoFactory<D>
    {
        public static ModelSerializer Default = new ModelSerializer();
        public ModelSerializer() { }

        A IProtoFactory<A>.Create(ISerializationContext context) => new A();
        B IProtoFactory<B>.Create(ISerializationContext context) => new B();
        C IProtoFactory<C>.Create(ISerializationContext context) => new C();
        D IProtoFactory<D>.Create(ISerializationContext context) => new D();

        //void IProtoFactory<A, A>.Copy(SerializationContext context, A from, A to)
        //{
        //    to.AVal = from.AVal;
        //}

        //void IProtoFactory<A, B>.Copy(SerializationContext context, A from, B to)
        //{
        //    if (from is B b)
        //    {
        //        to.BVal = b.BVal;
        //    }

        //    ((IProtoFactory<A, A>)Serializer).Copy(context, from, to);
        //}

        //void IProtoFactory<A, C>.Copy(SerializationContext context, A from, C to)
        //{
        //    if (from is C c)
        //    {
        //        to.CVal = c.CVal;
        //    }

        //    if (from is B b)
        //    {
        //        ((IProtoFactory<A, B>)Serializer).Copy(context, b, to);
        //    }
        //    else
        //    {
        //        ((IProtoFactory<A, A>)Serializer).Copy(context, from, to);
        //    }
        //}

        void IProtoSerializer<A>.Write(ProtoWriter writer, ref ProtoWriter.State state, A value)
            => ProtoWriter.WriteBaseType<A>(value, writer, ref state, this);
        A IProtoDeserializer<A>.Read(ProtoReader reader, ref ProtoReader.State state, A value)
            => reader.ReadBaseType<A, A>(ref state, value, this);
        B IProtoDeserializer<B>.Read(ProtoReader reader, ref ProtoReader.State state, B value)
            => reader.ReadBaseType<A, B>(ref state, value, this);
        C IProtoDeserializer<C>.Read(ProtoReader reader, ref ProtoReader.State state, C value)
            => reader.ReadBaseType<A, C>(ref state, value, this);

        void IProtoSubTypeSerializer<A>.WriteSubType(ProtoWriter writer, ref ProtoWriter.State state, A value)
        {
            if (TypeModel.IsSubType<A>(value))
            {
                if (value is B b)
                {
                    ProtoWriter.WriteFieldHeader(4, WireType.String, writer, ref state);
                    ProtoWriter.WriteSubType<B>(b, writer, ref state, this);
                }
                else
                {
                    TypeModel.ThrowUnexpectedSubtype<A>(value);
                }
            }
            if (value.AVal != 0)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer, ref state);
                ProtoWriter.WriteInt32(value.AVal, writer, ref state);
            }
        }

        A IProtoSubTypeSerializer<A>.ReadSubType(ProtoReader reader, ref ProtoReader.State state, SubTypeState<A> value)
        {
            int field;
            value.OnBeforeDeserialize((obj, ctx) => obj.OnBeforeDeserialize());
            while ((field = reader.ReadFieldHeader(ref state)) != 0)
            {
                switch (field)
                {
                    case 1:
                        value.Value.AVal = reader.ReadInt32(ref state);
                        break;
                    case 4:
                        value.ReadSubType<B>(reader, ref state, this);
                        break;
                    default:
                        reader.SkipField(ref state);
                        break;
                }
            }
            return value.OnAfterDeserialize((obj, ctx) => obj.OnAfterDeserialize());
        }

        void IProtoSubTypeSerializer<B>.WriteSubType(ProtoWriter writer, ref ProtoWriter.State state, B value)
        {
            if (TypeModel.IsSubType<B>(value))
            {
                if (value is C c)
                {
                    ProtoWriter.WriteFieldHeader(5, WireType.String, writer, ref state);
                    ProtoWriter.WriteSubType<C>(c, writer, ref state, this);
                }
                else
                {
                    TypeModel.ThrowUnexpectedSubtype<B>(value);
                }
            }
            if (value.BVal != 0)
            {
                ProtoWriter.WriteFieldHeader(2, WireType.Variant, writer, ref state);
                ProtoWriter.WriteInt32(value.BVal, writer, ref state);
            }
        }

        B IProtoSubTypeSerializer<B>.ReadSubType(ProtoReader reader, ref ProtoReader.State state, SubTypeState<B> value)
        {
            int field;
            while ((field = reader.ReadFieldHeader(ref state)) != 0)
            {
                switch (field)
                {
                    case 2:
                        value.Value.BVal = reader.ReadInt32(ref state);
                        break;
                    case 5:
                        value.ReadSubType<C>(reader, ref state, this);
                        break;
                    default:
                        reader.SkipField(ref state);
                        break;
                }
            }
            return value.Value;
        }

        void IProtoSubTypeSerializer<C>.WriteSubType(ProtoWriter writer, ref ProtoWriter.State state, C value)
        {
            TypeModel.ThrowUnexpectedSubtype<C>(value);
            if (value.CVal != 0)
            {
                ProtoWriter.WriteFieldHeader(3, WireType.Variant, writer, ref state);
                ProtoWriter.WriteInt32(value.CVal, writer, ref state);
            }
        }

        C IProtoSubTypeSerializer<C>.ReadSubType(ProtoReader reader, ref ProtoReader.State state, SubTypeState<C> value)
        {
            int field;
            while ((field = reader.ReadFieldHeader(ref state)) != 0)
            {
                switch (field)
                {
                    case 3:
                        value.Value.CVal = reader.ReadInt32(ref state);
                        break;
                    default:
                        reader.SkipField(ref state);
                        break;
                }
            }
            return value.Value;
        }

        void IProtoSerializer<D>.Write(ProtoWriter writer, ref ProtoWriter.State state, D value)
        {
            TypeModel.ThrowUnexpectedSubtype<D>(value);
            if (value.DVal != 0)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer, ref state);
                ProtoWriter.WriteInt32(value.DVal, writer, ref state);
            }
        }

        D IProtoDeserializer<D>.Read(ProtoReader reader, ref ProtoReader.State state, D value)
        {
            if (value == null) value = reader.CreateInstance<D>(this);
            int field;
            while ((field = reader.ReadFieldHeader(ref state)) != 0)
            {
                switch (field)
                {
                    case 1:
                        value.DVal = reader.ReadInt32(ref state);
                        break;
                    default:
                        reader.SkipField(ref state);
                        break;
                }
            }
            return value;
        }
    }

    [ProtoContract]
    [ProtoInclude(4, typeof(B))]
    public class A
    {
        [ProtoMember(1)]
        public int AVal { get; set; }

        internal void OnAfterDeserialize() { }

        internal void OnBeforeDeserialize() { }
    }

    [ProtoContract]
    [ProtoInclude(5, typeof(C))]
    public class B : A
    {
        [ProtoMember(2)]
        public int BVal { get; set; }
    }
    [ProtoContract]
    public class C : B
    {
        [ProtoMember(3)]
        public int CVal { get; set; }

    }
    [ProtoContract(SkipConstructor = true)]
    public class D
    {
        [ProtoMember(1)]
        public int DVal { get; set; }

    }
}
