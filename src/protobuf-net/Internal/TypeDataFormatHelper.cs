using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf.Internal
{
    /// <summary>
    /// Resolves the cross-cutting per-type <see cref="DataFormat"/> default: the declaring type
    /// (walking base types), then the module, then the assembly — a sibling of
    /// <see cref="TypeCompatibilityHelper"/>, keyed per scalar type because the attribute is
    /// AllowMultiple.
    /// </summary>
    internal static class TypeDataFormatHelper
    {
        private static readonly Dictionary<Module, KeyValuePair<Type, DataFormat>[]> s_ByModule
            = new Dictionary<Module, KeyValuePair<Type, DataFormat>[]>();

        // ApplyDefaultBehaviour runs this once PER MEMBER (unlike TypeCompatibilityHelper's per-type
        // walk, which is deliberately left uncached), so the base-type chain itself needs caching too,
        // not just the module/assembly tail. Keyed on the declaring type, storing the fully-resolved
        // type-chain result so a repeat call - same or different scalarType, same declaringType -
        // never re-walks Attribute.GetCustomAttributes.
        private static readonly Dictionary<Type, KeyValuePair<Type, DataFormat>[]> s_ByType
            = new Dictionary<Type, KeyValuePair<Type, DataFormat>[]>();

        internal static DataFormat GetTypeDataFormat(Type declaringType, Type scalarType)
        {
            foreach (var pair in GetTypeDefaults(declaringType))
            {
                if (pair.Key == scalarType) return pair.Value;
            }
            foreach (var pair in GetModuleDefaults(declaringType.Module))
            {
                if (pair.Key == scalarType) return pair.Value;
            }
            return DataFormat.Default;
        }

        private static KeyValuePair<Type, DataFormat>[] GetTypeDefaults(Type declaringType)
        {
            if (declaringType is null) return Array.Empty<KeyValuePair<Type, DataFormat>>();
            lock (s_ByType)
            {
                if (s_ByType.TryGetValue(declaringType, out var alreadyKnown)) return alreadyKnown;
            }
            // calculated twice outside the lock rather than blocking other paths; indexer-set,
            // not Add — the same trade GetModuleDefaults (and TypeCompatibilityHelper) records
            var calculated = Calculate(declaringType);
            lock (s_ByType)
            {
                s_ByType[declaringType] = calculated;
            }
            return calculated;

            static KeyValuePair<Type, DataFormat>[] Calculate(Type declaringType)
            {
                var result = new List<KeyValuePair<Type, DataFormat>>();
                // most-derived first, matching the explicit base-type walk with inherit: false per
                // level this replaces: AllowMultiple = true makes
                // Attribute.GetCustomAttributes(..., inherit: true) merge base and derived
                // declarations with no defined winner, and derived must win - so a scalarType already
                // seen from a more-derived level is skipped when a base level declares it too.
                for (var current = declaringType; current is object; current = current.BaseType)
                {
                    foreach (ProtoDataFormatAttribute declared in Attribute.GetCustomAttributes(
                        current, typeof(ProtoDataFormatAttribute), inherit: false))
                    {
                        var seen = false;
                        foreach (var pair in result)
                        {
                            if (pair.Key == declared.Type) { seen = true; break; }
                        }
                        if (!seen)
                        {
                            result.Add(new KeyValuePair<Type, DataFormat>(declared.Type, declared.DataFormat));
                        }
                    }
                }
                return result.ToArray();
            }
        }

        private static KeyValuePair<Type, DataFormat>[] GetModuleDefaults(Module module)
        {
            if (module is null) return Array.Empty<KeyValuePair<Type, DataFormat>>();
            lock (s_ByModule)
            {
                if (s_ByModule.TryGetValue(module, out var alreadyKnown)) return alreadyKnown;
            }
            // calculated twice outside the lock rather than blocking other paths; indexer-set,
            // not Add — the same trade TypeCompatibilityHelper records
            var calculated = Calculate(module);
            lock (s_ByModule)
            {
                s_ByModule[module] = calculated;
            }
            return calculated;

            static KeyValuePair<Type, DataFormat>[] Calculate(Module module)
            {
                var result = new List<KeyValuePair<Type, DataFormat>>();
                // module first, then assembly, skipping types the module already declared —
                // module wins, as it does for CompatibilityLevel
                foreach (ProtoDataFormatAttribute declared in Attribute.GetCustomAttributes(
                    module, typeof(ProtoDataFormatAttribute), inherit: true))
                {
                    result.Add(new KeyValuePair<Type, DataFormat>(declared.Type, declared.DataFormat));
                }
                var assembly = module.Assembly;
                if (assembly is object)
                {
                    foreach (ProtoDataFormatAttribute declared in Attribute.GetCustomAttributes(
                        assembly, typeof(ProtoDataFormatAttribute), inherit: true))
                    {
                        var seen = false;
                        foreach (var pair in result)
                        {
                            if (pair.Key == declared.Type) { seen = true; break; }
                        }
                        if (!seen)
                        {
                            result.Add(new KeyValuePair<Type, DataFormat>(declared.Type, declared.DataFormat));
                        }
                    }
                }
                return result.ToArray();
            }
        }
    }
}
