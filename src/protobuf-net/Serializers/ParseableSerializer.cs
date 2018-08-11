#if !NO_RUNTIME
using System;
using System.Net;
using ProtoBuf.Meta;
using System.Reflection;

namespace ProtoBuf.Serializers
{
    internal sealed class ParseableSerializer : IProtoSerializer
    {
        private readonly MethodInfo parse;
        public static ParseableSerializer TryCreate(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
#if PORTABLE || COREFX || PROFILE259
			MethodInfo method = null;

#if COREFX || PROFILE259
			foreach (MethodInfo tmp in type.GetTypeInfo().GetDeclaredMethods("Parse"))
#else
            foreach (MethodInfo tmp in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
#endif
            {
                ParameterInfo[] p;
                if (tmp.Name == "Parse" && tmp.IsPublic && tmp.IsStatic && tmp.DeclaringType == type && (p = tmp.GetParameters()) != null && p.Length == 1 && p[0].ParameterType == typeof(string))
                {
                    method = tmp;
                    break;
                }
            }
#else
            MethodInfo method = type.GetMethod("Parse",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
                null, new Type[] { typeof(string) }, null);
#endif
            if (method != null && method.ReturnType == type)
            {
                if (Helpers.IsValueType(type))
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
#if PORTABLE || COREFX || PROFILE259
			MethodInfo method = Helpers.GetInstanceMethod(type, "ToString", Helpers.EmptyTypes);
            if (method == null || !method.IsPublic || method.IsStatic || method.DeclaringType != type) return null;
            return method;
#else

            return type.GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                        null, Helpers.EmptyTypes, null);
#endif
        }

        private ParseableSerializer(MethodInfo parse)
        {
            this.parse = parse;
        }

        public Type ExpectedType => parse.DeclaringType;

        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }

        public object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return parse.Invoke(null, new object[] { source.ReadString(ref state) });
        }

        public void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            ProtoWriter.WriteString(value.ToString(), dest, ref state);
        }

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Type type = ExpectedType;
            if (Helpers.IsValueType(type))
            {   // note that for structs, we've already asserted that a custom ToString
                // exists; no need to handle the box/callvirt scenario

                // force it to a variable if needed, so we can take the address
                using (Compiler.Local loc = ctx.GetLocalWithValue(type, valueFrom))
                {
                    ctx.LoadAddress(loc, type);
                    ctx.EmitCall(GetCustomToString(type));
                }
            }
            else
            {
                ctx.EmitCall(typeof(object).GetMethod("ToString"));
            }
            ctx.EmitBasicWrite("WriteString", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitBasicRead("ReadString", typeof(string));
            ctx.EmitCall(parse);
        }
#endif

    }
}
#endif