#nullable enable
using System.Collections.Generic;

namespace ProtoBuf.Reflection.Internal.CodeGen.Error;

internal sealed class CodeGenErrorContainer
{
    public ICollection<CodeGenError> Errors { get; } = new List<CodeGenError>();

    /// <summary>
    /// Saves FATAL error in a container.
    /// Always returns null to help generator code respond with non-built type.
    /// </summary>
    /// <param name="errorDescription">manual description of what has happened to fire an error</param>
    /// <param name="symbolType">type of symbol, which has an error</param>
    /// <param name="symbolLocation">location of symbol, which has an error</param>
    public TCodeGen? SaveFatal<TCodeGen>(string errorDescription, string symbolType, string symbolLocation)
        where TCodeGen : class
    {
        return Save<TCodeGen>(CodeGenErrorLevel.Fatal, errorDescription, symbolType, symbolLocation);
    }
    
    /// <summary>
    /// Saves WARNING error in a container.
    /// Always returns null to help generator code respond with non-built type.
    /// </summary>
    /// <param name="errorDescription">manual description of what has happened to fire an error</param>
    /// <param name="symbolType">type of symbol, which has an error</param>
    /// <param name="symbolLocation">location of symbol, which has an error</param>
    public TCodeGen? SaveWarning<TCodeGen>(string errorDescription, string symbolType, string symbolLocation)
        where TCodeGen : class
    {
        return Save<TCodeGen>(CodeGenErrorLevel.Warning, errorDescription, symbolType, symbolLocation);
    }
    
    /// <summary>
    /// Saves error in a container.
    /// Always returns null to help generator code respond with non-built type.
    /// </summary>
    /// <param name="errorLevel">level of error. Can be fatal (will be thrown), warning (probably useful to output) and etc</param>
    /// <param name="errorDescription">manual description of what has happened to fire an error</param>
    /// <param name="symbolType">type of symbol, which has an error</param>
    /// <param name="symbolLocation">location of symbol, which has an error</param>
    public TCodeGen? Save<TCodeGen>(CodeGenErrorLevel errorLevel, string errorDescription, string symbolType, string symbolLocation)
        where TCodeGen : class
    {
        Errors.Add(new CodeGenError
        {
            Level = errorLevel,
            SymbolType = symbolType,
            Location = symbolLocation,
            Description = errorDescription
        });
        
        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Throw()
    {
        
    }
}
