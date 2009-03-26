using System;

#if NET_3_0
using System.ServiceModel;
#endif
using System.Reflection;
using System.IO;

namespace ProtoBuf.ServiceModel
{
    /// <summary>
    /// Utility operations common to RPC implementations.
    /// </summary>
    public class RpcUtils
    {
        internal const string HTTP_RPC_VERSION_HEADER = "pb-net-rpc";
        internal const string HTTP_RPC_MIME_TYPE = "application/x-protobuf";
        internal static string GetActionName(MethodInfo method)
        {
#if NET_3_0
            OperationContractAttribute oca = (OperationContractAttribute)Attribute.GetCustomAttribute(
                method, typeof(OperationContractAttribute));
            if (oca != null && !string.IsNullOrEmpty(oca.Action)) return oca.Action;
#endif
            return method.Name;
        }

        /// <summary>
        /// Indicates whether the given parameter forms part of a request - i.e.
        /// is "in" or "ref".
        /// </summary>
        /// <param name="parameter">The parameter to test.</param>
        /// <returns>True if the given parameter is part of a request.</returns>
        public static bool IsRequestArgument(ParameterInfo parameter)
        {
            if (parameter == null) throw new ArgumentNullException("parameter");
            if (parameter.ParameterType.IsByRef)
            {
                return (parameter.Attributes & ParameterAttributes.Out) == 0;
            }
            return true;
        }

        /// <summary>
        /// Indicates whether the given parameter forms part of a response - i.e.
        /// is "out" or "ref".
        /// </summary>
        /// <param name="parameter">The parameter to test.</param>
        /// <returns>True if the given parameter is part of a response.</returns>
        public static bool IsResponseArgument(ParameterInfo parameter)
        {
            if (parameter == null) throw new ArgumentNullException("parameter");
            return parameter.ParameterType.IsByRef;
        }
        internal static void PackArgs(Stream stream, MethodInfo method, object result, object[] args, Getter<ParameterInfo, bool> predicate)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (result != null && method.ReturnType != typeof(void))
            {
                Serializer.NonGeneric.SerializeWithLengthPrefix(stream, result, PrefixStyle.Base128, 1);
            }
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo p = parameters[i];
                if (predicate(p) && args[i] != null)
                {
                    Serializer.NonGeneric.SerializeWithLengthPrefix(stream, args[i], PrefixStyle.Base128, i + 2);
                }
            }       
        }
        internal static object UnpackArgs(Stream stream, MethodInfo method, object[] args, Getter<ParameterInfo, bool> predicate)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (args.Length != parameters.Length) throw new ArgumentException("The argument and parameter count do not agree");
            for (int i = 0; i < parameters.Length; i++)
            {
                if (predicate(parameters[i])) args[i] = null; // if the
            }
            object result = null, lastItem;
            int lastPos = -1;
            while (Serializer.NonGeneric.TryDeserializeWithLengthPrefix(stream, PrefixStyle.Base128,
                    delegate(int tag)
                    {
                        lastPos = tag - 2;
                        if (lastPos == -1)
                        {
                            return method.ReturnType == typeof(void) ? null : method.ReturnType;
                        }
                        else if (lastPos >= 0 && lastPos < args.Length && predicate(parameters[lastPos]))
                        {
                            return parameters[lastPos].ParameterType;
                        }
                        return null;
                    }, out lastItem))
            {
                if (lastPos == -1)
                {
                    result = lastItem;
                }
                else
                {
                    args[lastPos] = lastItem;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the name associated with a service contract.
        /// </summary>
        /// <param name="type">The service-contract type.</param>
        /// <returns>The name of the service.</returns>
        public static string GetServiceName(Type type)
        {
            string basicName = type.Name;
            return basicName[0] == 'I' ? basicName.Substring(1) : basicName;
        }
    }

}
