using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtoBuf.Models
{
    public class DecodeModel
    {
        private ArraySegment<byte> data;
        public bool Deep { get; }

        public int SkipField { get; }

        private DecodeModel(byte[] data, bool deep, int offset, int count, int skipField = 0)
        {
            this.data = data == null
                ? default
                : new ArraySegment<byte>(data, offset, count);
            Deep = deep;
            SkipField = skipField;
        }
        public DecodeModel(byte[] data, bool deep) : this(data, deep, 0, data?.Length ?? 0) { }

        public string AsHex() => ContainsValue ? BitConverter.ToString(data.Array, data.Offset, data.Count) : null;

        public string AsHex(long offset, long count) => ContainsValue ? BitConverter.ToString(data.Array,
            checked((int)(data.Offset + offset)), checked((int)count)) : null;
        public string AsBase64() => ContainsValue ? Convert.ToBase64String(data.Array, data.Offset, data.Count) : null;
        public string AsString()
        {
            try
            {
                return Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
            }
            catch { return null; }
        }
        public int Count => data.Count;
        public ProtoReader GetReader()
        {
            var ms = new MemoryStream(data.Array, data.Offset, data.Count, false);
            return ProtoReader.Create(ms, null, null);
        }
        public bool ContainsValue => data.Array != null;
        public bool CouldBeProto()
        {
            if (!ContainsValue) return false;
            try
            {
                using (var reader = GetReader())
                {
                    int field;
                    while ((field = reader.ReadFieldHeader()) > 0)
                    {
                        reader.SkipField();
                    }
                    return reader.Position == Count; // MemoryStream will let you seek out of bounds!
                }
            }
            catch
            {
                return false;
            }
        }
        public DecodeModel Slice(long offset, long count, int skipField = 0) => new DecodeModel(data.Array, Deep,
            checked((int)(data.Offset + offset)), checked((int)count), skipField);
    }
}
