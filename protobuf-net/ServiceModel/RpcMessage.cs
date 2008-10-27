using System.Collections.Generic;

namespace ProtoBuf.ServiceModel
{

    

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