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
        public DecodeModel(byte[] data)
        {
            Data = data;

        }

        public ProtoReader GetReader()
        {
            var ms = new MemoryStream(Data, 0, Data.Length, false);
#pragma warning disable CS0618
            return ProtoReader.Create(ms, null, null);
#pragma warning restore CS0618

        }

        public byte[] Data { get; private set; }

    }
}
