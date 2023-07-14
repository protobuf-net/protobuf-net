using System.ServiceModel;
using ProtoBuf.Grpc.Configuration;

interface INonGrpc
{

}

[ServiceContract]
interface IWcfGrpc
{

}

[Service]
interface IPBGrpc
{

}
interface ISomeBasicService : IGrpcService
{

}
