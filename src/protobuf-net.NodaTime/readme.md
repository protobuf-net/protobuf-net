# protobuf-net.NodaTime

[NodaTime](https://nodatime.org/) support for [protobuf-net](https://www.nuget.org/packages/protobuf-net):
`Instant` and `Duration` serialize as the protobuf well-known types (`google.protobuf.Timestamp` and
`google.protobuf.Duration`), so the payloads interoperate with other protobuf implementations.

With a compile-time model (`[ProtoModel]`, as used for native AOT), those two need **no registration at
all**: the pairings are declared at assembly level in this package, so any generated model in a project that
references it picks them up.

For the runtime model, register them:

```csharp
RuntimeTypeModel.Default.AddNodaTime();
```

`AddNodaTime` also covers `LocalDate`, `LocalTime` and `IsoDayOfWeek`, which map to the `google.type.*`
schemas; those are not part of the automatic wiring, so a compile-time model that uses them still needs the
runtime model.

Conversions between the two worlds are also available directly, via `NodaTimeExtensions`.

## More

- [Using protobuf-net with Noda Time](https://docs.protobuf-net.dev/nodatime)
- [Release notes](https://github.com/protobuf-net/protobuf-net/releases)
