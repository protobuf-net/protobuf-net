using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
#if !SILVERLIGHT && !CF
using System.Runtime.Serialization;
#endif

namespace ProtoBuf
{
    internal enum StreamState
    {
        /// <summary>
        /// Indicates that an EOF is not anticipated, and so will throw an exception.
        /// </summary>
        Normal,
        
        /// <summary>
        /// Indicates that an EOF is acceptable at the current time and will
        /// not throw an exception.
        /// </summary>
        EofExpected,

        /// <summary>
        /// Indicates that we have previously obtained a field value from
        /// the stream that should be consumed next.
        /// </summary>
        Peeked,

        /// <summary>
        /// Indicates that we have found the end of the stream; this is **only**
        /// used to commicate to "Try", and should not persist.
        /// </summary>
        Eof
    }

    internal sealed partial class SerializationContext
    {
#if !SILVERLIGHT && !CF
        internal static readonly StreamingContext EmptyStreamingContext
            = new StreamingContext(StreamingContextStates.All);
#endif
        
        public const string VerboseSymbol = "VERBOSE", DebugCategory = "protobuf-net";
        private Stack<object> objectStack = new Stack<object>();
        private Stack<int> groupStack;

        

        public bool TryPeekFieldPrefix(uint fieldPrefix)
        {
            uint value;
            if(!TryDecodeUInt32(out value) || value == 0) return false;
            if (value == fieldPrefix) return true;
            streamState = StreamState.Peeked;
            peekedValue = value;
            return false;
        }
        public bool TryReadFieldPrefix(out uint value)
        {
            return TryDecodeUInt32(out value) && value != 0;
        }
        public bool TryDecodeUInt32(out uint value)
        {
            if (position >= maxReadPosition)
            {
                value = 0;
                return false;
            }
            switch (streamState)
            {
                case StreamState.Normal:
                    streamState = StreamState.EofExpected;
                    value = this.DecodeUInt32();
                    if (value == 0 && streamState == StreamState.Eof)
                    {
                        streamState = StreamState.Normal; // restore to avoid loss
                        return false;                     // with substreams etc
                    }
                    streamState = StreamState.Normal;
                    return true;
                case StreamState.Peeked:
                    value = peekedValue;
                    streamState = StreamState.Normal;
                    return true;
                default:
                    value = 0;
                    return false;
            }
        }

        private uint peekedValue;

        private StreamState streamState;
        private readonly Stream stream;
        
        public Stream Stream
        {
            get { return stream; }
        }
        private long position = 0, maxReadPosition = long.MaxValue;
        public long Position { get { return position; } }
        public long MaxReadPosition { get { return maxReadPosition; } set { maxReadPosition = value; } }

        const int BLIT_BUFFER_SIZE = 4096;
        
        public long Length { get { return stream.Length; } }
        public bool CanRead { get { return stream.CanRead; } }
        public bool CanWrite { get { return stream.CanWrite; } }
        
        private byte[] workspace;
        public byte[] Workspace
        {
            get { return workspace; }
        }

        


        int stackDepth;
        private const int RecursionThreshold = 20;

        /// <summary>
        /// Allows for recursion detection by capturing
        /// the call tree; this only takes effect after
        /// an initial threshold call-depth is reached.
        /// If the object is already in the call-tree,
        /// an exception is thrown.
        /// </summary>
        /// <param name="obj">The item being processed (start).</param>
        public void Push(object obj)
        {
            if (++stackDepth > RecursionThreshold)
            {
                CheckStackForRecursion(obj);
            }            
        }

        /// <summary>
        /// Only used during debugging for the text nest-level
        /// </summary>
        //[Conditional(SerializationContext.VerboseSymbol)]
        public void Push() { stackDepth++; }

        /// <summary>
        /// Only used during debugging for the text nest-level
        /// </summary>
        //[Conditional(SerializationContext.VerboseSymbol)]
        public void Pop() { stackDepth--; }

        private void CheckStackForRecursion(object item)
        {
            foreach (object stackItem in objectStack)
            {
                if (ReferenceEquals(stackItem, item))
                {
                    throw new ProtoException("Recursive structure detected; only object trees (not full graphs) can be serialized");
                }
            }
            objectStack.Push(item);
        }

