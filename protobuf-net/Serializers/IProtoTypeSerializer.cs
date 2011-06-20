#if !NO_RUNTIME
using ProtoBuf.Meta;
namespace ProtoBuf.Serializers
{
    interface IProtoTypeSerializer : IProtoSerializer
    {
        bool HasCallbacks(TypeModel.CallbackType callbackType);
        void Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context);
#if FEAT_COMPILER
        void EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType);
#endif
    }
}
#endif