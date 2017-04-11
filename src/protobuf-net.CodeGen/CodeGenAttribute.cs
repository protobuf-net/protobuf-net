using CodeGeneration.Roslyn;
using System;
using System.Diagnostics;

namespace ProtoBuf.CodeGen
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    [CodeGenerationAttribute(typeof(CodeGenGenerator))]
    [Conditional("CodeGeneration")]
    public class CodeGenAttribute : Attribute
    { }
}
