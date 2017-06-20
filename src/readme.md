Source
-

The solution is intended to be built with Visual Studio 2017; the .csproj *should* work with `dotnet`, however, the support for .NET 2.0/3.5 (the `net20` / `net35` targets) is not currently great in `dotnet`.
If you wish to build with the `dotnet` command line (but without `net20` / `net35` support), then run:

    dotnet restore
    dotnet build

or

    dotnet restore
    dotnet build -c Release

You may find it easier to use Visual Studio 2017, which [can be downloaded here](https://www.visualstudio.com/downloads/). I haven't tried it, but it *should* work with the "Community" edition, which is free.
To include the `net20` / `net35` targets: switch to the `VS` configuration.

Binaries
-

If you're more of a "just give me the dll" person, it is available [on nuget as `protobuf-net`](https://www.nuget.org/packages/protobuf-net), or:

    install-package protobuf-net

in the Package Manager Console.