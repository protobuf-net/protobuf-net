using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoBuf
{
    internal sealed partial class FixedSerializer : ISerializer<int>, ISerializer<long>, ISerializer<double>
    {
        public static readonly FixedSerializer Default = new FixedSerializer();
        private FixedSerializer()
        {
        }
    }
}
