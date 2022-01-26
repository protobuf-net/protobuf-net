using System;
using System.Collections.Generic;
using ProtoBuf.Meta;
using System.Collections;
using ProtoBuf.Internal;

namespace ProtoBuf
{
    /// <summary>
    /// Simple base class for supporting unexpected fields allowing
    /// for loss-less round-tips/merge, even if the data is not understod.
    /// The additional fields are (by default) stored in-memory in a buffer.
    /// </summary>
    /// <remarks>As an example of an alternative implementation, you might
    /// choose to use the file system (temporary files) as the back-end, tracking
    /// only the paths [such an object would ideally be IDisposable and use
    /// a finalizer to ensure that the files are removed].</remarks>
    /// <seealso cref="IExtensible"/>
    public abstract class Extensible : ITypedExtensible, IExtensible
    {
        // note: not marked ProtoContract - no local state, and can't 
        // predict sub-classes

        private IExtension extensionObject;

#pragma warning disable CS0618 // access to deprecated GetExtensionObject API
        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
            => GetExtensionObject(createIfMissing);

        // if the type requested is the object-type, use the virtual method - otherwise go direct to our private implementation
        IExtension ITypedExtensible.GetExtensionObject(Type type, bool createIfMissing)
            => ReferenceEquals(type, GetType()) ? GetExtensionObject(createIfMissing) : GetExtensionObject(ref extensionObject, type, createIfMissing);
#pragma warning restore CS0618 // access to deprecated GetExtensionObject API

        /// <summary>
        /// Retrieves the <see cref="IExtension">extension</see> object for the current
        /// instance, optionally creating it if it does not already exist.
        /// </summary>
        /// <param name="createIfMissing">Should a new extension object be
        /// created if it does not already exist?</param>
        /// <returns>The extension object if it exists (or was created), or null
        /// if the extension object does not exist or is not available.</returns>
        /// <remarks>The <c>createIfMissing</c> argument is false during serialization,
        /// and true during deserialization upon encountering unexpected fields.</remarks>
        [Obsolete("This API is considered, and may no longer be used in all scenarios (in particular when inheritance is involved); it is not recommended to rely on this API")]
        protected virtual IExtension GetExtensionObject(bool createIfMissing)
            => GetExtensionObject(ref extensionObject, GetType(), createIfMissing);

        /// <summary>
        /// Provides a simple, default implementation for <see cref="IExtension">extension</see> support,
        /// optionally creating it if it does not already exist. Designed to be called by
        /// classes implementing <see cref="IExtensible"/>.
        /// </summary>
        /// <param name="createIfMissing">Should a new extension object be
        /// created if it does not already exist?</param>
        /// <param name="type">The <see cref="Type"/> that holds the fields, in terms of the inheritance model; the same <c>tag</c> key can appear against different <c>type</c> levels for the same <c>instance</c>, with different values.</param>
        /// <param name="extensionObject">The extension field to check (and possibly update).</param>
        /// <returns>The extension object if it exists (or was created), or null
        /// if the extension object does not exist or is not available.</returns>
        /// <remarks>The <c>createIfMissing</c> argument is false during serialization,
        /// and true during deserialization upon encountering unexpected fields.</remarks>
        public static IExtension GetExtensionObject(ref IExtension extensionObject, Type type, bool createIfMissing)
        {
            if (type is null) ThrowHelper.ThrowArgumentNullException(nameof(type));

            // look for a pre-existing node that represents the specified type
            BufferExtension root = extensionObject as BufferExtension, current = root;
            if (root is null)
            {
                if (extensionObject is not null) ThrowHelper.ThrowNotSupportedException($"Custom extension implementations should not be passed to {nameof(GetExtensionObject)}");
            }
            else
            {
                while (current is not null)
                {
                    var targetType = current.Type;
                    if (targetType is null) ThrowHelper.ThrowInvalidOperationException("Typed and untyped extension data cannot be mixed");
                    if (ReferenceEquals(targetType, type)) return current;
                    current = current.Tail;
                }
            }

            if (createIfMissing)
            {
                // create a new node for this level, and add it to the chain
                var newNode = new BufferExtension();
                newNode.SetTail(type, root);
                extensionObject = current = newNode;
            }

            return current;
        }

