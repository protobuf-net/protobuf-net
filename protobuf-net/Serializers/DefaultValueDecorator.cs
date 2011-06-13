#if !NO_RUNTIME
using System;
using System.Reflection;



namespace ProtoBuf.Serializers
{
    sealed class DefaultValueDecorator : ProtoDecoratorBase
    {

        public override Type ExpectedType { get { return Tail.ExpectedType; } }
        public override bool RequiresOldValue { get { return Tail.RequiresOldValue; } }
        public override bool ReturnsValue { get { return Tail.ReturnsValue; } }
        private readonly object defaultValue;
        public DefaultValueDecorator(object defaultValue, IProtoSerializer tail) : base(tail)
        {
            if (defaultValue == null) throw new ArgumentNullException("defaultValue");
            if (defaultValue.GetType() != tail.ExpectedType)
            {
                throw new ArgumentException("Default value is of incorrect type", "defaultValue");
            }
            this.defaultValue = defaultValue;
        }
        public override void Write(object value, ProtoWriter dest)
        {
            if (!object.Equals(value, defaultValue))
            {
                Tail.Write(value, dest);
            }
        }
        public override object Read(object value, ProtoReader source)
        {
            return Tail.Read(value, source);
        }

#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Compiler.CodeLabel done = ctx.DefineLabel();
            if (valueFrom == null)
            {
                ctx.CopyValue(); // on the stack
                Compiler.CodeLabel needToPop = ctx.DefineLabel();
                EmitBranchIfDefaultValue(ctx, needToPop);
                Tail.EmitWrite(ctx, null);
                ctx.Branch(done, true);
                ctx.MarkLabel(needToPop);
                ctx.DiscardValue();
            }
            else
            {
                ctx.LoadValue(valueFrom); // variable/parameter
                EmitBranchIfDefaultValue(ctx, done);
                Tail.EmitWrite(ctx, valueFrom);
            }
            ctx.MarkLabel(done);
        }
        private void EmitBeq(Compiler.CompilerContext ctx, Compiler.CodeLabel label, Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    ctx.BranchIfEqual(label, false);
                    break;
                default:
                    MethodInfo method = type.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static,
                        null, new Type[] { type, type }, null);
                    if (method == null || method.ReturnType != typeof(bool))
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
            switch (Type.GetTypeCode(ExpectedType))
            {
                case TypeCode.Boolean:
                    if ((bool)defaultValue)
                    {
                        ctx.BranchIfTrue(label, false);
                    }
                    else
                    {
                        ctx.BranchIfFalse(label, false);
                    }
                    break;
                case TypeCode.Byte:
                    if ((byte)defaultValue == (byte)0)
                    {
                        ctx.BranchIfFalse(label, false);
                    }
                    else
                    {
                        ctx.LoadValue((int)(byte)defaultValue);
                        EmitBeq(ctx, label, ExpectedType);
                    }
                    break;
                case TypeCode.SByte:
                    if ((sbyte)defaultValue == (sbyte)0)
                    {
                        ctx.BranchIfFalse(label, false);
                    }
                    else
                    {
                        ctx.LoadValue((int)(sbyte)defaultValue);
                        EmitBeq(ctx, label, ExpectedType);
                    }
                    break;
                case TypeCode.Int16:
                    if ((short)defaultValue == (short)0)
                    {
                        ctx.BranchIfFalse(label, false);
                    }
                    else
                    {
                        ctx.LoadValue((int)(short)defaultValue);
                        EmitBeq(ctx, label, ExpectedType);
                    }
                    break;
                case TypeCode.UInt16:
                    if ((ushort)defaultValue == (ushort)0)
                    {
                        ctx.BranchIfFalse(label, false);
                    }
                    else
                    {
                        ctx.LoadValue((int)(ushort)defaultValue);
                        EmitBeq(ctx, label, ExpectedType);
                    }
                    break;
                case TypeCode.Int32:
                    if ((int)defaultValue == (int)0)
                    {
                        ctx.BranchIfFalse(label, false);
                    }
                    else
                    {
                        ctx.LoadValue((int)defaultValue);
                        EmitBeq(ctx, label, ExpectedType);
                    }
                    break;
                case TypeCode.UInt32:
                    if ((uint)defaultValue == (uint)0)
                    {
                        ctx.BranchIfFalse(label, false);
                    }
                    else
                    {
                        ctx.LoadValue((int)(uint)defaultValue);
                        EmitBeq(ctx, label, ExpectedType);
                    }
                    break;
                case TypeCode.Char:
                    if ((char)defaultValue == (char)0)
                    {
                        ctx.BranchIfFalse(label, false);
                    }
                    else
                    {
                        ctx.LoadValue((int)(char)defaultValue);
                        EmitBeq(ctx, label, ExpectedType);
                    }
                    break;
                case TypeCode.Int64:
                    ctx.LoadValue((long)defaultValue);
                    EmitBeq(ctx, label, ExpectedType);
                    break;
                case TypeCode.UInt64:
                    ctx.LoadValue((long)(ulong)defaultValue);
                    EmitBeq(ctx, label, ExpectedType);
                    break;
                case TypeCode.Double:
                    ctx.LoadValue((double)defaultValue);
                    EmitBeq(ctx, label, ExpectedType);
                    break;
                case TypeCode.Single:
                    ctx.LoadValue((float)defaultValue);
                    EmitBeq(ctx, label, ExpectedType);
                    break;
                case TypeCode.String:
                    ctx.LoadValue((string)defaultValue);
                    EmitBeq(ctx, label, ExpectedType);
                    break;
                case TypeCode.Decimal:
                    {
                        decimal d = (decimal)defaultValue;
                        ctx.LoadValue(d);
                        EmitBeq(ctx, label, ExpectedType);
                    }
                    break;
                default:
                    if (ExpectedType == typeof(TimeSpan))
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
                        EmitBeq(ctx, label, ExpectedType);
                    }
                    else if (ExpectedType == typeof(Guid))
                    {
                        ctx.LoadValue((Guid)defaultValue);
                        EmitBeq(ctx, label, ExpectedType);
                    }
                    else if (ExpectedType == typeof(DateTime))
                    {
#if FX11
                        ctx.LoadValue(((DateTime)defaultValue).ToFileTime());
                        ctx.EmitCall(typeof(DateTime).GetMethod("FromFileTime"));                      
#else
                        ctx.LoadValue(((DateTime)defaultValue).ToBinary());
                        ctx.EmitCall(typeof(DateTime).GetMethod("FromBinary"));
#endif
                        
                        EmitBeq(ctx, label, ExpectedType);
                    }
                    else
                    {
                        throw new NotSupportedException("Type cannot be represented as a default value: " + ExpectedType.FullName);
                    }
                    break;
            }
        }
        protected override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Tail.EmitRead(ctx, valueFrom);
        }
#endif
    }
}
#endif