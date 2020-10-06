using ProtoBuf.Internal;
using System.Runtime.Serialization;

namespace ProtoBuf
{
    /// <summary>
    /// Additional information about a serialization operation
    /// </summary>
    public sealed class SerializationContext
    {
        private bool frozen;
        internal void Freeze() { frozen = true; }
        private void ThrowIfFrozen() { if (frozen) ThrowHelper.ThrowInvalidOperationException("The serialization-context cannot be changed once it is in use"); }
        private object context;
        /// <summary>
        /// Gets or sets a user-defined object containing additional information about this serialization/deserialization operation.
        /// </summary>
        public object Context
        {
            get { return context; }
            set { if (context != value) { ThrowIfFrozen(); context = value; } }
        }

        /// <summary>
        /// A default SerializationContext, with minimal information.
        /// </summary>
        internal static SerializationContext Default { get; } = new SerializationContext { frozen = true };

        private StreamingContextStates state = StreamingContextStates.Persistence;

        /// <summary>
        /// Gets or sets the source or destination of the transmitted data.
        /// </summary>
        public StreamingContextStates State
        {
            get { return state; }
            set { if (state != value) { ThrowIfFrozen(); state = value; } }
        }

        /// <summary>
        /// Convert a SerializationContext to a StreamingContext
        /// </summary>
        public static implicit operator StreamingContext(SerializationContext ctx)
        {
            if (ctx is null) return new StreamingContext(StreamingContextStates.Persistence);
            return new StreamingContext(ctx.state, ctx.context);
        }
        /// <summary>
        /// Convert a StreamingContext to a SerializationContext
        /// </summary>
        public static implicit operator SerializationContext(StreamingContext ctx)
        {
            SerializationContext result = new SerializationContext
            {
                Context = ctx.Context,
                State = ctx.State
            };

            return result;
        }

        /// <summary>
        /// Create a StreamingContext from a serialization context
        /// </summary>
        public static StreamingContext AsStreamingContext(ISerializationContext context)
        {
            var userState = context?.UserState;
            if (userState is SerializationContext ctx) return new StreamingContext(ctx.state, ctx.context);
            return new StreamingContext(StreamingContextStates.Persistence, userState);
        }

        /// <summary>
        /// Creates a frozen SerializationContext from a serialization context
        /// </summary>
        public static SerializationContext AsSerializationContext(ISerializationContext context)
        {
            var userState = context?.UserState;
            return userState switch
            {
                null => Default,
                SerializationContext ctx => ctx,
                _ => new SerializationContext
                {
                    context = context,
                    frozen = true,
                },
            };
        }
    }

}
