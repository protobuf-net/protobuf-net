
namespace ProtoBuf
{
    internal sealed partial class TwosComplementSerializer
    {
        private TwosComplementSerializer() { }
        public static readonly TwosComplementSerializer Default = new TwosComplementSerializer();
        public WireType WireType { get { return WireType.Variant; } }

    }
}
