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

        internal static DataFormat GetTypeDataFormat(Type declaringType, Type scalarType)
        {
            // explicit base-type walk with inherit: false per level: AllowMultiple = true makes
            // Attribute.GetCustomAttributes(..., inherit: true) merge base and derived declarations
            // with no defined winner, and derived must win
            for (var current = declaringType; current is object; current = current.BaseType)
            {
                if (FindDeclared(Attribute.GetCustomAttributes(
                    current, typeof(ProtoDataFormatAttribute), inherit: false), scalarType) is { } declared)
                {
                    return declared;
                }
            }
            foreach (var pair in GetModuleDefaults(declaringType.Module))
            {
                if (pair.Key == scalarType) return pair.Value;
            }
            return DataFormat.Default;
        }

        private static DataFormat? FindDeclared(Attribute[] attributes, Type scalarType)
        {
            foreach (var attribute in attributes)
            {
                if (attribute is ProtoDataFormatAttribute declared && declared.Type == scalarType)
                {
                    return declared.DataFormat;
                }
            }
            return null;
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
