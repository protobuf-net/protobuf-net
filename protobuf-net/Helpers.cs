
namespace ProtoBuf
{
    internal class Helpers
    {
        private Helpers() { }
        public static bool IsNullOrEmpty(string value)
        { // yes, FX11 lacks this!
            return value == null || value.Length == 0;
        }
    }
}
