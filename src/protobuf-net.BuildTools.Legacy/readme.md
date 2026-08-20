# protobuf-net.BuildTools.Legacy

The protobuf-net build-time tooling — contract analyzers, and code generation from `.proto` files listed as
`<AdditionalFiles>` — built against Roslyn/build SDK 3, for projects on toolchains too old to load the
current analyzers.

**If your build is current, you do not need this package**: the same tooling ships inside
[protobuf-net](https://www.nuget.org/packages/protobuf-net) and reaches every consumer by default. This one
exists only for the older SDKs, and does not include the compile-time serializer generator
([native AOT and trimming](https://docs.protobuf-net.dev/aot) support), which requires a newer toolchain.

## More

- [Build tools](https://docs.protobuf-net.dev/build_tools)
- [Working with `.proto` files](https://docs.protobuf-net.dev/contract_first)
- [Release notes](https://github.com/protobuf-net/protobuf-net/releases)
