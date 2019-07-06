#if !NO_RUNTIME
using System;
using ProtoBuf.Meta;
using System.Reflection;

namespace ProtoBuf.Serializers
{
    internal sealed class SurrogateSerializer : IProtoTypeSerializer
    {
        bool IProtoTypeSerializer.HasCallbacks(ProtoBuf.Meta.TypeModel.CallbackType callbackType) { return false; }
#if FEAT_COMPILER
        void IProtoTypeSerializer.EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, ProtoBuf.Meta.TypeModel.CallbackType callbackType) { }
        void IProtoTypeSerializer.EmitCreateInstance(Compiler.CompilerContext ctx) { throw new NotSupportedException(); }
#endif
        bool IProtoTypeSerializer.CanCreateInstance() => false;

        object IProtoTypeSerializer.CreateInstance(ProtoReader source) => throw new NotSupportedException();

        void IProtoTypeSerializer.Callback(object value, ProtoBuf.Meta.TypeModel.CallbackType callbackType, SerializationContext context) { }

        public bool ReturnsValue => false;

        public bool RequiresOldValue => true;

        public Type ExpectedType { get; }

        private readonly Type declaredType;
        private readonly MethodInfo toTail, fromTail;
        private readonly IProtoTypeSerializer rootTail;

        public SurrogateSerializer(Type forType, Type declaredType, IProtoTypeSerializer rootTail)
        {
            Helpers.DebugAssert(forType != null, "forType");
            Helpers.DebugAssert(declaredType != null, "declaredType");
            Helpers.DebugAssert(rootTail != null, "rootTail");
            Helpers.DebugAssert(rootTail.RequiresOldValue, "RequiresOldValue");
            Helpers.DebugAssert(!rootTail.ReturnsValue, "ReturnsValue");
            Helpers.DebugAssert(declaredType == rootTail.ExpectedType || Helpers.IsSubclassOf(declaredType, rootTail.ExpectedType));
            ExpectedType = forType;
            this.declaredType = declaredType;
            this.rootTail = rootTail;
            toTail = GetConversion(true);
            fromTail = GetConversion(false);
        }
        private static bool HasCast(Type type, Type from, Type to, out MethodInfo op)
        {
#if PROFILE259
			System.Collections.Generic.List<MethodInfo> list = new System.Collections.Generic.List<MethodInfo>();
            foreach (var item in type.GetRuntimeMethods())
            {
                if (item.IsStatic) list.Add(item);
            }
            MethodInfo[] found = list.ToArray();
#else
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo[] found = type.GetMethods(flags);
#endif
            ParameterInfo[] paramTypes;
            Type convertAttributeType = null;
            for (int i = 0; i < found.Length; i++)
            {
                MethodInfo m = found[i];
                if (m.ReturnType != to) continue;
                paramTypes = m.GetParameters();
                if (paramTypes.Length == 1 && paramTypes[0].ParameterType == from)
                {
                    if (convertAttributeType == null)
                    {
                        convertAttributeType = typeof(ProtoConverterAttribute);
                        if (convertAttributeType == null)
                        { // attribute isn't defined in the source assembly: stop looking
                            break;
                        }
                    }
                    if (m.IsDefined(convertAttributeType, true))
                    {
                        op = m;
                        return true;
                    }
                }
            }

            for (int i = 0; i < found.Length; i++)
            {
                MethodInfo m = found[i];
                if ((m.Name != "op_Implicit" && m.Name != "op_Explicit") || m.ReturnType != to)
                {
                    continue;
                }
                paramTypes = m.GetParameters();
                if (paramTypes.Length == 1 && paramTypes[0].ParameterType == from)
                {
                    op = m;
                    return true;
                }
            }
            op = null;
            return false;
        }

        public static MethodInfo GetConversion(Type expectedType, Type declaredType, bool toTail)
        {
            Type to = toTail ? declaredType : expectedType;
            Type from = toTail ? expectedType : declaredType;
            if (HasCast(declaredType, from, to, out MethodInfo op) || HasCast(expectedType, from, to, out op))
            {
                return op;
            }
            throw new InvalidOperationException("No suitable conversion operator found for surrogate: " +
                expectedType.FullName + " / " + declaredType.FullName);
        }

        public MethodInfo GetConversion(bool toTail)
        {
            return GetConversion(ExpectedType, declaredType, toTail);
        }

        public void Write(ProtoWriter writer, ref ProtoWriter.State state, object value)
        {
            rootTail.Write(writer, ref state, toTail.Invoke(null, new object[] { value }));
        }

        public object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            // convert the incoming value
            object[] args = { value };
            value = toTail.Invoke(null, args);

            // invoke the tail and convert the outgoing value
            args[0] = rootTail.Read(source, ref state, value);
            return fromTail.Invoke(null, args);
        }

#if FEAT_COMPILER
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Helpers.DebugAssert(valueFrom != null); // don't support stack-head for this
            using (Compiler.Local converted = new Compiler.Local(ctx, declaredType)) // declare/re-use local
            {
                ctx.LoadValue(valueFrom); // load primary onto stack
                ctx.EmitCall(toTail); // static convert op, primary-to-surrogate
                ctx.StoreValue(converted); // store into surrogate local

                rootTail.EmitRead(ctx, converted); // downstream processing against surrogate local

                ctx.LoadValue(converted); // load from surrogate local
                ctx.EmitCall(fromTail);  // static convert op, surrogate-to-primary
                ctx.StoreValue(valueFrom); // store back into primary
            }
        }

        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.EmitCall(toTail);
            rootTail.EmitWrite(ctx, null);
        }
#endif
    }
}
#endif