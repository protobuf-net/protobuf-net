using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
        Peeked
    }

    internal sealed partial class SerializationContext
    {
        //TODO: reinstate
        //public readonly ProtoGuid GuidTemplate = null; //new ProtoGuid();
        
        public const string VerboseSymbol = "VERBOSE", DebugCategory = "protobuf-net";
        private Stack<object> objectStack = new Stack<object>();
        private Stack<int> groupStack;

        

        public bool TryPeekFieldPrefix(uint fieldPrefix)
        {
            uint value = TryReadFieldPrefix();
            if (value == 0) return false;
            if (value == fieldPrefix) return true;
            streamState = StreamState.Peeked;
            peekedValue = value;
            return false;
        }
        public uint TryReadFieldPrefix()
        {
            if (position >= maxReadPosition) return 0;
            uint value;
            switch (streamState)
            {
                case StreamState.Normal:
                    streamState = StreamState.EofExpected;
                    value = this.DecodeUInt32();
                    break;
                case StreamState.Peeked:
                    value = peekedValue;
                    break;
                default:
                    value = 0;
                    break;
            }
            
            streamState = StreamState.Normal;
            return value;
        }

        private uint peekedValue;

        public bool IsEofExpected
        {
            get { return streamState == StreamState.EofExpected; }
        }

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
        [Conditional(SerializationContext.VerboseSymbol)]
        public void Push() { stackDepth++; }

        /// <summary>
        /// Only used during debugging for the text nest-level
        /// </summary>
        [Conditional(SerializationContext.VerboseSymbol)]
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
            if(stackDepth-- >= RecursionThreshold)
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
            if (stackDepth != 0) throw new ProtoException("Stack corruption; the stack depth ended as: " + stackDepth.ToString());
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
                int newLen = workspace.Length * 2; // try doubling
                if (length > newLen) newLen = length; // as long as that gives us enough ;-p
                // note Array.Resize not available on CF
                byte[] tmp = workspace;
                workspace = new byte[newLen]; 
                Buffer.BlockCopy(tmp, 0, workspace, 0, tmp.Length);
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

            TraceChangeOrigin(context);

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
            int len = (int)this.DecodeUInt32();
            long oldMaxPos = this.MaxReadPosition;
            this.MaxReadPosition = this.Position + len;
            return oldMaxPos;
        }
    }
}
