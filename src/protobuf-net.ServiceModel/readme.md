# protobuf-net.ServiceModel

WCF (`System.ServiceModel`) integration for [protobuf-net](https://www.nuget.org/packages/protobuf-net):
service operations serialize their messages with protobuf instead of `DataContractSerializer`, keeping the
WCF programming model unchanged.

Mark the operations that should use it (on both the client and the server):

```csharp
[ServiceContract]
public interface IMyService
{
    [OperationContract, ProtoBehavior]
    MyResponse Execute(MyRequest request);
}
```

`ProtoEndpointBehavior` applies the same thing across a whole endpoint. Either way, the types being
exchanged need to be protobuf-net contracts (`[ProtoContract]` / `[ProtoMember]`, or the equivalent runtime
configuration).

For new work over the network, code-first gRPC — [protobuf-net.Grpc](https://grpc.protobuf-net.dev/) — offers
a very similar contract-based model on a modern transport.

## More

- [Documentation](https://docs.protobuf-net.dev/)
- [Release notes](https://github.com/protobuf-net/protobuf-net/releases)
