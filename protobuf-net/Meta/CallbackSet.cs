#if !NO_RUNTIME
using System;
using System.Reflection;


namespace ProtoBuf.Meta
{
    /// <summary>
    /// Represents the set of serialization callbacks to be used when serializing/deserializing a type.
    /// </summary>
    public class CallbackSet
    {
        private readonly MetaType metaType;
        internal CallbackSet(MetaType metaType)
        {
            if (metaType == null) throw new ArgumentNullException("metaType");
            this.metaType = metaType;
        }
        internal MethodInfo this[TypeModel.CallbackType callbackType]
        {
            get
            {
                switch (callbackType)
                {
                    case TypeModel.CallbackType.BeforeSerialize: return beforeSerialize;
                    case TypeModel.CallbackType.AfterSerialize: return afterSerialize;
                    case TypeModel.CallbackType.BeforeDeserialize: return beforeDeserialize;
                    case TypeModel.CallbackType.AfterDeserialize: return afterDeserialize;
                    default: throw new ArgumentException();
                }
            }
        }
        private MethodInfo beforeSerialize, afterSerialize, beforeDeserialize, afterDeserialize;
        /// <summary>Called before serializing an instance</summary>
        public MethodInfo BeforeSerialize
        {
            get { return beforeSerialize; }
            set { beforeSerialize = SanityCheckCallback(value); }
        }
        private MethodInfo SanityCheckCallback(MethodInfo callback)
        {
            metaType.ThrowIfFrozen();
            if (callback == null) return callback; // fine
            if (callback.IsStatic) throw new ArgumentException("Callbacks cannot be static", "callback");
            ParameterInfo[] args = callback.GetParameters();
            if (callback.ReturnType == typeof(void) && (args.Length == 0
#if PLAT_BINARYFORMATTER
                || (args.Length == 1 && (args[0].ParameterType == typeof(System.Runtime.Serialization.StreamingContext)
                   || args[0].ParameterType == typeof(SerializationContext)) )
#endif
                )) { }
            else throw new ArgumentException("Invalid callback signature", "callback");
            return callback;
        }
        /// <summary>Called before deserializing an instance</summary>
        public MethodInfo BeforeDeserialize
        {
            get { return beforeDeserialize; }
            set { beforeDeserialize = SanityCheckCallback(value); }
        }
        /// <summary>Called after serializing an instance</summary>
        public MethodInfo AfterSerialize
        {
            get { return afterSerialize; }
            set { afterSerialize = SanityCheckCallback(value); }
        }
        /// <summary>Called after deserializing an instance</summary>
        public MethodInfo AfterDeserialize
        {
            get { return afterDeserialize; }
            set { afterDeserialize = SanityCheckCallback(value); }
        }
        /// <summary>
        /// True if any callback is set, else False
        /// </summary>
        public bool NonTrivial
        {
            get
            {
                return beforeSerialize != null || beforeDeserialize != null
                    || afterSerialize != null || afterDeserialize != null;
            }
        }
    }
}
#endif