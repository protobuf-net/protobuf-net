using System;

namespace ProtoBuf
{

    public interface IAllocator<T> : IDisposable
    {
        T Allocate(SerializationContext context, int length);
        void Release(SerializationContext context, T value);
    }
}
