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
            this.metaType = metaType ?? throw new ArgumentNullException(nameof(metaType));
        }

        internal MethodInfo this[TypeModel.CallbackType callbackType]
        {
            get
            {
                return callbackType switch
                {
                    TypeModel.CallbackType.BeforeSerialize => beforeSerialize,
                    TypeModel.CallbackType.AfterSerialize => afterSerialize,
                    TypeModel.CallbackType.BeforeDeserialize => beforeDeserialize,
                    TypeModel.CallbackType.AfterDeserialize => afterDeserialize,
                    _ => throw new ArgumentException("Callback type not supported: " + callbackType.ToString(), nameof(callbackType)),
                };
            }
        }

        internal static bool CheckCallbackParameters(MethodInfo method)
        {
            ParameterInfo[] args = method.GetParameters();
            for (int i = 0; i < args.Length; i++)
            {
                Type paramType = args[i].ParameterType;
                if (paramType == typeof(SerializationContext)) { }
                else if (paramType == typeof(System.Type)) { }
                else if (paramType == typeof(System.Runtime.Serialization.StreamingContext)) { }
                else { return false; }
            }
            return true;
        }

        private MethodInfo SanityCheckCallback(MethodInfo callback)
        {
            metaType.ThrowIfFrozen();
            if (callback is null) return callback; // fine
            if (callback.IsStatic) throw new ArgumentException("Callbacks cannot be static", nameof(callback));
            if (callback.ReturnType != typeof(void)
                || !CheckCallbackParameters(callback))
            {
                throw CreateInvalidCallbackSignature(callback);
            }
            return callback;
        }

        internal static Exception CreateInvalidCallbackSignature(MethodInfo method)
        {
            return new NotSupportedException("Invalid callback signature in " + method.DeclaringType.FullName + "." + method.Name);
        }

        private MethodInfo beforeSerialize, afterSerialize, beforeDeserialize, afterDeserialize;

        /// <summary>Called before serializing an instance</summary>
        public MethodInfo BeforeSerialize
        {
            get { return beforeSerialize; }
            set { beforeSerialize = SanityCheckCallback(value); }
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
                return beforeSerialize is not null || beforeDeserialize is not null
                    || afterSerialize is not null || afterDeserialize is not null;
            }
        }
    }
}