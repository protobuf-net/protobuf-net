#if !SILVERLIGHT // for silver, see http://weblogs.asp.net/mschwarz/archive/2008/03/07/silverlight-2-and-sockets.aspx
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
#if NET_3_0
using System.Runtime.InteropServices;
using System.ServiceModel;
#endif
using System.Threading;

#if DEBUG
namespace ProtoBuf.ServiceModel
{
    /// <summary>
    /// A client for cmomunicating with a server using the proto-rpc pattern.
    /// </summary>
    public class RpcClient : IDisposable
    {



        private readonly Type interfaceType;
        /// <summary>
        /// Create a new RpcClient for communicating over the given service.
        /// </summary>
        /// <param name="interfaceType">The service represented by this client.</param>
        public RpcClient(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException("interfaceType");
            if (!interfaceType.IsInterface) throw new ArgumentException(interfaceType.Name + " is not an interface.", "interfaceType");
            this.interfaceType = interfaceType;
        }
        private enum RpcClientState
        {
            Created, Open, Closed, Faulted
        }

        private RpcClientState state = RpcClientState.Created;

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            Close();
        }

        /// <summary>
        /// Releases any resources held by this RpcClient.
        /// </summary>
        public void Close()
        {
            Dispose(true);
        }

        /// <summary>
        /// Closes the connection to the server.
        /// </summary>
        protected void Disconnect()
        {
            switch (state)
            {
                case RpcClientState.Open:
                case RpcClientState.Faulted:
                    try
                    {
                        RpcMessage message = new RpcMessage();
                        message.Type = RpcMessageType.Disconnect;
                        Send(message);
                    }
                    finally
                    {
                        try
                        {
                            channel.Close();
                        }
                        catch { } // best endeavors
                        try
                        {
                            tcpClient.Close();
                        }
                        catch { }
                        channel = null;
                        tcpClient = null;
                        state = RpcClientState.Closed;
                    }
                    break;
            }
        }

