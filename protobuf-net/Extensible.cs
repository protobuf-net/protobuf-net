using System;
using System.IO;
using System.Collections.Generic;

namespace ProtoBuf
{
    /// <summary>
    /// Simple base class for supporting unexpected fields allowing
    /// for loss-less round-tips/merge, even if the data is not understod.
    /// The additional fields are stored in-memory in a buffer.
    /// </summary>
    /// <remarks>As an example of an alternative implementation, you might
    /// choose to use the file system (temporary files) as the back-end, tracking
    /// only the paths [such an object would ideally be IDisposable and use
    /// a finalizer to ensure that the files are removed].</remarks>
    [ProtoContract]
    public abstract class Extensible : IExtensible
    {
        private byte[] extendedData;

        int IExtensible.GetLength()
        {
            return Extensible.GetLength(extendedData);
        }
        /// <summary>
        /// Used to implement IExtensible.GetLength() for simple byte[]-based implementations;
        /// returns the length of the current buffer.
        /// </summary>
        /// <param name="buffer">The current buffer instance (can be null).</param>
        /// <returns>The length of the buffer, or 0 if null.</returns>
        public static int GetLength(byte[] buffer)
        {
            return buffer == null ? 0 : buffer.Length;
        }


        Stream IExtensible.BeginAppend()
        {
            return Extensible.BeginAppend();
        }
        /// <summary>
        /// Used to implement IExtensible.EndAppend() for simple byte[]-based implementations;
        /// obtains a new Stream suitable for storing data as a simple buffer.
        /// </summary>
        /// <returns>The stream for storing data.</returns>
        public static Stream BeginAppend()
        {
            return new MemoryStream();
        }


        void IExtensible.EndAppend(Stream stream, bool commit)
        {
            extendedData = Extensible.EndAppend(extendedData, stream, commit);
        }
        /// <summary>
        /// Used to implement IExtensible.EndAppend() for simple byte[]-based implementations;
        /// creates/resizes the buffer accordingly (copying any existing data), and places
        /// the new data at the end of the buffer.
        /// </summary>
        /// <param name="buffer">The current buffer instance (can be null).</param>
        /// <param name="stream">The stream previously obtained from BeginAppend.</param>
        /// <param name="commit">Should the data be stored? Or just close the stream?</param>
        /// <returns>The updated buffer.</returns>
        public static byte[] EndAppend(byte[] buffer, Stream stream, bool commit)
        {
            
            using (stream)
            {
                int len;
                if (commit && (len = (int)stream.Length)>0)
                {
                    MemoryStream ms = (MemoryStream)stream;
                    
                    // note: Array.Resize not available on CF
                    int offset;
                    if (buffer == null)
                    {   // allocate new buffer
                        offset = 0;
                        buffer = new byte[len];
                    }
                    else
                    {   // resize and copy the data
                        offset = buffer.Length;
                        byte[] tmp = buffer;
                        buffer = new byte[offset + len];
                        Buffer.BlockCopy(tmp, 0, buffer, 0, offset);
                    }
                    // copy the data from the stream
                    byte[] raw = ms.GetBuffer();
                    Buffer.BlockCopy(raw, 0, buffer, offset, len);
                }
                return buffer;
            }
        }

