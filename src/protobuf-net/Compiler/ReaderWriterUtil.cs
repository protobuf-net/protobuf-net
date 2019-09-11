using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf.Compiler
{
#if FEAT_COMPILER
    internal static class ReaderUtil
    {
        internal static readonly Type ByRefStateType = typeof(ProtoReader.State).MakeByRefType();
        internal static readonly Type[] StateTypeArray = new[] { ByRefStateType },
            ReaderStateTypeArray = new[] { typeof(ProtoReader), ByRefStateType };
    }
    internal static class WriterUtil
    {
        internal static readonly Type ByRefStateType = typeof(ProtoWriter.State).MakeByRefType();

        internal static MethodInfo GetStaticMethod(string name) =>
            MethodWrapper<ProtoWriter>.GetStaticMethod(name);
        internal static MethodInfo GetStaticMethod<T>(string name) =>
            MethodWrapper<T>.GetStaticMethod(name);

        internal static class MethodWrapper<T>
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

                    if (typeof(T) == typeof(ProtoWriter))
                    {
                        if (method.Name == nameof(ProtoWriter.Create)) continue; // ignore all of these
                        if (method.Name == nameof(ProtoWriter.WriteBytes)
                            && (args.Length == 5
                            || (args.Length != 0 && args[0].ParameterType == typeof(System.Buffers.ReadOnlySequence<byte>))
                        ))
                        {   // special omissions
                            continue;
                        }
                    }

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
                    if (_staticWriteMethods.ContainsKey(method.Name))
                        throw new InvalidOperationException($"Ambiguous method: '{method.DeclaringType.Name}.{method.Name}'");
                    _staticWriteMethods.Add(method.Name, method);
                }
            }
        }
    }

}
#endif
