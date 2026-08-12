using ProtoBuf.Nano;
using System;
using System.Collections.Generic;

namespace ProtoBuf.Nano.Bench.DescriptorModel;

// The descriptor.proto object model, hand-written in the shape the nano generator pass emits -
// this is the north-star milestone artifact (docs/nano-core.md), meant to be read top-to-bottom:
// value-in/value-out statics, ??= construction, tag-local dispatch, repeated fields as run loops
// (the tag read as the do-while condition, miss handed back to dispatch via continue), packed
// scalars as a length scope drained on AtScopeEnd with the unpacked run as a sibling label, the
// group sentinel in the switch default, and message fields accepting BOTH framings - length
// prefix or group, without prejudice, exactly as legacy always has - through paired case labels
// over one PushScope(tag) body.
//
// Review-shaped conventions (2026-08-12, the first human read):
// - every case label says what it is - "// options, field 7, group" - because comments are free
//   in every build configuration and the reader is the audience;
// - the DTOs expose auto-PROPERTIES, not fields: the generator's real targets are properties,
//   and a field-assigning benchmark would measure a capability the emitted code does not have;
// - enums are real enums (cast over varint, erased by the JIT), not annotated ints;
// - scalar fields carry full wire-type tolerance labels (varint/fixed32/fixed64), matching what
//   legacy accepts at runtime - measured against canonical-only labels on this very payload, see
//   DescriptorParseResults.md. The one exception is double (fixed64 only): a fixed32 float
//   promotion needs Int32BitsToSingle, which netfx lacks;
// - ??= construction at method entry is the SEALED-type shape; a [ProtoInclude] hierarchy must
//   defer construction until the sub-type marker or first member touch (see docs/nano-core.md).

public sealed class FileDescriptorSet
{
    public List<FileDescriptorProto> Files { get; } = [];
}

public sealed class FileDescriptorProto
{
    public string Name { get; set; }
    public string Package { get; set; }
    public List<string> Dependencies { get; } = [];
    public List<int> PublicDependencies { get; } = [];
    public List<int> WeakDependencies { get; } = [];
    public List<DescriptorProto> MessageTypes { get; } = [];
    public List<EnumDescriptorProto> EnumTypes { get; } = [];
    public List<ServiceDescriptorProto> Services { get; } = [];
    public List<FieldDescriptorProto> Extensions { get; } = [];
    public FileOptions Options { get; set; }
    public SourceCodeInfo SourceCodeInfo { get; set; }
    public string Syntax { get; set; }
}

public sealed class DescriptorProto
{
    public string Name { get; set; }
    public List<FieldDescriptorProto> Fields { get; } = [];
    public List<FieldDescriptorProto> Extensions { get; } = [];
    public List<DescriptorProto> NestedTypes { get; } = [];
    public List<EnumDescriptorProto> EnumTypes { get; } = [];
    public List<ExtensionRange> ExtensionRanges { get; } = [];
    public List<OneofDescriptorProto> OneofDecls { get; } = [];
    public MessageOptions Options { get; set; }
    public List<ReservedRange> ReservedRanges { get; } = [];
    public List<string> ReservedNames { get; } = [];
}

public sealed class ExtensionRange
{
    public int? Start { get; set; }
    public int? End { get; set; }
    public ExtensionRangeOptions Options { get; set; }
}

public sealed class ReservedRange
{
    public int? Start { get; set; }
    public int? End { get; set; }
}

public sealed class ExtensionRangeOptions
{
    public List<UninterpretedOption> UninterpretedOptions { get; } = [];
}

public enum FieldLabel
{
    LabelOptional = 1,
    LabelRequired = 2,
    LabelRepeated = 3,
}

public enum FieldType
{
    TypeDouble = 1,
    TypeFloat = 2,
    TypeInt64 = 3,
    TypeUint64 = 4,
    TypeInt32 = 5,
    TypeFixed64 = 6,
    TypeFixed32 = 7,
    TypeBool = 8,
    TypeString = 9,
    TypeGroup = 10,
    TypeMessage = 11,
    TypeBytes = 12,
    TypeUint32 = 13,
    TypeEnum = 14,
    TypeSfixed32 = 15,
    TypeSfixed64 = 16,
    TypeSint32 = 17,
    TypeSint64 = 18,
}

public sealed class FieldDescriptorProto
{
    public string Name { get; set; }
    public int? Number { get; set; }
    public FieldLabel? Label { get; set; }
    public FieldType? Type { get; set; }
    public string TypeName { get; set; }
    public string Extendee { get; set; }
    public string DefaultValue { get; set; }
    public int? OneofIndex { get; set; }
    public string JsonName { get; set; }
    public FieldOptions Options { get; set; }
    public bool? Proto3Optional { get; set; }
}

public sealed class OneofDescriptorProto
{
    public string Name { get; set; }
    public OneofOptions Options { get; set; }
}

public sealed class EnumDescriptorProto
{
    public string Name { get; set; }
    public List<EnumValueDescriptorProto> Values { get; } = [];
    public EnumOptions Options { get; set; }
    public List<EnumReservedRange> ReservedRanges { get; } = [];
    public List<string> ReservedNames { get; } = [];
}

public sealed class EnumReservedRange
{
    public int? Start { get; set; }
    public int? End { get; set; }
}

public sealed class EnumValueDescriptorProto
{
    public string Name { get; set; }
    public int? Number { get; set; }
    public EnumValueOptions Options { get; set; }
}

public sealed class ServiceDescriptorProto
{
    public string Name { get; set; }
    public List<MethodDescriptorProto> Methods { get; } = [];
    public ServiceOptions Options { get; set; }
}

public sealed class MethodDescriptorProto
{
    public string Name { get; set; }
    public string InputType { get; set; }
    public string OutputType { get; set; }
    public MethodOptions Options { get; set; }
    public bool? ClientStreaming { get; set; }
    public bool? ServerStreaming { get; set; }
}

public enum OptimizeMode
{
    Speed = 1,
    CodeSize = 2,
    LiteRuntime = 3,
}

