using ProtoBuf.Nano;
using System;
using System.Collections.Generic;

namespace ProtoBuf.Nano.Bench.DescriptorModel;

// The descriptor.proto object model, hand-written in the shape the nano generator pass emits -
// this is the north-star milestone artifact (docs/nano-core.md), meant to be read top-to-bottom:
// value-in/value-out statics, ??= construction, tag-local dispatch, repeated fields as run loops
// (the tag read as the do-while condition, miss handed back to dispatch via continue), packed
// scalars as a length scope drained on AtScopeEnd with the unpacked run as a sibling label, and
// the group sentinel in the switch default.
//
// The field set mirrors protobuf-net.Reflection's Descriptor.cs exactly (the legacy row
// deserializes those DTOs), with nullable scalars where that DTO tracks presence. Two deliberate
// abbreviations against full generator output, neither of which affects measurement: scalar
// wire-type tolerance labels are omitted (measured free on net10, MatrixResults.md - unhit labels
// only deepen an unhit search), and message fields carry no group-framing label (the payload
// writer emits length-prefixed; a group-framed document would fall to SkipTag, which throws on
// unexpected groups rather than mis-parsing).

public sealed class FileDescriptorSet
{
    public List<FileDescriptorProto> Files { get; } = [];
}

public sealed class FileDescriptorProto
{
    public string Name;
    public string Package;
    public List<string> Dependencies { get; } = [];
    public List<int> PublicDependencies { get; } = [];
    public List<int> WeakDependencies { get; } = [];
    public List<DescriptorProto> MessageTypes { get; } = [];
    public List<EnumDescriptorProto> EnumTypes { get; } = [];
    public List<ServiceDescriptorProto> Services { get; } = [];
    public List<FieldDescriptorProto> Extensions { get; } = [];
    public FileOptions Options;
    public SourceCodeInfo SourceCodeInfo;
    public string Syntax;
}

public sealed class DescriptorProto
{
    public string Name;
    public List<FieldDescriptorProto> Fields { get; } = [];
    public List<FieldDescriptorProto> Extensions { get; } = [];
    public List<DescriptorProto> NestedTypes { get; } = [];
    public List<EnumDescriptorProto> EnumTypes { get; } = [];
    public List<ExtensionRange> ExtensionRanges { get; } = [];
    public List<OneofDescriptorProto> OneofDecls { get; } = [];
    public MessageOptions Options;
    public List<ReservedRange> ReservedRanges { get; } = [];
    public List<string> ReservedNames { get; } = [];
}

public sealed class ExtensionRange
{
    public int? Start, End;
    public ExtensionRangeOptions Options;
}

public sealed class ReservedRange
{
    public int? Start, End;
}

public sealed class ExtensionRangeOptions
{
    public List<UninterpretedOption> UninterpretedOptions { get; } = [];
}

public sealed class FieldDescriptorProto
{
    public string Name;
    public int? Number;
    public int? Label; // FieldDescriptorProto.Label
    public int? Type;  // FieldDescriptorProto.Type
    public string TypeName;
    public string Extendee;
    public string DefaultValue;
    public int? OneofIndex;
    public string JsonName;
    public FieldOptions Options;
    public bool? Proto3Optional;
}

public sealed class OneofDescriptorProto
{
    public string Name;
    public OneofOptions Options;
}

public sealed class EnumDescriptorProto
{
    public string Name;
    public List<EnumValueDescriptorProto> Values { get; } = [];
    public EnumOptions Options;
    public List<EnumReservedRange> ReservedRanges { get; } = [];
    public List<string> ReservedNames { get; } = [];
}

public sealed class EnumReservedRange
{
    public int? Start, End;
}

public sealed class EnumValueDescriptorProto
{
    public string Name;
    public int? Number;
    public EnumValueOptions Options;
}

public sealed class ServiceDescriptorProto
{
    public string Name;
    public List<MethodDescriptorProto> Methods { get; } = [];
    public ServiceOptions Options;
}

public sealed class MethodDescriptorProto
{
    public string Name;
    public string InputType;
    public string OutputType;
    public MethodOptions Options;
    public bool? ClientStreaming, ServerStreaming;
}

