using System;
using System.Buffers;

namespace ProtoBuf.Internal
{
    internal sealed unsafe class FixedMemoryManager : MemoryManager<byte>
    {
        private byte* _pointer;
        private int _length;

        internal Memory<byte> Init(byte* pointer, int length)
        {
            _pointer = pointer;
            _length = length;
            return Memory;
        }

        public override Span<byte> GetSpan() => new Span<byte>(_pointer, _length);

        public override MemoryHandle Pin(int elementIndex = 0)
            => throw new NotSupportedException();

        public override void Unpin()
            => throw new NotSupportedException();

        protected override void Dispose(bool disposing) { }
    }
}