        Stream IExtensible.BeginQuery()
        {
            return Extensible.BeginQuery(extendedData);
        }
        /// <summary>
        /// Used to implement ISerializable.BeginQuery() for simple byte[]-based implementations;
        /// returns a stream representation of the current buffer.
        /// </summary>
        /// <param name="buffer">The current buffer instance (can be null).</param>
        /// <returns>A stream representation of the buffer.</returns>
        public static Stream BeginQuery(byte[] buffer)
        {
            return buffer == null ? Stream.Null : new MemoryStream(buffer);
        }
        /// <summary>
        /// Used to implement ISerializable.BeginQuery() for simple byte[]-based implementations;
        /// closes the stream.</summary>
        /// <param name="stream">The stream previously obtained from BeginQuery.</param>
        void IExtensible.EndQuery(Stream stream)
        {
            Extensible.EndQuery(stream);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        public static void EndQuery(Stream stream)
        {
            using (stream) { }
        }

        /// <summary>
        /// Appends the value as an additional (unexpected) data-field for the instance.
        /// Note that for non-repeated sub-objects, this equates to a merge operation;
        /// for repeated sub-objects this adds a new instance to the set; for simple
        /// values the new value supercedes the old value.
        /// </summary>
        /// <remarks>Note that appending a value does not remove the old value from
        /// the stream; avoid repeatedly appending values for the same field.</remarks>
        /// <typeparam name="TValue">The type of the value to append.</typeparam>
        /// <param name="instance">The extensible object to append the value to.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <param name="value">The value to append.</param>
        public static void AppendValue<TValue>(IExtensible instance, int tag, TValue value)
        {
            AppendValue<TValue>(instance, tag, DataFormat.Default, value);
        }
        /// <summary>
        /// Appends the value as an additional (unexpected) data-field for the instance.
        /// Note that for non-repeated sub-objects, this equates to a merge operation;
        /// for repeated sub-objects this adds a new instance to the set; for simple
        /// values the new value supercedes the old value.
        /// </summary>
        /// <remarks>Note that appending a value does not remove the old value from
        /// the stream; avoid repeatedly appending values for the same field.</remarks>
        /// <typeparam name="TValue">The data-type of the field.</typeparam>
        /// <param name="format">The data-format to use when encoding the value.</param>
        /// <param name="instance">The extensible object to append the value to.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <param name="value">The value to append.</param>
        public static void AppendValue<TValue>(IExtensible instance, int tag, DataFormat format, TValue value)
        {
            ExtensibleUtil.AppendExtendValue<TValue>(instance, tag, format, value);
        }
        /// <summary>
        /// Queries an extensible object for an additional (unexpected) data-field for the instance.
        /// The value returned is the composed value after merging any duplicated content; if the
        /// value is "repeated" (a list), then use GetValues instead.
        /// </summary>
        /// <typeparam name="TValue">The data-type of the field.</typeparam>
        /// <param name="instance">The extensible object to obtain the value from.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <returns>The effective value of the field, or the default value if not found.</returns>
        public static TValue GetValue<TValue>(IExtensible instance, int tag)
        {
            return GetValue<TValue>(instance, tag, DataFormat.Default);
        }
        /// <summary>
        /// Queries an extensible object for an additional (unexpected) data-field for the instance.
        /// The value returned is the composed value after merging any duplicated content; if the
        /// value is "repeated" (a list), then use GetValues instead.
        /// </summary>
        /// <typeparam name="TValue">The data-type of the field.</typeparam>
        /// <param name="format">The data-format to use when decoding the value.</param>
        /// <param name="instance">The extensible object to obtain the value from.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <returns>The effective value of the field, or the default value if not found.</returns>
        public static TValue GetValue<TValue>(IExtensible instance, int tag, DataFormat format)
        {
            TValue value;
            TryGetValue<TValue>(instance, tag, format, out value);
            return value;
        }
        /// <summary>
        /// Queries an extensible object for an additional (unexpected) data-field for the instance.
        /// The value returned (in "value") is the composed value after merging any duplicated content;
        /// if the value is "repeated" (a list), then use GetValues instead.
        /// </summary>
        /// <typeparam name="TValue">The data-type of the field.</typeparam>
        /// <param name="value">The effective value of the field, or the default value if not found.</param>
        /// <param name="instance">The extensible object to obtain the value from.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <returns>True if data for the field was present, false otherwise.</returns>
        public static bool TryGetValue<TValue>(IExtensible instance, int tag, out TValue value)
        {
            return TryGetValue<TValue>(instance, tag, DataFormat.Default, out value);
        }
        /// <summary>
        /// Queries an extensible object for an additional (unexpected) data-field for the instance.
        /// The value returned (in "value") is the composed value after merging any duplicated content;
        /// if the value is "repeated" (a list), then use GetValues instead.
        /// </summary>
        /// <typeparam name="TValue">The data-type of the field.</typeparam>
        /// <param name="value">The effective value of the field, or the default value if not found.</param>
        /// <param name="instance">The extensible object to obtain the value from.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <param name="format">The data-format to use when decoding the value.</param>
        /// <returns>True if data for the field was present, false otherwise.</returns>
        public static bool TryGetValue<TValue>(IExtensible instance, int tag, DataFormat format, out TValue value)
        {
            value = default(TValue);
            bool set = false;
            foreach (TValue val in ExtensibleUtil.GetExtendedValues<TValue>(instance, tag, format, true))
            {
                // expecting at most one yield...
                // but don't break; need to read entire stream
                value = val;
                set = true;
            }
            return set;
        }
        /// <summary>
        /// Queries an extensible object for an additional (unexpected) data-field for the instance.
        /// Each occurrence of the field is yielded separately, making this usage suitable for "repeated"
        /// (list) fields.
        /// </summary>
        /// <remarks>The extended data is processed lazily as the enumerator is iterated.</remarks>
        /// <typeparam name="TValue">The data-type of the field.</typeparam>
        /// <param name="instance">The extensible object to obtain the value from.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <returns>An enumerator that yields each occurrence of the field.</returns>
        public static IEnumerable<TValue> GetValues<TValue>(IExtensible instance, int tag)
        {
            return ExtensibleUtil.GetExtendedValues<TValue>(instance, tag, DataFormat.Default, false);
        }
        /// <summary>
        /// Queries an extensible object for an additional (unexpected) data-field for the instance.
        /// Each occurrence of the field is yielded separately, making this usage suitable for "repeated"
        /// (list) fields.
        /// </summary>
        /// <remarks>The extended data is processed lazily as the enumerator is iterated.</remarks>
        /// <typeparam name="TValue">The data-type of the field.</typeparam>
        /// <param name="instance">The extensible object to obtain the value from.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <param name="format">The data-format to use when decoding the value.</param>
        /// <returns>An enumerator that yields each occurrence of the field.</returns>
        public static IEnumerable<TValue> GetValues<TValue>(IExtensible instance, int tag, DataFormat format)
        {
            return ExtensibleUtil.GetExtendedValues<TValue>(instance, tag, format, false);
        }



    }
}
