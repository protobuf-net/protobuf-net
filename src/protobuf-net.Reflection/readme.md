# protobuf-net.Reflection

The `.proto` schema tooling behind protobuf-net, as a library: parse proto2/proto3 schemas into a
`FileDescriptorSet`, inspect or manipulate the descriptors, and generate C# or VB source from them.

```csharp
var set = new FileDescriptorSet();
set.AddImportPath(".");
set.Add("person.proto", includeInOutput: true);
set.Process();

foreach (var file in CSharpCodeGenerator.Default.Generate(set))
{
    Console.WriteLine(file.Name);
    Console.WriteLine(file.Text);
}
```

The common imports (`google/protobuf/*.proto` and friends) are embedded, so they resolve without being on
disk. This is the same code generation used by the
[protogen](https://www.nuget.org/packages/protobuf-net.Protogen) command-line tool and by
[protobuf-net.dev](https://protobuf-net.dev/); if you just want generated types, prefer one of those, or the
build-time generation described below.

## More

- [Generating code from `.proto` files](https://docs.protobuf-net.dev/contract_first)
- [Schema analysis tools](https://docs.protobuf-net.dev/schemas)
- [Release notes](https://github.com/protobuf-net/protobuf-net/releases)
