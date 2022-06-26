using Google.Protobuf.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace ProtoBuf.Internal
{

    internal class ObjectDecodeVisitor : DecodeVisitor
    {
        public Func<FieldDescriptorProto, string> FieldNameSelector { get; set; }

        private string GetName(FieldDescriptorProto field)
            => FieldNameSelector?.Invoke(field) ?? field.Name;

        public static class FieldNameSelectors
        {
            public static Func<FieldDescriptorProto, string> Default { get; } = field => field.Name;
            public static Func<FieldDescriptorProto, string> Json { get; } = field =>
            {
                var jsonName = field.JsonName;
                return string.IsNullOrWhiteSpace(jsonName) ? field.Name : jsonName;
            };

        }

        protected override object OnBeginMessage(FieldDescriptorProto field, DescriptorProto message)
        {
            base.OnBeginMessage(field, message);
            if (field is not null && Current is IDictionary<string, object> lookup && lookup.TryGetValue(GetName(field), out var existing))
            {
                return existing;
            }
            return new ExpandoObject();
        }

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
        protected override void OnField(FieldDescriptorProto field, EnumValueDescriptorProto @enum, int value) => Store(field, value); // TODO: box handling for 0/small/-1?

        protected override void OnFieldFallback(FieldDescriptorProto field, string value)
            => throw new NotImplementedException("Unexpected usage of " + nameof(OnFieldFallback));
    }
}
