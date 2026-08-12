using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;

namespace ProtoBuf
{
    public partial class ProtoReader
    {
        /// <summary>
        /// Get the default state associated with this reader
        /// </summary>
        protected internal abstract State DefaultState();

        /// <summary>
        /// Holds state used by the deserializer
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        public ref partial struct State
        {
            // the storage lives in Nano/ProtoReader.State.NanoCore.cs: this shell holds the
            // construction entry points and the solid/liquid plumbing. The old class-backend
            // window (Span/OffsetInCurrent/Consume and friends) is GONE - see PORTING.md.

            /// <summary>
            /// Creates a reader over the supplied buffer
            /// </summary>
            public static State Create(ReadOnlyMemory<byte> source, TypeModel model, object userState = null)
            {
                var state = new State(source);
                state._model = model;
                state._userState = userState;
                return state;
            }

            /// <summary>
            /// Creates a reader over the supplied buffer
            /// </summary>
            public static State Create(ReadOnlySequence<byte> source, TypeModel model, object userState = null)
            {
                var state = new State(in source);
                state._model = model;
                state._userState = userState;
                return state;
            }

            /// <summary>
            /// Creates a reader over the supplied stream; length of TO_EOF (-1) consumes to the
            /// end of the stream, otherwise exactly that many bytes are the document
            /// </summary>
            public static State Create(Stream source, TypeModel model, object userState = null, long length = ProtoReader.TO_EOF)
            {
                // the MemoryStream unwrap, with full legacy parity: reflection fallback for
                // non-exposable buffers, position respected, length cap applied, and the source
                // SEEKED past the consumed data
                if (TryConsumeSegmentRespectingPosition(source, out var segment, length))
                {
                    var state = new State(segment.Array, segment.Offset, segment.Count);
                    state._model = model;
                    state._userState = userState;
                    return state;
                }
                var streamState = new State(source, length);
                streamState._model = model;
                streamState._userState = userState;
                return streamState;
            }

            internal readonly SolidState Solidify() => new SolidState(Snapshot());
        }

        [StructLayout(LayoutKind.Auto)]
        internal readonly struct SolidState : IDisposable
        {
            private readonly ReaderSnapshot _snapshot;
            internal SolidState(in ReaderSnapshot snapshot) => _snapshot = snapshot;
            internal State Liquify() => new State(in _snapshot);
            public void Dispose()
            {
                // a solidified state still owns any lease; disposal returns it exactly as the
                // liquid Dispose would
                var buffer = _snapshot.Buffer;
                if (_snapshot.Leased && buffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
    }
}
