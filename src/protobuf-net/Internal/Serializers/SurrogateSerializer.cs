using ProtoBuf.Compiler;
using ProtoBuf.Serializers;
using System;
using System.Diagnostics;
using System.Reflection;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class SurrogateSerializer<TBase, T> : SurrogateSerializer<T> //, ISubTypeSerializer<T>
        where TBase : class
        where T : class, TBase
    {
        public SurrogateSerializer(Type declaredType, MethodInfo toTail, MethodInfo fromTail, IRuntimeProtoSerializerNode rootTail, SerializerFeatures features) : base(declaredType, toTail, fromTail, rootTail, features) { }
        public override Type BaseType => typeof(TBase);

        public override bool IsSubType => typeof(TBase) != typeof(T);
    }
    internal class SurrogateSerializer<T> : IProtoTypeSerializer, ISerializer<T>
    {
        public SerializerFeatures Features => features;
        public virtual bool IsSubType => false;
        bool IProtoTypeSerializer.HasCallbacks(ProtoBuf.Meta.TypeModel.CallbackType callbackType) { return false; }
        void IProtoTypeSerializer.EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, ProtoBuf.Meta.TypeModel.CallbackType callbackType) { }
        void IProtoTypeSerializer.EmitCreateInstance(Compiler.CompilerContext ctx, bool callNoteObject) { throw new NotSupportedException(); }
        bool IProtoTypeSerializer.ShouldEmitCreateInstance => false;
        bool IProtoTypeSerializer.CanCreateInstance() => false;

        bool IRuntimeProtoSerializerNode.IsScalar => features.IsScalar();

        object IProtoTypeSerializer.CreateInstance(ISerializationContext source) => throw new NotSupportedException();

        void IProtoTypeSerializer.Callback(object value, ProtoBuf.Meta.TypeModel.CallbackType callbackType, ISerializationContext context) { }


        public virtual T Read(ref ProtoReader.State state, T value)
            => (T)Read(ref state, (object)value);

        public virtual void Write(ref ProtoWriter.State state, T value)
            => Write(ref state, (object)value);

        public bool ReturnsValue => true; // because always changes

        public bool RequiresOldValue => rootTail.RequiresOldValue;

        public Type ExpectedType => typeof(T);
        public virtual Type BaseType => ExpectedType;

        private readonly Type declaredType;
        private readonly MethodInfo toTail, fromTail;
        private readonly IRuntimeProtoSerializerNode rootTail;
        private readonly SerializerFeatures features;

        public SurrogateSerializer(Type declaredType, MethodInfo toTail, MethodInfo fromTail, IRuntimeProtoSerializerNode rootTail, SerializerFeatures features)
        {
            Debug.Assert(declaredType is not null, "declaredType");
            Debug.Assert(rootTail is object, "rootTail");
            Debug.Assert(declaredType == rootTail.ExpectedType || Helpers.IsSubclassOf(declaredType, rootTail.ExpectedType), "surrogate type mismatch");
            this.declaredType = declaredType;
            this.rootTail = rootTail;
            this.toTail = toTail ?? GetConversion(true);
            this.fromTail = fromTail ?? GetConversion(false);
            this.features = features;
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
                    if (convertAttributeType is null)
                    {
                        convertAttributeType = typeof(ProtoConverterAttribute);
                        if (convertAttributeType is null)
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
            var rootType = BaseType;
            Type to = toTail ? declaredType : rootType;
            Type from = toTail ? rootType : declaredType;
            if (HasCast(declaredType, from, to, out MethodInfo op) || HasCast(rootType, from, to, out op))
            {
                return op;
            }
            throw new InvalidOperationException("No suitable conversion operator found for surrogate: " +
                rootType.FullName + " / " + declaredType.FullName);
        }

        public void Write(ref ProtoWriter.State state, object value)
        {
            rootTail.Write(ref state, value is null ? null : toTail.Invoke(null, new object[] { value }));
        }

        public object Read(ref ProtoReader.State state, object value)
        {
            // convert the incoming value
            object[] args = new object[1];

            if (value is null)
            {
                // GIGO
            }
            else if (rootTail.RequiresOldValue)
            {
                args[0] = value;
                value = toTail.Invoke(null, args);
            }
            else
            {
                value = null;
            }

            // invoke the tail and convert the outgoing value
            value = rootTail.Read(ref state, value);
            if (value is not null)
            {
                args[0] = value;
                value = fromTail.Invoke(null, args);
            }
            return value;
        }

        public virtual void EmitReadRoot(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
            => ((IRuntimeProtoSerializerNode)this).EmitRead(ctx, valueFrom);

        public virtual void EmitWriteRoot(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
            => ((IRuntimeProtoSerializerNode)this).EmitWrite(ctx, valueFrom);

        bool IProtoTypeSerializer.HasInheritance => false; // treat as simple type; all magic happens inside tail
        public virtual void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.Debug(">> surrogate serializer");
            // inline the tail
            // Debug.Assert(valueFrom is object, "surrogate value on stack-head"); // don't support stack-head for this
            using Compiler.Local converted = new Compiler.Local(ctx, declaredType);

            if (rootTail.RequiresOldValue)
            {
                ctx.LoadValue(valueFrom); // load primary onto stack
                ctx.EmitCall(toTail); // static convert op, primary-to-surrogate
                ctx.StoreValue(converted); // store into surrogate local
            }

            // downstream processing against surrogate local
            var tail = rootTail;
            ctx.Debug(">> tail");
            if (rootTail is IProtoTypeSerializer { HasInheritance: true} forType)
            {
                tail = forType;
                forType.EmitReadRoot(ctx, converted);
            }
            else
            {
                rootTail.EmitRead(ctx, converted);    
            }
            ctx.Debug("<< tail");
            if (!tail.ReturnsValue) // otherwise: already pon stack
            {
                ctx.LoadValue(converted); // load from surrogate local
            }
            ctx.EmitCall(fromTail); // static convert op, surrogate-to-primary
            if (IsSubType && ReturnsValue)
            {
                // "return {expr}" to "return (T){expr}"
                ctx.Cast(ExpectedType);
            }
            // leave on stack for exit
            ctx.Debug("<< surrogate serializer");
        }

        public virtual void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            // inline the tail
            ctx.LoadValue(valueFrom);
            ctx.EmitCall(toTail);
            rootTail.EmitWrite(ctx, null);
        }
    }
}