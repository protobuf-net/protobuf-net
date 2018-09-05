namespace ProtoBuf
{
    public interface IStringSerializer
    {
        string ReadString(byte[] buffer, int offset, int count);
        
        int GetLength(string value);

        void WriteString(string value, byte[] buffer, int offset);
    }

    public static class StringSerializer 
    {
        public static IStringSerializer Instance { get; private set; }

        static StringSerializer()
        {
            Instance = new DefaultSerializer();
        }


        public static void SetSerializer(IStringSerializer instance)
        {
            if (instance == null)
            {
                throw new ArgumentException();
            }

            Instance = instance;
        }

        private class DefaultSerializer : IStringSerializer
        {
            public String ReadString(Byte[] buffer, Int32 offset, Int32 count)
            {
                return Encoding.UTF8.GetString(buffer, offset, count);
            }

            public Int32 GetLength(String value)
            {
                return Encoding.UTF8.GetByteCount(value);
            }

            public Int32 WriteString(String value, Byte[] buffer, Int32 offset)
            {
                return Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, offset);
            }
        }
    }
}