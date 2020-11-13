//#if PLAT_UTF8_STRING

//using ProtoBuf.Serializers;
//using System;

//namespace ProtoBuf.Internal
//{
//    internal sealed class Utf8String // mock-up for now
//    {
//        public int Length => throw new NotImplementedException();
//        public ReadOnlyMemory<byte> AsMemory() => throw new NotImplementedException();
//        public static Utf8String Empty => throw new NotImplementedException();
//    }
//    partial class PrimaryTypeProvider :
//        IMeasuringSerializer<Utf8String>,
//        IFactory<Utf8String>,
//        IValueChecker<Utf8String>
//    {
//        Utf8String ISerializer<Utf8String>.Read(ref ProtoReader.State state, Utf8String value)
//            => throw new NotImplementedException(); // should use public static Utf8String Create<TState>(int length, TState state, SpanAction<byte, TState> action)
//        void ISerializer<Utf8String>.Write(ref ProtoWriter.State state, Utf8String value) => state.WriteBytes(value.AsMemory());
//        SerializerFeatures ISerializer<Utf8String>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;
//        int IMeasuringSerializer<Utf8String>.Measure(ISerializationContext context, WireType wireType, Utf8String value)
//            => wireType switch
//            {
//                WireType.String => value.Length,
//                _ => -1,
//            };
//        Utf8String IFactory<Utf8String>.Create(ISerializationContext context) => Utf8String.Empty;

//        bool IValueChecker<Utf8String>.HasNonTrivialValue(Utf8String value) => value is object; //  note: we write "" (when found), for compat
//        bool IValueChecker<Utf8String>.IsNull(Utf8String value) => value is null; //  note: we write "" (when found), for compat
//    }
//}
//#endif