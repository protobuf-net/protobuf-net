namespace Somewheres
{
    // [global::ClientProxyAttribute(typeof(ServiceProxy0))]
    partial interface IWcfGrpc
    {

    }

    sealed file class ServiceProxy0 : global::Grpc.Core.ClientBase<ServiceProxy0>
    {
        // public ServiceProxy0() : base() {}
        // public ServiceProxy0(global::Grpc.Core.ChannelBase channel) : base(channel) {}
        public ServiceProxy0(global::Grpc.Core.CallInvoker callInvoker) : base(callInvoker) {}
        private ServiceProxy0(global::Grpc.Core.ClientBase.ClientBaseConfiguration configuration) : base(configuration) {}
        protected override ServiceProxy0 NewInstance(global::Grpc.Core.ClientBase.ClientBaseConfiguration configuration) => new ServiceProxy0(configuration);

        private const string _pbn_ServiceName = "IWcfGrpc";

        private static readonly global::Grpc.Core.Method<global::Somewheres.Foo, global::Somewheres.Foo> _pbn_Method0 = new global::Grpc.Core.Method<global::Somewheres.Foo, global::Somewheres.Foo>(global::Grpc.Core.MethodType.Unary, _pbn_ServiceName, "Do", null!, null!);

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
                // public ServiceProxy1() : base() {}
                // public ServiceProxy1(global::Grpc.Core.ChannelBase channel) : base(channel) {}
                public ServiceProxy1(global::Grpc.Core.CallInvoker callInvoker) : base(callInvoker) {}
                private ServiceProxy1(global::Grpc.Core.ClientBase.ClientBaseConfiguration configuration) : base(configuration) {}
                protected override ServiceProxy1 NewInstance(global::Grpc.Core.ClientBase.ClientBaseConfiguration configuration) => new ServiceProxy1(configuration);

                private const string _pbn_ServiceName = "IPBGrpc";

                private static readonly global::Grpc.Core.Method<global::Somewheres.Foo, global::Somewheres.Foo> _pbn_Method0 = new global::Grpc.Core.Method<global::Somewheres.Foo, global::Somewheres.Foo>(global::Grpc.Core.MethodType.Unary, _pbn_ServiceName, "Do", null!, null!);

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
        // public ServiceProxy2() : base() {}
        // public ServiceProxy2(global::Grpc.Core.ChannelBase channel) : base(channel) {}
        public ServiceProxy2(global::Grpc.Core.CallInvoker callInvoker) : base(callInvoker) {}
        private ServiceProxy2(global::Grpc.Core.ClientBase.ClientBaseConfiguration configuration) : base(configuration) {}
        protected override ServiceProxy2 NewInstance(global::Grpc.Core.ClientBase.ClientBaseConfiguration configuration) => new ServiceProxy2(configuration);

        private const string _pbn_ServiceName = "ISomeBasicService";

        private static readonly global::Grpc.Core.Method<global::Somewheres.Foo, global::Somewheres.Foo> _pbn_Method0 = new global::Grpc.Core.Method<global::Somewheres.Foo, global::Somewheres.Foo>(global::Grpc.Core.MethodType.Unary, _pbn_ServiceName, "Do", null!, null!);

    }

}
