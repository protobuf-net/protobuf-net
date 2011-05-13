#if !NO_RUNTIME
using System;
using System.Net;
using System.Reflection;



namespace ProtoBuf.Serializers
{
    sealed class ParseableSerializer : IProtoSerializer
    {
        private readonly MethodInfo parse;
        public static ParseableSerializer TryCreate(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
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
            return type.GetMethod("ToString", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
                        null, Helpers.EmptyTypes, null);
        }
        private ParseableSerializer(MethodInfo parse)
        {
            this.parse = parse;
        }
        public Type ExpectedType { get { return parse.DeclaringType; } }
        public void Write(object value, ProtoWriter dest)
        {
            ProtoWriter.WriteString(value.ToString(), dest);
        }
        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
        public object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return parse.Invoke(null, new object[] { source.ReadString() });
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Type type = ExpectedType;
            if (type.IsValueType)
            {
                ctx.EmitCall(GetCustomToString(type));
            }
            else {
                ctx.EmitCall(typeof(object).GetMethod("ToString"));
            }
            ctx.EmitBasicWrite("WriteString", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadString", typeof(string));
            ctx.EmitCall(parse);
        }
#endif

    }
}
#endif