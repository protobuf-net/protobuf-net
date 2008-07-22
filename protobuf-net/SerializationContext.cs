using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ProtoBuf
{
    enum Eof
    {
        Unexpected, Expected, Ended
    }
    internal sealed class SerializationContext
    {
        private List<object> stack = new List<object>();
        public Eof Eof { get { return eof; } set { eof = value; } }
        private Eof eof;
        private readonly Stream stream;
        public Stream Stream { get { return stream; } }
        private byte[] workspace;
        public byte[] Workspace { get { return workspace; } }

        public void Push(object obj)
        {
            if(obj == null) throw new ArgumentNullException("obj");
            foreach(object stackItem in stack) {
                if(ReferenceEquals(stackItem, obj))
                    throw new SerializationException("Recursive structure detected; only object trees (not full graphs) can be serialized");
            }
            stack.Add(obj);
        }
        public void Pop(object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            int index = stack.Count - 1;
            object last = index < 0 ? null : stack[index];
            if (!ReferenceEquals(obj, last))
            {
                throw new SerializationException("Stack corruption; incorrect object popped");
            }
            stack.RemoveAt(index);
        }


        public void ReadFrom(SerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            this.workspace = context.workspace;
            this.workspaceIndex = context.workspaceIndex;
            this.stack = context.stack;
        }
        // not used at the moment; if anything wants to
        // use non-zero workspace offsets then uncomment
        // the field / property
        private int workspaceIndex;
        public int WorkspaceIndex
        {
            get { return workspaceIndex; }
            set
            {
                if (value < 0 || value >= workspace.Length)
                {
                    throw new ArgumentOutOfRangeException("WorkspaceIndex");
                }
                workspaceIndex = value;
            }
        }
     
        public SerializationContext(Stream stream)
        {
            this.stream = stream;
            workspace = new byte[STD_BUFFER_LEN];
        }
        private const int STD_BUFFER_LEN = 32;
        public void CheckSpace()
        {
            CheckSpace(STD_BUFFER_LEN);
        }
        public void CheckSpace(int length)
        {
            length += WorkspaceIndex;
            if (workspace.Length < length)
            {
                int newLen = workspace.Length * 2; // try doubling
                if (length > newLen) newLen = length; // as long as that gives us enough ;-p
                Array.Resize(ref workspace, newLen);
            }
        }

        public int Write(int count) // return is for fluent calling
        {
            Stream.Write(Workspace, WorkspaceIndex, count);
            return count;
        }
    }
}
