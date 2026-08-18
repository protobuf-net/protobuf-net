# protobuf-net.Protogen

`protogen` is a cross-platform .NET global tool that generates C# or VB from `.proto` schemas, for use with
[protobuf-net](https://www.nuget.org/packages/protobuf-net).

```txt
dotnet tool install --global protobuf-net.Protogen
protogen --csharp_out=. -I=schemas schemas/person.proto
```

The calling convention deliberately mirrors `protoc`, so existing scripts mostly transfer as-is: `-IPATH` /
`--proto_path=PATH` for imports, `--descriptor_set_out=FILE` to write a `FileDescriptorSet` instead of code,
`--version`, `--help`, and `*.proto` / `**/*.proto` inputs. proto2, proto3 and the `google/protobuf/*.proto`
imports are all supported, and the common imports are embedded, so they resolve without being on disk.

protobuf-net-specific generator options are `+name=value` pairs, for example:

```txt
protogen --csharp_out=. +names=original +oneof=enum +services=grpc person.proto
```

Run `protogen --help` for the full list.

Same generator, other packagings: build-time generation from `<AdditionalFiles>` (no tool to install — see
[working with `.proto` files](https://docs.protobuf-net.dev/contract_first)), and in-browser at
[protobuf-net.dev](https://protobuf-net.dev/), where nothing leaves your machine.

## More

- [Generating code from `.proto` files](https://docs.protobuf-net.dev/contract_first)
- [Documentation](https://docs.protobuf-net.dev/)
- [Release notes](https://github.com/protobuf-net/protobuf-net/releases)
