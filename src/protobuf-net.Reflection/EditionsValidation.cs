using ProtoBuf.Reflection;
using ProtoBuf.Reflection.Internal;
using System;
using System.Collections.Generic;

namespace Google.Protobuf.Reflection
{
    partial class FileDescriptorProto
    {
        [Flags]
        internal enum FeatureTarget
        {
            None = 0,
            File = 1 << 0,
            ExtensionRange = 1 << 1,
            Message = 1 << 2,
            Field = 1 << 3,
            Oneof = 1 << 4,
            Enum = 1 << 5,
            EnumEntry = 1 << 6,
            Service = 1 << 7,
            Method = 1 << 8,
        }

        /// <summary>
        /// The rules protoc enforces around editions features: which targets each feature may be
        /// set on, which editions it exists in, the shapes that cannot state presence, and the
        /// consequences of the resolved features (open enums need a zero, implicit presence
        /// cannot have defaults, naming style, symbol visibility). Runs after feature and type
        /// resolution; message texts follow the gist of protoc's own.
        /// </summary>
        internal void ValidateFeatures(ParserContext ctx)
        {
            var isEditions = Syntax == SyntaxEditions;

            CheckFeatureUse(ctx, Options?.Features, FeatureTarget.File, "file", FirstToken);
            foreach (var message in MessageTypes) ValidateMessage(ctx, message, isEditions);
            foreach (var @enum in EnumTypes) ValidateEnum(ctx, @enum, isEditions);
            foreach (var extension in Extensions) ValidateField(ctx, extension, isEditions, insideOneof: false);
            foreach (var service in Services)
            {
                CheckFeatureUse(ctx, service.Options?.Features, FeatureTarget.Service, $"service '{service.Name}'", FirstToken);
                var svcFeatures = ResolvedFeatures.Apply(service.Options?.Features);
                CheckName(ctx, svcFeatures, service.Name, NameStyle.TitleCase, "service", FirstToken);
                foreach (var method in service.Methods)
                {
                    CheckFeatureUse(ctx, method.Options?.Features, FeatureTarget.Method, $"method '{method.Name}'", FirstToken);
                    var methodFeatures = svcFeatures.Apply(method.Options?.Features);
                    CheckName(ctx, methodFeatures, method.Name, NameStyle.TitleCase, "method", FirstToken);
                }
            }
            if (!string.IsNullOrEmpty(Package))
            {
                foreach (var part in Package.Split('.'))
                {
                    CheckName(ctx, ResolvedFeatures, part, NameStyle.LowerSnakeCase, "package", FirstToken);
                }
            }
        }

        private void ValidateMessage(ParserContext ctx, DescriptorProto message, bool isEditions)
        {
            CheckFeatureUse(ctx, message.Options?.Features, FeatureTarget.Message, $"message '{message.Name}'", message.SourceLocation);
            CheckName(ctx, message.ResolvedFeatures, message.Name, NameStyle.TitleCase, "message", message.SourceLocation);

            var oneofFields = new HashSet<int>();
            foreach (var oneof in message.OneofDecls)
            {
                CheckFeatureUse(ctx, oneof.Options?.Features, FeatureTarget.Oneof, $"oneof '{oneof.Name}'", message.SourceLocation);
                var oneofFeatures = message.ResolvedFeatures.Apply(oneof.Options?.Features);
                CheckName(ctx, oneofFeatures, oneof.Name, NameStyle.LowerSnakeCase, "oneof", message.SourceLocation);
            }
            foreach (var field in message.Fields)
            {
                bool insideOneof = field.ShouldSerializeOneofIndex() && !field.Proto3Optional;
                ValidateField(ctx, field, isEditions, insideOneof);
                CheckVisibility(ctx, field);
            }
            foreach (var extension in message.Extensions) ValidateField(ctx, extension, isEditions, insideOneof: false);
            foreach (var range in message.ExtensionRanges)
            {
                CheckFeatureUse(ctx, range.Options?.Features, FeatureTarget.ExtensionRange, "extension range", message.SourceLocation);
            }
            foreach (var nested in message.NestedTypes) ValidateMessage(ctx, nested, isEditions);
            foreach (var @enum in message.EnumTypes) ValidateEnum(ctx, @enum, isEditions);
        }

