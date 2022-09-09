using Google.Protobuf.Reflection;
using ProtoBuf.Reflection;
using ProtoBuf.Reflection.Internal;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;
using System.Collections.Generic;
using System.IO;

namespace ProtoBuf.Reflection.Internal.CodeGen;

/// <summary>
/// Abstract root for a general purpose code-generator
/// </summary>
internal abstract class CodeGenCodeGenerator
{
    /// <summary>
    /// The logical name of this code generator
    /// </summary>
    public abstract string Name { get; }
    /// <summary>
    /// Get a string representation of the instance
    /// </summary>
    public override string ToString() => Name;

    /// <summary>
    /// Execute the code generator against a FileDescriptorSet, yielding a sequence of files
    /// </summary>
    public abstract IEnumerable<CodeFile> Generate(CodeGenSet set, Dictionary<string, string> options = null);

    /// <summary>
    /// Eexecute this code generator against a code file
    /// </summary>
    public CompilerResult Compile(CodeFile file) => Compile(new[] { file });
    /// <summary>
    /// Eexecute this code generator against a set of code file
    /// </summary>
    public CompilerResult Compile(params CodeFile[] files)
    {
        var set = new FileDescriptorSet();
        foreach (var file in files)
        {
            using var reader = new StringReader(file.Text);
#if DEBUG_COMPILE
                Console.WriteLine($"Parsing {file.Name}...");
#endif
            set.Add(file.Name, true, reader);
        }
        set.Process();
        var results = new List<CodeFile>();
        var ctx = new CodeGenParseContext();
        var cgSet = CodeGenSet.Parse(set, ctx);

        try
        {
            results.AddRange(Generate(cgSet));
        }
        catch (Exception ex)
        {
            var errorCode = ex is ParserException pe ? pe.ErrorCode : ErrorCode.Undefined;
            set.Errors.Add(new Error(default, ex.Message, true, errorCode));
        }
        var errors = set.GetErrors();

        return new CompilerResult(errors, results.ToArray());
    }
}