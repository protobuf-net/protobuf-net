using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ProtoBuf.ProtoBcl;
using ProtoBuf.Property;

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

    internal sealed class SerializationContext
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
                    value = Base128Variant.DecodeUInt32(this);
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

        public int ReadByte()
        {
            int b = stream.ReadByte();
            if (b >= 0) position++;
            return b;
        }
        public int Read(int count)
        {
            int read = stream.Read(workspace, 0, count);
            if (read > 0) position += read;
            return read;
        }
        public int Read(byte[] buffer, int offset, int count)
        {
            int read = stream.Read(buffer, offset, count);
            if (read > 0) position += read;
            return read;
        }
        public void WriteByte(byte value)
        {
            stream.WriteByte(value);
            position++;
        }
        public void Write(int count)
        {
            stream.Write(workspace, 0, count);
            position += count;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
            position += count;
        }
        public bool TrySeek(int offset)
        {
            if (stream.CanSeek)
            {
                stream.Seek(offset, SeekOrigin.Current);
                position += offset;
                return true;
            }
            return false;
        }
        public void ReadBlock(int count)
        {
            ReadBlock(workspace, count);
        }
        public void ReadBlock(byte[] buffer, int count)
        {
            int read, index = 0;
            position += count;
            while ((count > 0) && ((read = stream.Read(buffer, index, count)) > 0))
            {
                index += read;
                count -= read;
            }
            if (count != 0) throw new EndOfStreamException();
        }
        const int BLIT_BUFFER_SIZE = 4096;
        public void WriteFrom(Stream source, int length)
        {
            CheckSpace(length > BLIT_BUFFER_SIZE ? BLIT_BUFFER_SIZE : length);
            int max = workspace.Length, read;
            position += length;
            while ((length >= max) && (read = source.Read(workspace, 0, max)) > 0)
            {
                Write(workspace, 0, read);
                length -= read;
            }
            while ((length >= 0) && (read = source.Read(workspace, 0, length)) > 0)
            {
                Write(workspace, 0, read);
                length -= read;
            }
            if (length != 0) throw new EndOfStreamException();
        }
        public void Flush()
        {
            stream.Flush();
        }
        public long Length { get { return stream.Length; } }
        public bool CanRead { get { return stream.CanRead; } }
        public bool CanWrite { get { return stream.CanWrite; } }
        public void WriteTo(Stream destination, int length)
        {
            CheckSpace(length > BLIT_BUFFER_SIZE ? BLIT_BUFFER_SIZE : length);
            int max = workspace.Length, read;
            position += length;
            while ((length >= max) && (read = stream.Read(workspace, 0, max)) > 0)
            {
                destination.Write(workspace, 0, read);
                length -= read;
            }
            while ((length > 0) && (read = stream.Read(workspace, 0, length)) > 0)
            {
                destination.Write(workspace, 0, read);
                length -= read;
            }
            if (length != 0) throw new EndOfStreamException();
        }
        public void WriteTo(SerializationContext destination, int length)
        {
            WriteTo(destination.stream, length);
            destination.position += length;
        }
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

        public void ReadFrom(SerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            
            this.workspace = context.workspace;
            this.objectStack = context.objectStack;
            this.stackDepth = context.stackDepth;
            this.streamState = context.streamState;
            this.peekedValue = context.peekedValue;
            TraceChangeOrigin(context);

            // IMPORTANT: don't copy the group stack; we want to 
            // validate that the group-stack is empty when finding the end of a stream
        }

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
    
        public SerializationContext(Stream stream)
        {
            this.stream = stream;
            workspace = new byte[InitialBufferLength];
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


        internal int WriteLengthPrefixed<TValue>(TValue value, int underEstimatedLength, ILengthProperty<TValue> property)
        {
            MemoryStream ms = stream as MemoryStream;
            if (ms != null)
            {
                // we'll write to out current stream, optimising
                // for the case when the length-prefix is 1-byte;
                // if not we'll have to BlockCopy
                int startIndex = (int)ms.Position,
                    guessLength = underEstimatedLength,
                    guessPrefixLength = Base128Variant.EncodeInt32(guessLength, this),
                    actualLength = property.Serialize(value, this);

                if (guessLength == actualLength)
                { // good guess! nothing to do...
                    return guessPrefixLength + actualLength;
                }

                int actualPrefixLength = Base128Variant.GetLength(actualLength);

                if (actualPrefixLength < guessPrefixLength)
                {
                    throw new ProtoException("Internal error; the serializer over-estimated the length. Sorry, but this shouldn't have happened.");
                }
                else if (actualPrefixLength > guessPrefixLength)
                {
                    // our guess of the length turned out to be bad; we need to
                    // fix things...

                    // extend the buffer to ensure we have space
                    for (int i = actualPrefixLength - guessPrefixLength; i > 0; i--)
                    {
                        ms.WriteByte(0);
                        position++;
                    }

                    // move the data
                    // (note; we MUST call GetBuffer *after* extending it,
                    // otherwise there the buffer might change if we extend
                    // over a boundary)
                    byte[] buffer = ms.GetBuffer();
                    Buffer.BlockCopy(buffer, startIndex + guessPrefixLength,
                        buffer, startIndex + actualPrefixLength, actualLength);
                }

                // back-fill the actual length into the buffer
                Base128Variant.EncodeUInt64((ulong)actualLength, ms.GetBuffer(), startIndex);
                return actualPrefixLength + actualLength;

            }
            else
            {
                // create a temporary stream and write the final result
                using (ms = new MemoryStream())
                {
                    SerializationContext ctx = new SerializationContext(ms);
                    ctx.ReadFrom(this);
                    int len = property.Serialize(value, ctx);
                    this.ReadFrom(ctx);

                    int preambleLen = Base128Variant.EncodeInt32(len, this);
                    byte[] buffer = ms.GetBuffer();
                    this.Write(buffer, 0, len);
                    return preambleLen + len;
                }
            }
        }
        /*
        internal int WriteLengthPrefixed<TValue>(TValue value, ILengthSerializer<TValue> serializer)
        {
            MemoryStream ms = stream as MemoryStream;
            if (ms != null)
            {
                // we'll write to out current stream, optimising
                // for the case when the length-prefix is 1-byte;
                // if not we'll have to BlockCopy
                int startIndex = (int) ms.Position,
                    guessLength = serializer.UnderestimateLength(value),
                    guessPrefixLength = TwosComplementSerializer.WriteToStream(guessLength, this),
                    actualLength = serializer.Serialize(value, this);

                if (guessLength == actualLength)
                { // good guess! nothing to do...
                    return guessPrefixLength + actualLength;
                }

                int actualPrefixLength = TwosComplementSerializer.GetLength(actualLength);

                if (actualPrefixLength < guessPrefixLength)
                {
                    throw new ProtoException("Internal error; the serializer over-estimated the length. Sorry, but this shouldn't have happened.");
                }
                else if (actualPrefixLength > guessPrefixLength)
                {
                    // our guess of the length turned out to be bad; we need to
                    // fix things...

                    // extend the buffer to ensure we have space
                    for (int i = actualPrefixLength - guessPrefixLength; i > 0; i--)
                    {
                        ms.WriteByte(0);
                        position++;
                    }

                    // move the data
                    // (note; we MUST call GetBuffer *after* extending it,
                    // otherwise there the buffer might change if we extend
                    // over a boundary)
                    byte[] buffer = ms.GetBuffer();
                    Buffer.BlockCopy(buffer, startIndex + guessPrefixLength,
                        buffer, startIndex + actualPrefixLength, actualLength);
                }

                // back-fill the actual length into the buffer
                Base128Variant.EncodeInt64ToWorkspace(actualLength, this);
                Buffer.BlockCopy(workspace, 0, ms.GetBuffer(), startIndex, actualPrefixLength);
                return actualPrefixLength + actualLength;

            }
            else
            {
                // create a temporary stream and write the final result
                using (ms = new MemoryStream())
                {
                    SerializationContext ctx = new SerializationContext(ms);
                    ctx.ReadFrom(this);
                    int len = serializer.Serialize(value, ctx);
                    this.ReadFrom(ctx);

                    int preambleLen = TwosComplementSerializer.WriteToStream(len, this);
                    byte[] buffer = ms.GetBuffer();
                    this.Write(buffer, 0, len);
                    return preambleLen + len;
                }
            }
        }*/

        public static void Reverse4(byte[] buffer)
        {
            byte tmp = buffer[0];
            buffer[0] = buffer[3];
            buffer[3] = tmp;
            tmp = buffer[1];
            buffer[1] = buffer[2];
            buffer[2] = tmp;
        }
        public static void Reverse8(byte[] buffer)
        {
            byte tmp = buffer[0];
            buffer[0] = buffer[7];
            buffer[7] = tmp;
            tmp = buffer[1];
            buffer[1] = buffer[6];
            buffer[6] = tmp;
            tmp = buffer[2];
            buffer[2] = buffer[5];
            buffer[5] = tmp;
            tmp = buffer[3];
            buffer[3] = buffer[4];
            buffer[4] = tmp;
        }

        internal long LimitByLengthPrefix()
        {
            // length-prefixed
            int len = Base128Variant.DecodeInt32(this);
            long oldMaxPos = this.MaxReadPosition;
            this.MaxReadPosition = this.Position + len;
            return oldMaxPos;
        }
    }
}
