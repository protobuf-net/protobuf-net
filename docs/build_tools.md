# Build Tools for protobuf-net and protobuf-net.Grpc

It can be very frustrating and confusing seeing errors at runtime in serialization and RPC because of simple usage errors in the code - for
example duplicating a field number in a data-contract, or using an incompatible RPC signature in a service-contract.

Wouldn't it be great if you could see these problems at build time? Well: that's where `protobuf-net.BuildTools` comes in!

`protobuf-net.BuildTools` is a NuGet package that contaings C# "analyzers" that warn you about problems it can see, that works with most build tools,
including Visual Studio and `dotnet build` at the command-line. It has no runtime dependencies and doesn't need to be shipped with your application.

## Turning it off

Everything `protobuf-net.BuildTools` contributes at build time — the analyzers, the `.proto`
generator, the AOT model generator — can be switched off with one property:

``` xml
<PropertyGroup>
  <ProtoBufDisableBuildTools>true</ProtoBufDisableBuildTools>
</PropertyGroup>
```

This is read *before* anything else does any work, so the cost of having the tooling present but
unwanted is a single property lookup rather than a walk of your compilation. Useful if the package
arrives transitively, or if you want it in most of a solution but not in one project.

To silence individual rules rather than all of them, prefer the usual mechanisms — `<NoWarn>` for a
specific id, or `.editorconfig`:

```
dotnet_diagnostic.PBN0022.severity = none
```

## Installation

To install `protobuf-net.BuildTools`, you would add - to your csproj:

(note: this example correct at time of writing, but make sure to use the most recent version number here!)

``` xml
<PackageReference Include="protobuf-net.BuildTools" Version="3.0.81" PrivateAssets="all"
    IncludeAssets="runtime;build;native;contentfiles;analyzers;buildtransitive" />
```

This tool uses the .NET 5 SDK; if you don't *have* the .NET 5 SDK, there is also a version available that targets the .NET 3.1 SDK:

``` xml
<PackageReference Include="protobuf-net.BuildTools.Legacy" Version="3.0.81" PrivateAssets="all"
    IncludeAssets="runtime;build;native;contentfiles;analyzers;buildtransitive" />
```

(the `protobuf-net.BuildTools.Legacy` package does not include "generator" support, [discussed here](https://docs.protobuf-net.dev/contract_first))

## Usage

To use the tool; simply write code! If you make a usage error, the tool will tell you live in the IDE, or at build time. For example, we've accidentally duplicated a field number here:

``` c#
[ProtoContract]
public class Foo
{
    [ProtoMember(1)]
    public int Id { get; set; }
    [ProtoMember(2)]
    public string Name { get; set; }
    [ProtoMember(2)]
    public double Value { get; set; }
    [ProtoMember(4)]
    public string Description { get; set; }
}
```

and the following is *not* a valid gRPC method signature for protobuf-net.Grpc:

``` c#
[Service]
public interface IMyService
{
    ValueTask<Foo> GetAsync(int x, int y);
}
```

The tool will spot these errors and many more. All diagnostics from the tool use the `PBN` prefix for their messages, and can be suppressed (if appropriate) in any of the usual ways.

## Limitations

Right now, the tool limits itself to types explicitly marked as `[ProtoContract]` or `[Service]`; this will be extended to include the additional API shapes that
protobuf-net and protobuf-net.Grpc support, in due course.

The problems spotted right now is not exhaustive, but this will grow over time - especially as we start poking at code-gen areas.

Suggestions for additional problem scenarios to detect are welcomed!