#if PLAT_SPANS
using ProtoBuf.Meta;
using System;
using System.Buffers;

namespace ProtoBuf
{
    public partial class ProtoReader
    {
        /// <summary>
        /// Creates a new reader against a multi-segment buffer
        /// </summary>
        /// <param name="source">The source buffer</param>
        /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
        /// <param name="context">Additional context about this serialization operation</param>
        public static ProtoReader Create(ReadOnlySequence<byte> source, TypeModel model, SerializationContext context = null)
        {
            var reader = ReadOnlySequenceProtoReader.GetRecycled()
                ?? new ReadOnlySequenceProtoReader();
            reader.Init(source, model, context);
            return reader;
        }

        private sealed class ReadOnlySequenceProtoReader : ProtoReader
        {
            [ThreadStatic]
            private static ReadOnlySequenceProtoReader s_lastReader;
            private ReadOnlySequence<byte> _source;
            private Memory<byte> _current;
            private int _offsetInCurrent, _remainingInCurrent;

            internal static ReadOnlySequenceProtoReader GetRecycled()
            {
                var tmp = s_lastReader;
                s_lastReader = null;
                return tmp;
            }

            internal void Init(ReadOnlySequence<byte> source, TypeModel model, SerializationContext context)
            {
                base.Init(model, context);
                _source = source;
            }

            internal override void Recycle()
            {
                Dispose();
                s_lastReader = this;
            }

            public override void Dispose()
            {
                base.Dispose();
                _source = default;
            }

            private protected override void ImplReadBytes(ArraySegment<byte> target)
            {
                throw new NotImplementedException();
            }

            private protected override string ImplReadString(int bytes)
            {
                throw new NotImplementedException();
            }

            private protected override uint ImplReadUInt32Fixed()
            {
                throw new NotImplementedException();
            }

            internal override void ImplSkipBytes(long count)
            {
                throw new NotImplementedException();
            }

            private protected override ulong ImplReadUInt64Fixed()
            {
                throw new NotImplementedException();
            }

            internal override int TryReadUInt32VariantWithoutMoving(bool trimNegative, out uint value)
            {
                throw new NotImplementedException();
            }
            private protected override int TryReadUInt64VariantWithoutMoving(out ulong value)
            {
                throw new NotImplementedException();
            }

            private protected override bool IsFullyConsumed =>
                _remainingInCurrent == 0 && _source.IsEmpty;
        }
    }
}
#endif