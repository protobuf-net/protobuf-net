//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.IO;

//namespace ProtoBuf
//{
//    /// <summary>
//    /// The idea behind a clone stream is that it allows our existing
//    /// deserialization code to parse an input stream, but with the
//    /// side-effect that everything we *read* actually gets written
//    /// to the *destination* stream. This is used to facilitate storage
//    /// of complex "group" sub-messages without the need to buffer.
//    /// </summary>
//    /// <remarks>Neither the source nor destination stream
//    /// is owned/closed by the CloneStream</remarks>
//    internal class CloneStream : Stream
//    {
//        public CloneStream(SerializationContext source, SerializationContext destination)
//        {
//            if (source == null) throw new ArgumentNullException("source");
//            if (destination == null) throw new ArgumentNullException("destination");
//            this.source = source;
//            this.destination = destination;
//        }

//        private SerializationContext source, destination;
//        protected void CheckDisposed()
//        {
//            if (source == null)
//            {
//                throw new ObjectDisposedException(GetType().Name);
//            }
//        }
//        public override bool CanRead
//        {
//            get {
//                CheckDisposed();
//                return source.CanRead;
//            }
//        }

//        public override bool CanSeek
//        {
//            get {
//                CheckDisposed();
//                return false;
//            }
//        }

//        public override bool CanTimeout
//        {
//            get
//            {
//                CheckDisposed(); 
//                return false;
//            }
//        }

//        public override bool CanWrite
//        {
//            get
//            {
//                CheckDisposed();
//                return false;
//            }
//        }

//        public override void Close()
//        {
//            source = null;
//            destination = null;
//            base.Close();
//        }

//        public override void Write(byte[] buffer, int offset, int count)
//        {
//            throw new NotSupportedException();
//        }

//        public override void SetLength(long value)
//        {
//            throw new NotSupportedException();
//        }

//        public override long Seek(long offset, SeekOrigin origin)
//        {
//            throw new NotSupportedException();
//        }

//        public override long Position
//        {
//            get
//            {
//                CheckDisposed();
//                return source.Position;
//            }
//            set
//            {
//                throw new NotSupportedException();
//            }
//        }

//        public override long Length
//        {
//            get
//            {
//                CheckDisposed();
//                return source.Length;
//            }
//        }

//        public override void Flush()
//        {
//            CheckDisposed();
//            destination.Flush();
//        }

//        public override int ReadByte()
//        {
//            CheckDisposed();
//            int b = source.ReadByte();
//            if (b >= 0)
//            {
//                destination.WriteByte((byte)b);
//            }
//            return b;
//        }

//        public override int Read(byte[] buffer, int offset, int count)
//        {
//            CheckDisposed();
//            int bytes = source.Read(buffer, offset, count);
//            if(bytes > 0)
//            {
//                destination.WriteBlock(buffer, offset, bytes);
//            }
//            return bytes;
//        }        
//    }
//}
