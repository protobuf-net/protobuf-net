namespace Somewheres
{
    [global::ProtoBuf.Grpc.Configuration.ProxyAttribute(typeof(GeneratedServiceProxy0))]
    partial interface IWcfGrpc
    {
        private sealed class GeneratedServiceProxy0 : global::Grpc.Core.ClientBase<GeneratedServiceProxy0>, global::Somewheres.IWcfGrpc
        {
            // public GeneratedServiceProxy0() : base() {}
            // public GeneratedServiceProxy0(global::Grpc.Core.ChannelBase channel) : base(channel) {}
            public GeneratedServiceProxy0(global::Grpc.Core.CallInvoker callInvoker) : base(callInvoker) {}
            private GeneratedServiceProxy0(global::Grpc.Core.ClientBase.ClientBaseConfiguration configuration) : base(configuration) {}
            protected override GeneratedServiceProxy0 NewInstance(global::Grpc.Core.ClientBase.ClientBaseConfiguration configuration) => new GeneratedServiceProxy0(configuration);


            private const string _pbn_Service0 = "IWcfGrpc";
            private static readonly global::Grpc.Core.Method<global::Somewheres.Foo, global::Somewheres.Foo> _pbn_Method0 = new global::Grpc.Core.Method<global::Somewheres.Foo, global::Somewheres.Foo>(global::Grpc.Core.MethodType.Unary, _pbn_Service0, "Do", null!, null!);

            // implement global::Somewheres.IWcfGrpc
            global::Somewheres.Foo global::Somewheres.IWcfGrpc.Do(global::Somewheres.Foo value)
            {
                throw new global::System.NotImplementedException("Do"); // via _pbn_Method0
            }

        }

    }


}
namespace Somewheres
{
    partial class Foo
    {
        partial struct Bar
        {
            [global::ProtoBuf.Grpc.Configuration.ProxyAttribute(typeof(GeneratedServiceProxy1))]
            partial interface IPBGrpc
            {
                private sealed class GeneratedServiceProxy1 : global::Grpc.Core.ClientBase<GeneratedServiceProxy1>, global::Somewheres.Foo.Bar.IPBGrpc
                {
                    // public GeneratedServiceProxy1() : base() {}
                    // public GeneratedServiceProxy1(global::Grpc.Core.ChannelBase channel) : base(channel) {}
                    public GeneratedServiceProxy1(global::Grpc.Core.CallInvoker callInvoker) : base(callInvoker) {}
                    private GeneratedServiceProxy1(global::Grpc.Core.ClientBase.ClientBaseConfiguration configuration) : base(configuration) {}
                    protected override GeneratedServiceProxy1 NewInstance(global::Grpc.Core.ClientBase.ClientBaseConfiguration configuration) => new GeneratedServiceProxy1(configuration);


                    private const string _pbn_Service0 = "IPBGrpc";
                    private static readonly global::Grpc.Core.Method<global::Somewheres.Foo, global::Somewheres.Foo> _pbn_Method0 = new global::Grpc.Core.Method<global::Somewheres.Foo, global::Somewheres.Foo>(global::Grpc.Core.MethodType.Unary, _pbn_Service0, "Do", null!, null!);

                    // implement global::Somewheres.Foo.Bar.IPBGrpc
                    global::Somewheres.Foo global::Somewheres.Foo.Bar.IPBGrpc.Do(global::Somewheres.Foo value)
                    {
                        throw new global::System.NotImplementedException("Do"); // via _pbn_Method0
                    }

                }

            }


        }

    }

}
namespace Somewheres.Deeper
{
    [global::ProtoBuf.Grpc.Configuration.ProxyAttribute(typeof(GeneratedServiceProxy2))]
    partial interface ISomeBasicService
    {
        private sealed class GeneratedServiceProxy2 : global::Grpc.Core.ClientBase<GeneratedServiceProxy2>, global::Somewheres.Deeper.ISomeBasicService
        {
            // public GeneratedServiceProxy2() : base() {}
            // public GeneratedServiceProxy2(global::Grpc.Core.ChannelBase channel) : base(channel) {}
            public GeneratedServiceProxy2(global::Grpc.Core.CallInvoker callInvoker) : base(callInvoker) {}
            private GeneratedServiceProxy2(global::Grpc.Core.ClientBase.ClientBaseConfiguration configuration) : base(configuration) {}
            protected override GeneratedServiceProxy2 NewInstance(global::Grpc.Core.ClientBase.ClientBaseConfiguration configuration) => new GeneratedServiceProxy2(configuration);


            private const string _pbn_Service0 = "ISomeBasicService";
            private static readonly global::Grpc.Core.Method<global::Somewheres.Foo, global::Somewheres.Foo> _pbn_Method0 = new global::Grpc.Core.Method<global::Somewheres.Foo, global::Somewheres.Foo>(global::Grpc.Core.MethodType.Unary, _pbn_Service0, "Do", null!, null!);

            // implement global::Somewheres.Deeper.ISomeBasicService
            global::Somewheres.Foo global::Somewheres.Deeper.ISomeBasicService.Do(global::Somewheres.Foo value)
            {
                throw new global::System.NotImplementedException("Do"); // via _pbn_Method0
            }

            // implement global::ProtoBuf.Grpc.Configuration.IGrpcService

        }

    }


}
