# Working with .proto files with protobuf-net and protobuf-net.BuildTools

protobuf-net originated as a code-first tool, but sometimes you're working with contract-first .proto files instead, whether because
the schemas are supplied externally, or because you want to ensure that your schemas are as suitable as they can be for cross-platform usage.

`protobuf-net.BuildTools` is the most convenient way to integrate .proto files with your build process, using the SDK from .NET 5 and the
new "generators" feature of Roslyn.

All you need to do is:

- add your .proto file (which is just a text file) to your project
- make the file available to generators:
  - in Visual Studio, change the "Build Action" of the file to "C# analyzer additional file", or (equivalently)
  - in the .csproj, declare the item as `<AdditionalFiles Include="your.proto" />`
- add the `protobuf-net.BuildTools` tool:
  - in the .csproj, add (using the most recent version) `<PackageReference Include="protobuf-net.BuildTools" Version="3.0.86" PrivateAssets="all" IncludeAssets="runtime;build;native;contentfiles;analyzers;buildtransitive" />`

Now, when you build your project - whether in the IDE, or at the command-line - the `protobuf-net.BuildTools` utility generates the C# for your types *on the fly*, and generates the required code - without you needing
to worry about how to configure it. For example:

- the tool knows what C# language version you are using, so you don't need to tell the tool
- the tool can see whether you're making use of `protobuf-net.Grpc` or `System.ServiceModel.Primitives`, and enable support for gRPC services automatically
- schema imports are automatically resolved relative to your files

So you just use the types as though they existed - they will show in the IDE with all the expected properties etc, *just as if they were C# files*. Because they now *are*.

A range of common `import` files (for example all of the `google/protobuf/*.proto` files) are included in the utility as resources, and do not need to be included in your project *unless* you want to
change the version/contents from the copy baked into the tool. Normally, standard imports should **just work**.

## Additional options

Additional configuration options can be specified at attributes against each `<AdditionalFiles>` node, to fine tune your options; this is very similar to the options available with the command-line tools:

- `ImportPaths` - specifies a comma delimited list of additional import locations; usually this isn't required, as schemas are resolved relative to each file; this is resolved relative to the current file, and
  can indicate "upwards" locations like `../..` - but the actual lookup happens *purely* within the virtual file system of `<AdditionalFiles>` nodes - it does not allow external file access
   - as an example of when this is useful, consider [`type.proto`](https://github.com/protocolbuffers/protobuf/blob/master/src/google/protobuf/type.proto), which has an import of `google/protobuf/any.proto`, which is actually
     in the **same folder**; adding `ImportPaths="../../"` means that `../../google/protobuf/any.proto` will successfully resolve back to the same location
- `LangVersion` - explicitly specifies a language version to use; this is not usually required, as the project's language version is assumed
- `ListSet` - controls whether list members should have `set` access
- `Names` - controls the name-normalizer rules to use
- `OneOf` - controls how `oneof` elements are handled (`enum` adds an enum discriminant)
- `Package` - overrides the namespace to use for the code (which is *broadly speaking* a "package" in .proto terms)
- `Services` - controls whether to generate services (this is defaulted based on your project references)
- `NullWrappers` - controls whether wrappers.proto should be generated as C# nullable types (int?)
- `CompatLevel` - controls whether well-known types should be marked with CompatibilityLevel instead of DataFormat
- `NullableValueType` - use `int?` etc for optional value types
- `RepeatedAsList` - use `List<T>` etc instead of `T[]` for *all* collections (i.e. including primitives)
- `IncludeInOutput` - controls whether the file is included in the output; this can be useful for adding a file to the virtual file system for importing without generating code as it might be generated elsewhere

## That doesn't work for you?

protobuf-net also provides:

- [protobuf-net.Protogen](https://www.nuget.org/packages/protobuf-net.Protogen/), a .NET Global Tool for command-line usage
- [https://protogen.marcgravell.com/](https://protogen.marcgravell.com/) - an online version of the same
- [protobuf-net.MSBuild](https://www.nuget.org/packages/protobuf-net.MSBuild/) - a ".targets" based way of integrated protobuf-net's tools into the build
- `protogen` - a standalone executable version of the same tools

All of these tools can be awkward, brittle, and non-obvious.
