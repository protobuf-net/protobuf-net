using System;
using System.IO;

namespace ProtoBuf
{
    enum Eof
    {
        Unexpected, Expected, Ended
    }
    internal sealed class SerializationContext
    {
        public Eof Eof { get { return eof; } set { eof = value; } }
        private Eof eof;
        private readonly Stream stream;
        public Stream Stream { get { return stream; } }
        private byte[] workspace;
        public byte[] Workspace { get { return workspace; } }
        
        public void ReadWorkspaceFrom(SerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            workspace = context.workspace;
            workspaceIndex = context.workspaceIndex;
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
