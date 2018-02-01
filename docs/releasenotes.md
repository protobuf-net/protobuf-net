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

## v3.* (not yet started)

- see: [protobuf-net: large data, and the future](http://blog.marcgravell.com/2017/05/protobuf-net-large-data-and-future.html)
- gRPC?

## v2.4.0 (not yet started)

- build-time tooling
- `dynamic` API over types known only via descriptors loaded at runtime
- `Any` support

## v2.3.6

- add .NET Standard 2.0 build target 

## v2.3.5

- add codegen support for C# 3.0; C# 6.0 is still the default, but can be overridden via CLI or .proto options; see [#343](https://github.com/mgravell/protobuf-net/issues/343)
- updated Google "protoc" tooling on the web-site
- better exception messages when inheritance problems are detected; [#186](https://github.com/mgravell/protobuf-net/pull/186) via TrexinanF14
- add switch to allow the string cache code to be disabled; [#333](https://github.com/mgravell/protobuf-net/pull/333) via solyutor

## v2.3.4

- fix [#341](https://github.com/mgravell/protobuf-net/issues/341) - dictionaries with nullable types

## v2.3.3

- fix protogen bug with `[DefaultValue]` for enums not including the fully qualified name when required
- fix pathological memory usage bug with large buffers (int-overflow); many thanks to [Mikhail Brinchuk](https://github.com/Thecentury)

## v2.3.2

- fix bug with `IgnoreListHandling` not being respected for custom dictionary-like types (with "map" taking precedence)

## v2.3.1

- fix bug with `optional` being emitted for sub-types in proto3 schemas (#280)
- add setter to `ValueMember.Name` - in particular allows runtime enum name configuration (#281)
- fix bug with implicit map when `TKey` is an enum type (#289)
- fix build config (optimized build)

## v2.3.0

- include better information when rejecting jagged arrays / nested lists ([SO 45062514](https://stackoverflow.com/q/45062514/23354))

## v2.3.0-gamma

- fix issue with "map" detection of complex dictionaries-of-arrays incorrectly trying to configure a `MetaType` for the array type

## v2.3.0-beta

- fix issue with unwanted static constructors being detected (#276)
- explicitly prevent `MetaType` instances for arrays

## v2.3.0-alpha

- [further reading](http://blog.marcgravell.com/2017/06/protobuf-net-gets-proto3-support.html)
- proto2/proto3 DSL processing tools to make a resurgance; [preview is available here](https://protogen.marcgravell.com/)
- proto3 schema generation
- full support for `map<,>`, `Timestamp`, `Duration`
- dictionaries are now "maps" by default - duplicated keys *replace* values rather than causing exceptions
- support for one-of
- enums are now "pass thru" whenever possible - unknown values will not normally cause exceptions (this indirectly fixes #260, but proto3 semantics was the motivation)
- various bug-fixes
 - fix bug in schema output forn enums withut a zero value (#224)
 - fix bug in runtime handling of immutable collections (#264)
 - fix issue with serialization context being list (#268)
 - fix issue with type error message when type is generic (#267)
 - net20 / net35 targets reinstated for NuGet build (#262)
 - fix for `Uri` handling (#162 / #261)
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