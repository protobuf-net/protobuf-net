using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGen.Abstractions;
using ProtoBuf.Internal.CodeGen.Models;
using ProtoBuf.Internal.CodeGen.Parsers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Providers;

internal class SymbolCodeGenModelParserProvider
{
    private ISymbolCodeGenModelParser<INamespaceSymbol, CodeGenFile> _namespaceParser; 
    private ISymbolCodeGenModelParser<ITypeSymbol, CodeGenMessage> _messageParser; 
    private ISymbolCodeGenModelParser<ITypeSymbol, CodeGenEnum> _enumParser;
    private ISymbolCodeGenModelParser<IPropertySymbol, CodeGenEnum> _enumPropertyParser;
    private ISymbolCodeGenModelParser<IPropertySymbol, CodeGenField> _fieldPropertyParser; 
    private ISymbolCodeGenModelParser<IFieldSymbol, CodeGenEnumValue> _enumValueParser; 
    
    public CodeGenParseContext CodeGenParseContext { get; }
    
    public SymbolCodeGenModelParserProvider()
    {
        CodeGenParseContext = new CodeGenParseContext();
    }
    
    public ISymbolCodeGenModelParser<INamespaceSymbol, CodeGenFile> GetNamespaceParser()
    {
        return _namespaceParser ??= new NamespaceCodeGenModelParser(this);
    }

    public ISymbolCodeGenModelParser<ITypeSymbol, CodeGenMessage> GetMessageParser(CodeGenNamespaceParseContext namespaceParseContext)
    {
        return _messageParser ??= new MessageCodeGenModelParser(this, namespaceParseContext);
    }
    
    public ISymbolCodeGenModelParser<ITypeSymbol, CodeGenEnum> GetEnumParser()
    {
        return _enumParser ??= new EnumCodeGenModelParser(this);
    }
    
    public ISymbolCodeGenModelParser<IPropertySymbol, CodeGenField> GetFieldParser()
    {
        return _fieldPropertyParser ??= new FieldPropertyCodeGenModelParser(this);
    }
    
    public ISymbolCodeGenModelParser<IPropertySymbol, CodeGenEnum> GetEnumPropertyParser()
    {
        return _enumPropertyParser ??= new EnumPropertyCodeGenModelParser(this);
    }
    
    public ISymbolCodeGenModelParser<IFieldSymbol, CodeGenEnumValue> GetEnumValueParser()
    {
        return _enumValueParser ??= new EnumValueCodeGenModelParser(this);
    }
}