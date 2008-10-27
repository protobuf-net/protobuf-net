#if !SILVERLIGHT // for silver, see http://weblogs.asp.net/mschwarz/archive/2008/03/07/silverlight-2-and-sockets.aspx
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

namespace ProtoBuf.ServiceModel
{

    public class RpcClient : IDisposable
    {
        private readonly Type interfaceType;
        public RpcClient(Type interfaceType)
        {
            if(interfaceType == null) throw new ArgumentNullException("interfaceType");
            if(!interfaceType.IsInterface) throw new ArgumentException(interfaceType.Name + " is not an interface.", "interfaceType");
            this.interfaceType = interfaceType;
        }
        private enum RpcClientState
        {
            Created, Open, Closed, Faulted
        }

        private RpcClientState state = RpcClientState.Created;

        public void Dispose()
        {
            Close();
        }
        public void Close()
        {
            Dispose(true);
        }
        protected void Disconnect()
        {
            switch(state)
            {
                case RpcClientState.Open:
                case RpcClientState.Faulted:
                    try
                    {
                        RpcMessage message = new RpcMessage();
                        message.Type = RpcMessageType.Disconnect;
                        Send(message);
                    } finally
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
        protected virtual void Dispose(bool disposing)
        {
            if(disposing) {
                try {Disconnect();} catch {}
            }
            state = RpcClientState.Closed;
        }

        public void Open(IPEndPoint endPoint)
        {
            CheckState(RpcClientState.Created);
            state = RpcClientState.Faulted;
            tcpClient = new TcpClient();
            tcpClient.Connect(endPoint);
            channel = tcpClient.GetStream();
            state = RpcClientState.Open;
        }

        protected void CheckOpen()
        {
            CheckState(RpcClientState.Open);
        }
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
        protected object Send(MethodInfo method, params object[] args)
        {
            CheckOpen();
            if(method == null) throw new ArgumentNullException("method");
            if (args == null) args = EmptyArgs;

            RpcMessage message = new RpcMessage();
            message.Id = (uint)Interlocked.Increment(ref sequence);
            message.Name = method.Name;
            message.Type = RpcMessageType.Request;

            ParameterInfo[] parameters = method.GetParameters();
            if(parameters.Length != args.Length) throw new InvalidOperationException("Parameter count mismatch.");

            bool prefix = parameters.Length > 1;
            PrefixStyle style = prefix ? PrefixStyle.Base128 : PrefixStyle.None;
            byte[] buffer = prefix ? new byte[10] : null;

            
            using(MemoryStream ms = new MemoryStream())
            {
                for(int i = 0 ; i < args.Length ; i++)
                {
                    if (prefix)
                    {
                        uint token = (uint) (((i + 1) << 3) | ((int) WireType.String & 7));
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
            } catch
            {
                if (state == RpcClientState.Open) state = RpcClientState.Faulted;
                throw;
            }
        }

        static class Switch
        {
            internal static void Serialize(Stream stream, Type type, object value, PrefixStyle style)
            {
                typeof (Switch).GetMethod("SerializeGeneric", BindingFlags.Public | BindingFlags.Static)
                    .MakeGenericMethod(type).Invoke(null, new object[] { stream, value, style });
            }
            public static void SerializeGeneric<T>(Stream destination, T value, PrefixStyle style)
            {
                Serializer.SerializeWithLengthPrefix<T>(destination, value, style);
            }
        }

    }

    [ProtoContract(Name = "Type ")]
    internal enum RpcMessageType
    {
        [ProtoEnum(Name = "REQUEST")]
        Request = 1,
        [ProtoEnum(Name = "RESPONSE")]
        ResponseOK = 2,
        [ProtoEnum(Name = "RESPONSE_CANCEL")]
        ResponseCancel = 3,
        [ProtoEnum(Name = "RESPONSE_FAILED")]
        ResponseFailed = 4,
        [ProtoEnum(Name = "RESPONSE_NOT_IMPLEMENTED")]
        ResponseNotImplemented = 5,
        [ProtoEnum(Name = "DISCONNECT")]
        Disconnect = 6,
        [ProtoEnum(Name = "DESCRIPTOR_REQUEST")]
        DescriptorRequest = 7,
        [ProtoEnum(Name = "DESCRIPTOR_RESPONSE")]
        DescriptorResponse = 8
    }

    [ProtoContract(Name = "Message")]
    internal class RpcMessage
    {
        private RpcMessageType type;
        [ProtoMember(1, Name = "type", IsRequired = true)]
        public RpcMessageType Type
        {
            get { return type; }
            set { type = value; }
        }

        private uint? id;
        [ProtoMember(2, Name = "id", IsRequired = false)]
        public uint? Id
        {
            get { return id; }
            set { id = value; }
        }

        private string name;
        [ProtoMember(3, Name = "name", IsRequired = false)]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private byte[] buffer;
        [ProtoMember(4, Name = "buffer", IsRequired = false)]
        public byte[] Buffer
        {
            get { return buffer; }
            set { buffer = value; }
        }
    }

    [ProtoContract(Name = "DescriptorResponse")]
    internal class RpcDescriptor
    {
        public RpcDescriptor()
        {
            dependencies = new List<RpcDescriptor>();
        }

        private byte[] descriptor;
        [ProtoMember(1, Name="desc", IsRequired = true)]
        public byte[] Descriptor
        {
            get { return descriptor; }
            set { descriptor = value; }
        }

        private List<RpcDescriptor> dependencies;
        [ProtoMember(2, Name="deps")]
        public List<RpcDescriptor> Dependencies
        {
            get { return dependencies; }
            private set { dependencies = value; }
        }

        private string serviceName;
        [ProtoMember(3, Name="serviceName", IsRequired = false)]
        public string ServiceName
        {
            get { return serviceName; }
            set { serviceName = value; }
        }
    }
}
#endif