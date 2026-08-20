# protogen

The `protogen` code generator — which turns `.proto` schemas into C# or VB for
[protobuf-net](https://www.nuget.org/packages/protobuf-net) — shipped as a plain package containing the
executable, for build scripts that want to consume it that way.

**For command-line use, prefer [protobuf-net.Protogen](https://www.nuget.org/packages/protobuf-net.Protogen)**,
which is the same generator as a .NET global tool:

```txt
dotnet tool install --global protobuf-net.Protogen
protogen --csharp_out=. -I=schemas schemas/person.proto
```

Either way the calling convention mirrors `protoc`; run `protogen --help` for the options.

## More

- [Generating code from `.proto` files](https://docs.protobuf-net.dev/contract_first)
- [Documentation](https://docs.protobuf-net.dev/)
- [Release notes](https://github.com/protobuf-net/protobuf-net/releases)