public sealed class FileOptions
{
    public string JavaPackage { get; set; }              // 1
    public string JavaOuterClassname { get; set; }       // 8
    public bool? JavaMultipleFiles { get; set; }         // 10
    public bool? JavaGenerateEqualsAndHash { get; set; } // 20
    public bool? JavaStringCheckUtf8 { get; set; }       // 27
    public OptimizeMode? OptimizeFor { get; set; }       // 9
    public string GoPackage { get; set; }                // 11
    public bool? CcGenericServices { get; set; }         // 16
    public bool? JavaGenericServices { get; set; }       // 17
    public bool? PyGenericServices { get; set; }         // 18
    public bool? PhpGenericServices { get; set; }        // 42
    public bool? Deprecated { get; set; }                // 23
    public bool? CcEnableArenas { get; set; }            // 31
    public string ObjcClassPrefix { get; set; }          // 36
    public string CsharpNamespace { get; set; }          // 37
    public string SwiftPrefix { get; set; }              // 39
    public string PhpClassPrefix { get; set; }           // 40
    public string PhpNamespace { get; set; }             // 41
    public string PhpMetadataNamespace { get; set; }     // 44
    public string RubyPackage { get; set; }              // 45
    public List<UninterpretedOption> UninterpretedOptions { get; } = []; // 999
}

public sealed class MessageOptions
{
    public bool? MessageSetWireFormat { get; set; }         // 1
    public bool? NoStandardDescriptorAccessor { get; set; } // 2
    public bool? Deprecated { get; set; }                   // 3
    public bool? MapEntry { get; set; }                     // 7
    public List<UninterpretedOption> UninterpretedOptions { get; } = []; // 999
}

public enum CType
{
    String = 0,
    Cord = 1,
    StringPiece = 2,
}

public enum JSType
{
    JsNormal = 0,
    JsString = 1,
    JsNumber = 2,
}

public sealed class FieldOptions
{
    public CType? Ctype { get; set; }      // 1
    public bool? Packed { get; set; }      // 2
    public JSType? Jstype { get; set; }    // 6
    public bool? Lazy { get; set; }        // 5
    public bool? Deprecated { get; set; }  // 3
    public bool? Weak { get; set; }        // 10
    public List<UninterpretedOption> UninterpretedOptions { get; } = []; // 999
}

public sealed class OneofOptions
{
    public List<UninterpretedOption> UninterpretedOptions { get; } = []; // 999
}

public sealed class EnumOptions
{
    public bool? AllowAlias { get; set; }  // 2
    public bool? Deprecated { get; set; }  // 3
    public List<UninterpretedOption> UninterpretedOptions { get; } = []; // 999
}

public sealed class EnumValueOptions
{
    public bool? Deprecated { get; set; }  // 1
    public List<UninterpretedOption> UninterpretedOptions { get; } = []; // 999
}

public sealed class ServiceOptions
{
    public bool? Deprecated { get; set; }  // 33
    public List<UninterpretedOption> UninterpretedOptions { get; } = []; // 999
}

public enum IdempotencyLevel
{
    IdempotencyUnknown = 0,
    NoSideEffects = 1,
    Idempotent = 2,
}

public sealed class MethodOptions
{
    public bool? Deprecated { get; set; }                  // 33
    public IdempotencyLevel? IdempotencyLevel { get; set; } // 34
    public List<UninterpretedOption> UninterpretedOptions { get; } = []; // 999
}

public sealed class UninterpretedOption
{
    public List<NamePart> Names { get; } = [];       // 2
    public string IdentifierValue { get; set; }      // 3
    public ulong? PositiveIntValue { get; set; }     // 4
    public long? NegativeIntValue { get; set; }      // 5
    public double? DoubleValue { get; set; }         // 6
    public byte[] StringValue { get; set; }          // 7
    public string AggregateValue { get; set; }       // 8
}

public sealed class NamePart
{
    public string Name { get; set; }        // 1, required
    public bool? IsExtension { get; set; }  // 2, required
}

public sealed class SourceCodeInfo
{
    public List<Location> Locations { get; } = []; // 1
}

public sealed class Location
{
    public List<int> Path { get; } = [];  // 1, packed
    public List<int> Span { get; } = [];  // 2, packed
    public string LeadingComments { get; set; }   // 3
    public string TrailingComments { get; set; }  // 4
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
                case (1 << 3) | 2:  // file, field 1, length-prefixed
                case (1 << 3) | 3:  // file, field 1, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.Files.Add(ReadFileDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
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

