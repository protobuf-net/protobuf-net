using ProtoBuf.Reflection;
using System;

namespace Google.Protobuf.Reflection;

internal interface ISchemaFeatures
{
    ParsedFeatures ParsedFeatures { get; set; }
}

partial class DescriptorProto : ISchemaFeatures
{
    ParsedFeatures ISchemaFeatures.ParsedFeatures { get; set; }

    partial class ExtensionRange : ISchemaFeatures
    {
        ParsedFeatures ISchemaFeatures.ParsedFeatures { get; set; }
    }
}
partial class FileDescriptorProto : ISchemaFeatures
{
    ParsedFeatures ISchemaFeatures.ParsedFeatures { get; set; }
}
partial class FieldDescriptorProto : ISchemaFeatures
{
    ParsedFeatures ISchemaFeatures.ParsedFeatures { get; set; }
}
partial class EnumDescriptorProto : ISchemaFeatures
{
    ParsedFeatures ISchemaFeatures.ParsedFeatures { get; set; }
}
partial class EnumValueDescriptorProto : ISchemaFeatures
{
    ParsedFeatures ISchemaFeatures.ParsedFeatures { get; set; }
}
partial class ServiceDescriptorProto : ISchemaFeatures
{
    ParsedFeatures ISchemaFeatures.ParsedFeatures { get; set; }
}
partial class MethodDescriptorProto : ISchemaFeatures
{
    ParsedFeatures ISchemaFeatures.ParsedFeatures { get; set; }
}
partial class OneofDescriptorProto : ISchemaFeatures
{
    ParsedFeatures ISchemaFeatures.ParsedFeatures { get; set; }
}

internal static class SchemaFeatureExtensions
{
    internal static ParsedFeatures Apply(this ISchemaFeatures obj, ParsedFeatures features, ISchemaOptions options = null)
        => obj.ParsedFeatures = features.With(options);

    internal static ParsedFeatures GetFeatures(this ISchemaFeatures obj)
        => obj.ParsedFeatures.Assert();
}

