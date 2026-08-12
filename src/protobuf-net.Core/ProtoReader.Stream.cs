using ProtoBuf.Meta;
using System;
using System.IO;
using System.Reflection;

namespace ProtoBuf
{
    public partial class ProtoReader
    {
        // The stream BACKEND is gone: State reads streams natively via its own
        // shift-and-top-up core. What survives here is the museum Create overload and the
        // MemoryStream unwrap helper - which also serves the WRITER's extension-blit, its one
        // other caller.

        /// <summary>
        /// Creates a new reader against a stream
        /// </summary>
        /// <param name="source">The source stream</param>
        /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
        /// <param name="context">Additional context about this serialization operation</param>
        /// <param name="length">The number of bytes to read, or -1 to read until the end of the stream</param>
        [Obsolete(PreferStateAPI, false)]
        public static ProtoReader Create(Stream source, TypeModel model, SerializationContext context = null, long length = TO_EOF)
            => Create(source, model, (object)context, length);

        private static readonly FieldInfo s_origin = typeof(MemoryStream).GetField("_origin", BindingFlags.NonPublic | BindingFlags.Instance),
            s_buffer = typeof(MemoryStream).GetField("_buffer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static bool ReflectionTryGetBuffer(MemoryStream ms, out ArraySegment<byte> buffer)
        {
            if (s_origin is not null && s_buffer is not null && ms.GetType() == typeof(MemoryStream)) // don't consider subclasses
            {
                try
                {
                    int offset = (int)s_origin.GetValue(ms);
                    byte[] arr = (byte[])s_buffer.GetValue(ms);
                    buffer = new ArraySegment<byte>(arr, offset, checked((int)ms.Length));
                    return true;
                }
                catch { }
            }
            buffer = default;
            return false;
        }

        internal static bool TryConsumeSegmentRespectingPosition(Stream source, out ArraySegment<byte> data, long length)
        {
            if (source is MemoryStream ms && ms.CanSeek
                && (ms.TryGetBuffer(out var segment) || ReflectionTryGetBuffer(ms, out segment)))
            {
                int pos = checked((int)ms.Position);
                var count = segment.Count - pos;
                var offset = segment.Offset + pos;

                if (length >= 0 && length < count)
                {   // make sure we apply a length limit
                    count = (int)length;
                }
                data = new ArraySegment<byte>(segment.Array, offset, count);
                // skip the data in the source
                ms.Seek(count, SeekOrigin.Current);
                return true;
            }
            data = default;
            return false;
        }
    }
}