        /// <summary>
        /// Release any resources associated with this RpcClient.
        /// </summary>
        /// <param name="disposing">Is the client being disposed (as opposed to finalized).</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { Disconnect(); }
                catch { }
            }
            state = RpcClientState.Closed;
        }

        /// <summary>
        /// Opens a connection to the server at the given endpoint.
        /// </summary>
        /// <param name="endPoint">The address and port of the remote server.</param>
        public void Open(IPEndPoint endPoint)
        {
            CheckState(RpcClientState.Created);
            state = RpcClientState.Faulted;
            tcpClient = new TcpClient();
            tcpClient.Connect(endPoint);
            channel = tcpClient.GetStream();
            state = RpcClientState.Open;
        }

        /// <summary>
        /// Verify that the connection is currently open, else throw an exception.
        /// </summary>
        protected void CheckOpen()
        {
            CheckState(RpcClientState.Open);
        }
        /// <summary>
        /// Verify that the connection is currently in the expected state, else throw an exception.
        /// </summary>
        private void CheckState(RpcClientState wanted)
        {
            if (state != wanted)
            {
                switch (state)
                {
                    case RpcClientState.Open:
                        throw new InvalidOperationException("The client is already open.");
                    case RpcClientState.Created:
                        throw new InvalidOperationException("The client is not open.");
                    case RpcClientState.Faulted:
                        throw new InvalidOperationException("The client has faulted.");
                    case RpcClientState.Closed:
                        throw new ObjectDisposedException(GetType().Name);
                    default:
                        throw new InvalidOperationException("Invalid state: " + state);
                }
            }
        }
        private int sequence = 0;
        private Stream channel;
        private TcpClient tcpClient;

        /// <summary>
        /// Sends an untyped message to the remote server, using reflection
        /// to resolve any metadata.
        /// </summary>
        /// <param name="methodName">The name of the operation.</param>
        /// <param name="args">The request to send to the server; if multiple
        /// values are supplied, this will be mapped silently to a wrapper
        /// class that encapsulates the numerous values as sequential fields.</param>
        /// <returns>The response from the server (if any).</returns>
        public object Send(string methodName, params object[] args)
        {
            CheckOpen();
            if (string.IsNullOrEmpty(methodName)) throw new ArgumentNullException("methodName");

            MethodInfo method = interfaceType.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Instance);
            if (method == null) throw new ArgumentException("Unable to locate method: " + interfaceType.Name + "." + methodName, "methodName");
            return Send(method, args);
        }
        static readonly object[] EmptyArgs = new object[0];

        private static string GetOperationName(MethodInfo method)
        {
            string name = method.Name;
#if NET_3_0
            OperationContractAttribute attrib = AttributeUtils.GetAttribute<OperationContractAttribute>(method);
            if(attrib != null && !string.IsNullOrEmpty(attrib.Name)) name = attrib.Name;
#endif
            return name;
        }

        /// <summary>
        /// Sends an untyped message to the remote server, using reflection
        /// to resolve any metadata.
        /// </summary>
        /// <param name="method">The operation to perform.</param>
        /// <param name="args">The request to send to the server; if multiple
        /// values are supplied, this will be mapped silently to a wrapper
        /// class that encapsulates the numerous values as sequential fields.</param>
        /// <returns>The response from the server (if any).</returns>
        private object Send(MethodInfo method, params object[] args)
        {
            CheckOpen();
            if (method == null) throw new ArgumentNullException("method");
            if (args == null) args = EmptyArgs;

            RpcMessage message = new RpcMessage();
            message.Id = (uint) Interlocked.Increment(ref sequence);
            message.Name = method.Name;
            message.Type = RpcMessageType.Request;

            using (MemoryStream ms = new MemoryStream()) {
                PackRequestParameters(true, method, args, ms);
                message.Buffer = ms.ToArray();
            }

        Send(message);
            return null;
        }

        static ParameterInfo VerifyCanBePassedUnwrapped(MethodInfo method)
        {
            if(method == null) throw new ArgumentNullException("method");
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 1 || !IsInputParameter(parameters[0]) || IsOutputParameter(parameters[0])
                || !Serializer.IsEntityType(GetEffectiveType(parameters[0].ParameterType))
                || method.ReturnType == null || !Serializer.IsEntityType(method.ReturnType))
            {
                throw new InvalidOperationException("To be passed unwrapped, the RPC method must have a single argument and return value, both serializable classes.");
            }
            return parameters[0];
        }

        /// <summary>
        /// Pack request parameters for sending to an RPC call.
        /// </summary>
        protected void PackRequestParameters(bool wrapped, MethodInfo method, object[] args, Stream destination)
        {
            if (method == null) throw new ArgumentNullException("method");
            if (args == null) throw new ArgumentNullException("args");
            if (destination == null) throw new ArgumentNullException("destination");

            if(!wrapped)
            {
                ParameterInfo parameter = VerifyCanBePassedUnwrapped(method);

                if (args.Length != 1) throw new InvalidOperationException("Parameter count mismatch.");
                // this changes to the correct type and serializes
                Switch.Serialize(destination, GetEffectiveType(parameter.ParameterType), args[0], PrefixStyle.None);
                return;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != args.Length) throw new InvalidOperationException("Parameter count mismatch.");

            byte[] buffer = new byte[10];

            for (int i = 0; i < args.Length; i++)
            {
                ParameterInfo param = parameters[i];
                if(!IsInputParameter(param)) continue;

                PackString(GetEffectiveType(param.ParameterType), destination, i + 1, args[i], buffer);
            }
        }

        static Type GetEffectiveType(Type type)
        {
            return type.IsByRef ? type.GetElementType() : type;
        }
        static Type GetEffectiveType(Type type, out bool byRef)
        {
            byRef = type.IsByRef;
            return byRef ? type.GetElementType() : type;
        }

        /// <summary>
        /// Pack response parameters for returning from an RPC call.
        /// </summary>
        protected void PackResponseParameters(bool wrapped, MethodInfo method, object result, object[] args, Stream destination)
        {
            if (method == null) throw new ArgumentNullException("method");
            if (args == null) throw new ArgumentNullException("args");
            if (destination == null) throw new ArgumentNullException("destination");

            if (!wrapped)
            {
                ParameterInfo parameter = VerifyCanBePassedUnwrapped(method);

                // this changes to the correct type and serializes
                Switch.Serialize(destination, method.ReturnType, result, PrefixStyle.None);
                return;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != args.Length) throw new InvalidOperationException("Parameter count mismatch.");

            byte[] buffer = new byte[10];

            if(method.ReturnType != typeof(void))
            {
                PackString(method.ReturnType, destination, 1, result, buffer);
            }

            for (int i = 0; i < args.Length; i++)
            {
                ParameterInfo param = parameters[i];
                bool byRef;
                Type effectiveType = GetEffectiveType(param.ParameterType, out byRef);
                if (!byRef) continue; // !IsOutputParameter(param)

                PackString(effectiveType, destination, i + 2, args[i], buffer);
            }
        }

        private static void PackString(Type type, Stream destination, int tag, object value, byte[] buffer)
        {
            // write the field prefix
            uint token = (uint)((tag << 3) | ((int)WireType.String & 7));
            int len = SerializationContext.EncodeUInt32(token, buffer, 0);
            destination.Write(buffer, 0, len);

            // this changes to the correct type and serializes the value
            Switch.Serialize(destination, type, value, PrefixStyle.Base128);
        }
        /// <summary>
        /// Unpack request parameters for processing an RPC call.
        /// </summary>
        protected void UnpackRequestParameters(bool wrapped, MethodInfo method, object[] args, Stream source)
        {
            if (method == null) throw new ArgumentNullException("method");
            if (args == null) throw new ArgumentNullException("args");
            if (source == null) throw new ArgumentNullException("source");
          
            if (!wrapped)
            {
                ParameterInfo parameter = VerifyCanBePassedUnwrapped(method);
                if (1 != args.Length) throw new InvalidOperationException("Parameter count mismatch.");

                // this changes to the correct type and serializes
                args[0] = Switch.Deserialize(source, GetEffectiveType(parameter.ParameterType), PrefixStyle.None);
                return;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != args.Length) throw new InvalidOperationException("Parameter count mismatch.");

            uint token;
            while (SerializationContext.TryDecodeUInt32(source, out token))
            {
                if ((token & 7) != (int)WireType.String) throw new InvalidOperationException("Invalid field prefix found in response");
                token >>= 3;
                if ((token < args.Length + 1) && IsInputParameter(parameters[token - 1]))
                {
                    args[token - 1] = Switch.Deserialize(source, GetEffectiveType(parameters[token - 1].ParameterType),
                                                         PrefixStyle.Base128);
                } else
                {
                    SkipStringData(source);
                }
            }
        }

        static readonly byte[] trashBuffer = new byte[1024];
        static void SkipStringData(Stream stream)
        {
            int bytesRead, bytesRemaining = (int) SerializationContext.DecodeUInt32(stream);
            while (bytesRemaining > trashBuffer.Length && (bytesRead = stream.Read(trashBuffer, 0, trashBuffer.Length)) > 0)
            {
                bytesRemaining -= bytesRead;   
            }
            while (bytesRemaining > 0 && (bytesRead = stream.Read(trashBuffer, 0, bytesRemaining)) > 0)
            {
                bytesRemaining -= bytesRead;
            }
            if(bytesRemaining != 0) throw new EndOfStreamException();
        }

        /// <summary>
        /// Unpack response parameters for completing an RPC call.
        /// </summary>
        protected object UnpackResponseParameters(bool wrapped, MethodInfo method, object[] args, Stream source)
        {
            if (method == null) throw new ArgumentNullException("method");
            if (args == null) throw new ArgumentNullException("args");
            if (source == null) throw new ArgumentNullException("source");

            if(!wrapped)
            {
                VerifyCanBePassedUnwrapped(method);
                // this changes to the correct type and serializes
                return Switch.Deserialize(source, method.ReturnType, PrefixStyle.None);
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != args.Length) throw new InvalidOperationException("Parameter count mismatch.");

            uint token;
            object result = null;
            while(SerializationContext.TryDecodeUInt32(source, out token))
            {
                if((token & 7) != (int)WireType.String) throw new InvalidOperationException("Invalid field prefix found in response");
                token >>= 3;
                if(token == 1)
                {
                    result = Switch.Deserialize(source, method.ReturnType, PrefixStyle.Base128);
                }
                else if ((token < args.Length + 2) && IsOutputParameter(parameters[token - 2]))
                {
                    args[token - 2] = Switch.Deserialize(source, GetEffectiveType(parameters[token - 2].ParameterType),
                                                         PrefixStyle.Base128);
                } else
                {
                    SkipStringData(source);
                }
            }

            return result;
        }

        static bool IsInputParameter(ParameterInfo param)
        {   // can't use IsIn as it isn't supported on CF 2.0/3.5
            return param.Attributes == ParameterAttributes.None
                || ((param.Attributes & ParameterAttributes.In) == ParameterAttributes.In);
        }

        static bool IsOutputParameter(ParameterInfo param)
        {   // can't use IsOut as it isn't supported on CF 2.0/3.5
            return param.ParameterType.IsByRef
                || ((param.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out);
        }

        private void Send(RpcMessage message)
        {
            try
            {
                Serializer.SerializeWithLengthPrefix<RpcMessage>(channel, message, PrefixStyle.Fixed32);
                channel.Flush();
            }
            catch
            {
                if (state == RpcClientState.Open) state = RpcClientState.Faulted;
                throw;
            }
        }

        static class Switch
        {
            internal static void Serialize(Stream destination, Type type, object value, PrefixStyle style)
            {
                typeof(Switch).GetMethod("SerializeGeneric", BindingFlags.Public | BindingFlags.Static)
                    .MakeGenericMethod(type).Invoke(null, new object[] { destination, value, style });
            }
            internal static object Deserialize(Stream source, Type type, PrefixStyle style)
            {
                return typeof(Switch).GetMethod("DeserializeGeneric", BindingFlags.Public | BindingFlags.Static)
                    .MakeGenericMethod(type).Invoke(null, new object[] { source, style });
            }

            // these two need to be public for the Silverlight reflection security model - but
            // the class is internal, so that is fine
            public static void SerializeGeneric<T>(Stream destination, T value, PrefixStyle style)
            {
                Serializer.SerializeWithLengthPrefix<T>(destination, value, style);
            }
            public static T DeserializeGeneric<T>(Stream source, PrefixStyle style)
            {
                return Serializer.DeserializeWithLengthPrefix<T>(source, style);
            }

        }

    }
}
#endif
#endif