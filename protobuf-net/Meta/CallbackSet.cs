using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace ProtoBuf.Meta
{
    public class CallbackSet
    {
        private readonly MetaType metaType;
        internal CallbackSet(MetaType metaType)
        {
            if (metaType == null) throw new ArgumentNullException("metaType");
        }
        private MethodInfo beforeSerialize, afterSerialize, beforeDeserialize, afterDeserialize;
        /// <summary>Called before serializing an instance</summary>
        public MethodInfo BeforeSerialize
        {
            get { return beforeSerialize; }
            set { metaType.ThrowIfFrozen(); beforeSerialize = value; }
        }
        /// <summary>Called before deserializing an instance</summary>
        public MethodInfo BeforeDeserialize
        {
            get { return beforeDeserialize; }
            set { metaType.ThrowIfFrozen(); beforeDeserialize = value; }
        }
        /// <summary>Called after serializing an instance</summary>
        public MethodInfo AfterSerialize
        {
            get { return afterSerialize; }
            set { metaType.ThrowIfFrozen(); afterSerialize = value; }
        }
        /// <summary>Called after deserializing an instance</summary>
        public MethodInfo AfterDeserialize
        {
            get { return afterDeserialize; }
            set { metaType.ThrowIfFrozen(); afterDeserialize = value; }
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