        private void ValidateField(ParserContext ctx, FieldDescriptorProto field, bool isEditions, bool insideOneof)
        {
            var stated = field.Options?.Features;
            CheckFeatureUse(ctx, stated, FeatureTarget.Field, $"field '{field.Name}'", field.TypeToken);
            CheckName(ctx, field.ResolvedFeatures, field.Name, NameStyle.LowerSnakeCase, "field", field.TypeToken);

            if (stated is not null && stated.ShouldSerializefield_presence())
            {
                if (field.label == FieldDescriptorProto.Label.LabelRepeated)
                {
                    ctx.Errors.Error(field.TypeToken, "repeated fields cannot specify field presence", ErrorCode.FieldPresenceNotAllowed);
                }
                else if (insideOneof)
                {
                    ctx.Errors.Error(field.TypeToken, "oneof fields cannot specify field presence", ErrorCode.FieldPresenceNotAllowed);
                }
            }

            if (isEditions
                && field.ShouldSerializeDefaultValue()
                && field.label != FieldDescriptorProto.Label.LabelRepeated
                && field.ResolvedFeatures.FieldPresence == FeatureSet.FieldPresence.Implicit)
            {
                ctx.Errors.Error(field.TypeToken, "implicit-presence fields cannot specify a default value", ErrorCode.EditionsDefaultOnImplicit);
            }
        }

        private void ValidateEnum(ParserContext ctx, EnumDescriptorProto @enum, bool isEditions)
        {
            CheckFeatureUse(ctx, @enum.Options?.Features, FeatureTarget.Enum, $"enum '{@enum.Name}'", @enum.SourceLocation);
            CheckName(ctx, @enum.ResolvedFeatures, @enum.Name, NameStyle.TitleCase, "enum", @enum.SourceLocation);
            foreach (var value in @enum.Values)
            {
                CheckFeatureUse(ctx, value.Options?.Features, FeatureTarget.EnumEntry, $"enum value '{value.Name}'", @enum.SourceLocation);
                var valueFeatures = @enum.ResolvedFeatures.Apply(value.Options?.Features);
                CheckName(ctx, valueFeatures, value.Name, NameStyle.UpperSnakeCase, "enum value", @enum.SourceLocation);
            }

            if (@enum.ResolvedFeatures.EnumType == FeatureSet.EnumType.Open
                && @enum.Values.Count != 0 && @enum.Values[0].Number != 0)
            {
                ctx.Errors.Error(@enum.SourceLocation, $"the first value of open enum '{@enum.Name}' must be zero", ErrorCode.OpenEnumFirstValueNotZero);
            }
        }

        /// <summary>
        /// A cross-file type reference must target a visible symbol: not explicitly 'local', and
        /// not local by the declaring file's default (edition 2024's EXPORT_TOP_LEVEL makes
        /// nested types local by default; LOCAL_ALL and STRICT make everything so).
        /// </summary>
        private void CheckVisibility(ParserContext ctx, FieldDescriptorProto field)
        {
            var resolved = field.ResolvedType;
            if (resolved is null) return;
            var declaringFile = GetFile(resolved);
            if (declaringFile is null || ReferenceEquals(declaringFile, this)) return;

            SymbolVisibility visibility;
            string name;
            bool nested;
            switch (resolved)
            {
                case DescriptorProto message:
                    visibility = message.Visibility;
                    name = message.Name;
                    nested = message.Parent is DescriptorProto;
                    break;
                case EnumDescriptorProto @enum:
                    visibility = @enum.Visibility;
                    name = @enum.Name;
                    nested = @enum.Parent is DescriptorProto;
                    break;
                default:
                    return;
            }

            switch (visibility)
            {
                case SymbolVisibility.VisibilityExport:
                    return;
                case SymbolVisibility.VisibilityLocal:
                    ctx.Errors.Error(field.TypeToken, $"'{name}' (defined in '{declaringFile.Name}') is explicitly marked 'local', and is not visible from '{Name}'", ErrorCode.SymbolNotVisible);
                    return;
            }

            // unset: the declaring file's default decides
            switch (declaringFile.ResolvedFeatures.DefaultSymbolVisibility)
            {
                case FeatureSet.VisibilityFeature.DefaultSymbolVisibility.ExportTopLevel when nested:
                    ctx.Errors.Error(field.TypeToken, $"'{name}' (defined in '{declaringFile.Name}') is a nested type, which defaults to 'local' in edition 2024, and is not visible from '{Name}'; mark it 'export' to opt in", ErrorCode.SymbolNotVisible);
                    break;
                case FeatureSet.VisibilityFeature.DefaultSymbolVisibility.LocalAll:
                case FeatureSet.VisibilityFeature.DefaultSymbolVisibility.Strict:
                    ctx.Errors.Error(field.TypeToken, $"'{name}' (defined in '{declaringFile.Name}') defaults to 'local', and is not visible from '{Name}'; mark it 'export' to opt in", ErrorCode.SymbolNotVisible);
                    break;
            }
        }

