# protobuf-net.NodaTime

[NodaTime](https://nodatime.org/) support for [protobuf-net](https://www.nuget.org/packages/protobuf-net):
`Instant` and `Duration` serialize as the protobuf well-known types (`google.protobuf.Timestamp` and
`google.protobuf.Duration`), so the payloads interoperate with other protobuf implementations.

```csharp
RuntimeTypeModel.Default.AddNodaTime();
```

Conversions between the two worlds are also available directly, via `NodaTimeExtensions`.

## More

- [Using protobuf-net with Noda Time](https://docs.protobuf-net.dev/nodatime)
- [Release notes](https://github.com/protobuf-net/protobuf-net/releases)
