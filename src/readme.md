Source
-

The solution is intended to be built with Visual Studio 2017; the .csproj *should* work with `dotnet`, however, the support for .NET 2.0/3.5 (the `net20` / `net35` targets) is not currently great in `dotnet`.
If you only have access to the `dotnet` command line, just remove the `net20`/`net35` targets from the .csproj, then run:

    dotnet build

or

    dotnet build -c Release

However, it is much easier if you just use Visual Studio 2017, which [can be downloaded here](https://www.visualstudio.com/downloads/). I haven't tried it, but it *should* work with the "Community" edition, which is free.

Binaries
-

If you're more of a "just give me the dll" person, it is available [on nuget as `protobuf-net`](https://www.nuget.org/packages/protobuf-net), or:

    install-package protobuf-net

in the Package Manager Console.