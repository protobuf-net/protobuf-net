using System.IO;

namespace ProtoBuf.Models
{
    public class DecodeModel
    {
        public DecodeModel(byte[] data, bool showFullStrings)
        {
            Data = data;
            ShowFullStrings = showFullStrings;
        }

        public ProtoReader GetReader()
        {
            var ms = new MemoryStream(Data, 0, Data.Length, false);
#pragma warning disable CS0618
            return ProtoReader.Create(ms, null, null);
#pragma warning restore CS0618

        }

        public byte[] Data { get; }

        public bool ShowFullStrings { get; }

    }
}
