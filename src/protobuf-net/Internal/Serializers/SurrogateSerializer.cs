using ProtoBuf.Serializers;
using System;
using System.Diagnostics;
using System.Reflection;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class SurrogateSerializer<T> : IProtoTypeSerializer, ISerializer<T>
    {
        public SerializerFeatures Features => rootTail.Features;
        bool IProtoTypeSerializer.IsSubType => false;
        bool IProtoTypeSerializer.HasCallbacks(ProtoBuf.Meta.TypeModel.CallbackType callbackType) { return false; }
        void IProtoTypeSerializer.EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, ProtoBuf.Meta.TypeModel.CallbackType callbackType) { }
        void IProtoTypeSerializer.EmitCreateInstance(Compiler.CompilerContext ctx, bool callNoteObject) { throw new NotSupportedException(); }
        bool IProtoTypeSerializer.ShouldEmitCreateInstance => false;
        bool IProtoTypeSerializer.CanCreateInstance() => false;

        object IProtoTypeSerializer.CreateInstance(ISerializationContext source) => throw new NotSupportedException();

        void IProtoTypeSerializer.Callback(object value, ProtoBuf.Meta.TypeModel.CallbackType callbackType, ISerializationContext context) { }


        T ISerializer<T>.Read(ref ProtoReader.State state, T value)
            => (T)Read(ref state, value);

        void ISerializer<T>.Write(ref ProtoWriter.State state, T value)
            => Write(ref state, value);

        public bool ReturnsValue => false;

        public bool RequiresOldValue => true;

        public Type ExpectedType => typeof(T);
        Type IProtoTypeSerializer.BaseType => ExpectedType;

        private readonly Type declaredType;
        private readonly MethodInfo toTail, fromTail;
        private readonly IProtoTypeSerializer rootTail;

        public SurrogateSerializer(Type declaredType, IProtoTypeSerializer rootTail)
        {
            Debug.Assert(declaredType != null, "declaredType");
            Debug.Assert(rootTail != null, "rootTail");
            Debug.Assert(rootTail.RequiresOldValue, "RequiresOldValue");
            Debug.Assert(!rootTail.ReturnsValue, "ReturnsValue");
            Debug.Assert(declaredType == rootTail.ExpectedType || Helpers.IsSubclassOf(declaredType, rootTail.ExpectedType));
            this.declaredType = declaredType;
            this.rootTail = rootTail;
            toTail = GetConversion(true);
            fromTail = GetConversion(false);
        }
        private static bool HasCast(Type type, Type from, Type to, out MethodInfo op)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo[] found = type.GetMethods(flags);
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

        public MethodInfo GetConversion(bool toTail)
        {
            Type to = toTail ? declaredType : ExpectedType;
            Type from = toTail ? ExpectedType : declaredType;
            if (HasCast(declaredType, from, to, out MethodInfo op) || HasCast(ExpectedType, from, to, out op))
            {
                return op;
            }
            throw new InvalidOperationException("No suitable conversion operator found for surrogate: " +
                ExpectedType.FullName + " / " + declaredType.FullName);
        }

        public void Write(ref ProtoWriter.State state, object value)
        {
            rootTail.Write(ref state, toTail.Invoke(null, new object[] { value }));
        }

        public object Read(ref ProtoReader.State state, object value)
        {
            // convert the incoming value
            object[] args = { value };
            value = toTail.Invoke(null, args);

            // invoke the tail and convert the outgoing value
            args[0] = rootTail.Read(ref state, value);
            return fromTail.Invoke(null, args);
        }

        bool IProtoTypeSerializer.HasInheritance => false;

        void IProtoTypeSerializer.EmitReadRoot(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
            => ((IRuntimeProtoSerializerNode)this).EmitRead(ctx, valueFrom);

        void IProtoTypeSerializer.EmitWriteRoot(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
            => ((IRuntimeProtoSerializerNode)this).EmitWrite(ctx, valueFrom);

        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Debug.Assert(valueFrom != null); // don't support stack-head for this
            using Compiler.Local converted = new Compiler.Local(ctx, declaredType);
            ctx.LoadValue(valueFrom); // load primary onto stack
            ctx.EmitCall(toTail); // static convert op, primary-to-surrogate
            ctx.StoreValue(converted); // store into surrogate local

            rootTail.EmitRead(ctx, converted); // downstream processing against surrogate local

            ctx.LoadValue(converted); // load from surrogate local
            ctx.EmitCall(fromTail);  // static convert op, surrogate-to-primary
            ctx.StoreValue(valueFrom); // store back into primary
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.EmitCall(toTail);
            rootTail.EmitWrite(ctx, null);
        }
    }
}