    public static FileDescriptorProto ReadFileDescriptorProto(ref ReaderState state, FileDescriptorProto value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 2:  // name, field 1, length-prefixed
                    value.Name = state.ReadRawString();
                    break;
                case (2 << 3) | 2:  // package, field 2, length-prefixed
                    value.Package = state.ReadRawString();
                    break;
                case (3 << 3) | 2:  // dependency, field 3, length-prefixed run
                    do { value.Dependencies.Add(state.ReadRawString()); }
                    while ((tag = state.ReadRawTag()) == ((3 << 3) | 2));
                    continue;
                case (10 << 3) | 0: // public_dependency, field 10, unpacked run
                    do { value.PublicDependencies.Add(unchecked((int)state.ReadRawVarint32())); }
                    while ((tag = state.ReadRawTag()) == ((10 << 3) | 0));
                    continue;
                case (10 << 3) | 2: // public_dependency, field 10, packed
                {
                    var scope = state.PushLengthPrefix();
                    while (!state.AtScopeEnd) value.PublicDependencies.Add(unchecked((int)state.ReadRawVarint32()));
                    state.PopScope(scope);
                    break;
                }
                case (10 << 3) | 5: // public_dependency, field 10, fixed32
                    value.PublicDependencies.Add(unchecked((int)state.ReadRawFixed32()));
                    break;
                case (10 << 3) | 1: // public_dependency, field 10, fixed64
                    value.PublicDependencies.Add(checked((int)unchecked((long)state.ReadRawFixed64())));
                    break;
                case (11 << 3) | 0: // weak_dependency, field 11, unpacked run
                    do { value.WeakDependencies.Add(unchecked((int)state.ReadRawVarint32())); }
                    while ((tag = state.ReadRawTag()) == ((11 << 3) | 0));
                    continue;
                case (11 << 3) | 2: // weak_dependency, field 11, packed
                {
                    var scope = state.PushLengthPrefix();
                    while (!state.AtScopeEnd) value.WeakDependencies.Add(unchecked((int)state.ReadRawVarint32()));
                    state.PopScope(scope);
                    break;
                }
                case (11 << 3) | 5: // weak_dependency, field 11, fixed32
                    value.WeakDependencies.Add(unchecked((int)state.ReadRawFixed32()));
                    break;
                case (11 << 3) | 1: // weak_dependency, field 11, fixed64
                    value.WeakDependencies.Add(checked((int)unchecked((long)state.ReadRawFixed64())));
                    break;
                case (4 << 3) | 2:  // message_type, field 4, length-prefixed
                case (4 << 3) | 3:  // message_type, field 4, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.MessageTypes.Add(ReadDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
                }
                case (5 << 3) | 2:  // enum_type, field 5, length-prefixed
                case (5 << 3) | 3:  // enum_type, field 5, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.EnumTypes.Add(ReadEnumDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
                }
                case (6 << 3) | 2:  // service, field 6, length-prefixed
                case (6 << 3) | 3:  // service, field 6, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.Services.Add(ReadServiceDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
                }
                case (7 << 3) | 2:  // extension, field 7, length-prefixed
                case (7 << 3) | 3:  // extension, field 7, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.Extensions.Add(ReadFieldDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
                }
                case (8 << 3) | 2:  // options, field 8, length-prefixed
                case (8 << 3) | 3:  // options, field 8, group
                {
                    var scope = state.PushScope(tag);
                    value.Options = ReadFileOptions(ref state, value.Options);
                    state.PopScope(scope);
                    break;
                }
                case (9 << 3) | 2:  // source_code_info, field 9, length-prefixed
                case (9 << 3) | 3:  // source_code_info, field 9, group
                {
                    var scope = state.PushScope(tag);
                    value.SourceCodeInfo = ReadSourceCodeInfo(ref state, value.SourceCodeInfo);
                    state.PopScope(scope);
                    break;
                }
                case (12 << 3) | 2: // syntax, field 12, length-prefixed
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
                case (1 << 3) | 2:  // name, field 1, length-prefixed
                    value.Name = state.ReadRawString();
                    break;
                case (2 << 3) | 2:  // field, field 2, length-prefixed
                case (2 << 3) | 3:  // field, field 2, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.Fields.Add(ReadFieldDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
                }
                case (6 << 3) | 2:  // extension, field 6, length-prefixed
                case (6 << 3) | 3:  // extension, field 6, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.Extensions.Add(ReadFieldDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
                }
                case (3 << 3) | 2:  // nested_type, field 3, length-prefixed - the recursive dive
                case (3 << 3) | 3:  // nested_type, field 3, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.NestedTypes.Add(ReadDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
                }
                case (4 << 3) | 2:  // enum_type, field 4, length-prefixed
                case (4 << 3) | 3:  // enum_type, field 4, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.EnumTypes.Add(ReadEnumDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
                }
                case (5 << 3) | 2:  // extension_range, field 5, length-prefixed
                case (5 << 3) | 3:  // extension_range, field 5, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.ExtensionRanges.Add(ReadExtensionRange(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
                }
                case (8 << 3) | 2:  // oneof_decl, field 8, length-prefixed
                case (8 << 3) | 3:  // oneof_decl, field 8, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.OneofDecls.Add(ReadOneofDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
                }
                case (7 << 3) | 2:  // options, field 7, length-prefixed
                case (7 << 3) | 3:  // options, field 7, group
                {
                    var scope = state.PushScope(tag);
                    value.Options = ReadMessageOptions(ref state, value.Options);
                    state.PopScope(scope);
                    break;
                }
                case (9 << 3) | 2:  // reserved_range, field 9, length-prefixed
                case (9 << 3) | 3:  // reserved_range, field 9, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.ReservedRanges.Add(ReadReservedRange(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
                }
                case (10 << 3) | 2: // reserved_name, field 10, length-prefixed run
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
                case (1 << 3) | 0:  // start, field 1, varint
                    value.Start = unchecked((int)state.ReadRawVarint32());
                    break;
                case (1 << 3) | 5:  // start, field 1, fixed32
                    value.Start = unchecked((int)state.ReadRawFixed32());
                    break;
                case (1 << 3) | 1:  // start, field 1, fixed64
                    value.Start = checked((int)unchecked((long)state.ReadRawFixed64()));
                    break;
                case (2 << 3) | 0:  // end, field 2, varint
                    value.End = unchecked((int)state.ReadRawVarint32());
                    break;
                case (2 << 3) | 5:  // end, field 2, fixed32
                    value.End = unchecked((int)state.ReadRawFixed32());
                    break;
                case (2 << 3) | 1:  // end, field 2, fixed64
                    value.End = checked((int)unchecked((long)state.ReadRawFixed64()));
                    break;
                case (3 << 3) | 2:  // options, field 3, length-prefixed
                case (3 << 3) | 3:  // options, field 3, group
                {
                    var scope = state.PushScope(tag);
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
                case (1 << 3) | 0:  // start, field 1, varint
                    value.Start = unchecked((int)state.ReadRawVarint32());
                    break;
                case (1 << 3) | 5:  // start, field 1, fixed32
                    value.Start = unchecked((int)state.ReadRawFixed32());
                    break;
                case (1 << 3) | 1:  // start, field 1, fixed64
                    value.Start = checked((int)unchecked((long)state.ReadRawFixed64()));
                    break;
                case (2 << 3) | 0:  // end, field 2, varint
                    value.End = unchecked((int)state.ReadRawVarint32());
                    break;
                case (2 << 3) | 5:  // end, field 2, fixed32
                    value.End = unchecked((int)state.ReadRawFixed32());
                    break;
                case (2 << 3) | 1:  // end, field 2, fixed64
                    value.End = checked((int)unchecked((long)state.ReadRawFixed64()));
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
                case (999 << 3) | 2:  // uninterpreted_option, field 999, length-prefixed
                case (999 << 3) | 3:  // uninterpreted_option, field 999, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
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

    public static FieldDescriptorProto ReadFieldDescriptorProto(ref ReaderState state, FieldDescriptorProto value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 2:  // name, field 1, length-prefixed
                    value.Name = state.ReadRawString();
                    break;
                case (3 << 3) | 0:  // number, field 3, varint
                    value.Number = unchecked((int)state.ReadRawVarint32());
                    break;
                case (3 << 3) | 5:  // number, field 3, fixed32
                    value.Number = unchecked((int)state.ReadRawFixed32());
                    break;
                case (3 << 3) | 1:  // number, field 3, fixed64
                    value.Number = checked((int)unchecked((long)state.ReadRawFixed64()));
                    break;
                case (4 << 3) | 0:  // label, field 4, varint
                    value.Label = (FieldLabel)state.ReadRawVarint32();
                    break;
                case (4 << 3) | 5:  // label, field 4, fixed32
                    value.Label = (FieldLabel)unchecked((int)state.ReadRawFixed32());
                    break;
                case (4 << 3) | 1:  // label, field 4, fixed64
                    value.Label = (FieldLabel)checked((int)unchecked((long)state.ReadRawFixed64()));
                    break;
                case (5 << 3) | 0:  // type, field 5, varint
                    value.Type = (FieldType)state.ReadRawVarint32();
                    break;
                case (5 << 3) | 5:  // type, field 5, fixed32
                    value.Type = (FieldType)unchecked((int)state.ReadRawFixed32());
                    break;
                case (5 << 3) | 1:  // type, field 5, fixed64
                    value.Type = (FieldType)checked((int)unchecked((long)state.ReadRawFixed64()));
                    break;
                case (6 << 3) | 2:  // type_name, field 6, length-prefixed
                    value.TypeName = state.ReadRawString();
                    break;
                case (2 << 3) | 2:  // extendee, field 2, length-prefixed
                    value.Extendee = state.ReadRawString();
                    break;
                case (7 << 3) | 2:  // default_value, field 7, length-prefixed
                    value.DefaultValue = state.ReadRawString();
                    break;
                case (9 << 3) | 0:  // oneof_index, field 9, varint
                    value.OneofIndex = unchecked((int)state.ReadRawVarint32());
                    break;
                case (9 << 3) | 5:  // oneof_index, field 9, fixed32
                    value.OneofIndex = unchecked((int)state.ReadRawFixed32());
                    break;
                case (9 << 3) | 1:  // oneof_index, field 9, fixed64
                    value.OneofIndex = checked((int)unchecked((long)state.ReadRawFixed64()));
                    break;
                case (10 << 3) | 2: // json_name, field 10, length-prefixed
                    value.JsonName = state.ReadRawString();
                    break;
                case (8 << 3) | 2:  // options, field 8, length-prefixed
                case (8 << 3) | 3:  // options, field 8, group
                {
                    var scope = state.PushScope(tag);
                    value.Options = ReadFieldOptions(ref state, value.Options);
                    state.PopScope(scope);
                    break;
                }
                case (17 << 3) | 0:  // proto3_optional, field 17, varint
                    value.Proto3Optional = state.ReadRawVarint32() != 0;
                    break;
                case (17 << 3) | 5:  // proto3_optional, field 17, fixed32
                    value.Proto3Optional = state.ReadRawFixed32() != 0;
                    break;
                case (17 << 3) | 1:  // proto3_optional, field 17, fixed64
                    value.Proto3Optional = state.ReadRawFixed64() != 0;
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
                case (1 << 3) | 2:  // name, field 1, length-prefixed
                    value.Name = state.ReadRawString();
                    break;
                case (2 << 3) | 2:  // options, field 2, length-prefixed
                case (2 << 3) | 3:  // options, field 2, group
                {
                    var scope = state.PushScope(tag);
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
                case (1 << 3) | 2:  // name, field 1, length-prefixed
                    value.Name = state.ReadRawString();
                    break;
                case (2 << 3) | 2:  // value, field 2, length-prefixed
                case (2 << 3) | 3:  // value, field 2, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.Values.Add(ReadEnumValueDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
                }
                case (3 << 3) | 2:  // options, field 3, length-prefixed
                case (3 << 3) | 3:  // options, field 3, group
                {
                    var scope = state.PushScope(tag);
                    value.Options = ReadEnumOptions(ref state, value.Options);
                    state.PopScope(scope);
                    break;
                }
                case (4 << 3) | 2:  // reserved_range, field 4, length-prefixed
                case (4 << 3) | 3:  // reserved_range, field 4, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.ReservedRanges.Add(ReadEnumReservedRange(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
                }
                case (5 << 3) | 2:  // reserved_name, field 5, length-prefixed run
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
                case (1 << 3) | 0:  // start, field 1, varint
                    value.Start = unchecked((int)state.ReadRawVarint32());
                    break;
                case (1 << 3) | 5:  // start, field 1, fixed32
                    value.Start = unchecked((int)state.ReadRawFixed32());
                    break;
                case (1 << 3) | 1:  // start, field 1, fixed64
                    value.Start = checked((int)unchecked((long)state.ReadRawFixed64()));
                    break;
                case (2 << 3) | 0:  // end, field 2, varint
                    value.End = unchecked((int)state.ReadRawVarint32());
                    break;
                case (2 << 3) | 5:  // end, field 2, fixed32
                    value.End = unchecked((int)state.ReadRawFixed32());
                    break;
                case (2 << 3) | 1:  // end, field 2, fixed64
                    value.End = checked((int)unchecked((long)state.ReadRawFixed64()));
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
                case (1 << 3) | 2:  // name, field 1, length-prefixed
                    value.Name = state.ReadRawString();
                    break;
                case (2 << 3) | 0:  // number, field 2, varint
                    value.Number = unchecked((int)state.ReadRawVarint32());
                    break;
                case (2 << 3) | 5:  // number, field 2, fixed32
                    value.Number = unchecked((int)state.ReadRawFixed32());
                    break;
                case (2 << 3) | 1:  // number, field 2, fixed64
                    value.Number = checked((int)unchecked((long)state.ReadRawFixed64()));
                    break;
                case (3 << 3) | 2:  // options, field 3, length-prefixed
                case (3 << 3) | 3:  // options, field 3, group
                {
                    var scope = state.PushScope(tag);
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
                case (1 << 3) | 2:  // name, field 1, length-prefixed
                    value.Name = state.ReadRawString();
                    break;
                case (2 << 3) | 2:  // method, field 2, length-prefixed
                case (2 << 3) | 3:  // method, field 2, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.Methods.Add(ReadMethodDescriptorProto(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
                }
                case (3 << 3) | 2:  // options, field 3, length-prefixed
                case (3 << 3) | 3:  // options, field 3, group
                {
                    var scope = state.PushScope(tag);
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
                case (1 << 3) | 2:  // name, field 1, length-prefixed
                    value.Name = state.ReadRawString();
                    break;
                case (2 << 3) | 2:  // input_type, field 2, length-prefixed
                    value.InputType = state.ReadRawString();
                    break;
                case (3 << 3) | 2:  // output_type, field 3, length-prefixed
                    value.OutputType = state.ReadRawString();
                    break;
                case (4 << 3) | 2:  // options, field 4, length-prefixed
                case (4 << 3) | 3:  // options, field 4, group
                {
                    var scope = state.PushScope(tag);
                    value.Options = ReadMethodOptions(ref state, value.Options);
                    state.PopScope(scope);
                    break;
                }
                case (5 << 3) | 0:  // client_streaming, field 5, varint
                    value.ClientStreaming = state.ReadRawVarint32() != 0;
                    break;
                case (5 << 3) | 5:  // client_streaming, field 5, fixed32
                    value.ClientStreaming = state.ReadRawFixed32() != 0;
                    break;
                case (5 << 3) | 1:  // client_streaming, field 5, fixed64
                    value.ClientStreaming = state.ReadRawFixed64() != 0;
                    break;
                case (6 << 3) | 0:  // server_streaming, field 6, varint
                    value.ServerStreaming = state.ReadRawVarint32() != 0;
                    break;
                case (6 << 3) | 5:  // server_streaming, field 6, fixed32
                    value.ServerStreaming = state.ReadRawFixed32() != 0;
                    break;
                case (6 << 3) | 1:  // server_streaming, field 6, fixed64
                    value.ServerStreaming = state.ReadRawFixed64() != 0;
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
                case (1 << 3) | 2:  // java_package, field 1, length-prefixed
                    value.JavaPackage = state.ReadRawString();
                    break;
                case (8 << 3) | 2:  // java_outer_classname, field 8, length-prefixed
                    value.JavaOuterClassname = state.ReadRawString();
                    break;
                case (10 << 3) | 0:  // java_multiple_files, field 10, varint
                    value.JavaMultipleFiles = state.ReadRawVarint32() != 0;
                    break;
                case (10 << 3) | 5:  // java_multiple_files, field 10, fixed32
                    value.JavaMultipleFiles = state.ReadRawFixed32() != 0;
                    break;
                case (10 << 3) | 1:  // java_multiple_files, field 10, fixed64
                    value.JavaMultipleFiles = state.ReadRawFixed64() != 0;
                    break;
                case (20 << 3) | 0:  // java_generate_equals_and_hash, field 20, varint
                    value.JavaGenerateEqualsAndHash = state.ReadRawVarint32() != 0;
                    break;
                case (20 << 3) | 5:  // java_generate_equals_and_hash, field 20, fixed32
                    value.JavaGenerateEqualsAndHash = state.ReadRawFixed32() != 0;
                    break;
                case (20 << 3) | 1:  // java_generate_equals_and_hash, field 20, fixed64
                    value.JavaGenerateEqualsAndHash = state.ReadRawFixed64() != 0;
                    break;
                case (27 << 3) | 0:  // java_string_check_utf8, field 27, varint
                    value.JavaStringCheckUtf8 = state.ReadRawVarint32() != 0;
                    break;
                case (27 << 3) | 5:  // java_string_check_utf8, field 27, fixed32
                    value.JavaStringCheckUtf8 = state.ReadRawFixed32() != 0;
                    break;
                case (27 << 3) | 1:  // java_string_check_utf8, field 27, fixed64
                    value.JavaStringCheckUtf8 = state.ReadRawFixed64() != 0;
                    break;
                case (9 << 3) | 0:  // optimize_for, field 9, varint
                    value.OptimizeFor = (OptimizeMode)state.ReadRawVarint32();
                    break;
                case (9 << 3) | 5:  // optimize_for, field 9, fixed32
                    value.OptimizeFor = (OptimizeMode)unchecked((int)state.ReadRawFixed32());
                    break;
                case (9 << 3) | 1:  // optimize_for, field 9, fixed64
                    value.OptimizeFor = (OptimizeMode)checked((int)unchecked((long)state.ReadRawFixed64()));
                    break;
                case (11 << 3) | 2: // go_package, field 11, length-prefixed
                    value.GoPackage = state.ReadRawString();
                    break;
                case (16 << 3) | 0:  // cc_generic_services, field 16, varint
                    value.CcGenericServices = state.ReadRawVarint32() != 0;
                    break;
                case (16 << 3) | 5:  // cc_generic_services, field 16, fixed32
                    value.CcGenericServices = state.ReadRawFixed32() != 0;
                    break;
                case (16 << 3) | 1:  // cc_generic_services, field 16, fixed64
                    value.CcGenericServices = state.ReadRawFixed64() != 0;
                    break;
                case (17 << 3) | 0:  // java_generic_services, field 17, varint
                    value.JavaGenericServices = state.ReadRawVarint32() != 0;
                    break;
                case (17 << 3) | 5:  // java_generic_services, field 17, fixed32
                    value.JavaGenericServices = state.ReadRawFixed32() != 0;
                    break;
                case (17 << 3) | 1:  // java_generic_services, field 17, fixed64
                    value.JavaGenericServices = state.ReadRawFixed64() != 0;
                    break;
                case (18 << 3) | 0:  // py_generic_services, field 18, varint
                    value.PyGenericServices = state.ReadRawVarint32() != 0;
                    break;
                case (18 << 3) | 5:  // py_generic_services, field 18, fixed32
                    value.PyGenericServices = state.ReadRawFixed32() != 0;
                    break;
                case (18 << 3) | 1:  // py_generic_services, field 18, fixed64
                    value.PyGenericServices = state.ReadRawFixed64() != 0;
                    break;
                case (42 << 3) | 0:  // php_generic_services, field 42, varint
                    value.PhpGenericServices = state.ReadRawVarint32() != 0;
                    break;
                case (42 << 3) | 5:  // php_generic_services, field 42, fixed32
                    value.PhpGenericServices = state.ReadRawFixed32() != 0;
                    break;
                case (42 << 3) | 1:  // php_generic_services, field 42, fixed64
                    value.PhpGenericServices = state.ReadRawFixed64() != 0;
                    break;
                case (23 << 3) | 0:  // deprecated, field 23, varint
                    value.Deprecated = state.ReadRawVarint32() != 0;
                    break;
                case (23 << 3) | 5:  // deprecated, field 23, fixed32
                    value.Deprecated = state.ReadRawFixed32() != 0;
                    break;
                case (23 << 3) | 1:  // deprecated, field 23, fixed64
                    value.Deprecated = state.ReadRawFixed64() != 0;
                    break;
                case (31 << 3) | 0:  // cc_enable_arenas, field 31, varint
                    value.CcEnableArenas = state.ReadRawVarint32() != 0;
                    break;
                case (31 << 3) | 5:  // cc_enable_arenas, field 31, fixed32
                    value.CcEnableArenas = state.ReadRawFixed32() != 0;
                    break;
                case (31 << 3) | 1:  // cc_enable_arenas, field 31, fixed64
                    value.CcEnableArenas = state.ReadRawFixed64() != 0;
                    break;
                case (36 << 3) | 2: // objc_class_prefix, field 36, length-prefixed
                    value.ObjcClassPrefix = state.ReadRawString();
                    break;
                case (37 << 3) | 2: // csharp_namespace, field 37, length-prefixed
                    value.CsharpNamespace = state.ReadRawString();
                    break;
                case (39 << 3) | 2: // swift_prefix, field 39, length-prefixed
                    value.SwiftPrefix = state.ReadRawString();
                    break;
                case (40 << 3) | 2: // php_class_prefix, field 40, length-prefixed
                    value.PhpClassPrefix = state.ReadRawString();
                    break;
                case (41 << 3) | 2: // php_namespace, field 41, length-prefixed
                    value.PhpNamespace = state.ReadRawString();
                    break;
                case (44 << 3) | 2: // php_metadata_namespace, field 44, length-prefixed
                    value.PhpMetadataNamespace = state.ReadRawString();
                    break;
                case (45 << 3) | 2: // ruby_package, field 45, length-prefixed
                    value.RubyPackage = state.ReadRawString();
                    break;
                case (999 << 3) | 2:  // uninterpreted_option, field 999, length-prefixed
                case (999 << 3) | 3:  // uninterpreted_option, field 999, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
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

    public static MessageOptions ReadMessageOptions(ref ReaderState state, MessageOptions value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 0:  // message_set_wire_format, field 1, varint
                    value.MessageSetWireFormat = state.ReadRawVarint32() != 0;
                    break;
                case (1 << 3) | 5:  // message_set_wire_format, field 1, fixed32
                    value.MessageSetWireFormat = state.ReadRawFixed32() != 0;
                    break;
                case (1 << 3) | 1:  // message_set_wire_format, field 1, fixed64
                    value.MessageSetWireFormat = state.ReadRawFixed64() != 0;
                    break;
                case (2 << 3) | 0:  // no_standard_descriptor_accessor, field 2, varint
                    value.NoStandardDescriptorAccessor = state.ReadRawVarint32() != 0;
                    break;
                case (2 << 3) | 5:  // no_standard_descriptor_accessor, field 2, fixed32
                    value.NoStandardDescriptorAccessor = state.ReadRawFixed32() != 0;
                    break;
                case (2 << 3) | 1:  // no_standard_descriptor_accessor, field 2, fixed64
                    value.NoStandardDescriptorAccessor = state.ReadRawFixed64() != 0;
                    break;
                case (3 << 3) | 0:  // deprecated, field 3, varint
                    value.Deprecated = state.ReadRawVarint32() != 0;
                    break;
                case (3 << 3) | 5:  // deprecated, field 3, fixed32
                    value.Deprecated = state.ReadRawFixed32() != 0;
                    break;
                case (3 << 3) | 1:  // deprecated, field 3, fixed64
                    value.Deprecated = state.ReadRawFixed64() != 0;
                    break;
                case (7 << 3) | 0:  // map_entry, field 7, varint
                    value.MapEntry = state.ReadRawVarint32() != 0;
                    break;
                case (7 << 3) | 5:  // map_entry, field 7, fixed32
                    value.MapEntry = state.ReadRawFixed32() != 0;
                    break;
                case (7 << 3) | 1:  // map_entry, field 7, fixed64
                    value.MapEntry = state.ReadRawFixed64() != 0;
                    break;
                case (999 << 3) | 2:  // uninterpreted_option, field 999, length-prefixed
                case (999 << 3) | 3:  // uninterpreted_option, field 999, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
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

    public static FieldOptions ReadFieldOptions(ref ReaderState state, FieldOptions value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 0:  // ctype, field 1, varint
                    value.Ctype = (CType)state.ReadRawVarint32();
                    break;
                case (1 << 3) | 5:  // ctype, field 1, fixed32
                    value.Ctype = (CType)unchecked((int)state.ReadRawFixed32());
                    break;
                case (1 << 3) | 1:  // ctype, field 1, fixed64
                    value.Ctype = (CType)checked((int)unchecked((long)state.ReadRawFixed64()));
                    break;
                case (2 << 3) | 0:  // packed, field 2, varint
                    value.Packed = state.ReadRawVarint32() != 0;
                    break;
                case (2 << 3) | 5:  // packed, field 2, fixed32
                    value.Packed = state.ReadRawFixed32() != 0;
                    break;
                case (2 << 3) | 1:  // packed, field 2, fixed64
                    value.Packed = state.ReadRawFixed64() != 0;
                    break;
                case (6 << 3) | 0:  // jstype, field 6, varint
                    value.Jstype = (JSType)state.ReadRawVarint32();
                    break;
                case (6 << 3) | 5:  // jstype, field 6, fixed32
                    value.Jstype = (JSType)unchecked((int)state.ReadRawFixed32());
                    break;
                case (6 << 3) | 1:  // jstype, field 6, fixed64
                    value.Jstype = (JSType)checked((int)unchecked((long)state.ReadRawFixed64()));
                    break;
                case (5 << 3) | 0:  // lazy, field 5, varint
                    value.Lazy = state.ReadRawVarint32() != 0;
                    break;
                case (5 << 3) | 5:  // lazy, field 5, fixed32
                    value.Lazy = state.ReadRawFixed32() != 0;
                    break;
                case (5 << 3) | 1:  // lazy, field 5, fixed64
                    value.Lazy = state.ReadRawFixed64() != 0;
                    break;
                case (3 << 3) | 0:  // deprecated, field 3, varint
                    value.Deprecated = state.ReadRawVarint32() != 0;
                    break;
                case (3 << 3) | 5:  // deprecated, field 3, fixed32
                    value.Deprecated = state.ReadRawFixed32() != 0;
                    break;
                case (3 << 3) | 1:  // deprecated, field 3, fixed64
                    value.Deprecated = state.ReadRawFixed64() != 0;
                    break;
                case (10 << 3) | 0:  // weak, field 10, varint
                    value.Weak = state.ReadRawVarint32() != 0;
                    break;
                case (10 << 3) | 5:  // weak, field 10, fixed32
                    value.Weak = state.ReadRawFixed32() != 0;
                    break;
                case (10 << 3) | 1:  // weak, field 10, fixed64
                    value.Weak = state.ReadRawFixed64() != 0;
                    break;
                case (999 << 3) | 2:  // uninterpreted_option, field 999, length-prefixed
                case (999 << 3) | 3:  // uninterpreted_option, field 999, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
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

    public static OneofOptions ReadOneofOptions(ref ReaderState state, OneofOptions value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (999 << 3) | 2:  // uninterpreted_option, field 999, length-prefixed
                case (999 << 3) | 3:  // uninterpreted_option, field 999, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
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

    public static EnumOptions ReadEnumOptions(ref ReaderState state, EnumOptions value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (2 << 3) | 0:  // allow_alias, field 2, varint
                    value.AllowAlias = state.ReadRawVarint32() != 0;
                    break;
                case (2 << 3) | 5:  // allow_alias, field 2, fixed32
                    value.AllowAlias = state.ReadRawFixed32() != 0;
                    break;
                case (2 << 3) | 1:  // allow_alias, field 2, fixed64
                    value.AllowAlias = state.ReadRawFixed64() != 0;
                    break;
                case (3 << 3) | 0:  // deprecated, field 3, varint
                    value.Deprecated = state.ReadRawVarint32() != 0;
                    break;
                case (3 << 3) | 5:  // deprecated, field 3, fixed32
                    value.Deprecated = state.ReadRawFixed32() != 0;
                    break;
                case (3 << 3) | 1:  // deprecated, field 3, fixed64
                    value.Deprecated = state.ReadRawFixed64() != 0;
                    break;
                case (999 << 3) | 2:  // uninterpreted_option, field 999, length-prefixed
                case (999 << 3) | 3:  // uninterpreted_option, field 999, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
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

    public static EnumValueOptions ReadEnumValueOptions(ref ReaderState state, EnumValueOptions value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 0:  // deprecated, field 1, varint
                    value.Deprecated = state.ReadRawVarint32() != 0;
                    break;
                case (1 << 3) | 5:  // deprecated, field 1, fixed32
                    value.Deprecated = state.ReadRawFixed32() != 0;
                    break;
                case (1 << 3) | 1:  // deprecated, field 1, fixed64
                    value.Deprecated = state.ReadRawFixed64() != 0;
                    break;
                case (999 << 3) | 2:  // uninterpreted_option, field 999, length-prefixed
                case (999 << 3) | 3:  // uninterpreted_option, field 999, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
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

    public static ServiceOptions ReadServiceOptions(ref ReaderState state, ServiceOptions value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (33 << 3) | 0:  // deprecated, field 33, varint
                    value.Deprecated = state.ReadRawVarint32() != 0;
                    break;
                case (33 << 3) | 5:  // deprecated, field 33, fixed32
                    value.Deprecated = state.ReadRawFixed32() != 0;
                    break;
                case (33 << 3) | 1:  // deprecated, field 33, fixed64
                    value.Deprecated = state.ReadRawFixed64() != 0;
                    break;
                case (999 << 3) | 2:  // uninterpreted_option, field 999, length-prefixed
                case (999 << 3) | 3:  // uninterpreted_option, field 999, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
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

    public static MethodOptions ReadMethodOptions(ref ReaderState state, MethodOptions value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (33 << 3) | 0:  // deprecated, field 33, varint
                    value.Deprecated = state.ReadRawVarint32() != 0;
                    break;
                case (33 << 3) | 5:  // deprecated, field 33, fixed32
                    value.Deprecated = state.ReadRawFixed32() != 0;
                    break;
                case (33 << 3) | 1:  // deprecated, field 33, fixed64
                    value.Deprecated = state.ReadRawFixed64() != 0;
                    break;
                case (34 << 3) | 0:  // idempotency_level, field 34, varint
                    value.IdempotencyLevel = (IdempotencyLevel)state.ReadRawVarint32();
                    break;
                case (34 << 3) | 5:  // idempotency_level, field 34, fixed32
                    value.IdempotencyLevel = (IdempotencyLevel)unchecked((int)state.ReadRawFixed32());
                    break;
                case (34 << 3) | 1:  // idempotency_level, field 34, fixed64
                    value.IdempotencyLevel = (IdempotencyLevel)checked((int)unchecked((long)state.ReadRawFixed64()));
                    break;
                case (999 << 3) | 2:  // uninterpreted_option, field 999, length-prefixed
                case (999 << 3) | 3:  // uninterpreted_option, field 999, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.UninterpretedOptions.Add(ReadUninterpretedOption(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
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

    public static UninterpretedOption ReadUninterpretedOption(ref ReaderState state, UninterpretedOption value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (2 << 3) | 2:  // name, field 2, length-prefixed
                case (2 << 3) | 3:  // name, field 2, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.Names.Add(ReadNamePart(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
                }
                case (3 << 3) | 2:  // identifier_value, field 3, length-prefixed
                    value.IdentifierValue = state.ReadRawString();
                    break;
                case (4 << 3) | 0:  // positive_int_value, field 4, varint
                    value.PositiveIntValue = state.ReadRawVarint64();
                    break;
                case (4 << 3) | 5:  // positive_int_value, field 4, fixed32
                    value.PositiveIntValue = state.ReadRawFixed32();
                    break;
                case (4 << 3) | 1:  // positive_int_value, field 4, fixed64
                    value.PositiveIntValue = state.ReadRawFixed64();
                    break;
                case (5 << 3) | 0:  // negative_int_value, field 5, varint
                    value.NegativeIntValue = unchecked((long)state.ReadRawVarint64());
                    break;
                case (5 << 3) | 5:  // negative_int_value, field 5, fixed32
                    value.NegativeIntValue = unchecked((int)state.ReadRawFixed32());
                    break;
                case (5 << 3) | 1:  // negative_int_value, field 5, fixed64
                    value.NegativeIntValue = unchecked((long)state.ReadRawFixed64());
                    break;
                case (6 << 3) | 1:  // double_value, field 6, fixed64
                    value.DoubleValue = BitConverter.Int64BitsToDouble(unchecked((long)state.ReadRawFixed64()));
                    break;
                case (7 << 3) | 2:  // string_value, field 7, length-prefixed
                    value.StringValue = state.ReadRawBytes();
                    break;
                case (8 << 3) | 2:  // aggregate_value, field 8, length-prefixed
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
                case (1 << 3) | 2:  // name_part, field 1, length-prefixed
                    value.Name = state.ReadRawString();
                    break;
                case (2 << 3) | 0:  // is_extension, field 2, varint
                    value.IsExtension = state.ReadRawVarint32() != 0;
                    break;
                case (2 << 3) | 5:  // is_extension, field 2, fixed32
                    value.IsExtension = state.ReadRawFixed32() != 0;
                    break;
                case (2 << 3) | 1:  // is_extension, field 2, fixed64
                    value.IsExtension = state.ReadRawFixed64() != 0;
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
                case (1 << 3) | 2:  // location, field 1, length-prefixed
                case (1 << 3) | 3:  // location, field 1, group
                {
                    var last = tag;
                    do
                    {
                        var scope = state.PushScope(last);
                        value.Locations.Add(ReadLocation(ref state, null));
                        state.PopScope(scope);
                    } while ((tag = state.ReadRawTag()) == last);
                    continue;
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

    public static Location ReadLocation(ref ReaderState state, Location value)
    {
        value ??= new();
        uint tag = state.ReadRawTag();
        while (tag != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 2:  // path, field 1, packed
                {
                    var scope = state.PushLengthPrefix();
                    while (!state.AtScopeEnd) value.Path.Add(unchecked((int)state.ReadRawVarint32()));
                    state.PopScope(scope);
                    break;
                }
                case (1 << 3) | 0:  // path, field 1, unpacked run
                    do { value.Path.Add(unchecked((int)state.ReadRawVarint32())); }
                    while ((tag = state.ReadRawTag()) == ((1 << 3) | 0));
                    continue;
                case (1 << 3) | 5:  // path, field 1, fixed32
                    value.Path.Add(unchecked((int)state.ReadRawFixed32()));
                    break;
                case (1 << 3) | 1:  // path, field 1, fixed64
                    value.Path.Add(checked((int)unchecked((long)state.ReadRawFixed64())));
                    break;
                case (2 << 3) | 2:  // span, field 2, packed
                {
                    var scope = state.PushLengthPrefix();
                    while (!state.AtScopeEnd) value.Span.Add(unchecked((int)state.ReadRawVarint32()));
                    state.PopScope(scope);
                    break;
                }
                case (2 << 3) | 0:  // span, field 2, unpacked run
                    do { value.Span.Add(unchecked((int)state.ReadRawVarint32())); }
                    while ((tag = state.ReadRawTag()) == ((2 << 3) | 0));
                    continue;
                case (2 << 3) | 5:  // span, field 2, fixed32
                    value.Span.Add(unchecked((int)state.ReadRawFixed32()));
                    break;
                case (2 << 3) | 1:  // span, field 2, fixed64
                    value.Span.Add(checked((int)unchecked((long)state.ReadRawFixed64())));
                    break;
                case (3 << 3) | 2:  // leading_comments, field 3, length-prefixed
                    value.LeadingComments = state.ReadRawString();
                    break;
                case (4 << 3) | 2:  // trailing_comments, field 4, length-prefixed
                    value.TrailingComments = state.ReadRawString();
                    break;
                case (6 << 3) | 2:  // leading_detached_comments, field 6, length-prefixed run
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
