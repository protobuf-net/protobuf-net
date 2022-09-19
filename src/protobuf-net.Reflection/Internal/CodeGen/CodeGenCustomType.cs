using ProtoBuf.Reflection.Internal.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtoBuf.Internal.CodeGen
{
    internal class CodeGenCustomType : CodeGenType
    {
        public CodeGenCustomType(string name, string fullyQualifiedPrefix)
            : base(name, fullyQualifiedPrefix)
        {
        }
    }
}
