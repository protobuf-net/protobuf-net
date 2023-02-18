using Google.Protobuf.Reflection;
using ProtoBuf.Reflection.Internal;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;

namespace ProtoBuf.Reflection
{
    /// <summary>
    /// Visitor implementation for interpreting protobuf payloads into ExpandoObject/dynamic objects from a runtime-provided schema
    /// </summary>
    public class ExpandoObjectDecodeVisitor : ObjectDecodeVisitor
    {
        /// <summary>
        /// Creates a visitor configured for typical JSON scenarios
        /// </summary>
        public static ExpandoObjectDecodeVisitor ForJson()
            => new ExpandoObjectDecodeVisitor { FieldNames = FieldNameMode.JsonName, Enums = EnumMode.Name };

        /// <inheritdoc/>
        protected override object CreateMessageObject(in VisitContext callingContext, FieldDescriptorProto field) => new ExpandoObject();

        /// <inheritdoc/>
        public new ExpandoObject Visit(ReadOnlyMemory<byte> source, FileDescriptorSet schema, string rootMessageType)
            => (ExpandoObject)base.Visit(source, schema, rootMessageType);

        /// <inheritdoc/>
        public new ExpandoObject Visit(ReadOnlySequence<byte> source, FileDescriptorSet schema, string rootMessageType)
            => (ExpandoObject)base.Visit(source, schema, rootMessageType);

        /// <inheritdoc/>
        public new ExpandoObject Visit(Stream source, FileDescriptorSet schema, string rootMessageType)
            => (ExpandoObject)base.Visit(source, schema, rootMessageType);

        /// <inheritdoc/>
        public new ExpandoObject Visit(ref ProtoReader.State source, FileDescriptorSet schema, string rootMessageType)
            => (ExpandoObject)base.Visit(ref source, schema, rootMessageType);
    }

    /// <summary>
    /// Visitor implementation for interpreting protobuf payloads into objects from a runtime-provided schema
    /// </summary>
    public abstract class ObjectDecodeVisitor : DecodeVisitor
    {
        /// <summary>
        /// Indicates how enum values should be interpreted
        /// </summary>
        public enum EnumMode
        {
            /// <summary>
            /// Use the integer value of the enum
            /// </summary>
            Value,
            /// <summary>
            /// Use the string name of the enum definition
            /// </summary>
            Name,
        }

        /// <summary>
        /// Indicates how field names should be interpreted
        /// </summary>
        public enum FieldNameMode
        {
            /// <summary>
            /// Use the defined name of the field
            /// </summary>
            Name,
            /// <summary>
            /// Use the defined json name of the field
            /// </summary>
            JsonName,
            /// <summary>
            /// Use a custom field name handler <see cref="CustomFieldName"/>
            /// </summary>
            Custom,
        }
        /// <summary>
        /// Indicates whether default values should be applied
        /// </summary>
        public bool ApplyDefaultValues { get; set; } = true;

        /// <summary>
        /// Allows for custom field name interpretations
        /// </summary>
        public Func<FieldDescriptorProto, string> CustomFieldName { get; set; }

        /// <summary>
        /// Indicates how field names should be interpreted
        /// </summary>
        public FieldNameMode FieldNames { get; set; } = FieldNameMode.Name;

        /// <summary>
        /// Indicates how enum values should be interpreted
        /// </summary>
        public EnumMode Enums { get; set; } = EnumMode.Value;

        /// <summary>
        /// Get the effective name of a given field
        /// </summary>
        protected virtual string GetName(FieldDescriptorProto field)
        {
            string name;
            switch (FieldNames)
            {
                case FieldNameMode.JsonName:
                    name = field.JsonName;
                    break;
                case FieldNameMode.Custom:
                    name = CustomFieldName?.Invoke(field);
                    break;
                default:
                    return field.Name;
            }
            // double-check value from json/cusom is valid
            if (string.IsNullOrWhiteSpace(name))
            {
                name = field.Name;
            }
            return name;
        }

        /// <summary>
        /// Provides a new message object of the given type
        /// </summary>

        protected abstract object CreateMessageObject(in VisitContext context, FieldDescriptorProto field);

        /// <summary>
        /// Attempt to obtain an already-existing message instance of a given field
        /// </summary>
        protected virtual bool TryGetObject(in VisitContext context, FieldDescriptorProto field, out object existing)
        {
            if (field is not null && context.Current is IDictionary<string, object> lookup && lookup.TryGetValue(GetName(field), out existing))
            {
                return true;
            }
            existing = default;
            return false;
        }

