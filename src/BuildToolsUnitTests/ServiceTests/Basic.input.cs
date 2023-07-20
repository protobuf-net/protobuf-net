using System;
using System.ServiceModel;
using ProtoBuf.Grpc.Configuration;

partial interface INonGrpc
{
    Foo Do(Foo value);
}

namespace Somewheres
{
    [ServiceContract]
    partial interface IWcfGrpc
    {
        Foo Do(Foo value);

        // trouble, deliberately
        event EventHandler SomeEvent;
        string Name { get; }
        public abstract static IWcfGrpc operator +(IWcfGrpc a, IWcfGrpc b);
        void Bar() => throw new InvalidOperationException();
    }

    partial class Foo
    {
        partial struct Bar
        {
            [Service]
            partial interface IPBGrpc
            {
                Foo Do(Foo value);
            }
        }
    }

    namespace Deeper
    {
        partial interface ISomeBasicService : IGrpcService
        {
            Foo Do(Foo value);
        }
    }

}

class Foo { }