using System;
using ProtoBuf.Serializers;
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
            // the typed path where the format selects nothing, so that reading back what
            // AppendValue wrote does not need reflection either; see TryGetExtendedValuesTyped.
            //
            // The argument checks are repeated here rather than skipped: the untyped overload below
            // does them, and going straight to the extension object bypassed them - which
            // Examples.Extensibility's invalid-tag tests caught, since they expect
            // ArgumentOutOfRangeException and were getting an empty result instead.
            if (instance is null) ThrowHelper.ThrowArgumentNullException(nameof(instance));
            if (tag <= 0) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(tag));
            if (format == DataFormat.Default)
            {
                var typed = new List<TValue>();
                if (TryGetExtendedValuesTyped<TValue>(model, instance.GetExtensionObject(false), tag,
                    singleton, typed))
                {
                    return typed;
                }
            }
            return Untyped();

            IEnumerable<TValue> Untyped()
            {
                foreach (TValue value in GetExtendedValues(model, typeof(TValue), instance, tag, format, singleton, allowDefinedTag))
                {
                    yield return value;
                }
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

        /// <summary>
        /// Write a value through the *typed* path, or report that this type has no such serializer.
        /// </summary>
        /// <remarks>
        /// <c>ResolveSerializer&lt;T&gt;</c> is gated on <c>RuntimeFeature.IsDynamicCodeSupported</c>,
        /// so under native AOT it resolves through the model - which a generated model answers - and
        /// never falls back to reflection. Under JIT it behaves exactly as before.
        /// </remarks>
        private static bool TryWriteTyped<TValue>(ref ProtoWriter.State state, int tag, TValue value)
        {
            ISerializer<TValue> serializer;
            try
            {
                serializer = TypeModel.ResolveSerializer<TValue>(state.Model);
            }
            catch
            {
                return false; // no typed serializer; the caller falls back and then reports
            }
            if (serializer is null) return false;
            state.WriteAny<TValue>(tag, value, serializer);
            return true;
        }

        /// <summary>
        /// The typed read: scan the stored bytes for <paramref name="tag"/> and read each occurrence
        /// as <typeparamref name="TValue"/>, with no reflection.
        /// </summary>
        /// <remarks>
        /// The mirror of <see cref="TryWriteTyped"/>, and needed with it rather than instead of it:
        /// an API you can append to but not read back from would be worse than one that refuses.
        /// Returns false when there is no typed serializer, so the caller can fall back.
        /// </remarks>
        internal static bool TryGetExtendedValuesTyped<TValue>(TypeModel model, IExtension extn, int tag,
            bool singleton, List<TValue> results)
        {
            model ??= TypeModel.DefaultModel;
            if (extn is null) return true; // nothing stored; vacuously handled

            ISerializer<TValue> serializer;
            try
            {
                serializer = TypeModel.ResolveSerializer<TValue>(model);
            }
            catch
            {
                return false;
            }
            if (serializer is null) return false;

            Stream stream = extn.BeginQuery();
            try
            {
                // no .Solidify() here, unlike the reflective reader below: that one is an iterator,
                // which cannot hold a ref struct, whereas this fills a list and can use the real state
                var state = ProtoReader.State.Create(stream, model, new SerializationContext(), ProtoReader.TO_EOF);
                try
                {
                    TValue current = default;
                    var any = false;
                    int field;
                    while ((field = state.ReadFieldHeader()) > 0)
                    {
                        if (field != tag)
                        {
                            state.SkipField();
                            continue;
                        }
                        // singleton merges successive occurrences into one value, exactly as the
                        // reflective path does by passing the previous value back in
                        current = state.ReadAny<TValue>(default, singleton ? current : default, serializer);
                        any = true;
                        if (!singleton)
                        {
                            results.Add(current);
                            current = default;
                        }
                    }
                    if (singleton && any) results.Add(current);
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
            return true;
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

        internal static void AppendExtendValue<TValue>(TypeModel model, IExtensible instance, int tag, DataFormat format, TValue value)
        {
            if (instance is null) ThrowHelper.ThrowArgumentNullException(nameof(instance));
            if (value is null) ThrowHelper.ThrowArgumentNullException(nameof(value));
            // obtain the extension object and prepare to write
            AppendExtendValue<TValue>(model, instance.GetExtensionObject(true), tag, format, value);
        }

        // Generic all the way down, deliberately. Every public entry point knows TValue; the previous
        // shape boxed to `object` here and then asked TrySerializeAuxiliaryType to work the type out
        // again by reflection, which is precisely what native AOT cannot do. Keeping TValue means a
        // contract the model knows is resolved without reflection.
        internal static void AppendExtendValue<TValue>(TypeModel model, IExtension extn, int tag, DataFormat format, TValue value)
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
                    // The typed path first: TValue is known at every public entry point, and throwing
                    // it away is the whole reason this could not work under AOT. ResolveSerializer<T>
                    // is the same resolution a generated model's members use, so a contract the model
                    // knows is resolved without reflection.
                    //
                    // Only for DataFormat.Default: any other format selects a wire type, and the
                    // serializer's own features are what WriteAny would use instead. Rather than
                    // reimplement that mapping here, those keep the reflective path - which is the
                    // pre-existing behaviour, and still reports rather than losing the value.
                    if (format == DataFormat.Default && TryWriteTyped(ref state, tag, value))
                    {
                        state.Close();
                        commit = true;
                        return;
                    }

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