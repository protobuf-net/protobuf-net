using System.ServiceModel;
using ProtoBuf.Grpc.Configuration;

partial interface INonGrpc
{

}

namespace Somewheres
{
    [ServiceContract]
    partial interface IWcfGrpc
    {

    }

    partial class Foo
    {
        partial struct Bar
        {
            [Service]
            partial interface IPBGrpc
            {

            }
        }
    }

    namespace Deeper
    {
        partial interface ISomeBasicService : IGrpcService
        {

        }
    }

}