public sealed class FileOptions
{
    public string JavaPackage;              // 1
    public string JavaOuterClassname;       // 8
    public bool? JavaMultipleFiles;         // 10
    public bool? JavaGenerateEqualsAndHash; // 20
    public bool? JavaStringCheckUtf8;       // 27
    public int? OptimizeFor;                // 9 (OptimizeMode)
    public string GoPackage;                // 11
    public bool? CcGenericServices;         // 16
    public bool? JavaGenericServices;       // 17
    public bool? PyGenericServices;         // 18
    public bool? PhpGenericServices;        // 42
    public bool? Deprecated;                // 23
    public bool? CcEnableArenas;            // 31
    public string ObjcClassPrefix;          // 36
    public string CsharpNamespace;          // 37
    public string SwiftPrefix;              // 39
    public string PhpClassPrefix;           // 40
    public string PhpNamespace;             // 41
    public string PhpMetadataNamespace;     // 44
    public string RubyPackage;              // 45
    public List<UninterpretedOption> UninterpretedOptions { get; } = []; // 999
}

public sealed class MessageOptions
{
    public bool? MessageSetWireFormat;           // 1
    public bool? NoStandardDescriptorAccessor;   // 2
    public bool? Deprecated;                     // 3
    public bool? MapEntry;                       // 7
    public List<UninterpretedOption> UninterpretedOptions { get; } = []; // 999
}

public sealed class FieldOptions
{
    public int? Ctype;      // 1 (CType)
    public bool? Packed;    // 2
    public int? Jstype;     // 6 (JSType)
    public bool? Lazy;      // 5
    public bool? Deprecated;// 3
    public bool? Weak;      // 10
    public List<UninterpretedOption> UninterpretedOptions { get; } = []; // 999
}

public sealed class OneofOptions
{
    public List<UninterpretedOption> UninterpretedOptions { get; } = []; // 999
}

public sealed class EnumOptions
{
    public bool? AllowAlias;  // 2
    public bool? Deprecated;  // 3
    public List<UninterpretedOption> UninterpretedOptions { get; } = []; // 999
}

public sealed class EnumValueOptions
{
    public bool? Deprecated;  // 1
    public List<UninterpretedOption> UninterpretedOptions { get; } = []; // 999
}

public sealed class ServiceOptions
{
    public bool? Deprecated;  // 33
    public List<UninterpretedOption> UninterpretedOptions { get; } = []; // 999
}

public sealed class MethodOptions
{
    public bool? Deprecated;       // 33
    public int? IdempotencyLevel;  // 34
    public List<UninterpretedOption> UninterpretedOptions { get; } = []; // 999
}

public sealed class UninterpretedOption
{
    public List<NamePart> Names { get; } = []; // 2
    public string IdentifierValue;             // 3
    public ulong? PositiveIntValue;            // 4
    public long? NegativeIntValue;             // 5
    public double? DoubleValue;                // 6
    public byte[] StringValue;                 // 7
    public string AggregateValue;              // 8
}

public sealed class NamePart
{
    public string Name;        // 1, required
    public bool? IsExtension;  // 2, required
}

public sealed class SourceCodeInfo
{
    public List<Location> Locations { get; } = []; // 1
}

public sealed class Location
{
    public List<int> Path { get; } = [];  // 1, packed
    public List<int> Span { get; } = [];  // 2, packed
    public string LeadingComments;        // 3
    public string TrailingComments;       // 4
    public List<string> LeadingDetachedComments { get; } = []; // 6
}

