//using ProtoBuf.Serializers;
//using System;

//namespace ProtoBuf.Internal
//{
//    partial class PrimaryTypeProvider : IMeasuringSerializer<GuidString>, IValueChecker<GuidString>, IMeasuringSerializer<GuidBytes>, IValueChecker<GuidBytes>
//    {
//        SerializerFeatures ISerializer<GuidBytes>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;

//        bool IValueChecker<GuidBytes>.HasNonTrivialValue(GuidBytes value) => !value.Value.Equals(Guid.Empty);

//        bool IValueChecker<GuidBytes>.IsNull(GuidBytes value) => false;

//        int IMeasuringSerializer<GuidBytes>.Measure(ISerializationContext context, WireType wireType, GuidBytes value)
//          => value.Equals(Guid.Empty) ? 0 : GuidHelper.WRITE_STRING_LENGTH;

//        unsafe GuidBytes ISerializer<GuidBytes>.Read(ref ProtoReader.State state, GuidBytes value)
//            => GuidHelper.Read(ref state);

//        void ISerializer<GuidBytes>.Write(ref ProtoWriter.State state, GuidBytes value)
//            => GuidHelper.Write(ref state, in value.ValueField, asBytes: true);

//        SerializerFeatures ISerializer<GuidString>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;

//        bool IValueChecker<GuidString>.HasNonTrivialValue(GuidString value) => !value.Value.Equals(Guid.Empty);

//        bool IValueChecker<GuidString>.IsNull(GuidString value) => false;

//        int IMeasuringSerializer<GuidString>.Measure(ISerializationContext context, WireType wireType, GuidString value)
//          => value.Equals(Guid.Empty) ? 0 : GuidHelper.WRITE_BYTES_LENGTH;

//        unsafe GuidString ISerializer<GuidString>.Read(ref ProtoReader.State state, GuidString value)
//            => GuidHelper.Read(ref state);

//        void ISerializer<GuidString>.Write(ref ProtoWriter.State state, GuidString value)
//            => GuidHelper.Write(ref state, in value.ValueField, asBytes: false);
//    }
//}