        /// <summary>
        /// Gets the suggested type for a given field
        /// </summary>
        protected virtual Type GetSuggestedType(FieldDescriptorProto field) => field.type switch
        {
            FieldDescriptorProto.Type.TypeBool => typeof(bool),
            FieldDescriptorProto.Type.TypeDouble => typeof(double),
            FieldDescriptorProto.Type.TypeFloat => typeof(float),
            FieldDescriptorProto.Type.TypeFixed32 or FieldDescriptorProto.Type.TypeInt32
                or FieldDescriptorProto.Type.TypeSfixed32 or FieldDescriptorProto.Type.TypeSint32 => typeof(int),
            FieldDescriptorProto.Type.TypeFixed64 or FieldDescriptorProto.Type.TypeInt64
                or FieldDescriptorProto.Type.TypeSfixed64 or FieldDescriptorProto.Type.TypeSint64 => typeof(long),
            FieldDescriptorProto.Type.TypeUint64 => typeof(ulong),
            FieldDescriptorProto.Type.TypeUint32 => typeof(uint),
            FieldDescriptorProto.Type.TypeString => typeof(string),
            FieldDescriptorProto.Type.TypeBytes => typeof(byte[]),
            FieldDescriptorProto.Type.TypeEnum => Enums == EnumMode.Name ? typeof(string) : typeof(int),
            _ => typeof(object),
        };

