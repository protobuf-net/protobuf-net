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
    internal static ParsedFeatures Apply(this ISchemaFeatures obj, ParsedFeatures features, ISchemaOptions options)
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

        EnumTypeOpen = 1 << 2,
        EnumTypeClosed = 1 << 3,

        FieldPresenceImplicit = 1 << 4,
        FieldPresenceExplicit = 1 << 5,
        FieldPresenceLegacyRequired = FieldPresenceImplicit | FieldPresenceExplicit,

        JsonFormatAllow = 1 << 6,
        JsonFormatBestEffort = 1 << 7,

        MessageFormatLengthPrefixed = 1 << 8,
        MessageFormatDelimited = 1 << 9,

        RepeatedFieldEncodingPacked = 1 << 10,
        RepeatedFieldEncodingExpanded = 1 << 11,

        Utf8ValidationVerify = 1 << 12,
        Utf8ValidationNone = 1 << 13,
    }

    private readonly FeatureFlags flags;
    private ParsedFeatures(FeatureFlags flags) => this.flags = flags;
    private static FeatureFlags With(FeatureFlags flags, FeatureFlags mask, FeatureFlags add) => (flags & ~mask) | (add | mask);

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
        if (features is null) return this;
        var flags = this.flags;
        if (features.ShouldSerializeenum_type())
        {
            flags = With(flags, FeatureFlags.EnumTypeOpen | FeatureFlags.EnumTypeClosed,
                features.enum_type switch
                {
                    FeatureSet.EnumType.EnumTypeUnknown => 0,
                    FeatureSet.EnumType.Open => FeatureFlags.EnumTypeOpen,
                    FeatureSet.EnumType.Closed => FeatureFlags.EnumTypeClosed,
                    _ => throw new ArgumentOutOfRangeException(nameof(features.enum_type)),
                });
        }
        if (features.ShouldSerializefield_presence())
        {
            flags = With(flags, FeatureFlags.FieldPresenceLegacyRequired, features.field_presence switch
            {
                FeatureSet.FieldPresence.FieldPresenceUnknown => 0,
                FeatureSet.FieldPresence.Implicit => FeatureFlags.FieldPresenceImplicit,
                FeatureSet.FieldPresence.Explicit => FeatureFlags.FieldPresenceExplicit,
                FeatureSet.FieldPresence.LegacyRequired => FeatureFlags.FieldPresenceLegacyRequired,
                _ => throw new ArgumentOutOfRangeException(nameof(features.field_presence)),
            });
        }
        if (features.ShouldSerializejson_format())
        {
            flags = With(flags, FeatureFlags.JsonFormatAllow | FeatureFlags.JsonFormatBestEffort, features.json_format switch
            {
                FeatureSet.JsonFormat.JsonFormatUnknown => 0,
                FeatureSet.JsonFormat.Allow => FeatureFlags.JsonFormatAllow,
                FeatureSet.JsonFormat.LegacyBestEffort => FeatureFlags.JsonFormatBestEffort,
                _ => throw new ArgumentOutOfRangeException(nameof(features.json_format)),
            });
        }
        if (features.ShouldSerializemessage_encoding())
        {
            flags = With(flags, FeatureFlags.MessageFormatLengthPrefixed | FeatureFlags.MessageFormatDelimited, features.message_encoding switch
            {
                FeatureSet.MessageEncoding.MessageEncodingUnknown => 0,
                FeatureSet.MessageEncoding.LengthPrefixed => FeatureFlags.MessageFormatLengthPrefixed,
                FeatureSet.MessageEncoding.Delimited => FeatureFlags.MessageFormatDelimited,
                _ => throw new ArgumentOutOfRangeException(nameof(features.message_encoding)),
            });
        }
        if (features.ShouldSerializerepeated_field_encoding())
        {
            flags = With(flags, FeatureFlags.RepeatedFieldEncodingPacked | FeatureFlags.
                RepeatedFieldEncodingExpanded, features.repeated_field_encoding switch
            {
                FeatureSet.RepeatedFieldEncoding.RepeatedFieldEncodingUnknown => 0,
                FeatureSet.RepeatedFieldEncoding.Packed => FeatureFlags.RepeatedFieldEncodingPacked,
                FeatureSet.RepeatedFieldEncoding.Expanded => FeatureFlags.RepeatedFieldEncodingExpanded,
                _ => throw new ArgumentOutOfRangeException(nameof(features.repeated_field_encoding)),
            });
        }
        if (features.ShouldSerializeutf8_validation())
        {
            flags = With(flags, FeatureFlags.Utf8ValidationVerify | FeatureFlags.
                Utf8ValidationNone, features.utf8_validation switch
                {
                    FeatureSet.Utf8Validation.Utf8ValidationUnknown => 0,
                    FeatureSet.Utf8Validation.Verify => FeatureFlags.Utf8ValidationVerify,
                    FeatureSet.Utf8Validation.None => FeatureFlags.Utf8ValidationNone,
                    _ => throw new ArgumentOutOfRangeException(nameof(features.utf8_validation)),
                });
        }

        return new(flags);
    }

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

    private static ParsedFeatures Proto2 { get; } = new ParsedFeatures(FeatureFlags.KindProto2);
    private static ParsedFeatures Proto3 { get; } = new ParsedFeatures(FeatureFlags.KindProto3);
    private static ParsedFeatures Edition2023 { get; } = new ParsedFeatures(FeatureFlags.KindEditions);
}