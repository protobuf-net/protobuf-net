using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
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
            model.Add(typeof(E));
            model.Compile("EmitManualSerializer", "EmitManualSerializer.dll");
            PEVerify.Verify("EmitManualSerializer.dll", 0, deleteOnSuccess: false);
        }
#endif

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadWriteAutomated_StreamReaderWriter(bool withState)
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            using var ms = new MemoryStream();
            var obj = new C { AVal = 123, BVal = 456, CVal = 789 };
            ProtoWriter.State writeState = default;
#pragma warning disable CS0618
            using (var writer = withState ? ProtoWriter.Create(out writeState, ms, null) : ProtoWriter.Create(ms, null))
#pragma warning restore CS0618
            {
                writer.Serialize(ref writeState, obj);
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

            A raw;
            if (withState)
            {
                using var readState = ProtoReader.State.Create(ms, null);
                raw = readState.Deserialize<A>(null);
            }
            else
            {
#pragma warning disable CS0618
                using var reader = ProtoReader.Create(ms, null);
#pragma warning restore CS0618
                raw = reader.DefaultState().Deserialize<A>(null);
            }
            var clone = Assert.IsType<C>(raw);
            Assert.NotSame(obj, clone);
            Assert.Equal(123, clone.AVal);
            Assert.Equal(456, clone.BVal);
            Assert.Equal(789, clone.CVal);
        }

        [Fact]
        public void ReadWriteAutomated_Stream()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            using var ms = new MemoryStream();
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
            var raw = model.Deserialize<A>(ms);
            var clone = Assert.IsType<C>(raw);
            Assert.NotSame(obj, clone);
            Assert.Equal(123, clone.AVal);
            Assert.Equal(456, clone.BVal);
            Assert.Equal(789, clone.CVal);
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
            using var pipe = Pipelines.Sockets.Unofficial.Buffers.BufferWriter<byte>.Create();
            var obj = new C { AVal = 123, BVal = 456, CVal = 789 };
            using (var writer = ProtoWriter.Create(out var writeState, pipe.Writer, null))
            {
                writer.Serialize(ref writeState, obj);
                Assert.Equal(0, writer.Depth);
                writer.Close(ref writeState);
            }
            using var result = pipe.Flush();
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

            using var readState = ProtoReader.State.Create(result.Value, null);
            var raw = readState.Deserialize<A>(null);
            var clone = Assert.IsType<C>(raw);
            Assert.NotSame(obj, clone);
            Assert.Equal(123, clone.AVal);
            Assert.Equal(456, clone.BVal);
            Assert.Equal(789, clone.CVal);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadWriteManual_StreamReaderWriter(bool withState)
        {
            using var ms = new MemoryStream();
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
            A raw;
            if (withState)
            {
                using var readState = ProtoReader.State.Create(ms, null);
                raw = readState.Deserialize<A>(null);
            }
            else
            {
#pragma warning disable CS0618
                using var reader = ProtoReader.Create(ms, null);
#pragma warning restore CS0618
                raw = reader.DefaultState().Deserialize<A>(null);
            }
            var clone = Assert.IsType<C>(raw);
            Assert.NotSame(obj, clone);
            Assert.Equal(123, clone.AVal);
            Assert.Equal(456, clone.BVal);
            Assert.Equal(789, clone.CVal);
        }

        [Fact]
        public void ReadWriteManual_PipeReaderWriter()
        {
            using var pipe = Pipelines.Sockets.Unofficial.Buffers.BufferWriter<byte>.Create();
            var obj = new C { AVal = 123, BVal = 456, CVal = 789 };
            using (var writer = ProtoWriter.Create(out var writeState, pipe.Writer, null))
            {
                var bytes = writer.Serialize<A>(ref writeState, obj, ModelSerializer.Default);
                Assert.Equal(12, bytes);
                Assert.Equal(0, writer.Depth);
                writer.Close(ref writeState);
            }
            Assert.Equal(12, pipe.Length);

            using var result = pipe.Flush();
            var hex = Hex(result.Value);
            Assert.Equal("22-08-2A-03-18-95-06-10-C8-03-08-7B", hex);

            using var readState = ProtoReader.State.Create(result.Value, null);
            var raw = readState.Deserialize<A>(null, ModelSerializer.Default);
            var clone = Assert.IsType<C>(raw);
            Assert.NotSame(obj, clone);
            Assert.Equal(123, clone.AVal);
            Assert.Equal(456, clone.BVal);
            Assert.Equal(789, clone.CVal);
        }
    }

    class ModelSerializer :
        IProtoSerializer<A>, IProtoSubTypeSerializer<A>, IProtoFactory<A>,
        IProtoSerializer<B>, IProtoSubTypeSerializer<B>, IProtoFactory<B>,
        IProtoSerializer<C>, IProtoSubTypeSerializer<C>, IProtoFactory<C>,
        IProtoSerializer<D>, IProtoFactory<D>
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
            => ((IProtoSubTypeSerializer<A>)this).WriteSubType(writer, ref state, value);
        void IProtoSerializer<B>.Write(ProtoWriter writer, ref ProtoWriter.State state, B value)
            => ((IProtoSubTypeSerializer<A>)this).WriteSubType(writer, ref state, value);
        void IProtoSerializer<C>.Write(ProtoWriter writer, ref ProtoWriter.State state, C value)
            => ((IProtoSubTypeSerializer<A>)this).WriteSubType(writer, ref state, value);

        A IProtoSerializer<A>.Read(ProtoReader reader, ref ProtoReader.State state, A value)
            => ((IProtoSubTypeSerializer<A>)this).ReadSubType(reader, ref state, SubTypeState<A>.Create<A>(reader, value));
        B IProtoSerializer<B>.Read(ProtoReader reader, ref ProtoReader.State state, B value)
            => (B)((IProtoSubTypeSerializer<A>)this).ReadSubType(reader, ref state, SubTypeState<A>.Create<B>(reader, value));
        C IProtoSerializer<C>.Read(ProtoReader reader, ref ProtoReader.State state, C value)
            => (C)((IProtoSubTypeSerializer<A>)this).ReadSubType(reader, ref state, SubTypeState<A>.Create<C>(reader, value));

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
                ProtoWriter.WriteFieldHeader(1, WireType.Varint, writer, ref state);
                ProtoWriter.WriteInt32(value.AVal, writer, ref state);
            }
        }

        A IProtoSubTypeSerializer<A>.ReadSubType(ProtoReader reader, ref ProtoReader.State state, SubTypeState<A> value)
        {
            int field;
            value.OnBeforeDeserialize((obj, ctx) => obj.OnBeforeDeserialize());
            while ((field = state.ReadFieldHeader()) != 0)
            {
                switch (field)
                {
                    case 1:
                        value.Value.AVal = state.ReadInt32();
                        break;
                    case 4:
                        value.ReadSubType<B>(ref state, this);
                        break;
                    default:
                        state.SkipField();
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
                ProtoWriter.WriteFieldHeader(2, WireType.Varint, writer, ref state);
                ProtoWriter.WriteInt32(value.BVal, writer, ref state);
            }
        }

        B IProtoSubTypeSerializer<B>.ReadSubType(ProtoReader reader, ref ProtoReader.State state, SubTypeState<B> value)
        {
            int field;
            while ((field = state.ReadFieldHeader()) != 0)
            {
                switch (field)
                {
                    case 2:
                        value.Value.BVal = state.ReadInt32();
                        break;
                    case 5:
                        value.ReadSubType<C>(ref state, this);
                        break;
                    default:
                        state.SkipField();
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
                ProtoWriter.WriteFieldHeader(3, WireType.Varint, writer, ref state);
                ProtoWriter.WriteInt32(value.CVal, writer, ref state);
            }
        }

        C IProtoSubTypeSerializer<C>.ReadSubType(ProtoReader reader, ref ProtoReader.State state, SubTypeState<C> value)
        {
            int field;
            while ((field = state.ReadFieldHeader()) != 0)
            {
                switch (field)
                {
                    case 3:
                        value.Value.CVal = state.ReadInt32();
                        break;
                    default:
                        state.SkipField();
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
                ProtoWriter.WriteFieldHeader(1, WireType.Varint, writer, ref state);
                ProtoWriter.WriteInt32(value.DVal, writer, ref state);
            }
        }

        D IProtoSerializer<D>.Read(ProtoReader reader, ref ProtoReader.State state, D value)
        {
            if (value == null) value = state.CreateInstance<D>(this);
            int field;
            while ((field = state.ReadFieldHeader()) != 0)
            {
                switch (field)
                {
                    case 1:
                        value.DVal = state.ReadInt32();
                        break;
                    default:
                        state.SkipField();
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

    [ProtoContract]
    public sealed class E
    {
        [ProtoMember(1)]
        public E PublicAssignable { get; set; }

        [ProtoMember(2)]
        public E PrivateAssignable { get; private set; }

        [ProtoMember(3)]
        public E NonAssignable => _nonAssignable ?? (_nonAssignable = new E());
        private E _nonAssignable;

        [ProtoMember(4)]
        public List<E> PublicAssignableList { get; set; }

        [ProtoMember(5)]
        public List<E> PrivateAssignableList { get; private set; }

        [ProtoMember(6)]
        public List<E> NonAssignableList => _nonAssignableList ?? (_nonAssignableList = new List<E>());
        public List<E> _nonAssignableList;

        [Browsable(false)]
        public bool ShouldSerializeNonAssignableList() => _nonAssignableList != null && _nonAssignableList.Count != 0;
    }
}
