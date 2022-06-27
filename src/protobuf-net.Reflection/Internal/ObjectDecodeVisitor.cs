using Google.Protobuf.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;

namespace ProtoBuf.Internal
{

    internal class ObjectDecodeVisitor : DecodeVisitor
    {
        public new ExpandoObject Visit(Stream stream, FileDescriptorProto file, string rootMessageType)
            => (ExpandoObject)base.Visit(stream, file, rootMessageType);
        public enum EnumMode
        {
            Value,
            Name,
        }
        public enum FieldNameMode
        {
            Name,
            JsonName,
        }
        public bool ApplyDefaultValues { get; set; } = true;
        public FieldNameMode FieldNames { get; set; } = FieldNameMode.Name;
        public EnumMode Enums { get; set; } = EnumMode.Value;

        public static ObjectDecodeVisitor ForJson()
            => new ObjectDecodeVisitor { FieldNames = FieldNameMode.JsonName, Enums = EnumMode.Name };

        private string GetName(FieldDescriptorProto field)
        {
            switch (FieldNames)
            {
                case FieldNameMode.JsonName:
                    var name = field.JsonName;
                    if (!string.IsNullOrWhiteSpace(name)) return name;
                    break;
            }
            return field.Name;
        }

        protected override object OnBeginMessage(in VisitContext callingContext, FieldDescriptorProto field)
        {
            base.OnBeginMessage(in callingContext, field);
            if (field is not null && callingContext.Current is IDictionary<string, object> lookup && lookup.TryGetValue(GetName(field), out var existing))
            {
                return existing;
            }

            // create new holder object with initialized defaults
            var obj = new ExpandoObject();
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


        protected override object OnBeginMap<TKey, TValue>(in VisitContext ctx, FieldDescriptorProto field)
        {
            base.OnBeginMap<TKey, TValue>(in ctx, field);
            if (field is not null && ctx.Current is IDictionary<string, object> lookup)
            {
                var name = GetName(field);
                if (lookup.TryGetValue(name, out var existing))
                {
                    return existing;
                }
                return lookup[name] = new Dictionary<TKey, TValue>();
            }
            return null;
        }

        protected override void OnMapEntry<TKey, TValue>(in VisitContext ctx, FieldDescriptorProto.Type valueType, TKey key, TValue value)
        {
            base.OnMapEntry(in ctx, valueType, key, value);
            if (ctx.Current is Dictionary<TKey, TValue> typed)
            {
                typed[key] = value;
            }
        }

        protected override object OnBeginRepeated(in VisitContext ctx, FieldDescriptorProto field)
        {
            base.OnBeginRepeated(in ctx, field);
            if (field is not null && ctx.Current is IDictionary<string, object> lookup && lookup.TryGetValue(GetName(field), out var existing))
            {
                return existing;
            }

            return field.type switch
            {
                FieldDescriptorProto.Type.TypeBool => new List<bool>(),
                FieldDescriptorProto.Type.TypeDouble => new List<double>(),
                FieldDescriptorProto.Type.TypeFloat => new List<float>(),
                FieldDescriptorProto.Type.TypeInt64 => new List<long>(),
                FieldDescriptorProto.Type.TypeUint64 => new List<ulong>(),
                FieldDescriptorProto.Type.TypeInt32 => new List<int>(),
                FieldDescriptorProto.Type.TypeFixed64 => new List<ulong>(),
                FieldDescriptorProto.Type.TypeFixed32 => new List<uint>(),
                FieldDescriptorProto.Type.TypeString => new List<string>(),
                FieldDescriptorProto.Type.TypeBytes => new List<byte[]>(),
                FieldDescriptorProto.Type.TypeUint32 => new List<uint>(),
                FieldDescriptorProto.Type.TypeEnum when Enums == EnumMode.Name => new List<object>(),
                FieldDescriptorProto.Type.TypeEnum => new List<int>(),
                FieldDescriptorProto.Type.TypeSfixed32 => new List<int>(),
                FieldDescriptorProto.Type.TypeSfixed64 => new List<long>(),
                FieldDescriptorProto.Type.TypeSint32 => new List<int>(),
                FieldDescriptorProto.Type.TypeSint64 => new List<long>(),
                FieldDescriptorProto.Type.TypeMessage => new List<ExpandoObject>(),
                FieldDescriptorProto.Type.TypeGroup => new List<ExpandoObject>(),
                _ => throw new NotSupportedException($"Unexpected field type: {field.type}"),
            };
        }

        private static bool IsRepeated(FieldDescriptorProto field) => field.label == FieldDescriptorProto.Label.LabelRepeated;

        protected override void OnEndMessage(in VisitContext ctx, FieldDescriptorProto field)
        {
            if (field is not null & ctx.Current is not null)
            {
                if (IsRepeated(field))
                {
                    if (ctx.Parent is IList list) list.Add(ctx.Current);
                }
                else 
                {
                    if (ctx.Parent is IDictionary<string, object> lookup)
                    {
                        lookup[GetName(field)] = ctx.Current;
                    }
                }
            }
            base.OnEndMessage(in ctx, field);
        }

        private void Store<T>(in VisitContext ctx, FieldDescriptorProto field, T value, Func<T, object> box = null)
        {
            if (IsRepeated(field))
            {
                if (ctx.Current is IList<T> typed) typed.Add(value);
                else if (ctx.Current is IList untyped) untyped.Add(box is null ? value : box(value));
            }
            else
            {
                if (ctx.Current is IDictionary<string, object> lookup) lookup[GetName(field)] = box is null ? value : box(value);
            }
        }

        protected override void OnEndRepeated(in VisitContext ctx, FieldDescriptorProto field)
        {
            if (ctx.Parent is IDictionary<string, object> lookup)
            {
                lookup[GetName(field)] = ctx.Current;
            }
            base.OnEndRepeated(in ctx, field);
        }

        static readonly object BoxedTrue = true, BoxedFalse = false;
        protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, bool value) => Store(in ctx, field, value, static value => value ? BoxedTrue : BoxedFalse);

        protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, byte[] value) => Store(in ctx, field, value);

        protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, double value) => Store(in ctx, field, value); // TODO: box handling for 0/1/-1?
        protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, float value) => Store(in ctx, field, value); // TODO: box handling for 0/1/-1?
        protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, int value)
        {
            if (value == 0) System.Diagnostics.Debugger.Break();
            Store(in ctx, field, value); // TODO: box handling for 0/small/-1?
        }
        protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, long value) => Store(in ctx, field, value); // TODO: box handling for 0/small/-1?
        protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, string value) => Store(in ctx, field, value);
        protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, uint value) => Store(in ctx, field, value); // TODO: box handling for 0/small?
        protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, ulong value) => Store(in ctx, field, value); // TODO: box handling for 0/small?
        protected override void OnField(in VisitContext ctx, FieldDescriptorProto field, EnumValueDescriptorProto @enum, int value)
        {
            switch (Enums)
            {
                case EnumMode.Name:
                    var name = @enum?.Name;
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        Store(in ctx, field, name);
                        return;
                    }
                    break;
            }
            Store(in ctx, field, value); // TODO: box handling for 0/small/-1?
        }

        protected override void OnFieldFallback(in VisitContext ctx, FieldDescriptorProto field, string value)
            => throw new NotImplementedException("Unexpected usage of " + nameof(OnFieldFallback));
    }
}