        private protected override object OnBeginMessage(in VisitContext callingContext, FieldDescriptorProto field)
        {
            base.OnBeginMessage(in callingContext, field);
            if (TryGetObject(in callingContext, field, out var existing))
            {
                return existing;
            }

            // create new holder object with initialized defaults
            var obj = CreateMessageObject(in callingContext, field);
            var ctx = callingContext.StepIn(obj);
            if (ApplyDefaultValues)
            {
                foreach (var f in ctx.MessageType.Fields)
                {
                    if (f.label == FieldDescriptorProto.Label.LabelOptional && !f.Proto3Optional)
                    {
                        switch (f.type)
                        {
                            case FieldDescriptorProto.Type.TypeBool:
                                OnField(in ctx, f, ParseDefaultBoolean(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeDouble:
                                OnField(in ctx, f, ParseDefaultDouble(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeFloat:
                                OnField(in ctx, f, ParseDefaultSingle(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeFixed32:
                            case FieldDescriptorProto.Type.TypeInt32:
                            case FieldDescriptorProto.Type.TypeSfixed32:
                            case FieldDescriptorProto.Type.TypeSint32:
                                OnField(in ctx, f, ParseDefaultInt32(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeFixed64:
                            case FieldDescriptorProto.Type.TypeInt64:
                            case FieldDescriptorProto.Type.TypeSfixed64:
                            case FieldDescriptorProto.Type.TypeSint64:
                                OnField(in ctx, f, ParseDefaultInt64(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeUint64:
                                OnField(in ctx, f, ParseDefaultUInt64(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeUint32:
                                OnField(in ctx, f, ParseDefaultUInt32(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeString:
                                OnField(in ctx, f, ParseDefaultString(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeBytes:
                                OnField(in ctx, f, ParseDefaultBytes(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeEnum:
                                OnField(in ctx, f, ParseDefaultEnum(f, out var value), value);
                                break;
                            default:
                                if (!string.IsNullOrEmpty(f.DefaultValue))
                                {
                                    throw new NotSupportedException($"Unhandled default type: {f.type}: '{f.DefaultValue}'");
                                }
                                break;

                        }
                    }
                }
            }
            return obj;
        }

        private EnumValueDescriptorProto ParseDefaultEnum(FieldDescriptorProto field, out int value)
        {
            if (TryGetEnumType(field, out var enumDescriptor))
            {
                if (string.IsNullOrEmpty(field.DefaultValue))
                {
                    if (enumDescriptor.Values.Count == 0)
                    {
                        value = 0;
                        return null;
                    }
                    var tmp = enumDescriptor.Values[0];
                    value = tmp.Number;
                    return tmp;

                }
                foreach (var defined in enumDescriptor.Values)
                {
                    if (defined.Name == field.DefaultValue)
                    {
                        value = defined.Number;
                        return defined;
                    }
                }
                value = ParseDefaultInt32(field.DefaultValue);
                foreach (var defined in enumDescriptor.Values)
                {
                    if (defined.Number == value)
                    {
                        return defined;
                    }
                }
                return null;
            }
            throw new FormatException();
        }

        private byte[] ParseDefaultBytes(string defaultValue)
        {
            if (string.IsNullOrEmpty(defaultValue)) return Array.Empty<byte>();
            throw new FormatException();
        }

        private double ParseDefaultDouble(string defaultValue)
        {
            if (string.IsNullOrEmpty(defaultValue)) return 0;
            if (defaultValue == "+inf") return double.PositiveInfinity;
            if (defaultValue == "-inf") return double.NegativeInfinity;
            if (double.TryParse(defaultValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)) return value;
            throw new FormatException();
        }

        private bool ParseDefaultBoolean(string defaultValue)
        {
            if (string.IsNullOrEmpty(defaultValue)) return false;
            if (!bool.TryParse(defaultValue, out var tmp)) return tmp;
            return defaultValue switch
            {
                "0" => false,
                "1" => true,
                _ => throw new FormatException(),
            };
        }

        private float ParseDefaultSingle(string defaultValue)
            => (float)ParseDefaultDouble(defaultValue);

        private int ParseDefaultInt32(string defaultValue)
            => checked((int)ParseDefaultUInt64(defaultValue));

        private long ParseDefaultInt64(string defaultValue)
        {
            if (string.IsNullOrEmpty(defaultValue)) return 0;
            if (long.TryParse(defaultValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)) return value;
            throw new FormatException();
        }

        private ulong ParseDefaultUInt64(string defaultValue)
        {
            if (string.IsNullOrEmpty(defaultValue)) return 0;
            if (ulong.TryParse(defaultValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)) return value;
            throw new FormatException();
        }

        private uint ParseDefaultUInt32(string defaultValue)
            => checked((uint)ParseDefaultUInt64(defaultValue));

        private string ParseDefaultString(string defaultValue)
            => defaultValue ?? "";


        /// <summary>
        /// Create a new map (dictionary) for the given suggested types
        /// </summary>
        protected virtual object CreateMapObject<TKey, TValue>(in VisitContext context, FieldDescriptorProto field) => new Dictionary<TKey, TValue>();

        private protected override object OnBeginMap<TKey, TValue>(in VisitContext ctx, FieldDescriptorProto field)
        {
            base.OnBeginMap<TKey, TValue>(in ctx, field);
            if (TryGetObject(ctx, field, out var existing))
            {
                return existing;
            }

            var obj = CreateMapObject<TKey, TValue>(in ctx, field);
            Store(ctx, field, obj, BoxFunctions.Object);
            return obj;
        }

        private protected override void OnMapEntry<TKey, TValue>(in VisitContext ctx, FieldDescriptorProto.Type valueType, TKey key, TValue value)
        {
            base.OnMapEntry(in ctx, valueType, key, value);
            if (ctx.Current is Dictionary<TKey, TValue> typed)
            {
                typed[key] = value;
            }
        }

        /// <summary>
        /// Create a new collection for the given suggested types
        /// </summary>
        protected virtual object CreateRepeatedObject<T>(in VisitContext context, FieldDescriptorProto field) => new List<T>();

        private protected override object OnBeginRepeated(in VisitContext ctx, FieldDescriptorProto field)
        {
            base.OnBeginRepeated(in ctx, field);
            if (TryGetObject(ctx, field, out var existing))
            {
                return existing;
            }
            return field.type switch
            {
                FieldDescriptorProto.Type.TypeBool => CreateRepeatedObject<bool>(ctx, field),
                FieldDescriptorProto.Type.TypeDouble => CreateRepeatedObject<double>(ctx, field),
                FieldDescriptorProto.Type.TypeFloat => CreateRepeatedObject<float>(ctx, field),
                FieldDescriptorProto.Type.TypeInt64 => CreateRepeatedObject<long>(ctx, field),
                FieldDescriptorProto.Type.TypeUint64 => CreateRepeatedObject<ulong>(ctx, field),
                FieldDescriptorProto.Type.TypeInt32 => CreateRepeatedObject<int>(ctx, field),
                FieldDescriptorProto.Type.TypeFixed64 => CreateRepeatedObject<ulong>(ctx, field),
                FieldDescriptorProto.Type.TypeFixed32 => CreateRepeatedObject<uint>(ctx, field),
                FieldDescriptorProto.Type.TypeString => CreateRepeatedObject<string>(ctx, field),
                FieldDescriptorProto.Type.TypeBytes => CreateRepeatedObject<byte[]>(ctx, field),
                FieldDescriptorProto.Type.TypeUint32 => CreateRepeatedObject<uint>(ctx, field),
                FieldDescriptorProto.Type.TypeEnum when Enums == EnumMode.Name => CreateRepeatedObject<string>(ctx, field),
                FieldDescriptorProto.Type.TypeEnum => CreateRepeatedObject<int>(ctx, field),
                FieldDescriptorProto.Type.TypeSfixed32 => CreateRepeatedObject<int>(ctx, field),
                FieldDescriptorProto.Type.TypeSfixed64 => CreateRepeatedObject<long>(ctx, field),
                FieldDescriptorProto.Type.TypeSint32 => CreateRepeatedObject<int>(ctx, field),
                FieldDescriptorProto.Type.TypeSint64 => CreateRepeatedObject<long>(ctx, field),
                FieldDescriptorProto.Type.TypeMessage => CreateRepeatedObject<object>(ctx, field),
                FieldDescriptorProto.Type.TypeGroup => CreateRepeatedObject<object>(ctx, field),
                _ => throw new NotSupportedException($"Unexpected field type: {field.type}"),
            };
        }

        private static bool IsRepeated(FieldDescriptorProto field) => field.label == FieldDescriptorProto.Label.LabelRepeated;

        private protected override void OnEndMessage(in VisitContext parentContext, in VisitContext ctx, FieldDescriptorProto field)
        {
            if (field is not null & ctx.Current is not null)
            {
                if (IsRepeated(field))
                {
                    if (ctx.Parent is IList list) list.Add(ctx.Current);
                }
                else 
                {
                    Store(in parentContext, field, ctx.Current, BoxFunctions.Object);
                }
            }
            base.OnEndMessage(in parentContext, in ctx, field);
        }

        /// <summary>
        /// Store a typed field value against the current instance
        /// </summary>
        protected virtual void Store<T>(in VisitContext ctx, FieldDescriptorProto field, T value, Func<T, object> box)
        {
            if (ctx.Index >= 0) // then is an append operation
            {
                if (ctx.Current is IList<T> typed) typed.Add(value);
                else if (ctx.Current is IList untyped) untyped.Add(box(value));
            }
            else
            {
                if (ctx.Current is IDictionary<string, object> lookup) lookup[GetName(field)] = box(value);
            }
        }

        private protected override void OnEndRepeated(in VisitContext parentContext, in VisitContext ctx, FieldDescriptorProto field)
        {
            Store(in parentContext, field, ctx.Current, BoxFunctions.Object);
            base.OnEndRepeated(in parentContext, in ctx, field);
        }

        private protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, bool value) => Store(in ctx, field, value, BoxFunctions.Boolean);
        private protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, byte[] value) => Store(in ctx, field, value, BoxFunctions.ByteArray);
        private protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, double value) => Store(in ctx, field, value, BoxFunctions.Double);
        private protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, float value) => Store(in ctx, field, value, BoxFunctions.Single);
        private protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, int value) => Store(in ctx, field, value, BoxFunctions.Int32);
        private protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, long value) => Store(in ctx, field, value, BoxFunctions.Int64);
        private protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, string value) => Store(in ctx, field, value, BoxFunctions.String);
        private protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, uint value) => Store(in ctx, field, value, BoxFunctions.UInt32);
        private protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, ulong value) => Store(in ctx, field, value, BoxFunctions.UInt64);
        private protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, EnumValueDescriptorProto @enum, int value)
        {
            switch (Enums)
            {
                case EnumMode.Name:
                    var name = @enum?.Name;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = BoxFunctions.Int32String(value, FormatProvider); // want to at least store a string to be type-consistent
                    }
                    Store(in ctx, field, name, BoxFunctions.String);
                    break;
                default:
                    Store(in ctx, field, value, BoxFunctions.Int32);
                    break;
            }
        }

        private protected override void OnFieldFallback(in VisitContext ctx, FieldDescriptorProto field, string value)
            => throw new NotImplementedException("Unexpected usage of " + nameof(OnFieldFallback));
    }
}
