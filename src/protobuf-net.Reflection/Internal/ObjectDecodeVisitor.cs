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

        protected override object OnBeginMessage(FieldDescriptorProto field, DescriptorProto message)
        {
            base.OnBeginMessage(field, message);
            if (field is not null && Current is IDictionary<string, object> lookup && lookup.TryGetValue(GetName(field), out var existing))
            {
                return existing;
            }

            // create new holder object with initialized defaults
            var obj = new ExpandoObject();
            if (ApplyDefaultValues)
            {
                foreach (var f in message.Fields)
                {
                    if (f.label == FieldDescriptorProto.Label.LabelOptional && !f.Proto3Optional)
                    {
                        switch (f.type)
                        {
                            case FieldDescriptorProto.Type.TypeBool:
                                OnField(f, ParseDefaultBoolean(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeDouble:
                                OnField(f, ParseDefaultDouble(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeFloat:
                                OnField(f, ParseDefaultSingle(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeFixed32:
                            case FieldDescriptorProto.Type.TypeInt32:
                            case FieldDescriptorProto.Type.TypeSfixed32:
                            case FieldDescriptorProto.Type.TypeSint32:
                                OnField(f, ParseDefaultInt32(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeFixed64:
                            case FieldDescriptorProto.Type.TypeInt64:
                            case FieldDescriptorProto.Type.TypeSfixed64:
                            case FieldDescriptorProto.Type.TypeSint64:
                                OnField(f, ParseDefaultInt64(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeUint64:
                                OnField(f, ParseDefaultUInt64(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeUint32:
                                OnField(f, ParseDefaultUInt32(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeString:
                                OnField(f, ParseDefaultString(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeBytes:
                                OnField(f, ParseDefaultBytes(f.DefaultValue));
                                break;
                            case FieldDescriptorProto.Type.TypeEnum:
                                OnField(f, ParseDefaultEnum(f, out var value), value);
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

        protected override object OnBeginRepeated(FieldDescriptorProto field)
        {
            base.OnBeginRepeated(field);
            if (field is not null && Current is IDictionary<string, object> lookup && lookup.TryGetValue(GetName(field), out var existing))
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
                FieldDescriptorProto.Type.TypeEnum => new List<int>(),
                FieldDescriptorProto.Type.TypeSfixed32 => new List<int>(),
                FieldDescriptorProto.Type.TypeSfixed64 => new List<long>(),
                FieldDescriptorProto.Type.TypeSint32 => new List<int>(),
                FieldDescriptorProto.Type.TypeSint64 => new List<long>(),
                _ => new List<object>(),
            };
        }

        private static bool IsRepeated(FieldDescriptorProto field) => field.label == FieldDescriptorProto.Label.LabelRepeated;

        protected override void OnEndMessage(FieldDescriptorProto field, DescriptorProto message)
        {
            if (field is not null & Current is not null)
            {
                if (IsRepeated(field))
                {
                    if (Parent is IList list) list.Add(Current);
                }
                else 
                {
                    if (Parent is IDictionary<string, object> lookup)
                    {
                        lookup[GetName(field)] = Current;
                    }
                }
            }
            base.OnEndMessage(field, message);
        }

        private void Store<T>(FieldDescriptorProto field, T value, Func<T, object> box = null)
        {
            if (IsRepeated(field))
            {
                if (Current is IList<T> typed) typed.Add(value);
                else if (Current is IList untyped) untyped.Add(box is null ? value : box(value));
            }
            else
            {
                if (Current is IDictionary<string, object> lookup) lookup[GetName(field)] = box is null ? value : box(value);
            }
        }

        protected override void OnEndRepeated(FieldDescriptorProto field)
        {
            if (Parent is IDictionary<string, object> lookup)
            {
                lookup[GetName(field)] = Current;
            }
            base.OnEndRepeated(field);
        }

        static readonly object BoxedTrue = true, BoxedFalse = false;
        protected override void OnField(FieldDescriptorProto field, bool value) => Store(field, value, static value => value ? BoxedTrue : BoxedFalse);

        protected override void OnField(FieldDescriptorProto field, byte[] value) => Store(field, value);

        protected override void OnField(FieldDescriptorProto field, double value) => Store(field, value); // TODO: box handling for 0/1/-1?
        protected override void OnField(FieldDescriptorProto field, float value) => Store(field, value); // TODO: box handling for 0/1/-1?
        protected override void OnField(FieldDescriptorProto field, int value) => Store(field, value); // TODO: box handling for 0/small/-1?
        protected override void OnField(FieldDescriptorProto field, long value) => Store(field, value); // TODO: box handling for 0/small/-1?
        protected override void OnField(FieldDescriptorProto field, string value) => Store(field, value);
        protected override void OnField(FieldDescriptorProto field, uint value) => Store(field, value); // TODO: box handling for 0/small?
        protected override void OnField(FieldDescriptorProto field, ulong value) => Store(field, value); // TODO: box handling for 0/small?
        protected override void OnField(FieldDescriptorProto field, EnumValueDescriptorProto @enum, int value)
        {
            switch (Enums)
            {
                case EnumMode.Name:
                    var name = @enum?.Name;
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        Store(field, name);
                        return;
                    }
                    break;
            }
            Store(field, value); // TODO: box handling for 0/small/-1?
        }

        protected override void OnFieldFallback(FieldDescriptorProto field, string value)
            => throw new NotImplementedException("Unexpected usage of " + nameof(OnFieldFallback));
    }
}
