using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoBuf
{
    public struct SubItemToken
    {
        internal readonly int value;
        internal SubItemToken(int value) {
            this.value = value;
        }
    }
}
