namespace Somewheres
{
    // [global::ClientProxyAttribute(typeof(ServiceProxy0))]
    partial interface IWcfGrpc
    {

    }

    sealed file class ServiceProxy0 : global::Grpc.Core.ClientBase<ServiceProxy0>
    {
        protected override ServiceProxy0 NewInstance(global::Grpc.Core.ClientBaseConfiguration configuration) => new ServiceProxy0(configuration);
        public ServiceProxy0(global::Grpc.Core.ChannelBase channel) : base(channel) {}
        public ServiceProxy0(global::Grpc.Core.CallInvoker callInvoker) : base(callInvoker) {}
        public ServiceProxy0(global::Grpc.Core.ClientBaseConfiguration configuration) : base(configuration) {}
        public ServiceProxy0() : base() {}

    }

}
namespace Somewheres
{
    partial class Foo
    {
        partial struct Bar
        {
            // [global::ClientProxyAttribute(typeof(ServiceProxy1))]
            partial interface IPBGrpc
            {

            }

            sealed file class ServiceProxy1 : global::Grpc.Core.ClientBase<ServiceProxy1>
            {
                protected override ServiceProxy1 NewInstance(global::Grpc.Core.ClientBaseConfiguration configuration) => new ServiceProxy1(configuration);
                public ServiceProxy1(global::Grpc.Core.ChannelBase channel) : base(channel) {}
                public ServiceProxy1(global::Grpc.Core.CallInvoker callInvoker) : base(callInvoker) {}
                public ServiceProxy1(global::Grpc.Core.ClientBaseConfiguration configuration) : base(configuration) {}
                public ServiceProxy1() : base() {}

            }

        }

    }

}
namespace Somewheres.Deeper
{
    // [global::ClientProxyAttribute(typeof(ServiceProxy2))]
    partial interface ISomeBasicService
    {

    }

    sealed file class ServiceProxy2 : global::Grpc.Core.ClientBase<ServiceProxy2>
    {
        protected override ServiceProxy2 NewInstance(global::Grpc.Core.ClientBaseConfiguration configuration) => new ServiceProxy2(configuration);
        public ServiceProxy2(global::Grpc.Core.ChannelBase channel) : base(channel) {}
        public ServiceProxy2(global::Grpc.Core.CallInvoker callInvoker) : base(callInvoker) {}
        public ServiceProxy2(global::Grpc.Core.ClientBaseConfiguration configuration) : base(configuration) {}
        public ServiceProxy2() : base() {}

    }

}
