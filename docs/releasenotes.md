# Release Notes

Packages are available on NuGet: [protobuf-net](https://www.nuget.org/packages/protobuf-net)

protobuf-net needs to be built with MSBuild, due to some of the target platforms.

The easiest way to do this is via Visual Studio 2017 ([community edition is free](https://www.visualstudio.com/downloads/)) - build `src\protobuf-net.sln`

## (not yet started)

- gRPC?
- build-time tooling from code-first
- `dynamic` API over types known only via descriptors loaded at runtime
- `Any` support


## v3.0.0-alpha.3

- **breaking change** (hence 3.0) if you are using `new ProtoReader(...)` - you must now use `ProtoReader.Create(...)`
- if using `ProtoReader` you *should* now move to the `ref State` API too, although the old API will continue to
  work with `Stream`-based readers; it **will not** work with `ReadOnlySequence<byte>` readers
- "pipelines" (`ReadOnlySequence<byte>`) support for the **read** API (not write yet)
- significant performance improvements in all read scenarios
- new `CreateForAssembly(...)` API (various overloads) for working with precompiled (at runtime) type models (faster than `RuntimeTypeModel`, but less flexible)
- significant amounts of code tidying; many yaks were shawn

## v2.4.0

- fix #442 - switched to 2.4.0 due to new versioning implementation breaking the assembly version; oops

## v2.3.17

- (#430/#431) - ensure build output from `protobuf-net.MSBuild` makes it into build output; add error codes
- #429 - use `$IntermediateOutputPath` correctly from build tools

## v2.3.16

- new MSBuild .proto tools added (huge thanks go to Mark Pflug here)
- fix error where extension GetValues might only report the last item
- switch to git-based versioning implementation; versioning now unified over all tools
- extensions codegen (C#): add `Get*` and `Add*` implementations for `repeated`; add `Set*` implementations for regular
- update `protoc` to 3.6.1
- give advance warning of possible removal of ProtoReader/ProtoWriter constructors
- codegen (C#): implement "listset" option to control whether lists/maps get `set` accessors
- `GetProto<T>` now emits `oneof`-style .proto syntax for inheritance

## protobuf-net v2.3.15

- merge #412/fix #408 - `ReadObject`/`WriteObject` failed on value types
- merge #421 - support `IReadOnlyCollection` members
- merge #424 - make WCF configuration features available on TFMs that support them
- merge #396 - remove unnecessary #if defs

## protogen v1.0.10

- fix error in generated C# when using enums in discriminated unions (#423)

## protobuf-net v2.3.14

- add UAP TFM

## protogen v1.0.9

- fix #406 - relative and wildcard paths (`*.proto` etc) failed on `netcoreapp2.1`, impacting the "global tool"

## protobuf-net v2.3.13

- **IMPORTANT** fix #403 - key cache was incorrect in some cases involving multi-level inheritance; update from 2.3.8 or above is highly recommended

## protobuf-net v2.3.12

- fix #402 - zero `decimal` with non-trivial sign/scale should round-trip correctly
- fix additional scenarios for #401

## protobuf-net v2.3.11

- fix #401 - error introduced in the new key cache from v2.3.8

## protobuf-net v2.3.10

- fix #388 - stability when `DynamicMethod` is not available (UWP, iOS, etc)

## protogen v1.0.8

- move default .proto imports (from v1.0.7) to embedded resources that work for all consumers

## protogen v1.0.7

- ship default google and protobuf-net imports with the "global tool" install

## protobuf-net v2.3.9

- fix behaviour of `DiscriminatedUnion*` for `None` enum case

## protogen v1.0.6

- add #393 - optional ability to emit enums for `oneof` [similar to Google's C# generator](https://developers.google.com/protocol-buffers/docs/reference/csharp-generated#oneof)
- extend C# support down to 2.0 and up to 7.1, and VB support down to VB 9
- add website support for additional options (as above)

## protobuf-net v2.3.8

- speculative fix for iOS issues (#381)
- add discriminator accessor to discriminated union types, for protogen v1.0.6
- improve performance of ProtoWriter.DemandSpace (#378 from szehetner)
- protogen - better support for wildcard paths (#390 from RansomVO)
- fix #313 immutable arrays (#346 from BryantL)
- improve LOH behaviour (#307 from mintsoft)
- allow model precompilation for unknown types (#326 from daef)
- improve type-key lookup performance (#310 from alex-sherman)

## protogen v1.0.5

- allow default package name using #FILE# and #DIR# tokens
- more fixes for VB.NET idioms

## protogen v1.0.4

- fixes for VB.NET code-gen (especially: overflow in default values)
- add wildcard+recursive generation modes for all languages
- fix resolution of rooted types in imports without a package

## protogen v1.0.3

- VB.NET code-gen support added (from: alpha2)
- packaging updates for "global tools" (from: alpha1)

## protogen v1.0.2

- packaging updates (no code changes)

## protogen v1.0.1

- unknown fields (`IExtensible`) now preserved by default, in line with Google's v3.5.0 release

## protobuf-net v2.3.7

- add .NET Standard 1.0 "profile 259" support - contributed by Lorick Russow

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

- [further reading](https://blog.marcgravell.com/2017/06/protobuf-net-gets-proto3-support.html)
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

(see also: [protobuf-net: large data, and the future](https://blog.marcgravell.com/2017/05/protobuf-net-large-data-and-future.html))

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