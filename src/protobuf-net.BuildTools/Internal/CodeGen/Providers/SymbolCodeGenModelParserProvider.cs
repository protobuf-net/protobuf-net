using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGen.Abstractions;
using ProtoBuf.Internal.CodeGen.Parsers;
using ProtoBuf.Reflection.Internal.CodeGen;
using ProtoBuf.Reflection.Internal.CodeGen.Error;

namespace ProtoBuf.Internal.CodeGen.Providers;

internal class SymbolCodeGenModelParserProvider
{
    private ISymbolCodeGenModelParser<INamedTypeSymbol, object?> _namedTypeParser;
    private ISymbolCodeGenModelParser<ITypeSymbol, CodeGenMessage> _messageParser;
    private ISymbolCodeGenModelParser<ITypeSymbol, CodeGenEnum> _enumParser;
    private ISymbolCodeGenModelParser<ITypeSymbol, CodeGenService> _serviceParser;
    private ISymbolCodeGenModelParser<IMethodSymbol, CodeGenServiceMethod> _serviceMethodParser;
    private ISymbolCodeGenModelParser<IPropertySymbol, CodeGenEnum> _enumPropertyParser;
    private ISymbolCodeGenModelParser<IPropertySymbol, CodeGenField> _fieldPropertyParser;
    private ISymbolCodeGenModelParser<IFieldSymbol, CodeGenEnumValue> _enumValueParser;

    public CodeGenParseContext ParseContext { get; }
    public CodeGenErrorContainer ErrorContainer { get; }

    public SymbolCodeGenModelParserProvider()
    {
        ParseContext = new();
        ErrorContainer = new();
    }

    public ISymbolCodeGenModelParser<INamedTypeSymbol, object?> GetNamedTypeParser()
    {
        return _namedTypeParser ??= new NamedTypeCodeGenModelParser(this);
    }

    public ISymbolCodeGenModelParser<ITypeSymbol, CodeGenMessage> GetMessageParser()
    {
        return _messageParser ??= new MessageCodeGenModelParser(this);
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
    
    public ISymbolCodeGenModelParser<ITypeSymbol, CodeGenService> GetServiceParser()
    {
        return _serviceParser ??= new ServiceCodeGenModelParser(this);
    }

    public ISymbolCodeGenModelParser<IMethodSymbol, CodeGenServiceMethod> GetServiceMethodParser()
    {
        return _serviceMethodParser ??= new ServiceMethodCodeGenModelParser(this);
    }
}