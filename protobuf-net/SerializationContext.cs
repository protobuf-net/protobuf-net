using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf
{
    internal enum Eof
    {
        /// <summary>
        /// Indicates that an EOF is not anticipated, and so will throw an exception.
        /// </summary>
        Unexpected,
        
        /// <summary>
        /// Indicates that an EOF is acceptable at the current time and will
        /// not throw an exception.
        /// </summary>
        Expected,
        
        /// <summary>
        /// Indicates that an anticipated EOF was found.
        /// </summary>
        Ended
    }

    internal sealed class SerializationContext
    {
        public readonly ProtoDecimal DecimalTemplate = new ProtoDecimal();
        public readonly ProtoGuid GuidTemplate = new ProtoGuid();
        public readonly ProtoTimeSpan TimeSpanTemplate = new ProtoTimeSpan();

        public const string VerboseSymbol = "VERBOSE", DebugCategory = "protobuf-net";
        private Stack<object> objectStack = new Stack<object>();
        private Stack<int> groupStack;
        public Eof Eof
        {
            get { return eof; }
            set { eof = value; }
        }

        private Eof eof;
        private readonly Stream stream;
        
        public Stream Stream
        {
            get { return stream; }
        }
        private long position = 0, maxReadPosition = long.MaxValue;
        public long Position { get { return position; } }
        public long MaxReadPosition { get { return maxReadPosition; } set { maxReadPosition = value; } }

        public bool IsDataAvailable { get { return position < maxReadPosition; } }

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

        private const int InitialBufferLength = 32;
        
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


        internal int WriteEntity<TEntity>(TEntity value) where TEntity : class, new()
        {
            MemoryStream ms = stream as MemoryStream;
            if (ms != null)
            {
                // we'll write to out current stream, optimising
                // for the case when the length-prefix is 1-byte;
                // if not we'll have to BlockCopy
                int startIndex = (int) ms.Position;
                ms.WriteByte(0); // fill this in later!
                int len = Serializer<TEntity>.Serialize(value, this, null);
                if (len == 0)
                { // lucky guess!
                    position++;
                    return 1;
                } else if (len < 128) {
                    // fix the length...
                    ms.GetBuffer()[startIndex] = (byte)len;
                    position += len + 1;
                    return len + 1;
                }

                // damn; we needed a multi-byte prefix!
                int preambleLen = TwosComplementSerializer.GetLength(len);
                for (int i = 1; i < preambleLen; i++)
                { // extend the buffer if needed...
                    ms.WriteByte(0);
                }
                byte[] buffer = ms.GetBuffer();
                Buffer.BlockCopy(buffer, startIndex + 1, buffer, startIndex + preambleLen, len);
                // now back-fill the actual size
                Base128Variant.EncodeInt64(len, workspace);
                for (int i = 0; i < preambleLen; i++)
                {
                    buffer[startIndex + i] = workspace[i];
                }
                len += preambleLen;
                position += len;
                return len;
            }
            else
            {
                // create a temporary stream and write the final result
                using (ms = new MemoryStream())
                {
                    SerializationContext ctx = new SerializationContext(ms);
                    ctx.ReadFrom(this);
                    int len = Serializer<TEntity>.Serialize(value, ctx, null);
                    this.ReadFrom(ctx);

                    int preambleLen = TwosComplementSerializer.WriteToStream(len, this);
                    byte[] buffer = ms.GetBuffer();
                    this.Write(buffer, 0, len);
                    return preambleLen + len;
                }
            }
        }
    }
}
