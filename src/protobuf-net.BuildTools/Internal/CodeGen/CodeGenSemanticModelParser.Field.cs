#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Parsers;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;

namespace ProtoBuf.Internal.CodeGen;

internal partial class CodeGenSemanticModelParser
{
    private CodeGenField ParseField(IPropertySymbol symbol)
    {

        var codeGenField = new CodeGenField(0, symbol.Name, symbol);
        Configure(symbol, codeGenField);
        return codeGenField;
    }

    sealed class StateBag
    {
        public CodeGenField Field { get; }
        public DataFormat? DataFormat { get; set; }
        public StateBag(CodeGenField field) => Field = field;
    }
    private void Configure(IPropertySymbol symbol, CodeGenField field)
    {
        var bag = new StateBag(field);
        foreach (var attrib in symbol.GetAttributes())
        {
            var ac = attrib.AttributeClass;
            if (ac is null) continue;

            switch (ac.Name)
            {
                case nameof(ProtoMemberAttribute) when ac.InProtoBufNamespace():
                    ParseAttribute(symbol, attrib, bag, TryParseProtoMemberAttribute);
                    break;
                case nameof(ObsoleteAttribute) when ac.InNamespace("System"):
                    field.IsDeprecated = true;
                    break;
                default:
                    if (ac.InProtoBufNamespace())
                    {
                        ReportDiagnostic(ParseUtils.UnhandledAttribute, symbol, ac.Name);
                    }
                    break;
            }
        }
        field.Type = symbol.Type.ResolveCodeGenType(bag.DataFormat, Context, out var repeated, symbol);
        field.Repeated = repeated;

        static bool TryParseProtoMemberAttribute(string name, bool ctor, StateBag bag, TypedConstant value)
        {
            switch (name)
            {
                case "tag" when ctor && value.TryGetInt32(out var i32):
                    bag.Field.FieldNumber = i32;
                    return true;
                case nameof(ProtoMemberAttribute.Name) when value.TryGetString(out var s):
                    bag.Field.OriginalName = s;
                    return true;
                case nameof(ProtoMemberAttribute.DataFormat) when value.TryGetInt32(out var i32):
                    bag.DataFormat = (DataFormat)i32;
                    break;
                case nameof(ProtoMemberAttribute.IsRequired) when value.TryGetBoolean(out var boolValue):
                    bag.Field.IsRequired = boolValue;
                    break;
            }
            return false;
        }
    }
}