readonly internal struct ParsedFeatures
{
    [Flags]
    private enum FeatureFlags
    {
        KindNone = 0, // we should never see this in a fully parsed model
        KindProto2 = 1 << 0,
        KindProto3 = 1 << 1,
        KindEditions = KindProto2 | KindProto3,
        KindMask = KindProto2 | KindProto3 | KindEditions,

        EnumTypeOpen = 1 << 2,
        EnumTypeClosed = 1 << 3,
        EnumTypeMask = EnumTypeOpen | EnumTypeClosed,

        FieldPresenceImplicit = 1 << 4,
        FieldPresenceExplicit = 1 << 5,
        FieldPresenceLegacyRequired = FieldPresenceImplicit | FieldPresenceExplicit,
        FieldPresenceMask = FieldPresenceImplicit | FieldPresenceExplicit | FieldPresenceLegacyRequired,

        JsonFormatAllow = 1 << 6,
        JsonFormatBestEffort = 1 << 7,
        JsonFormatMask = JsonFormatAllow | JsonFormatBestEffort,

        MessageFormatLengthPrefixed = 1 << 8,
        MessageFormatDelimited = 1 << 9,
        MessageFormatMask = MessageFormatLengthPrefixed | MessageFormatDelimited,

        RepeatedFieldEncodingPacked = 1 << 10,
        RepeatedFieldEncodingExpanded = 1 << 11,
        RepeatedFieldEncodingMask = RepeatedFieldEncodingPacked | RepeatedFieldEncodingExpanded,

        Utf8ValidationVerify = 1 << 12,
        Utf8ValidationNone = 1 << 13,
        Utf8ValidationMask = Utf8ValidationVerify | Utf8ValidationNone,
    }

    private readonly FeatureFlags flags;
    private ParsedFeatures(FeatureFlags flags) => this.flags = flags;
    private static ParsedFeatures With(FeatureFlags flags, FeatureFlags mask, FeatureFlags add) => new((flags & ~mask) | (add | mask));

    public ParsedFeatures Assert()
    {
        if ((flags & FeatureFlags.KindEditions) == 0) Throw();
        return this;

        static void Throw()
            => throw new InvalidOperationException("Features have not been computed");
    }

    public ParsedFeatures With(ISchemaOptions options) => options?.Features is { } features ? WithSlow(features) : this;

    private ParsedFeatures WithSlow(FeatureSet features)
    {
        var value = this;
        if (features is not null)
        {
            if (features.ShouldSerializeenum_type())
            {
                value = With(features.enum_type);
            }
            if (features.ShouldSerializefield_presence())
            {
                value = With(features.field_presence);
            }
            if (features.ShouldSerializejson_format())
            {
                value = With(features.json_format);
            }
            if (features.ShouldSerializemessage_encoding())
            {
                value = With(features.message_encoding);
            }
            if (features.ShouldSerializerepeated_field_encoding())
            {
                value = With(features.repeated_field_encoding);
            }
            if (features.ShouldSerializeutf8_validation())
            {
                value = value.With(features.utf8_validation);
            }
        }
        return value;
    }

    public ParsedFeatures With(FeatureSet.Utf8Validation value)
        => With(flags, FeatureFlags.Utf8ValidationMask, value switch
        {
            FeatureSet.Utf8Validation.Utf8ValidationUnknown => 0,
            FeatureSet.Utf8Validation.Verify => FeatureFlags.Utf8ValidationVerify,
            FeatureSet.Utf8Validation.None => FeatureFlags.Utf8ValidationNone,
            _ => throw new ArgumentOutOfRangeException(nameof(FeatureSet.utf8_validation)),
        });

    public ParsedFeatures With(FeatureSet.RepeatedFieldEncoding value)
        => With(flags, FeatureFlags.RepeatedFieldEncodingMask, value switch
        {
            FeatureSet.RepeatedFieldEncoding.RepeatedFieldEncodingUnknown => 0,
            FeatureSet.RepeatedFieldEncoding.Packed => FeatureFlags.RepeatedFieldEncodingPacked,
            FeatureSet.RepeatedFieldEncoding.Expanded => FeatureFlags.RepeatedFieldEncodingExpanded,
            _ => throw new ArgumentOutOfRangeException(nameof(FeatureSet.repeated_field_encoding)),
        });

    public ParsedFeatures With(FeatureSet.MessageEncoding value)
        => With(flags, FeatureFlags.MessageFormatMask, value switch
        {
            FeatureSet.MessageEncoding.MessageEncodingUnknown => 0,
            FeatureSet.MessageEncoding.LengthPrefixed => FeatureFlags.MessageFormatLengthPrefixed,
            FeatureSet.MessageEncoding.Delimited => FeatureFlags.MessageFormatDelimited,
            _ => throw new ArgumentOutOfRangeException(nameof(FeatureSet.message_encoding)),
        });

    public ParsedFeatures With(FeatureSet.JsonFormat value)
        => With(flags, FeatureFlags.JsonFormatMask, value switch
        {
            FeatureSet.JsonFormat.JsonFormatUnknown => 0,
            FeatureSet.JsonFormat.Allow => FeatureFlags.JsonFormatAllow,
            FeatureSet.JsonFormat.LegacyBestEffort => FeatureFlags.JsonFormatBestEffort,
            _ => throw new ArgumentOutOfRangeException(nameof(FeatureSet.json_format)),
        });

    public ParsedFeatures With(FeatureSet.FieldPresence value)
        => With(flags, FeatureFlags.FieldPresenceMask, value switch
        {
            FeatureSet.FieldPresence.FieldPresenceUnknown => 0,
            FeatureSet.FieldPresence.Implicit => FeatureFlags.FieldPresenceImplicit,
            FeatureSet.FieldPresence.Explicit => FeatureFlags.FieldPresenceExplicit,
            FeatureSet.FieldPresence.LegacyRequired => FeatureFlags.FieldPresenceLegacyRequired,
            _ => throw new ArgumentOutOfRangeException(nameof(FeatureSet.field_presence)),
        });

    public ParsedFeatures With(FeatureSet.EnumType value)
        => With(flags, FeatureFlags.EnumTypeMask, value switch
        {
            FeatureSet.EnumType.EnumTypeUnknown => 0,
            FeatureSet.EnumType.Open => FeatureFlags.EnumTypeOpen,
            FeatureSet.EnumType.Closed => FeatureFlags.EnumTypeClosed,
            _ => throw new ArgumentOutOfRangeException(nameof(FeatureSet.enum_type)),
        });

    public static ParsedFeatures Create(FileDescriptorProto fileDescriptor)
    {
        switch (fileDescriptor.Edition)
        {
            case Edition.EditionProto2:
            case Edition.EditionUnknown when fileDescriptor.Syntax is FileDescriptorProto.SyntaxProto2 or null or "":
                return Proto2;
            case Edition.EditionProto3:
            case Edition.EditionUnknown when fileDescriptor.Syntax is FileDescriptorProto.SyntaxProto3:
                return Proto3;
            case Edition.Edition2023: return Edition2023;
            default: throw new ArgumentOutOfRangeException(nameof(fileDescriptor.Edition));
        }
    }

    private static ParsedFeatures Proto2 { get; } = new ParsedFeatures(FeatureFlags.KindProto2
        | FeatureFlags.EnumTypeClosed | FeatureFlags.FieldPresenceExplicit
        | FeatureFlags.JsonFormatBestEffort | FeatureFlags.Utf8ValidationNone
        | FeatureFlags.MessageFormatLengthPrefixed | FeatureFlags.RepeatedFieldEncodingExpanded);

    private static ParsedFeatures Proto3 { get; } = new ParsedFeatures(FeatureFlags.KindProto3
        | FeatureFlags.EnumTypeOpen | FeatureFlags.FieldPresenceImplicit
        | FeatureFlags.JsonFormatAllow | FeatureFlags.Utf8ValidationVerify
        | FeatureFlags.MessageFormatLengthPrefixed | FeatureFlags.RepeatedFieldEncodingPacked);

    private static ParsedFeatures Edition2023 { get; } = new ParsedFeatures(FeatureFlags.KindEditions
        | FeatureFlags.EnumTypeOpen | FeatureFlags.FieldPresenceExplicit
        | FeatureFlags.JsonFormatAllow | FeatureFlags.Utf8ValidationVerify
        | FeatureFlags.MessageFormatLengthPrefixed | FeatureFlags.RepeatedFieldEncodingPacked);


    public bool IsProto2 => (flags & FeatureFlags.KindMask) == FeatureFlags.KindProto2;
    public bool IsProto3 => (flags & FeatureFlags.KindMask) == FeatureFlags.KindProto3;
    public bool IsEditions => (flags & FeatureFlags.KindMask) == FeatureFlags.KindEditions;

    public FeatureSet.FieldPresence FieldPresence => (flags & FeatureFlags.FieldPresenceMask) switch
    {
        FeatureFlags.FieldPresenceExplicit => FeatureSet.FieldPresence.Explicit,
        FeatureFlags.FieldPresenceImplicit => FeatureSet.FieldPresence.Implicit,
        FeatureFlags.FieldPresenceLegacyRequired => FeatureSet.FieldPresence.LegacyRequired,
        _ => FeatureSet.FieldPresence.FieldPresenceUnknown,
    };


}