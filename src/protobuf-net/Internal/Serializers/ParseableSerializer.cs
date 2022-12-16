using System;
using System.Diagnostics;
using System.Reflection;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class ParseableSerializer : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => true;
        private readonly MethodInfo parse;
        public static ParseableSerializer TryCreate(Type type)
        {
            if (type is null) throw new ArgumentNullException(nameof(type));
            MethodInfo method = type.GetMethod(nameof(int.Parse),
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
                null, new Type[] { typeof(string) }, null);
            if (method is not null && method.ReturnType == type)
            {
                if (type.IsValueType)
                {
                    MethodInfo toString = GetCustomToString(type);
                    if (toString is null || toString.ReturnType != typeof(string)) return null; // need custom ToString, fools
                }
                return new ParseableSerializer(method);
            }
            return null;
        }
        private static MethodInfo GetCustomToString(Type type)
        {
            return type.GetMethod(nameof(object.ToString), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
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
            Debug.Assert(value is null); // since replaces
            return parse.Invoke(null, new object[] { state.ReadString() });
        }

        public void Write(ref ProtoWriter.State state, object value)
        {
            state.WriteString(value.ToString());
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Type type = ExpectedType;
            using var loc = ctx.GetLocalWithValue(ExpectedType, valueFrom);
            ctx.LoadState();
            ctx.LoadAddress(loc, type);
            if (type.IsValueType)
            {   // note that for structs, we've already asserted that a custom ToString
                // exists; no need to handle the box/callvirt scenario

                // force it to a variable if needed, so we can take the address
                ctx.EmitCall(GetCustomToString(type));
            }
            else
            {
                ctx.EmitCall(typeof(object).GetMethod(nameof(object.ToString)));
            }
            ctx.LoadNullRef(); // map
            ctx.EmitCall(typeof(ProtoWriter.State).GetMethod(nameof(ProtoWriter.State.WriteString), BindingFlags.Instance | BindingFlags.Public,
                null, new[] { typeof(string), typeof(StringMap) }, null));
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.LoadState();
            ctx.LoadNullRef(); // map
            ctx.EmitCall(typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.ReadString), BindingFlags.Instance | BindingFlags.Public,
                null, new[] { typeof(StringMap) }, null));
            ctx.EmitCall(parse);
        }
    }
}