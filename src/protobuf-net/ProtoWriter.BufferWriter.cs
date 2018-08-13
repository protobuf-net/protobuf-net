//#if PLAT_SPANS
//using ProtoBuf.Meta;
//using System;
//using System.Buffers;

//namespace ProtoBuf
//{
//    partial class ProtoWriter
//    {

//        /// <summary>
//        /// Create a new ProtoWriter that tagets a buffer writer
//        /// </summary>
//        public static ProtoWriter Create(out ProtoWriter.State state, IBufferWriter<byte> writer, TypeModel model, SerializationContext context = null)
//        {
//            if (writer == null) throw new ArgumentNullException(nameof(writer));
//            state = default;
//            return new BufferWriterProtoWriter(writer, model, context);
//        }

//        private sealed class BufferWriterProtoWriter : ProtoWriter
//        {
//            private IBufferWriter<byte> _writer;
//            internal BufferWriterProtoWriter(IBufferWriter<byte> writer, TypeModel model, SerializationContext context)
//                : base(model, context)
//                => _writer = writer;

//            private protected override bool DemandFlushOnDispose => true;

//            private protected override bool TryFlush(ref State state)
//            {
//                if(state.Active)
//                {
//                    _writer.Advance(state.Reset());
//                }
//                return true;
//            }
//        }
//    }
//}
//#endif