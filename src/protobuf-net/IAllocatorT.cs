namespace ProtoBuf
{

    public interface IAllocator<T>
    {
        T Allocate(SerializationContext context, int length);
        void Release(SerializationContext context, T value);
    }
}
