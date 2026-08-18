# protobuf-net.AspNetCore

MVC formatters that let ASP.NET Core controllers accept and return protobuf payloads, using
[protobuf-net](https://www.nuget.org/packages/protobuf-net) contracts. `application/protobuf`,
`application/x-protobuf` and `application/vnd.google.protobuf` are all recognised.

```csharp
builder.Services.AddControllers().AddProtoBufNet();
```

Options are available on the same call — the `TypeModel` to serialize with, the buffering threshold for
reads, and a maximum write length:

```csharp
builder.Services.AddControllers().AddProtoBufNet(options =>
{
    options.Model = MyCustomModel;
});
```

This is for MVC / web API endpoints. For code-first gRPC services, see
[protobuf-net.Grpc](https://grpc.protobuf-net.dev/).

## More

- [Documentation](https://docs.protobuf-net.dev/)
- [Release notes](https://github.com/protobuf-net/protobuf-net/releases)