        /// <summary>
        /// Removes an object from the call-tree.
        /// </summary>
        /// <remarks>The object is not checked for validity (peformance);
        /// ensure that objects are pushed/popped correctly.</remarks>
        /// <param name="obj">The item being processed (end).</param>
        public void Pop(object obj)
        {
            if(--stackDepth >= RecursionThreshold)
            {
                objectStack.Pop();
            }
        }

        public void StartGroup(int tag)
        {
            if (groupStack == null) groupStack = new Stack<int>();
            groupStack.Push(tag);
        }

        public void EndGroup(int tag)
        {
            if (groupStack == null || groupStack.Count == 0 || groupStack.Pop() != tag)
            {
                throw new ProtoException("Mismatched group tags detected in message");
            }
        }

        private byte[] ioBuffer;
        private int ioBufferIndex;
        const int IO_BUFFER_SIZE = 64;

       
        [Conditional(SerializationContext.VerboseSymbol)]
        private void TraceChangeOrigin(SerializationContext context)
        {
            long newPos = stream.Position, oldPos = context.stream.Position;
            if(oldPos != newPos)
            {
                Debug.WriteLine(new string('!', stackDepth) +
                    string.Format(" re-based: {0} now {1}", oldPos, newPos),
                    SerializationContext.DebugCategory);
            }
        }

        public void CheckStackClean()
        {
            if (stackDepth != 0 || objectStack.Count != 0) throw new ProtoException("Stack corruption; the stack depth ended as: " + stackDepth.ToString());
            CheckNoRemainingGroups();
        }
        public void CheckNoRemainingGroups()
        {
            if (groupStack != null && groupStack.Count > 0)
            {
                throw new ProtoException("Unterminated group(s) in a message or sub-message");
            }
        }

        public SerializationContext(Stream stream, SerializationContext parent)
        {
            this.stream = stream;
            if (parent == null)
            {
                workspace = new byte[InitialBufferLength];
                ioBuffer = new byte[IO_BUFFER_SIZE];
            }
            else
            {
                ReadFrom(parent);
            }
        }

        internal const int InitialBufferLength = 32;
        
        public void CheckSpace(int length)
        {
            if (workspace.Length < length)
            {
                // note that the workspace is a scratch area, and can be
                // discarded; no need to preserve the contents
                int newLen = workspace.Length * 2; // try doubling
                if (length > newLen) newLen = length; // as long as that gives us enough ;-p
                workspace = new byte[newLen]; 
            }
        }

        public int Depth { get { return stackDepth; } }


        public void ReadFrom(SerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            this.workspace = context.workspace;
            this.objectStack = context.objectStack;
            this.stackDepth = context.stackDepth;
            this.streamState = context.streamState;
            this.peekedValue = context.peekedValue;
            this.ioBuffer = context.ioBuffer;
            this.ioBufferIndex = context.ioBufferIndex;
            this.inputStreamAvailable = context.inputStreamAvailable;
            this.ioBufferEffectiveSize = context.ioBufferEffectiveSize;

            TraceChangeOrigin(context); // note ConditionalAttribute

            // IMPORTANT: don't copy the group stack; we want to 
            // validate that the group-stack is empty when finding the end of a stream
        }

        public static void Reverse4(byte[] buffer, int index)
        {
            byte tmp = buffer[index + 0];
            buffer[index + 0] = buffer[index + 3];
            buffer[index + 3] = tmp;
            tmp = buffer[index + 1];
            buffer[index + 1] = buffer[index + 2];
            buffer[index + 2] = tmp;
        }
        public static void Reverse8(byte[] buffer, int index)
        {
            byte tmp = buffer[index + 0];
            buffer[index + 0] = buffer[index + 7];
            buffer[index + 7] = tmp;
            tmp = buffer[index + 1];
            buffer[index + 1] = buffer[index + 6];
            buffer[index + 6] = tmp;
            tmp = buffer[index + 2];
            buffer[index + 2] = buffer[index + 5];
            buffer[index + 5] = tmp;
            tmp = buffer[index + 3];
            buffer[index + 3] = buffer[index + 4];
            buffer[index + 4] = tmp;
        }

        internal long LimitByLengthPrefix()
        {
            // length-prefixed
            return Limit(this.DecodeUInt32());
        }

        internal long Limit(uint length)
        {
            long oldMaxPos = this.MaxReadPosition;
            this.MaxReadPosition = this.Position + length;
            return oldMaxPos;
        }

        
    }
}
