using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf.Internal
{
    internal static class TypeCompatibilityHelper
    {
        private static readonly Dictionary<Module, CompatibilityLevel> s_ByModule = new Dictionary<Module, CompatibilityLevel>();
        internal static CompatibilityLevel GetTypeCompatibilityLevel(Type type, CompatibilityLevel defaultLevel)
        {   // we don't expect to call this lots of times per type, so don't cache that; only cache per module (which also handles assembly)
            if (Attribute.GetCustomAttribute(type, typeof(CompatibilityLevelAttribute), true) is CompatibilityLevelAttribute defined
                && defined.Level > CompatibilityLevel.NotSpecified)
            {
                return defined.Level;
            }

            var module = type.Module;
            if (module is object)
            {
                lock (s_ByModule)
                {
                    if (s_ByModule.TryGetValue(module, out var alreadyKnown))
                    {
                        return alreadyKnown;
                    }
                }
                // I'd rather calculate it twice *outside* the lock than have a single lock
                // that could be blocking multiple paths; so: use indexer-set instead of Add
                var calculated = CalculateFor(module);
                lock (s_ByModule)
                {
                    s_ByModule[module] = calculated;
                }
                return calculated;
            }
            return defaultLevel < CompatibilityLevel.Level200 ? CompatibilityLevel.Level200 : defaultLevel;

            static CompatibilityLevel CalculateFor(Module module)
            {
                if (Attribute.GetCustomAttribute(module, typeof(CompatibilityLevelAttribute), true) is CompatibilityLevelAttribute forModule
                    && forModule.Level > CompatibilityLevel.NotSpecified)
                {
                    return forModule.Level;
                }

                var assembly = module.Assembly;
                if (assembly is object)
                {
                    if (Attribute.GetCustomAttribute(assembly, typeof(CompatibilityLevelAttribute), true) is CompatibilityLevelAttribute forAssembly
                        && forAssembly.Level > CompatibilityLevel.NotSpecified)
                    {
                        return forAssembly.Level;
                    }
                }

                return CompatibilityLevel.Level200;
            }
        }

        internal static CompatibilityLevel GetMemberCompatibilityLevel(MemberInfo member, CompatibilityLevel typeLevel)
            => Attribute.GetCustomAttribute(member, typeof(CompatibilityLevelAttribute), true) is CompatibilityLevelAttribute forMember
                && forMember.Level > CompatibilityLevel.NotSpecified ? forMember.Level : typeLevel;
    }
}
