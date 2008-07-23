using System;
using System.IO;
using System.Collections.Generic;

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

        private byte[] workspace;
        public byte[] Workspace
        {
            get { return workspace; }
        }

        public void Push(object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            foreach (object stackItem in objectStack)
            {
                if (ReferenceEquals(stackItem, obj))
                {
                    throw new ProtoException("Recursive structure detected; only object trees (not full graphs) can be serialized");
                }
            }
            objectStack.Push(obj);
        }

        public void Pop(object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (objectStack.Count == 0 || !ReferenceEquals(objectStack.Pop(), obj))
            {
                throw new ProtoException("Stack corruption; incorrect object popped");
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
            this.workspaceIndex = context.workspaceIndex;
            this.objectStack = context.objectStack;

            // IMPORTANT: don't copy the group stack; we want to 
            // validate that the group-stack is empty when finding the end of a stream
        }

        public void CheckNoRemainingGroups()
        {
            if (groupStack != null && groupStack.Count > 0)
            {
                throw new ProtoException("Unterminated group(s) in a message or sub-message");
            }
        }

        // not used at the moment; if anything wants to
        // use non-zero workspace offsets then uncomment
        // the field / property
        private int workspaceIndex;
        public int WorkspaceIndex
        {
            get
            {
                return workspaceIndex;
            }

            set
            {
                if (value < 0 || value >= workspace.Length)
                {
                    throw new ArgumentOutOfRangeException("value", "WorkspaceIndex must be inside the current workspace.");
                }
                workspaceIndex = value;
            }
        }
     
        public SerializationContext(Stream stream)
        {
            this.stream = stream;
            workspace = new byte[InitialBufferLength];
        }

        private const int InitialBufferLength = 32;
        public void CheckSpace()
        {
            CheckSpace(InitialBufferLength);
        }

        public void CheckSpace(int length)
        {
            length += WorkspaceIndex;
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

        public int Write(int count) // return is for fluent calling
        {
            Stream.Write(Workspace, WorkspaceIndex, count);
            return count;
        }
    }
}
