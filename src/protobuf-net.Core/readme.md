# protobuf-net.Core

The protobuf-net serialization engine: readers, writers, and the serializer contracts, without the
reflection/ref-emit type model. It is also where the build-time tooling lives — the contract analyzers and
the compile-time serializer generator that make native AOT and trimming work.

**Most people should reference [protobuf-net](https://www.nuget.org/packages/protobuf-net) instead**, which
brings this in and adds `Serializer` and `RuntimeTypeModel`. Reference this package directly only when the
serializers are generated at compile time and you never need the runtime model.

Opt out of the build tooling with `<ProtoBufDisableBuildTools>true</ProtoBufDisableBuildTools>`.

## More

- [Documentation](https://docs.protobuf-net.dev/)
- [Native AOT and trimming](https://docs.protobuf-net.dev/aot)
- [Build tools](https://docs.protobuf-net.dev/build_tools)
- [Release notes](https://github.com/protobuf-net/protobuf-net/releases)
