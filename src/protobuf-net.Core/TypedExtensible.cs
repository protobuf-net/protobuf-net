using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtoBuf
{
    /// <summary>
    /// Provides etension methods to access extended (unknown) fields against an instance
    /// </summary>
    public static class TypedExtensible
    {
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
        /// <param name="type">The <see cref="Type"/> that holds the fields, in terms of the inheritance model; the same <c>tag</c> key can appear against different <c>type</c> levels for the same <c>instance</c>, with different values.</param>
        /// <returns>True if data for the field was present, false otherwise.</returns>
        public static bool TryGetValue<TValue>(this ITypedExtensible instance, int tag, out TValue value, Type type = null, DataFormat format = DataFormat.Default, TypeModel model = default)
        {
            var extn = GetExtension(instance, type, false, ref model);
            value = default;
            bool set = false;
            if (extn is not null)
            {
                foreach (TValue val in ExtensibleUtil.GetExtendedValues(model, typeof(TValue), extn, tag, format, true))
                {
                    // expecting at most one yield...
                    // but don't break; need to read entire stream
                    value = val;
                    set = true;
                }
            }
            return set;
        }

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
        /// <param name="type">The <see cref="Type"/> that holds the fields, in terms of the inheritance model; the same <c>tag</c> key can appear against different <c>type</c> levels for the same <c>instance</c>, with different values.</param>
        /// <returns>The effective value of the field, or the default value if not found.</returns>
        public static TValue GetValue<TValue>(this ITypedExtensible instance, int tag, Type type = null, DataFormat format = DataFormat.Default, TypeModel model = default)
            => TryGetValue<TValue>(instance, tag, out TValue value, type, format, model) ? value : default;

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
        /// <param name="type">The <see cref="Type"/> that holds the fields, in terms of the inheritance model; the same <c>tag</c> key can appear against different <c>type</c> levels for the same <c>instance</c>, with different values.</param>
        /// <returns>An enumerator that yields each occurrence of the field.</returns>
        public static IEnumerable<TValue> GetValues<TValue>(this ITypedExtensible instance, int tag, Type type = null, DataFormat format = DataFormat.Default, TypeModel model = default)
        {
            var extn = GetExtension(instance, type, false, ref model);
            return extn is null
                ? Enumerable.Empty<TValue>()
                : ExtensibleUtil.GetExtendedValues(model, typeof(TValue), extn, tag, format, false).Cast<TValue>();
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
        /// <param name="model">The model to use for serialization.</param>
        /// <param name="instance">The extensible object to append the value to.</param>
        /// <param name="tag">The field identifier; the tag should not be defined as a known data-field for the instance.</param>
        /// <param name="value">The value to append.</param>
        /// <param name="type">The <see cref="Type"/> that holds the fields, in terms of the inheritance model; the same <c>tag</c> key can appear against different <c>type</c> levels for the same <c>instance</c>, with different values.</param>
        public static void AppendValue<TValue>(this ITypedExtensible instance, int tag, TValue value, Type type = null, DataFormat format = DataFormat.Default, TypeModel model = default)
        {
            object valueObject = value;
            if (valueObject is null) ThrowHelper.ThrowArgumentNullException(nameof(value));
            ExtensibleUtil.AppendExtendValue(model, GetExtension(instance, type, true, ref model), tag, format, valueObject);
        }

        private static IExtension GetExtension(ITypedExtensible instance, Type type, bool createIfMissing, ref TypeModel model)
        {
            if (instance is null) ThrowHelper.ThrowArgumentNullException(nameof(instance));

            var objType = instance.GetType();
            type ??= objType;

            if (!type.IsClass) // rule out interfaces and structs as target types
            {
                ThrowHelper.ThrowNotSupportedException($"Extension fields can only be used with class target types ('{type.NormalizeName()}' is not valid)");
            }
            if (!ReferenceEquals(type, objType))
            {
                // need to assert that the caller is asking inside the inheritance tree; we'll also exclude
                // object and Extensible, and defer to the model for everything else
                model ??= TypeModel.DefaultModel; // note: if we're still using the null model, we skip the contract type test; it would always say "no"
                if (type == typeof(object) || type == typeof(Extensible) || !type.IsAssignableFrom(objType) || !(model is TypeModel.NullModel || model.CanSerializeContractType(type)))
                {
                    ThrowHelper.ThrowInvalidOperationException($"The extension field target type '{type.NormalizeName()}' is not a valid base-type of '{objType.NormalizeName()}'");
                }
            }
            return instance.GetExtensionObject(type, createIfMissing);
        }
    }
}