        private void CheckFeatureUse(ParserContext ctx, FeatureSet features, FeatureTarget target, string where, Token token)
        {
            if (features is null) return;

            Check(features.ShouldSerializefield_presence(), "field_presence", FeatureTarget.Field | FeatureTarget.File, Edition.Edition2023);
            Check(features.ShouldSerializeenum_type(), "enum_type", FeatureTarget.Enum | FeatureTarget.File, Edition.Edition2023);
            Check(features.ShouldSerializerepeated_field_encoding(), "repeated_field_encoding", FeatureTarget.Field | FeatureTarget.File, Edition.Edition2023);
            Check(features.ShouldSerializeutf8_validation(), "utf8_validation", FeatureTarget.Field | FeatureTarget.File, Edition.Edition2023);
            Check(features.ShouldSerializemessage_encoding(), "message_encoding", FeatureTarget.Field | FeatureTarget.File, Edition.Edition2023);
            Check(features.ShouldSerializejson_format(), "json_format", FeatureTarget.Message | FeatureTarget.Enum | FeatureTarget.File, Edition.Edition2023);
            Check(features.ShouldSerializeenforce_naming_style(), "enforce_naming_style",
                FeatureTarget.File | FeatureTarget.ExtensionRange | FeatureTarget.Message | FeatureTarget.Field | FeatureTarget.Oneof
                | FeatureTarget.Enum | FeatureTarget.EnumEntry | FeatureTarget.Service | FeatureTarget.Method, Edition.Edition2024);
            Check(features.ShouldSerializeDefaultSymbolVisibility(), "default_symbol_visibility", FeatureTarget.File, Edition.Edition2024);

            void Check(bool isSet, string feature, FeatureTarget allowed, Edition introduced)
            {
                if (!isSet) return;
                if ((allowed & target) == 0)
                {
                    ctx.Errors.Error(token, $"features.{feature} cannot be set on a {where.Split(' ')[0]}", ErrorCode.FeatureInvalidTarget);
                }
                if (ctx.Edition < introduced)
                {
                    ctx.Errors.Error(token, $"features.{feature} was not introduced until edition {(introduced == Edition.Edition2024 ? "2024" : "2023")}, and cannot be used here", ErrorCode.FeatureNotIntroduced);
                }
            }
        }

        private enum NameStyle
        {
            TitleCase,
            LowerSnakeCase,
            UpperSnakeCase,
        }

        private void CheckName(ParserContext ctx, in ParsedFeatures features, string name, NameStyle style, string kind, Token token)
        {
            if (features.EnforceNamingStyle != FeatureSet.EnforceNamingStyle.Style2024) return;
            if (string.IsNullOrEmpty(name)) return;

            bool ok;
            string expected;
            switch (style)
            {
                case NameStyle.TitleCase:
                    ok = char.IsUpper(name[0]) && name.IndexOf('_') < 0;
                    expected = "TitleCase";
                    break;
                case NameStyle.LowerSnakeCase:
                    ok = char.IsLower(name[0]);
                    foreach (var c in name)
                    {
                        if (!(char.IsLower(c) || char.IsDigit(c) || c == '_')) { ok = false; break; }
                    }
                    expected = "lower_snake_case";
                    break;
                default: // UpperSnakeCase
                    ok = char.IsUpper(name[0]);
                    foreach (var c in name)
                    {
                        if (!(char.IsUpper(c) || char.IsDigit(c) || c == '_')) { ok = false; break; }
                    }
                    expected = "UPPER_SNAKE_CASE";
                    break;
            }
            if (!ok)
            {
                ctx.Errors.Error(token, $"{kind} name '{name}' should be {expected} (features.enforce_naming_style = STYLE_LEGACY can be used to opt out)", ErrorCode.NamingStyle);
            }
        }
    }
}
