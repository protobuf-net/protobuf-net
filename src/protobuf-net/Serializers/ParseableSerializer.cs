using System;
using System.Diagnostics;
using System.Reflection;

namespace ProtoBuf.Serializers
{
    internal sealed class ParseableSerializer : IRuntimeProtoSerializerNode
    {
        private readonly MethodInfo parse;
        public static ParseableSerializer TryCreate(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            MethodInfo method = type.GetMethod("Parse",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
                null, new Type[] { typeof(string) }, null);
            if (method != null && method.ReturnType == type)
            {
                if (type.IsValueType)
                {
                    MethodInfo toString = GetCustomToString(type);
                    if (toString == null || toString.ReturnType != typeof(string)) return null; // need custom ToString, fools
                }
                return new ParseableSerializer(method);
            }
            return null;
        }
        private static MethodInfo GetCustomToString(Type type)
        {
            return type.GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                        null, Type.EmptyTypes, null);
        }

        private ParseableSerializer(MethodInfo parse)
        {
            this.parse = parse;
        }

        public Type ExpectedType => parse.DeclaringType;

        bool IRuntimeProtoSerializerNode.RequiresOldValue { get { return false; } }
        bool IRuntimeProtoSerializerNode.ReturnsValue { get { return true; } }

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value == null); // since replaces
            return parse.Invoke(null, new object[] { state.ReadString() });
        }

        public void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            ProtoWriter.WriteString(value.ToString(), dest, ref state);
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Type type = ExpectedType;
            if (type.IsValueType)
            {   // note that for structs, we've already asserted that a custom ToString
                // exists; no need to handle the box/callvirt scenario

                // force it to a variable if needed, so we can take the address
                using Compiler.Local loc = ctx.GetLocalWithValue(type, valueFrom);
                ctx.LoadAddress(loc, type);
                ctx.EmitCall(GetCustomToString(type));
            }
            else
            {
                ctx.EmitCall(typeof(object).GetMethod("ToString"));
            }
            ctx.EmitBasicWrite("WriteString", valueFrom, this);
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitStateBasedRead(nameof(ProtoReader.State.ReadString), typeof(string));
            ctx.EmitCall(parse);
        }
    }
}