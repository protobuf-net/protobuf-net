using Google.Protobuf.Reflection;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Reflection.Internal
{
    internal sealed class TextDecodeVisitor : DecodeVisitor
    {
        public TextWriter Output { get; }
        public TextDecodeVisitor(TextWriter output)
            => Output = output ?? throw new ArgumentNullException(nameof(output));
        public string Indent { get; set; } = " ";

        private protected override void Flush() => Output.Flush();

        private void WriteLine(string message)
        {
            for (int i = 0; i < Depth; i++)
                Output.Write(Indent);
            Output.WriteLine(message);
        }
        private protected override void OnUnkownField(in VisitContext ctx, ref ProtoReader.State reader)
        {
            WriteLine($"{reader.FieldNumber.ToString(FormatProvider)}: (unknown, {reader.WireType})");
            base.OnUnkownField(in ctx, ref reader); // skip the value
        }

        private protected override object OnBeginRepeated(in VisitContext ctx, FieldDescriptorProto field)
        {
            WriteLine($"{field.Number.ToString(FormatProvider)}: {field.Name}=[ ({field.type})");
            return base.OnBeginRepeated(in ctx, field);
        }
        private protected override void OnEndRepeated(in VisitContext parentContext, in VisitContext ctx, FieldDescriptorProto field)
        {
            base.OnEndRepeated(in parentContext, in ctx, field);
            WriteLine($"] // {field.Name}, count: {ctx.Index.ToString(FormatProvider)}");
        }
        private protected override void OnFieldFallback(in VisitContext ctx, FieldDescriptorProto field, string value)
        {
            if (ctx.Index < 0)
            {
                WriteLine($"{field.Number.ToString(FormatProvider)}: {field.Name}={value} ({field.type})");
            }
            else
            {
                WriteLine($"#{ctx.Index.ToString(FormatProvider)}={value}");
            }
        }

        private protected override object OnBeginMap<TKey, TValue>(in VisitContext ctx, FieldDescriptorProto field)
        {
            WriteLine($"{field.Number.ToString(FormatProvider)}: {field.Name}={{");
            return base.OnBeginMap<TKey, TValue>(in ctx, field);
        }
        private protected override void OnEndMap<TKey, TValue>(in VisitContext parentContext, in VisitContext ctx, FieldDescriptorProto field)
        {
            base.OnEndMap<TKey, TValue>(in parentContext, in ctx, field);
            WriteLine($"}} // {field.Name}, count: {ctx.Index.ToString(FormatProvider)}");
        }
        private protected override void OnMapEntry<TKey, TValue>(in VisitContext ctx, FieldDescriptorProto.Type valueType, TKey key, TValue value)
        {
            base.OnMapEntry(in ctx, valueType, key, value);
            switch (valueType)
            {
                case FieldDescriptorProto.Type.TypeMessage:
                case FieldDescriptorProto.Type.TypeGroup:
                    break; // already handled
                default:
                    WriteLine($"#{ctx.Index.ToString(FormatProvider)}: {KeyString(in ctx)}={Format<TValue>(value)}");
                    break;
            }
        }
        string Format<T>(T value)
        {
            if (typeof(T) == typeof(string)) return Unsafe.As<T, string>(ref value);
            if (typeof(T) == typeof(int)) return Unsafe.As<T, int>(ref value).ToString(FormatProvider);
            if (typeof(T) == typeof(uint)) return Unsafe.As<T, uint>(ref value).ToString(FormatProvider);
            if (typeof(T) == typeof(long)) return Unsafe.As<T, long>(ref value).ToString(FormatProvider);
            if (typeof(T) == typeof(ulong)) return Unsafe.As<T, ulong>(ref value).ToString(FormatProvider);
            if (typeof(T) == typeof(bool)) return Unsafe.As<T, bool>(ref value).ToString(FormatProvider);
            if (typeof(T) == typeof(float)) return Unsafe.As<T, float>(ref value).ToString(FormatProvider);
            if (typeof(T) == typeof(double)) return Unsafe.As<T, double>(ref value).ToString(FormatProvider);

            return value.ToString();
        }


        private protected override object OnBeginMessage(in VisitContext ctx, FieldDescriptorProto field)
        {
            if (field is not null)
            {
                switch (ctx.MapKeyKind)
                {
                    case MapKeyKind.Int32:
                    case MapKeyKind.Int64:
                    case MapKeyKind.UInt32:
                    case MapKeyKind.UInt64:
                    case MapKeyKind.String:
                        WriteLine($"#{ctx.Index.ToString(FormatProvider)}: {KeyString(in ctx)}={{");
                        break;
                    case MapKeyKind.None when ctx.Index >= 0:
                        WriteLine($"#{ctx.Index.ToString(FormatProvider)}={{");
                        break;
                    default:
                        WriteLine($"{field.Number.ToString(FormatProvider)}: {field.Name}={{");
                        break;
                }
            }
            return base.OnBeginMessage(in ctx, field);
        }

        private string KeyString(in VisitContext ctx) => ctx.MapKeyKind switch
        {
            MapKeyKind.Int32 => ctx.MapKeyInt32.ToString(FormatProvider),
            MapKeyKind.Int64 => ctx.MapKeyInt64.ToString(FormatProvider),
            MapKeyKind.UInt32 => ctx.MapKeyUInt32.ToString(FormatProvider),
            MapKeyKind.UInt64 => ctx.MapKeyUInt64.ToString(FormatProvider),
            MapKeyKind.String => ctx.MapKeyString,
            _ => null,
        };

        private protected override void OnEndMessage(in VisitContext parentContext, in VisitContext ctx, FieldDescriptorProto field)
        {
            base.OnEndMessage(in parentContext, in ctx, field);
            if (field is not null)
            {
                WriteLine($"}} // {field.Name}");
            }
        }
    }
}