public static class DescriptorNanoReader
{
    public static FileDescriptorSet ReadFileDescriptorSet(ref ReaderState state, FileDescriptorSet value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.Files.Add(ReadFileDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((1 << 3) | 2));
                    continue;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static FileDescriptorProto ReadFileDescriptorProto(ref ReaderState state, FileDescriptorProto value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 2:
                    value.Name = state.ReadRawString();
                    break;
                case (2 << 3) | 2:
                    value.Package = state.ReadRawString();
                    break;
                case (3 << 3) | 2:
                    do { value.Dependencies.Add(state.ReadRawString()); }
                    while ((tag = state.ReadRawTag()) == ((3 << 3) | 2));
                    continue;
                case (10 << 3) | 0: // unpacked run
                    do { value.PublicDependencies.Add(unchecked((int)state.ReadRawVarint32())); }
                    while ((tag = state.ReadRawTag()) == ((10 << 3) | 0));
                    continue;
                case (10 << 3) | 2: // packed
                {
                    var scope = state.PushLengthPrefix();
                    while (!state.AtScopeEnd) value.PublicDependencies.Add(unchecked((int)state.ReadRawVarint32()));
                    state.PopScope(scope);
                    break;
                }
                case (11 << 3) | 0:
                    do { value.WeakDependencies.Add(unchecked((int)state.ReadRawVarint32())); }
                    while ((tag = state.ReadRawTag()) == ((11 << 3) | 0));
                    continue;
                case (11 << 3) | 2:
                {
                    var scope = state.PushLengthPrefix();
                    while (!state.AtScopeEnd) value.WeakDependencies.Add(unchecked((int)state.ReadRawVarint32()));
                    state.PopScope(scope);
                    break;
                }
                case (4 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.MessageTypes.Add(ReadDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((4 << 3) | 2));
                    continue;
                case (5 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.EnumTypes.Add(ReadEnumDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((5 << 3) | 2));
                    continue;
                case (6 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.Services.Add(ReadServiceDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((6 << 3) | 2));
                    continue;
                case (7 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.Extensions.Add(ReadFieldDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((7 << 3) | 2));
                    continue;
                case (8 << 3) | 2:
                {
                    var scope = state.PushLengthPrefix();
                    value.Options = ReadFileOptions(ref state, value.Options);
                    state.PopScope(scope);
                    break;
                }
                case (9 << 3) | 2:
                {
                    var scope = state.PushLengthPrefix();
                    value.SourceCodeInfo = ReadSourceCodeInfo(ref state, value.SourceCodeInfo);
                    state.PopScope(scope);
                    break;
                }
                case (12 << 3) | 2:
                    value.Syntax = state.ReadRawString();
                    break;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static DescriptorProto ReadDescriptorProto(ref ReaderState state, DescriptorProto value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 2:
                    value.Name = state.ReadRawString();
                    break;
                case (2 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.Fields.Add(ReadFieldDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((2 << 3) | 2));
                    continue;
                case (6 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.Extensions.Add(ReadFieldDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((6 << 3) | 2));
                    continue;
                case (3 << 3) | 2: // nested_type: the genuinely recursive dive
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.NestedTypes.Add(ReadDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((3 << 3) | 2));
                    continue;
                case (4 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.EnumTypes.Add(ReadEnumDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((4 << 3) | 2));
                    continue;
                case (5 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.ExtensionRanges.Add(ReadExtensionRange(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((5 << 3) | 2));
                    continue;
                case (8 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.OneofDecls.Add(ReadOneofDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((8 << 3) | 2));
                    continue;
                case (7 << 3) | 2:
                {
                    var scope = state.PushLengthPrefix();
                    value.Options = ReadMessageOptions(ref state, value.Options);
                    state.PopScope(scope);
                    break;
                }
                case (9 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.ReservedRanges.Add(ReadReservedRange(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((9 << 3) | 2));
                    continue;
                case (10 << 3) | 2:
                    do { value.ReservedNames.Add(state.ReadRawString()); }
                    while ((tag = state.ReadRawTag()) == ((10 << 3) | 2));
                    continue;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static ExtensionRange ReadExtensionRange(ref ReaderState state, ExtensionRange value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 0:
                    value.Start = unchecked((int)state.ReadRawVarint32());
                    break;
                case (2 << 3) | 0:
                    value.End = unchecked((int)state.ReadRawVarint32());
                    break;
                case (3 << 3) | 2:
                {
                    var scope = state.PushLengthPrefix();
                    value.Options = ReadExtensionRangeOptions(ref state, value.Options);
                    state.PopScope(scope);
                    break;
                }
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static ReservedRange ReadReservedRange(ref ReaderState state, ReservedRange value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 0:
                    value.Start = unchecked((int)state.ReadRawVarint32());
                    break;
                case (2 << 3) | 0:
                    value.End = unchecked((int)state.ReadRawVarint32());
                    break;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static ExtensionRangeOptions ReadExtensionRangeOptions(ref ReaderState state, ExtensionRangeOptions value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (999 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((999 << 3) | 2));
                    continue;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static FieldDescriptorProto ReadFieldDescriptorProto(ref ReaderState state, FieldDescriptorProto value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 2:
                    value.Name = state.ReadRawString();
                    break;
                case (3 << 3) | 0:
                    value.Number = unchecked((int)state.ReadRawVarint32());
                    break;
                case (4 << 3) | 0:
                    value.Label = unchecked((int)state.ReadRawVarint32());
                    break;
                case (5 << 3) | 0:
                    value.Type = unchecked((int)state.ReadRawVarint32());
                    break;
                case (6 << 3) | 2:
                    value.TypeName = state.ReadRawString();
                    break;
                case (2 << 3) | 2:
                    value.Extendee = state.ReadRawString();
                    break;
                case (7 << 3) | 2:
                    value.DefaultValue = state.ReadRawString();
                    break;
                case (9 << 3) | 0:
                    value.OneofIndex = unchecked((int)state.ReadRawVarint32());
                    break;
                case (10 << 3) | 2:
                    value.JsonName = state.ReadRawString();
                    break;
                case (8 << 3) | 2:
                {
                    var scope = state.PushLengthPrefix();
                    value.Options = ReadFieldOptions(ref state, value.Options);
                    state.PopScope(scope);
                    break;
                }
                case (17 << 3) | 0:
                    value.Proto3Optional = state.ReadRawVarint32() != 0;
                    break;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static OneofDescriptorProto ReadOneofDescriptorProto(ref ReaderState state, OneofDescriptorProto value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 2:
                    value.Name = state.ReadRawString();
                    break;
                case (2 << 3) | 2:
                {
                    var scope = state.PushLengthPrefix();
                    value.Options = ReadOneofOptions(ref state, value.Options);
                    state.PopScope(scope);
                    break;
                }
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static EnumDescriptorProto ReadEnumDescriptorProto(ref ReaderState state, EnumDescriptorProto value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 2:
                    value.Name = state.ReadRawString();
                    break;
                case (2 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.Values.Add(ReadEnumValueDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((2 << 3) | 2));
                    continue;
                case (3 << 3) | 2:
                {
                    var scope = state.PushLengthPrefix();
                    value.Options = ReadEnumOptions(ref state, value.Options);
                    state.PopScope(scope);
                    break;
                }
                case (4 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.ReservedRanges.Add(ReadEnumReservedRange(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((4 << 3) | 2));
                    continue;
                case (5 << 3) | 2:
                    do { value.ReservedNames.Add(state.ReadRawString()); }
                    while ((tag = state.ReadRawTag()) == ((5 << 3) | 2));
                    continue;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static EnumReservedRange ReadEnumReservedRange(ref ReaderState state, EnumReservedRange value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 0:
                    value.Start = unchecked((int)state.ReadRawVarint32());
                    break;
                case (2 << 3) | 0:
                    value.End = unchecked((int)state.ReadRawVarint32());
                    break;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static EnumValueDescriptorProto ReadEnumValueDescriptorProto(ref ReaderState state, EnumValueDescriptorProto value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 2:
                    value.Name = state.ReadRawString();
                    break;
                case (2 << 3) | 0:
                    value.Number = unchecked((int)state.ReadRawVarint32());
                    break;
                case (3 << 3) | 2:
                {
                    var scope = state.PushLengthPrefix();
                    value.Options = ReadEnumValueOptions(ref state, value.Options);
                    state.PopScope(scope);
                    break;
                }
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static ServiceDescriptorProto ReadServiceDescriptorProto(ref ReaderState state, ServiceDescriptorProto value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 2:
                    value.Name = state.ReadRawString();
                    break;
                case (2 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.Methods.Add(ReadMethodDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((2 << 3) | 2));
                    continue;
                case (3 << 3) | 2:
                {
                    var scope = state.PushLengthPrefix();
                    value.Options = ReadServiceOptions(ref state, value.Options);
                    state.PopScope(scope);
                    break;
                }
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static MethodDescriptorProto ReadMethodDescriptorProto(ref ReaderState state, MethodDescriptorProto value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 2:
                    value.Name = state.ReadRawString();
                    break;
                case (2 << 3) | 2:
                    value.InputType = state.ReadRawString();
                    break;
                case (3 << 3) | 2:
                    value.OutputType = state.ReadRawString();
                    break;
                case (4 << 3) | 2:
                {
                    var scope = state.PushLengthPrefix();
                    value.Options = ReadMethodOptions(ref state, value.Options);
                    state.PopScope(scope);
                    break;
                }
                case (5 << 3) | 0:
                    value.ClientStreaming = state.ReadRawVarint32() != 0;
                    break;
                case (6 << 3) | 0:
                    value.ServerStreaming = state.ReadRawVarint32() != 0;
                    break;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static FileOptions ReadFileOptions(ref ReaderState state, FileOptions value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 2:
                    value.JavaPackage = state.ReadRawString();
                    break;
                case (8 << 3) | 2:
                    value.JavaOuterClassname = state.ReadRawString();
                    break;
                case (10 << 3) | 0:
                    value.JavaMultipleFiles = state.ReadRawVarint32() != 0;
                    break;
                case (20 << 3) | 0:
                    value.JavaGenerateEqualsAndHash = state.ReadRawVarint32() != 0;
                    break;
                case (27 << 3) | 0:
                    value.JavaStringCheckUtf8 = state.ReadRawVarint32() != 0;
                    break;
                case (9 << 3) | 0:
                    value.OptimizeFor = unchecked((int)state.ReadRawVarint32());
                    break;
                case (11 << 3) | 2:
                    value.GoPackage = state.ReadRawString();
                    break;
                case (16 << 3) | 0:
                    value.CcGenericServices = state.ReadRawVarint32() != 0;
                    break;
                case (17 << 3) | 0:
                    value.JavaGenericServices = state.ReadRawVarint32() != 0;
                    break;
                case (18 << 3) | 0:
                    value.PyGenericServices = state.ReadRawVarint32() != 0;
                    break;
                case (42 << 3) | 0:
                    value.PhpGenericServices = state.ReadRawVarint32() != 0;
                    break;
                case (23 << 3) | 0:
                    value.Deprecated = state.ReadRawVarint32() != 0;
                    break;
                case (31 << 3) | 0:
                    value.CcEnableArenas = state.ReadRawVarint32() != 0;
                    break;
                case (36 << 3) | 2:
                    value.ObjcClassPrefix = state.ReadRawString();
                    break;
                case (37 << 3) | 2:
                    value.CsharpNamespace = state.ReadRawString();
                    break;
                case (39 << 3) | 2:
                    value.SwiftPrefix = state.ReadRawString();
                    break;
                case (40 << 3) | 2:
                    value.PhpClassPrefix = state.ReadRawString();
                    break;
                case (41 << 3) | 2:
                    value.PhpNamespace = state.ReadRawString();
                    break;
                case (44 << 3) | 2:
                    value.PhpMetadataNamespace = state.ReadRawString();
                    break;
                case (45 << 3) | 2:
                    value.RubyPackage = state.ReadRawString();
                    break;
                case (999 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((999 << 3) | 2));
                    continue;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static MessageOptions ReadMessageOptions(ref ReaderState state, MessageOptions value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 0:
                    value.MessageSetWireFormat = state.ReadRawVarint32() != 0;
                    break;
                case (2 << 3) | 0:
                    value.NoStandardDescriptorAccessor = state.ReadRawVarint32() != 0;
                    break;
                case (3 << 3) | 0:
                    value.Deprecated = state.ReadRawVarint32() != 0;
                    break;
                case (7 << 3) | 0:
                    value.MapEntry = state.ReadRawVarint32() != 0;
                    break;
                case (999 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((999 << 3) | 2));
                    continue;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static FieldOptions ReadFieldOptions(ref ReaderState state, FieldOptions value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 0:
                    value.Ctype = unchecked((int)state.ReadRawVarint32());
                    break;
                case (2 << 3) | 0:
                    value.Packed = state.ReadRawVarint32() != 0;
                    break;
                case (6 << 3) | 0:
                    value.Jstype = unchecked((int)state.ReadRawVarint32());
                    break;
                case (5 << 3) | 0:
                    value.Lazy = state.ReadRawVarint32() != 0;
                    break;
                case (3 << 3) | 0:
                    value.Deprecated = state.ReadRawVarint32() != 0;
                    break;
                case (10 << 3) | 0:
                    value.Weak = state.ReadRawVarint32() != 0;
                    break;
                case (999 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((999 << 3) | 2));
                    continue;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static OneofOptions ReadOneofOptions(ref ReaderState state, OneofOptions value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (999 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((999 << 3) | 2));
                    continue;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static EnumOptions ReadEnumOptions(ref ReaderState state, EnumOptions value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (2 << 3) | 0:
                    value.AllowAlias = state.ReadRawVarint32() != 0;
                    break;
                case (3 << 3) | 0:
                    value.Deprecated = state.ReadRawVarint32() != 0;
                    break;
                case (999 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((999 << 3) | 2));
                    continue;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static EnumValueOptions ReadEnumValueOptions(ref ReaderState state, EnumValueOptions value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 0:
                    value.Deprecated = state.ReadRawVarint32() != 0;
                    break;
                case (999 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((999 << 3) | 2));
                    continue;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static ServiceOptions ReadServiceOptions(ref ReaderState state, ServiceOptions value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (33 << 3) | 0:
                    value.Deprecated = state.ReadRawVarint32() != 0;
                    break;
                case (999 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((999 << 3) | 2));
                    continue;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static MethodOptions ReadMethodOptions(ref ReaderState state, MethodOptions value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (33 << 3) | 0:
                    value.Deprecated = state.ReadRawVarint32() != 0;
                    break;
                case (34 << 3) | 0:
                    value.IdempotencyLevel = unchecked((int)state.ReadRawVarint32());
                    break;
                case (999 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((999 << 3) | 2));
                    continue;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static UninterpretedOption ReadUninterpretedOption(ref ReaderState state, UninterpretedOption value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (2 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.Names.Add(ReadNamePart(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((2 << 3) | 2));
                    continue;
                case (3 << 3) | 2:
                    value.IdentifierValue = state.ReadRawString();
                    break;
                case (4 << 3) | 0:
                    value.PositiveIntValue = state.ReadRawVarint64();
                    break;
                case (5 << 3) | 0:
                    value.NegativeIntValue = unchecked((long)state.ReadRawVarint64());
                    break;
                case (6 << 3) | 1:
                    value.DoubleValue = BitConverter.Int64BitsToDouble(unchecked((long)state.ReadRawFixed64()));
                    break;
                case (7 << 3) | 2:
                    value.StringValue = state.ReadRawBytes();
                    break;
                case (8 << 3) | 2:
                    value.AggregateValue = state.ReadRawString();
                    break;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static NamePart ReadNamePart(ref ReaderState state, NamePart value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 2:
                    value.Name = state.ReadRawString();
                    break;
                case (2 << 3) | 0:
                    value.IsExtension = state.ReadRawVarint32() != 0;
                    break;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static SourceCodeInfo ReadSourceCodeInfo(ref ReaderState state, SourceCodeInfo value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 2:
                    do
                    {
                        var scope = state.PushLengthPrefix();
                        value.Locations.Add(ReadLocation(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == ((1 << 3) | 2));
                    continue;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }

    public static Location ReadLocation(ref ReaderState state, Location value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 2: // path: packed (the declared form)
                {
                    var scope = state.PushLengthPrefix();
                    while (!state.AtScopeEnd) value.Path.Add(unchecked((int)state.ReadRawVarint32()));
                    state.PopScope(scope);
                    break;
                }
                case (1 << 3) | 0: // path: unpacked run (tolerated)
                    do { value.Path.Add(unchecked((int)state.ReadRawVarint32())); }
                    while ((tag = state.ReadRawTag()) == ((1 << 3) | 0));
                    continue;
                case (2 << 3) | 2:
                {
                    var scope = state.PushLengthPrefix();
                    while (!state.AtScopeEnd) value.Span.Add(unchecked((int)state.ReadRawVarint32()));
                    state.PopScope(scope);
                    break;
                }
                case (2 << 3) | 0:
                    do { value.Span.Add(unchecked((int)state.ReadRawVarint32())); }
                    while ((tag = state.ReadRawTag()) == ((2 << 3) | 0));
                    continue;
                case (3 << 3) | 2:
                    value.LeadingComments = state.ReadRawString();
                    break;
                case (4 << 3) | 2:
                    value.TrailingComments = state.ReadRawString();
                    break;
                case (6 << 3) | 2:
                    do { value.LeadingDetachedComments.Add(state.ReadRawString()); }
                    while ((tag = state.ReadRawTag()) == ((6 << 3) | 2));
                    continue;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
            tag = state.ReadRawTag();
        }
        return value;
    }
}
