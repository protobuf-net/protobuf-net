using System;
using System.Reflection;
using System.Reflection.Emit;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class DefaultValueDecorator : ProtoDecoratorBase
    {
        public override Type ExpectedType => Tail.ExpectedType;

        public override bool RequiresOldValue => Tail.RequiresOldValue;

        public override bool ReturnsValue => Tail.ReturnsValue;

        private readonly object defaultValue;

        public DefaultValueDecorator(object defaultValue, IRuntimeProtoSerializerNode tail) : base(tail)
        {
            if (defaultValue is null) throw new ArgumentNullException(nameof(defaultValue));
            Type type = defaultValue.GetType();
            if (type != tail.ExpectedType)
            {
                throw new ArgumentException("Default value is of incorrect type", nameof(defaultValue));
            }
            this.defaultValue = defaultValue;
        }

        public override void Write(ref ProtoWriter.State state, object value)
        {
            if (!object.Equals(value, defaultValue))
            {
                Tail.Write(ref state, value);
            }
        }

        public override object Read(ref ProtoReader.State state, object value)
        {
            return Tail.Read(ref state, value);
        }

        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Compiler.CodeLabel done = ctx.DefineLabel();
            using(var loc = ctx.GetLocalWithValue(ExpectedType, valueFrom))
            {
                ctx.LoadValue(loc);
                EmitBranchIfDefaultValue(ctx, done);
                Tail.EmitWrite(ctx, loc);
            }
            ctx.MarkLabel(done);
        }
        private static void EmitBeq(Compiler.CompilerContext ctx, Compiler.CodeLabel label, Type type)
        {
            switch (Helpers.GetTypeCode(type))
            {
                case ProtoTypeCode.Boolean:
                case ProtoTypeCode.Byte:
                case ProtoTypeCode.Char:
                case ProtoTypeCode.Double:
                case ProtoTypeCode.Int16:
                case ProtoTypeCode.Int32:
                case ProtoTypeCode.Int64:
                case ProtoTypeCode.SByte:
                case ProtoTypeCode.Single:
                case ProtoTypeCode.UInt16:
                case ProtoTypeCode.UInt32:
                case ProtoTypeCode.UInt64:
                    ctx.BranchIfEqual(label, false);
                    break;
                default:
                    MethodInfo method = type.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static,
                        null, new Type[] { type, type }, null);
                    if (method is null || method.ReturnType != typeof(bool))
                    {
                        throw new InvalidOperationException("No suitable equality operator found for default-values of type: " + type.FullName);
                    }
                    ctx.EmitCall(method);
                    ctx.BranchIfTrue(label, false);
                    break;
            }
        }
        private void EmitBranchIfDefaultValue(Compiler.CompilerContext ctx, Compiler.CodeLabel label)
        {
            Type expected = ExpectedType;
            switch (Helpers.GetTypeCode(expected))
            {
                case ProtoTypeCode.Boolean:
                    if ((bool)defaultValue)
                    {
                        ctx.BranchIfTrue(label, false);
                    }
                    else
                    {
                        ctx.BranchIfFalse(label, false);
                    }
                    break;
                case ProtoTypeCode.Byte:
                    if ((byte)defaultValue == (byte)0)
                    {
                        ctx.BranchIfFalse(label, false);
                    }
                    else
                    {
                        ctx.LoadValue((int)(byte)defaultValue);
                        EmitBeq(ctx, label, expected);
                    }
                    break;
                case ProtoTypeCode.SByte:
                    if ((sbyte)defaultValue == (sbyte)0)
                    {
                        ctx.BranchIfFalse(label, false);
                    }
                    else
                    {
                        ctx.LoadValue((int)(sbyte)defaultValue);
                        EmitBeq(ctx, label, expected);
                    }
                    break;
                case ProtoTypeCode.Int16:
                    if ((short)defaultValue == (short)0)
                    {
                        ctx.BranchIfFalse(label, false);
                    }
                    else
                    {
                        ctx.LoadValue((int)(short)defaultValue);
                        EmitBeq(ctx, label, expected);
                    }
                    break;
                case ProtoTypeCode.UInt16:
                    if ((ushort)defaultValue == (ushort)0)
                    {
                        ctx.BranchIfFalse(label, false);
                    }
                    else
                    {
                        ctx.LoadValue((int)(ushort)defaultValue);
                        EmitBeq(ctx, label, expected);
                    }
                    break;
                case ProtoTypeCode.Int32:
                    if ((int)defaultValue == (int)0)
                    {
                        ctx.BranchIfFalse(label, false);
                    }
                    else
                    {
                        ctx.LoadValue((int)defaultValue);
                        EmitBeq(ctx, label, expected);
                    }
                    break;
                case ProtoTypeCode.UInt32:
                    if ((uint)defaultValue == (uint)0)
                    {
                        ctx.BranchIfFalse(label, false);
                    }
                    else
                    {
                        ctx.LoadValue((int)(uint)defaultValue);
                        EmitBeq(ctx, label, expected);
                    }
                    break;
                case ProtoTypeCode.Char:
                    if ((char)defaultValue == (char)0)
                    {
                        ctx.BranchIfFalse(label, false);
                    }
                    else
                    {
                        ctx.LoadValue((int)(char)defaultValue);
                        EmitBeq(ctx, label, expected);
                    }
                    break;
                case ProtoTypeCode.Int64:
                    ctx.LoadValue((long)defaultValue);
                    EmitBeq(ctx, label, expected);
                    break;
                case ProtoTypeCode.UInt64:
                    ctx.LoadValue((ulong)defaultValue);
                    EmitBeq(ctx, label, expected);
                    break;
                case ProtoTypeCode.Double:
                    ctx.LoadValue((double)defaultValue);
                    EmitBeq(ctx, label, expected);
                    break;
                case ProtoTypeCode.Single:
                    ctx.LoadValue((float)defaultValue);
                    EmitBeq(ctx, label, expected);
                    break;
                case ProtoTypeCode.String:
                    ctx.LoadValue((string)defaultValue);
                    EmitBeq(ctx, label, expected);
                    break;
                case ProtoTypeCode.Decimal:
                    {
                        decimal d = (decimal)defaultValue;
                        ctx.LoadValue(d);
                        EmitBeq(ctx, label, expected);
                    }
                    break;
                case ProtoTypeCode.TimeSpan:
                    {
                        TimeSpan ts = (TimeSpan)defaultValue;
                        if (ts == TimeSpan.Zero)
                        {
                            ctx.LoadValue(typeof(TimeSpan).GetField("Zero"));
                        }
                        else
                        {
                            ctx.LoadValue(ts.Ticks);
                            ctx.EmitCall(typeof(TimeSpan).GetMethod("FromTicks"));
                        }
                        EmitBeq(ctx, label, expected);
                        break;
                    }
                case ProtoTypeCode.Guid:
                    {
                        ctx.LoadValue((Guid)defaultValue);
                        EmitBeq(ctx, label, expected);
                        break;
                    }
                case ProtoTypeCode.DateTime:
                    {
                        ctx.LoadValue(((DateTime)defaultValue).ToBinary());
                        ctx.EmitCall(typeof(DateTime).GetMethod("FromBinary"));

                        EmitBeq(ctx, label, expected);
                        break;
                    }
                case ProtoTypeCode.IntPtr:
                    {
                        ctx.Emit(OpCodes.Conv_I8); // might be 32-bit system; no-op on 64-bit
                        ctx.LoadValue(((IntPtr)defaultValue).ToInt64());
                        EmitBeq(ctx, label, typeof(long));
                        break;
                    }
                case ProtoTypeCode.UIntPtr:
                    {
                        ctx.Emit(OpCodes.Conv_U8); // might be 32-bit system; no-op on 64-bit
                        ctx.LoadValue(((UIntPtr)defaultValue).ToUInt64());
                        EmitBeq(ctx, label, typeof(ulong));
                        break;
                    }
                default:
                    throw new NotSupportedException("Type cannot be represented as a default value: " + expected.FullName);
            }
        }

        protected override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Tail.EmitRead(ctx, valueFrom);
        }
    }
}