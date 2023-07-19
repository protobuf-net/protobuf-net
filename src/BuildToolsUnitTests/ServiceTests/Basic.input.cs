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