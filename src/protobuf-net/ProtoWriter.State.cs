using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf
{
    public partial class ProtoWriter
    {
#if FEAT_COMPILER
        internal static readonly Type ByRefStateType = typeof(State).MakeByRefType();

        internal static MethodInfo GetStaticMethod(string name) =>
            MethodWrapper<ProtoWriter>.GetStaticMethod(name);
        internal static MethodInfo GetStaticMethod<T>(string name) =>
            MethodWrapper<T>.GetStaticMethod(name);
        private static class MethodWrapper<T>
        {
            private static readonly Dictionary<string, MethodInfo> _staticWriteMethods;

            public static MethodInfo GetStaticMethod(string name) => _staticWriteMethods[name];

            static MethodWrapper()
            {
                _staticWriteMethods = new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (var method in typeof(T)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (method.IsDefined(typeof(ObsoleteAttribute), true)) continue;
                    var args = method.GetParameters();
                    if (args == null || args.Length == 0) continue;
                    bool haveState = false;
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i].ParameterType == ByRefStateType)
                        {
                            haveState = true;
                            break;
                        }
                    }
                    if (!haveState) continue;
                    _staticWriteMethods.Add(method.Name, method);
                }
            }
        }
#endif
        /// <summary>
        /// Writer state
        /// </summary>
        public ref struct State {}
    }
}
