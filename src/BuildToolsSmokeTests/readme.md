THIS PROJECT DOES NOT WORK WELL IN AN IDE

Usage:

1. build protobuf-net.BuildTools **in release mode**; this will generate a new .nupkg in /bin/release
2. edit BuildToolsSmokeTests.cspoj with the version (full name) from this new nupkg
3. at the console, `dotnet restore` and `dotnet test`

If you *make changes* to the analyzer, repeating step 1 **is not enough**, as it will have been cached;
you could *try* using `dotnet restore --no-cache`, but to be sure, clear the nuget cache; you can do this
manually by nuking (on Windows) everything in `%userprofile%\.nuget\packages\protobuf-net.buildtools\`