#if !SILVERLIGHT // for silver, see http://weblogs.asp.net/mschwarz/archive/2008/03/07/silverlight-2-and-sockets.aspx
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
#if NET_3_0
using System.ServiceModel;
#endif
using System.Threading;

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
            message.Id = (uint)Interlocked.Increment(ref sequence);
            message.Name = method.Name;
            message.Type = RpcMessageType.Request;

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != args.Length) throw new InvalidOperationException("Parameter count mismatch.");

            bool prefix = parameters.Length > 1;
            PrefixStyle style = prefix ? PrefixStyle.Base128 : PrefixStyle.None;
            byte[] buffer = prefix ? new byte[10] : null;


            using (MemoryStream ms = new MemoryStream())
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (prefix)
                    {
                        uint token = (uint)(((i + 1) << 3) | ((int)WireType.String & 7));
                        int len = SerializationContext.EncodeUInt32(token, buffer, 0);
                        ms.Write(buffer, 0, len);
                    }
                    ParameterInfo param = parameters[i];
                    Switch.Serialize(ms, param.ParameterType, args[i], style);
                }
                message.Buffer = ms.ToArray();
            }

            Send(message);
            return null;
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
            internal static void Serialize(Stream stream, Type type, object value, PrefixStyle style)
            {
                typeof(Switch).GetMethod("SerializeGeneric", BindingFlags.Public | BindingFlags.Static)
                    .MakeGenericMethod(type).Invoke(null, new object[] { stream, value, style });
            }
            public static void SerializeGeneric<T>(Stream destination, T value, PrefixStyle style)
            {
                Serializer.SerializeWithLengthPrefix<T>(destination, value, style);
            }
        }

    }
}
#endif