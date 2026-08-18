# protobuf-net

A contract-based serializer for .NET that reads and writes Google's "protocol buffers" wire format.
The API follows normal .NET patterns — broadly comparable to `XmlSerializer` or `DataContractSerializer` —
so you annotate the types you already have:

```csharp
[ProtoContract]
public class Person
{
    [ProtoMember(1)] public int Id { get; set; }
    [ProtoMember(2)] public string Name { get; set; }
}

using var file = File.Create("person.bin");
Serializer.Serialize(file, new Person { Id = 12345, Name = "Fred" });
```

Members are identified on the wire by number, not by name, so the numbers matter: pick them once and keep
them stable. Everything the attributes do can also be configured at runtime, via `RuntimeTypeModel`.

Contract-first works too: generate C#/VB from a `.proto` schema at build time, from the command line with
[protobuf-net.Protogen](https://www.nuget.org/packages/protobuf-net.Protogen), or in your browser at
[protobuf-net.dev](https://protobuf-net.dev/).

Build tools — analyzers that check your contracts, and the generator that emits serializers at compile time
so native AOT and trimming work — ship with this package by default; opt out with
`<ProtoBufDisableBuildTools>true</ProtoBufDisableBuildTools>`.

## More

- [Documentation](https://docs.protobuf-net.dev/)
- [Native AOT and trimming](https://docs.protobuf-net.dev/aot)
- [Working with `.proto` files](https://docs.protobuf-net.dev/contract_first)
- [Release notes](https://github.com/protobuf-net/protobuf-net/releases)
