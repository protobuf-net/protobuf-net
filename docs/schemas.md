# Schema analysis tools

In vanilla protobuf/gRPC usage, `protoc` is the tool used to parse .proto schemas for code-generation; protobuf-net provides *managed* tools that provide additional schema analysis tools,
via the [protobuf-net.Reflection](https://www.nuget.org/packages/protobuf-net.Reflection) package.

For example, let's consider the `TimeService.proto` from [the protobuf-net.Grpc examples](https://github.com/protobuf-net/protobuf-net.Grpc/tree/main/examples/grpc/Shared). At the time
of writing, this file contains:

``` proto
syntax = "proto3";
package MegaCorp;
import "google/protobuf/empty.proto";
import "google/protobuf/timestamp.proto";
option csharp_namespace = "MegaCorp";

message TimeResult {
	.google.protobuf.Timestamp Time = 1;
}

service TimeService {
	rpc Subscribe(.google.protobuf.Empty) returns (stream TimeResult);
}
```

This generates code including:

``` c#
static readonly string __ServiceName = "MegaCorp.TimeService";
...
static readonly grpc::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::MegaCorp.TimeResult> __Method_Subscribe = new grpc::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::MegaCorp.TimeResult>(
        grpc::MethodType.ServerStreaming,
        __ServiceName,
        "Subscribe",
        __Marshaller_google_protobuf_Empty,
        __Marshaller_MegaCorp_TimeResult);
```

This means that the `TimeService.Subscribe` method corresponds to the HTTP route: `/MegaCorp.TimeService/Subscribe`, but we've had to look at the generated code. So: can we do this from
the schema directly? We can with protobuf-net.Reflection!

## Parsing a schema

The key type here is `FileDescriptorSet`, which represents a composite parse operation. In particular, note that we aren't parsing a *single* file - the use of `import` means that multiple
files can be involved. In this case, these are all "standard" Google schemas, but this could also be other user schemas. With a `FileDescriptorSet`, we can add multiple files (although it is
very common to only add one file manually); this can be loaded from the file-system, or the file contents can be provided via a `TextReader`. In advanced scenarios (for
increased isolation, usually), a custom virtual file-system can be provided via the `.FileSystem` property. To load files, one or more folder paths (physical or virtual) must be
provided; then when adding individual files, these folders are checked in order. For example:

``` c#
FileDescriptorSet schemaSet = new();
schemaSet.AddImportPath(@"C:\Work\protobuf-net.Grpc\examples\grpc\Shared");
schemaSet.Add("TimeService.proto");
schemaSet.Process();
var errors = schemaSet.GetErrors();
foreach (var error in errors)
{
    Console.WriteLine($"{(error.IsWarning ? "warning" : "error")} {error.ErrorNumber}: {error.File}#{error.LineNumber}: {error.Message}");
}
```

This uses the protobuf-net.Grpc source folder, and adds the `TimeService.proto` file. When we call `.Process()`, all files added are parsed, which can mean loading *additional* files.
In this case, we will get the additional files from `google/protobuf/empty.proto` and `google/protobuf/timestamp.proto`. You might observe that those imported files don't actually exist on
the file system; protobuf-net.Reflection has many *standard* imports embedded directly: if no file is located in the available file systems, these resources are checked too.

Once we have parsed the schema (assuming there are no errors), each file has a `.Services` collection, each service has a `.Methods` collection, which we can iterate:

``` c#
foreach (var file in schemaSet.Files)
{
    Console.WriteLine($"{file.Name}: {file.Services.Count} services");
    // only inspect services for files that were added explicitly
    // (rather than implicitly via imports)
    if (file.IncludeInOutput)
    {
        Console.WriteLine($"package: '{file.Package}'");
        foreach (var service in file.Services)
        {
            Console.WriteLine($"service: '{service.Name}'; {service.Methods.Count} methods");
            foreach (var method in service.Methods)
            {
                Console.WriteLine($"> method: {method.Name}; CS: {method.ClientStreaming}, SS: {method.ServerStreaming}");
                Console.WriteLine($"  ({GetMethodType(method.ClientStreaming, method.ServerStreaming)}; {method.InputType}; {method.OutputType})");
            }
        }
    }

    static MethodType GetMethodType(bool clientStreaming, bool serverStreaming)
        => clientStreaming
            ? serverStreaming ? MethodType.DuplexStreaming : MethodType.ClientStreaming
            : serverStreaming ? MethodType.ServerStreaming : MethodType.Unary;
}
```

This gives us the output:

``` txt
TimeService.proto: 1 services
package: 'MegaCorp'
service: 'TimeService'; 1 methods
> method: Subscribe; CS: False, SS: True
  (ServerStreaming; .google.protobuf.Empty; .MegaCorp.TimeResult)
google/protobuf/empty.proto: 0 services
google/protobuf/timestamp.proto: 0 services
```

We can then *roughly* construct the final URL via:

``` c#
static string GetUri(string package, string service, string method)
{
    if (string.IsNullOrWhiteSpace(package))
    {
        return "/" + service.TrimStart('.') + "/" + method;
    }
    return "/" + package + "." + service.TrimStart('.') + "/" + method;
}
```

(noting that the package name is optional)

The same inspections apply to all the message/enum types in the schema - everything is available.