using ProtoBuf.Compiler;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Internal.Serializers
{
    abstract class SurrogateSerializer : IRuntimeProtoSerializerNode
    {
        public abstract SerializerFeatures Features { get; }
        public abstract Type ExpectedType { get; }
        public abstract bool RequiresOldValue { get; }
        public abstract bool ReturnsValue { get; }

        public abstract void EmitRead(CompilerContext ctx, Local entity);
        public abstract void EmitWrite(CompilerContext ctx, Local valueFrom);
        public abstract object Read(ref ProtoReader.State state, object value);
        public abstract void Write(ref ProtoWriter.State state, object value);
    }
    internal sealed class SurrogateSerializer<TDeclared, TSurrogate> : SurrogateSerializer, ISerializer<TDeclared>
    {
        private readonly TypeModel _model;
        public override SerializerFeatures Features
        {
            get
            {
                if (_serializer == null && _model != null && _model.CanSerialize(typeof(TSurrogate), true, true, true, out var category)
                    && category == SerializerFeatures.CategoryMessage)
                {   // looks like a message, then
                    return SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;
                }
                // otherwise, we'll have to actually resolve the serializer
                return Serializer.Features;
            }
        }

        TDeclared ISerializer<TDeclared>.Read(ref ProtoReader.State state, TDeclared value)
            => (TDeclared)Read(ref state, value);

        void ISerializer<TDeclared>.Write(ref ProtoWriter.State state, TDeclared value)
            => Write(ref state, value);

        public override bool ReturnsValue => true;

        public override bool RequiresOldValue => true;

        private ISerializer<TSurrogate> Serializer => _serializer ?? CreateSerializer();
        private ISerializer<TSurrogate> _serializer;
        [MethodImpl(MethodImplOptions.NoInlining)]
        private ISerializer<TSurrogate> CreateSerializer()
        {
            try
            {
                return _serializer = TypeModel.GetSerializer<TSurrogate>(_model);
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException($"Unable to create surrogate serializer for {typeof(TSurrogate).NormalizeName()}", ex);
            }
            
        }

        public override Type ExpectedType => typeof(TDeclared);
        // Type IProtoTypeSerializer.BaseType => ExpectedType;

        private readonly MethodInfo toTail, fromTail;

        public SurrogateSerializer(TypeModel model)
        {
            toTail = GetConversion(true);
            fromTail = GetConversion(false);
            _model = model;
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
            Type to = toTail ? typeof(TSurrogate) : typeof(TDeclared);
            Type from = toTail ? typeof(TDeclared) : typeof(TSurrogate);
            if (HasCast(typeof(TSurrogate), from, to, out MethodInfo op) || HasCast(typeof(TDeclared), from, to, out op))
            {
                return op;
            }
            throw new InvalidOperationException("No suitable conversion operator found for surrogate: " +
                typeof(TDeclared).FullName + " / " + typeof(TSurrogate).FullName);
        }

        public override void Write(ref ProtoWriter.State state, object value)
            => Serializer.Write(ref state, (TSurrogate)toTail.Invoke(null, new object[] { value }));

        public override object Read(ref ProtoReader.State state, object value)
        {
            // convert the incoming value
            object[] args = { value };
            value = toTail.Invoke(null, args);

            // invoke the tail and convert the outgoing value
            args[0] = state.ReadAny<TSurrogate>(SurrogateFeatures, (TSurrogate)value, _serializer);
            return fromTail.Invoke(null, args);
        }

        SerializerFeatures SurrogateFeatures => default;

        static readonly MethodInfo s_ReadAny =
            (from method in typeof(ProtoReader.State).GetMethods()
             where method.Name == nameof(ProtoReader.State.ReadAny)
             let args = method.GetParameters()
             where method.IsGenericMethodDefinition && args.Length == 3
             select method.MakeGenericMethod(typeof(TSurrogate))).Single();

        static readonly MethodInfo s_Write
            = typeof(ISerializer<TSurrogate>).GetMethod(nameof(ISerializer<TSurrogate>.Write));

        public override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            // snapshot the input
            using var loc = ctx.GetLocalWithValue(typeof(TDeclared), valueFrom);
            
            ctx.LoadState();
            ctx.LoadValue((int)SurrogateFeatures);
            ctx.LoadValue(loc);
            ctx.EmitCall(toTail); // convert it out
            ctx.LoadSelfAsService<ISerializer<TSurrogate>, TSurrogate>();
            ctx.EmitCall(s_ReadAny); // downstream processing against surrogate local

            // and convert it back
            ctx.EmitCall(fromTail);  // static convert op, surrogate-to-primary
            ctx.StoreValue(valueFrom); // store back into primary
        }

        public override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            // snapshot the input
            using var loc = ctx.GetLocalWithValue(typeof(TDeclared), valueFrom);

            
            var forSure = ctx.LoadSelfAsService<ISerializer<TSurrogate>, TSurrogate>();
            // if (!forSure)
            {
                var haveSerializer = ctx.DefineLabel();
                using var serializer = new Compiler.Local(ctx, typeof(ISerializer<TSurrogate>));
                ctx.StoreValue(serializer);
                ctx.LoadValue(serializer);
                ctx.BranchIfTrue(haveSerializer, true);
                ctx.MarkLabel(haveSerializer);
                ctx.LoadValue(serializer);
            }

            // (stack: serializer)
            ctx.LoadState();
            ctx.LoadValue(valueFrom);
            ctx.EmitCall(toTail);
            ctx.EmitCall(s_Write);
        }
    }
}