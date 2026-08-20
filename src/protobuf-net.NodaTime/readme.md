# protobuf-net.NodaTime

[NodaTime](https://nodatime.org/) support for [protobuf-net](https://www.nuget.org/packages/protobuf-net):
`Instant`, `Duration`, `LocalDate`, `LocalTime` and `IsoDayOfWeek` serialize as the matching protobuf
well-known and `google.type` messages, so the payloads interoperate with other protobuf implementations.

With a compile-time model (`[ProtoModel]`, as used for
[native AOT](https://docs.protobuf-net.dev/aot)), there is **nothing to register**: this package declares the
pairings at assembly level, so any generated model in a project that references it picks them up.

For the runtime model, register them once:

```csharp
RuntimeTypeModel.Default.AddNodaTime();
```

Conversions between the two worlds are also available directly, via `NodaTimeExtensions`.

## More

- [Using protobuf-net with Noda Time](https://docs.protobuf-net.dev/nodatime)
- [Release notes](https://github.com/protobuf-net/protobuf-net/releases)
