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

            // IMPORTANT: don't copy the group stack; we want to 
            // validate that the group-stack is empty when finding the end of a stream
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
    }
}