        /// <summary>
        /// Provides a simple, default implementation for <see cref="IExtension">extension</see> support,
        /// optionally creating it if it does not already exist. Designed to be called by
        /// classes implementing <see cref="IExtensible"/>.
        /// </summary>
        /// <param name="createIfMissing">Should a new extension object be
        /// created if it does not already exist?</param>
        /// <param name="extensionObject">The extension field to check (and possibly update).</param>
        /// <returns>The extension object if it exists (or was created), or null
        /// if the extension object does not exist or is not available.</returns>
        /// <remarks>The <c>createIfMissing</c> argument is false during serialization,
        /// and true during deserialization upon encountering unexpected fields.</remarks>
        public static IExtension GetExtensionObject(ref IExtension extensionObject, bool createIfMissing)
        {
            if (extensionObject is null)
            {
                if (createIfMissing)
                {
                    extensionObject = new BufferExtension();
                }
            }
            else if (extensionObject is BufferExtension be && be.Type is not null)
            {
                ThrowHelper.ThrowInvalidOperationException("Typed and untyped extension data cannot be mixed");
            }
            return extensionObject;
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
            => ExtensibleUtil.AppendExtendValue(default, instance, tag, DataFormat.Default, value);

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
            => ExtensibleUtil.AppendExtendValue(default, instance, tag, format, value);

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
        /// <param name="model">The model to use for serialization.</param>
        /// <param name="instance">The extensible object to append the value to.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <param name="value">The value to append.</param>
        public static void AppendValue<TValue>(TypeModel model, IExtensible instance, int tag, TValue value, DataFormat format = DataFormat.Default)
            => ExtensibleUtil.AppendExtendValue(model, instance, tag, format, value);

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
            => GetValue<TValue>(default, instance, tag, DataFormat.Default);

        /// <summary>
        /// Queries an extensible object for an additional (unexpected) data-field for the instance.
        /// The value returned is the composed value after merging any duplicated content; if the
        /// value is "repeated" (a list), then use GetValues instead.
        /// </summary>
        /// <typeparam name="TValue">The data-type of the field.</typeparam>
        /// <param name="instance">The extensible object to obtain the value from.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <param name="format">The data-format to use when decoding the value.</param>
        /// <returns>The effective value of the field, or the default value if not found.</returns>
        public static TValue GetValue<TValue>(IExtensible instance, int tag, DataFormat format)
            => GetValue<TValue>(default, instance, tag, format);

        /// <summary>
        /// Queries an extensible object for an additional (unexpected) data-field for the instance.
        /// The value returned is the composed value after merging any duplicated content; if the
        /// value is "repeated" (a list), then use GetValues instead.
        /// </summary>
        /// <typeparam name="TValue">The data-type of the field.</typeparam>
        /// <param name="model">The type model to use for deserialization.</param>
        /// <param name="instance">The extensible object to obtain the value from.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <param name="format">The data-format to use when decoding the value.</param>
        /// <returns>The effective value of the field, or the default value if not found.</returns>
        public static TValue GetValue<TValue>(TypeModel model, IExtensible instance, int tag, DataFormat format = DataFormat.Default)
            => TryGetValue<TValue>(model, instance, tag, out TValue value, format, false) ? value : default;

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
            => TryGetValue<TValue>(default, instance, tag, out value, DataFormat.Default, false);

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
            => TryGetValue<TValue>(default, instance, tag, out value, format, false);

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
        /// <param name="allowDefinedTag">Allow tags that are present as part of the definition; for example, to query unknown enum values.</param>
        /// <returns>True if data for the field was present, false otherwise.</returns>
        public static bool TryGetValue<TValue>(IExtensible instance, int tag, DataFormat format, bool allowDefinedTag, out TValue value)
            => TryGetValue<TValue>(default, instance, tag, out value, format, allowDefinedTag);

        /// <summary>
        /// Queries an extensible object for an additional (unexpected) data-field for the instance.
        /// The value returned (in "value") is the composed value after merging any duplicated content;
        /// if the value is "repeated" (a list), then use GetValues instead.
        /// </summary>
        /// <typeparam name="TValue">The data-type of the field.</typeparam>
        /// <param name="value">The effective value of the field, or the default value if not found.</param>
        /// <param name="instance">The extensible object to obtain the value from.</param>
        /// <param name="model">The type model to use for deserialization.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <param name="format">The data-format to use when decoding the value.</param>
        /// <param name="allowDefinedTag">Allow tags that are present as part of the definition; for example, to query unknown enum values.</param>
        /// <returns>True if data for the field was present, false otherwise.</returns>
        public static bool TryGetValue<TValue>(TypeModel model, IExtensible instance, int tag, out TValue value, DataFormat format = DataFormat.Default, bool allowDefinedTag = false)
        {
            value = default;
            bool set = false;
            foreach (TValue val in ExtensibleUtil.GetExtendedValues<TValue>(model, instance, tag, format, true, allowDefinedTag))
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
            => ExtensibleUtil.GetExtendedValues<TValue>(default, instance, tag, DataFormat.Default, false, false);

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
            => ExtensibleUtil.GetExtendedValues<TValue>(default, instance, tag, format, false, false);

        /// <summary>
        /// Queries an extensible object for an additional (unexpected) data-field for the instance.
        /// Each occurrence of the field is yielded separately, making this usage suitable for "repeated"
        /// (list) fields.
        /// </summary>
        /// <remarks>The extended data is processed lazily as the enumerator is iterated.</remarks>
        /// <typeparam name="TValue">The data-type of the field.</typeparam>
        /// <param name="instance">The extensible object to obtain the value from.</param>
        /// <param name="model">The type model to use for deserialization.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <param name="format">The data-format to use when decoding the value.</param>
        /// <returns>An enumerator that yields each occurrence of the field.</returns>
        public static IEnumerable<TValue> GetValues<TValue>(TypeModel model, IExtensible instance, int tag, DataFormat format = DataFormat.Default)
            => ExtensibleUtil.GetExtendedValues<TValue>(model, instance, tag, format, false, false);

        /// <summary>
        /// Queries an extensible object for an additional (unexpected) data-field for the instance.
        /// The value returned (in "value") is the composed value after merging any duplicated content;
        /// if the value is "repeated" (a list), then use GetValues instead.
        /// </summary>
        /// <param name="type">The data-type of the field.</param>
        /// <param name="model">The model to use for configuration.</param>
        /// <param name="value">The effective value of the field, or the default value if not found.</param>
        /// <param name="instance">The extensible object to obtain the value from.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <param name="format">The data-format to use when decoding the value.</param>
        /// <param name="allowDefinedTag">Allow tags that are present as part of the definition; for example, to query unknown enum values.</param>
        /// <returns>True if data for the field was present, false otherwise.</returns>
        public static bool TryGetValue(TypeModel model, Type type, IExtensible instance, int tag, DataFormat format, bool allowDefinedTag, out object value)
        {
            value = null;
            bool set = false;
            foreach (object val in ExtensibleUtil.GetExtendedValues(model, type, instance, tag, format, true, allowDefinedTag))
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
        /// <param name="model">The model to use for configuration.</param>
        /// <param name="type">The data-type of the field.</param>
        /// <param name="instance">The extensible object to obtain the value from.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <param name="format">The data-format to use when decoding the value.</param>
        /// <returns>An enumerator that yields each occurrence of the field.</returns>
        public static IEnumerable GetValues(TypeModel model, Type type, IExtensible instance, int tag, DataFormat format = DataFormat.Default)
            => ExtensibleUtil.GetExtendedValues(model, type, instance, tag, format, false, false);

        /// <summary>
        /// Appends the value as an additional (unexpected) data-field for the instance.
        /// Note that for non-repeated sub-objects, this equates to a merge operation;
        /// for repeated sub-objects this adds a new instance to the set; for simple
        /// values the new value supercedes the old value.
        /// </summary>
        /// <remarks>Note that appending a value does not remove the old value from
        /// the stream; avoid repeatedly appending values for the same field.</remarks>
        /// <param name="model">The model to use for configuration.</param>
        /// <param name="format">The data-format to use when encoding the value.</param>
        /// <param name="instance">The extensible object to append the value to.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <param name="value">The value to append.</param>
        public static void AppendValue(TypeModel model, IExtensible instance, int tag, DataFormat format, object value)
            => ExtensibleUtil.AppendExtendValue(model, instance, tag, format, value);
    }
}