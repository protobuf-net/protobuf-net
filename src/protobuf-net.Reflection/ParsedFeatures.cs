using System;

namespace Google.Protobuf.Reflection
{
    /// <summary>
    /// Fully-resolved editions feature values for a schema element. Resolution follows the
    /// protobuf editions model: per-edition defaults (the edition_defaults timelines declared
    /// in descriptor.proto), merged with explicitly-stated features, inherited lexically from
    /// the file down through messages to fields. The legacy syntaxes participate as the
    /// placeholder editions EDITION_PROTO2/EDITION_PROTO3, with the legacy constructs
    /// (required, group, [packed]) inferred onto the same axes, so that consumers can ask one
    /// question regardless of how the file spelled it.
    /// </summary>
    internal readonly struct ParsedFeatures
    {
        public FeatureSet.FieldPresence FieldPresence { get; }
        public FeatureSet.EnumType EnumType { get; }
        public FeatureSet.RepeatedFieldEncoding RepeatedFieldEncoding { get; }
        public FeatureSet.Utf8Validation Utf8Validation { get; }
        public FeatureSet.MessageEncoding MessageEncoding { get; }
        public FeatureSet.JsonFormat JsonFormat { get; }
        public FeatureSet.EnforceNamingStyle EnforceNamingStyle { get; }
        public FeatureSet.VisibilityFeature.DefaultSymbolVisibility DefaultSymbolVisibility { get; }

        private ParsedFeatures(
            FeatureSet.FieldPresence fieldPresence,
            FeatureSet.EnumType enumType,
            FeatureSet.RepeatedFieldEncoding repeatedFieldEncoding,
            FeatureSet.Utf8Validation utf8Validation,
            FeatureSet.MessageEncoding messageEncoding,
            FeatureSet.JsonFormat jsonFormat,
            FeatureSet.EnforceNamingStyle enforceNamingStyle,
            FeatureSet.VisibilityFeature.DefaultSymbolVisibility defaultSymbolVisibility)
        {
            FieldPresence = fieldPresence;
            EnumType = enumType;
            RepeatedFieldEncoding = repeatedFieldEncoding;
            Utf8Validation = utf8Validation;
            MessageEncoding = messageEncoding;
            JsonFormat = jsonFormat;
            EnforceNamingStyle = enforceNamingStyle;
            DefaultSymbolVisibility = defaultSymbolVisibility;
        }

        /// <summary>
        /// The feature defaults for a given edition, from the edition_defaults timelines in
        /// descriptor.proto: for each feature, the highest entry at-or-before the edition wins.
        /// </summary>
        public static ParsedFeatures Defaults(Edition edition) => new(
            fieldPresence: edition == Edition.EditionProto3
                ? FeatureSet.FieldPresence.Implicit : FeatureSet.FieldPresence.Explicit,
            enumType: edition < Edition.EditionProto3
                ? FeatureSet.EnumType.Closed : FeatureSet.EnumType.Open,
            repeatedFieldEncoding: edition < Edition.EditionProto3
                ? FeatureSet.RepeatedFieldEncoding.Expanded : FeatureSet.RepeatedFieldEncoding.Packed,
            utf8Validation: edition < Edition.EditionProto3
                ? FeatureSet.Utf8Validation.None : FeatureSet.Utf8Validation.Verify,
            messageEncoding: FeatureSet.MessageEncoding.LengthPrefixed,
            jsonFormat: edition < Edition.EditionProto3
                ? FeatureSet.JsonFormat.LegacyBestEffort : FeatureSet.JsonFormat.Allow,
            enforceNamingStyle: edition < Edition.Edition2024
                ? FeatureSet.EnforceNamingStyle.StyleLegacy : FeatureSet.EnforceNamingStyle.Style2024,
            defaultSymbolVisibility: edition < Edition.Edition2024
                ? FeatureSet.VisibilityFeature.DefaultSymbolVisibility.ExportAll
                : FeatureSet.VisibilityFeature.DefaultSymbolVisibility.ExportTopLevel);

        /// <summary>
        /// Merges explicitly-stated features over this set; unstated fields inherit.
        /// </summary>
        public ParsedFeatures Apply(FeatureSet features)
        {
            if (features is null) return this;
            return new(
                features.ShouldSerializefield_presence() ? features.field_presence : FieldPresence,
                features.ShouldSerializeenum_type() ? features.enum_type : EnumType,
                features.ShouldSerializerepeated_field_encoding() ? features.repeated_field_encoding : RepeatedFieldEncoding,
                features.ShouldSerializeutf8_validation() ? features.utf8_validation : Utf8Validation,
                features.ShouldSerializemessage_encoding() ? features.message_encoding : MessageEncoding,
                features.ShouldSerializejson_format() ? features.json_format : JsonFormat,
                features.ShouldSerializeenforce_naming_style() ? features.enforce_naming_style : EnforceNamingStyle,
                features.ShouldSerializeDefaultSymbolVisibility() ? features.DefaultSymbolVisibility : DefaultSymbolVisibility);
        }

        public ParsedFeatures With(FeatureSet.FieldPresence fieldPresence) => new(
            fieldPresence, EnumType, RepeatedFieldEncoding, Utf8Validation, MessageEncoding,
            JsonFormat, EnforceNamingStyle, DefaultSymbolVisibility);

        public ParsedFeatures With(FeatureSet.MessageEncoding messageEncoding) => new(
            FieldPresence, EnumType, RepeatedFieldEncoding, Utf8Validation, messageEncoding,
            JsonFormat, EnforceNamingStyle, DefaultSymbolVisibility);

        public ParsedFeatures With(FeatureSet.RepeatedFieldEncoding repeatedFieldEncoding) => new(
            FieldPresence, EnumType, repeatedFieldEncoding, Utf8Validation, MessageEncoding,
            JsonFormat, EnforceNamingStyle, DefaultSymbolVisibility);
    }
}
