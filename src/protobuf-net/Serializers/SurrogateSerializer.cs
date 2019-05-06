#if !NO_RUNTIME
using System;
using ProtoBuf.Meta;
using System.Reflection;

namespace ProtoBuf.Serializers
{
    sealed class SurrogateSerializer : IProtoTypeSerializer
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

        public Type ExpectedType => forType;

        private readonly Type forType, declaredType;
        private readonly MethodInfo toTail, fromTail;
        /// <summary>
        /// True if the <see cref="SerializationContext"/> should be passed as a 2nd argument when invoking <see cref="toTail"/>
        /// </summary>
        private bool useContextTo = false;
        /// <summary>
        /// True if the <see cref="SerializationContext"/> should be passed as a 2nd argument when invoking <see cref="fromTail"/>
        /// </summary>
        private bool useContextFrom = false;

        IProtoTypeSerializer rootTail;

        public SurrogateSerializer(TypeModel model, Type forType, Type declaredType, IProtoTypeSerializer rootTail)
        {
            Helpers.DebugAssert(forType != null, "forType");
            Helpers.DebugAssert(declaredType != null, "declaredType");
            Helpers.DebugAssert(rootTail != null, "rootTail");
            Helpers.DebugAssert(rootTail.RequiresOldValue, "RequiresOldValue");
            Helpers.DebugAssert(!rootTail.ReturnsValue, "ReturnsValue");
            Helpers.DebugAssert(declaredType == rootTail.ExpectedType || Helpers.IsSubclassOf(declaredType, rootTail.ExpectedType));
            this.forType = forType;
            this.declaredType = declaredType;
            this.rootTail = rootTail;
            toTail = GetConversion(model, true, out this.useContextTo);
            fromTail = GetConversion(model, false, out this.useContextFrom);
            
        }
        private static bool HasCastOrConvert(TypeModel model, Type type, Type from, Type to, out MethodInfo op, out bool useContext)
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
            useContext = false;
            for (int i = 0; i < found.Length; i++)
            {
                MethodInfo m = found[i];
                if (m.ReturnType != to) continue;
                paramTypes = m.GetParameters();
                if ((paramTypes.Length == 1 && paramTypes[0].ParameterType == from))
                {
                    if (convertAttributeType == null)
                    {
                        convertAttributeType = model.MapType(typeof(ProtoConverterAttribute), false);
                        if (convertAttributeType == null)
                        { // attribute isn't defined in the source assembly: stop looking
                            break;
                        }
                    }
                    if (m.IsDefined(convertAttributeType, true) || m.Name == "Convert")
                    {
                        op = m;
                        return true;
                    }
                }
                else if ((paramTypes.Length == 2 && paramTypes[0].ParameterType == from && paramTypes[1].ParameterType == typeof(SerializationContext)))
                {
                    if (convertAttributeType == null)
                    {
                        convertAttributeType = model.MapType(typeof(ProtoConverterAttribute), false);
                        if (convertAttributeType == null)
                        { // attribute isn't defined in the source assembly: stop looking
                            break;
                        }
                    }
                    if (m.Name == "Convert")
                    {
                        op = m;
                        useContext = true;
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


        public MethodInfo GetConversion(TypeModel model, bool toTail, out bool useContext)
        {
            Type to = toTail ? declaredType : forType;
            Type from = toTail ? forType : declaredType;
            MethodInfo op;
            if (HasCastOrConvert(model, declaredType, from, to, out op, out useContext) || 
                HasCastOrConvert(model, forType, from, to, out op, out useContext))
            {
                return op;
            }
            throw new InvalidOperationException("No suitable conversion operator or Convert method found for surrogate: " +
                forType.FullName + " / " + declaredType.FullName);
        }

        public void Write(object value, ProtoWriter writer)
        {
            if (useContextTo)
            {
                rootTail.Write(toTail.Invoke(null, new object[] { value, writer.Context }), writer);
            }
            else
            {
                rootTail.Write(toTail.Invoke(null, new object[] { value }), writer);
            }
        }

        public object Read(object value, ProtoReader source)
        {
            // convert the incoming value
            object[] args;
            if (useContextTo)
            {
                args = new object[] { value, source.Context };
            }
            else
            {
                args = new object[] { value };
            }

            // invoke the tail and convert the outgoing value
            value = rootTail.Read(toTail.Invoke(null, args), source);

            //If context is used on one coversion and not on the other we need to
            //reallocate args to a different sized array, otherwise it can be
            //reused for both method calls
            if (useContextTo != useContextFrom)
            {
                if (useContextFrom)
                {
                    args = new object[] { value, source.Context };
                }
                else
                {
                    args = new object[] { value };
                }
            }
            else
            {
                args[0] = value;
            }
            return fromTail.Invoke(null, args);
        }

#if FEAT_COMPILER
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Helpers.DebugAssert(valueFrom != null); // don't support stack-head for this
            using (Compiler.Local converted = new Compiler.Local(ctx, declaredType)) // declare/re-use local
            {
                ctx.LoadValue(valueFrom); // load primary onto stack
                if (useContextTo)
                {
                    ctx.LoadSerializationContext();
                }
                ctx.EmitCall(toTail); // static convert op, primary-to-surrogate
                ctx.StoreValue(converted); // store into surrogate local

                rootTail.EmitRead(ctx, converted); // downstream processing against surrogate local

                ctx.LoadValue(converted); // load from surrogate local
                if (useContextFrom)
                {
                    ctx.LoadSerializationContext();
                }
                ctx.EmitCall(fromTail);  // static convert op, surrogate-to-primary
                ctx.StoreValue(valueFrom); // store back into primary
            }
        }

        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            if (useContextTo)
            {
                ctx.LoadSerializationContext();
            }
            ctx.EmitCall(toTail);
            rootTail.EmitWrite(ctx, null);
        }
#endif
    }
}
#endif