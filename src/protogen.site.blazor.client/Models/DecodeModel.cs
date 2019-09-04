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

        public string AsHex(int offset, int count) => ContainsValue ? BitConverter.ToString(data.Array, data.Offset + offset, count) : null;
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
        public ProtoReader GetReader(out ProtoReader.State state)
        {
            var ms = new MemoryStream(data.Array, data.Offset, data.Count, false);
            return ProtoReader.Create(out state, ms, null, null);
        }
        public bool ContainsValue => data.Array != null;
        public bool CouldBeProto()
        {
            if (!ContainsValue) return false;
            try
            {
                using (var reader = GetReader(out var state))
                {
                    int field;
                    while ((field = reader.ReadFieldHeader(ref state)) > 0)
                    {
                        reader.SkipField(ref state);
                    }
                    return reader.GetPosition(ref state) == Count; // MemoryStream will let you seek out of bounds!
                }
            }
            catch
            {
                return false;
            }
        }
        public DecodeModel Slice(int offset, int count, int skipField = 0) => new DecodeModel(data.Array, Deep, data.Offset + offset, count, skipField);
    }
}
