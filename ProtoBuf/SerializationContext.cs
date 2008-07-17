using System;
using System.IO;

namespace ProtoBuf
{
    internal sealed class SerializationContext
    {
        private readonly Stream stream;
        public Stream Stream { get { return stream; } }
        private byte[] workspace;
        public byte[] Workspace { get { return workspace; } }
        private int workspaceIndex;
        public void ReadWorkspaceFrom(SerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            workspace = context.workspace;
            workspaceIndex = context.workspaceIndex;
        }
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
        public void Zero(int len)
        {
            int index = WorkspaceIndex;
            byte[] buffer = Workspace;
            switch (len)
            {
                case 0: break;
                case 1:
                    buffer[index] = 0;
                    break;
                case 2:
                    buffer[index++] = 0;
                    buffer[index] = 0;
                    break;
                case 4:
                    buffer[index++] = 0;
                    buffer[index++] = 0;
                    buffer[index++] = 0;
                    buffer[index] = 0;
                    break;
                case 8:
                    buffer[index++] = 0;
                    buffer[index++] = 0;
                    buffer[index++] = 0;
                    buffer[index++] = 0;
                    buffer[index++] = 0;
                    buffer[index++] = 0;
                    buffer[index++] = 0;
                    buffer[index] = 0;
                    break;
                default:
                    Array.Clear(buffer, index, len);
                    break;

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
