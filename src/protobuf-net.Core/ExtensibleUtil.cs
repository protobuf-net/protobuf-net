using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ProtoBuf.Internal;
using ProtoBuf.Meta;

namespace ProtoBuf
{
    /// <summary>
    /// This class acts as an internal wrapper allowing us to do a dynamic
    /// methodinfo invoke; an't put into Serializer as don't want on public
    /// API; can't put into Serializer&lt;T&gt; since we need to invoke
    /// across classes
    /// </summary>
    internal static class ExtensibleUtil
    {
        /// <summary>
        /// All this does is call GetExtendedValuesTyped with the correct type for "instance";
        /// this ensures that we don't get issues with subclasses declaring conflicting types -
        /// the caller must respect the fields defined for the type they pass in.
        /// </summary>
        internal static IEnumerable<TValue> GetExtendedValues<TValue>(TypeModel model, IExtensible instance, int tag, DataFormat format, bool singleton, bool allowDefinedTag)
        {
            foreach (TValue value in GetExtendedValues(model, typeof(TValue), instance, tag, format, singleton, allowDefinedTag))
            {
                yield return value;
            }
        }

#pragma warning disable RCS1163, IDE0060 // Unused parameter.
        /// <summary>
        /// All this does is call GetExtendedValuesTyped with the correct type for "instance";
        /// this ensures that we don't get issues with subclasses declaring conflicting types -
        /// the caller must respect the fields defined for the type they pass in.
        /// </summary>
        internal static IEnumerable GetExtendedValues(TypeModel model, Type type, IExtensible instance, int tag, DataFormat format, bool singleton, bool allowDefinedTag)
#pragma warning restore RCS1163, IDE0060 // Unused parameter.
        {
            if (instance is null) ThrowHelper.ThrowArgumentNullException(nameof(instance));
            if (tag <= 0) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(tag));
            return GetExtendedValues(model, type, instance.GetExtensionObject(false), tag, format, singleton);
        }

        internal static IEnumerable GetExtendedValues(TypeModel model, Type type, IExtension extn, int tag, DataFormat format, bool singleton)
        {
            model ??= TypeModel.DefaultModel;

            if (extn is null)
            {
                yield break;
            }

            Stream stream = extn.BeginQuery();
            try
            {
                object value = null;
                SerializationContext ctx = new SerializationContext();
                var state = ProtoReader.State.Create(stream, model, ctx, ProtoReader.TO_EOF).Solidify();
                try
                {
                    while (model.TryDeserializeAuxiliaryType(ref state, format, tag, type, ref value, true, true, false, false, null, isRoot: false) && value is not null)
                    {
                        if (!singleton)
                        {
                            yield return value;

                            value = null; // fresh item each time
                        }
                    }
                    if (singleton && value is not null)
                    {
                        yield return value;
                    }
                }
                finally
                {
                    state.Dispose();
                }
            }
            finally
            {
                extn.EndQuery(stream);
            }
        }

        internal static void AppendExtendValue(TypeModel model, IExtensible instance, int tag, DataFormat format, object value)
        {
            if (instance is null) ThrowHelper.ThrowArgumentNullException(nameof(instance));
            if (value is null) ThrowHelper.ThrowArgumentNullException(nameof(value));
            // obtain the extension object and prepare to write
            AppendExtendValue(model, instance.GetExtensionObject(true), tag, format, value);
        }
        internal static void AppendExtendValue(TypeModel model, IExtension extn, int tag, DataFormat format, object value)
        {
            model ??= TypeModel.DefaultModel;
            
            if (extn is null) ThrowHelper.ThrowInvalidOperationException("No extension object available; appended data would be lost.");
            bool commit = false;
            Stream stream = extn.BeginAppend();
            try
            {
                var state = ProtoWriter.State.Create(stream, model, null);
                try
                {
                    // The result was previously discarded, and `commit = true` set regardless - so a
                    // write that did not happen was committed as though it had. That is silent data
                    // loss on an API whose entire purpose is round-trip fidelity, and it is the
                    // normal outcome under native AOT: this path resolves the serializer
                    // reflectively (note the null type), which is exactly what trimming removes.
                    //
                    // Throwing leaves the append transaction abandoned by the catch below, so
                    // nothing is written either way; the difference is that the caller finds out.
                    if (!model.TrySerializeAuxiliaryType(ref state, null, format, tag, value, false, null, isRoot: false))
                    {
                        ThrowHelper.ThrowInvalidOperationException(
                            $"Unable to append a value of type {value.GetType().NormalizeName()}: no serializer "
                            + "could be resolved for it. This API resolves serializers by reflection, so it does "
                            + "not work under native AOT or aggressive trimming.");
                    }
                    state.Close();
                }
                catch
                {
                    state.Abandon();
                    throw;
                }
                finally
                {
                    state.Dispose();
                }

                commit = true;
            }
            finally
            {
                extn.EndAppend(stream, commit);
            }
        }
    }
}