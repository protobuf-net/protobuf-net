# Release Notes

Packages are available on NuGet: [protobuf-net](https://www.nuget.org/packages/protobuf-net)

If you prefer to build from source:

    git clone https://github.com/mgravell/protobuf-net.git
    cd protobuf-net\src\protobuf-net
    dotnet restore
    dotnet build -c Release

(it will tell you where the dlls and package have been written)

Alternatively, use Visual Studio 2017 ([community edition is free](https://www.visualstudio.com/downloads/)) to build `src\protobuf-net.sln`

If you feel like supporting my efforts, I won't stop you:

<a href='https://pledgie.com/campaigns/33946'><img alt='Click here to lend your support to: protobuf-net; fast binary serialization for .NET and make a donation at pledgie.com !' src='https://pledgie.com/campaigns/33946.png?skin_name=chrome' border='0' ></a>

If you can't, that's fine too.

## v3.* (future plans)

see: [protobuf-net: large data, and the future](http://blog.marcgravell.com/2017/05/protobuf-net-large-data-and-future.html)

## v2.3 (work in progress)

- proto2/proto3 DSL processing tools to make a resurgance

preview: [https://protogen.marcgravell.com/](https://protogen.marcgravell.com/)

## v2.2.2 (not yet released)

- fix: `Type` members should work with `GetProto<T>` (as `string`)

## v2.2.1

- critical bug fix [#256](https://github.com/mgravell/protobuf-net/issues/256) - length-based readers are failing; if you are using 2.2.0, please update as soon as possible (this bug was introduced in 2.2.0)
- fix #241 - check all callback parameters (signature validation)
- removed `[Obsolete]` markers left in place during 64-bit updates
- release string interner earlier (keeps a possibly-large array reachable)
- various documentation fixes (#184, #189, #216)

## v2.2.0

- enable 64-bit processing (2GiB+ file sizes) *within constraints* that no single sub-graph can exceed 2GiB; this is assisted by...
- new `IsGroup` property on `[ProtoContract(...)]` that indicates that a type should always be treated as a group (rather than having to specify "group" per-member); groups do not require length-prefix or buffering, so are trivially usable in huge files
- support get-only automatically-implemented properties (#188)
- support `ValueTuple<...>`
- fix bug with cyclic types resolving as lists (#167)
- optimized encoding of packed fixed-length primitives (in particular, arrays)

(see also: [protobuf-net: large data, and the future](http://blog.marcgravell.com/2017/05/protobuf-net-large-data-and-future.html))

## v2.1.0

- add support for custom static methods equivalent to static conversion operators, via `[ProtoConverter]`
- `GetSchema`: do not emit default values for non-optional members (#75)
- .NET Standard support
- protogen: allow use of native `protoc`; additional proto-path support (#119)
- protogen: fix name for getters and default value (#2)
- fix timeout issue on portable frameworks (#114)
- `DateTime` serialization can include `DateTimeKind`
- fix `Uri` serialization on PCLs (#98)
- documentation typos and tweaks (#99, #112)
- tupe serializer: fix issues with case sensitivity / i18n (#104)
- fix bug with returning empty byte arrays (#111)
- additional convenience `Deserilize` overload (#12)
- support serialization-context-aware callback methods

## v2.0.0.668

(baseline)