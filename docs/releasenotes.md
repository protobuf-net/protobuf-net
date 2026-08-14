# Release Notes

Packages are available on NuGet: [protobuf-net](https://www.nuget.org/packages/protobuf-net), or it can be built [from source](https://github.com/protobuf-net/protobuf-net/tree/main/src)

## Roadmap

- gRPC: see [protobuf-net.Grpc](https://github.com/protobuf-net/protobuf-net.Grpc)
- 2.4.*: critical maintenance only (no feature work planned)
- 3.0: new custom serializer API (message+scalar); "pipelines" support; split core and reflection code-bases into separate libs
- 3.1: adds model depth validation, which may impact some models; see `TypeModel.MaxDepth`
- 3.3: build-time serializer generation from code-first contracts, for [native AOT and trimming](https://protobuf-net.github.io/protobuf-net/aot); build tools included by default
- 4.0: rewritten reader core; optimized read emission for compile-time serializers (write optimization follows in a later 4.x)
- future: `Any` support; custom list API support; support for `[ReadOnly]Memory<T>`, `ReadOnlySequence<T>`, `IMemoryOwner<T>`
- future: protogen support for emitting pre-coded custom serializers

## unreleased (4.0-alpha)

- **the deserialization core is rewritten** — same public surface, new engine. Every consumer
  benefits without code changes (~29% faster on the descriptor parse benchmark for existing
  serializers); compile-time serializers additionally get an **optimized read emission** that
  reaches parity with hand-written readers (~2× the previous release, ~26% faster than
  Google.Protobuf on the same benchmark). This is why the major version revs: the internals
  moved substantially, even though the surface did not
- write optimization is deliberately **not** in this release; reads ship standalone, and the
  same design will follow for writes in a later 4.x
- `[ProtoModel(ClassicEmit = true)]` is the escape hatch: it reverts a model to the classic
  (non-optimized) emission in full. Only for use if you experience problems with the default
  optimized emit — and if it fixes a symptom, please report that symptom as an issue so the
  underlying difference can be fixed
- the new raw reader surface on `ProtoReader.State` is `[Experimental]` (`PBN9002`) while the
  write side may still reshape it; generated code carries its own suppression, so consumers
  only see the diagnostic if they call it by hand
- **protogen's descriptor serializer is now itself generated** by the compile-time model
  generator (previously a frozen hand-maintained export), so schema parsing rides the new
  reader and picks up every generator fix automatically
- **fix**: a string member carrying both `[DefaultValue]` and a `ShouldSerialize*()` method
  silently omitted a present-but-default value; the condition now replaces the declared-default
  guard, as it always did for other member kinds
- corrupt-input fidelity: a field-0 tag (including overlong encodings) throws `ProtoException`
  ("Invalid field in source data: 0") exactly as previous releases did
- `RepeatedSerializer.CreateReadOnySet` (a long-standing typo) is now `CreateReadOnlySet`;
  the old spelling remains as an `[Obsolete]` forwarder, so previously-generated code keeps
  working and the warning disappears on the next build (the generator ships with the library,
  so regeneration is automatic)
- **`protobuf-net.BuildTools` is no longer published** (last standalone version: 3.3.8, deprecated):
  the same tooling ships inside protobuf-net.Core and reaches every consumer by default;
  `protobuf-net.BuildTools.Legacy` (for very old SDKs) is unaffected
- the build-time tooling now ships inside **protobuf-net.Core** rather than protobuf-net, so
  compile-time serialization works with only a Core reference; it still reaches consumers who
  reference only protobuf-net (the dependency edge forwards it), and the legacy
  `protobuf-net.BuildTools` package alongside remains harmless
- **fix**: `protobuf-net.NodaTime` did not produce a package from a plain build (missing
  `GeneratePackageOnBuild`), and packed with a placeholder description
- **fix**: `PBN2010`'s example now shows `Model.Instance.Serialize` — the generated accessor that
  actually exists — rather than an imaginary camel-cased local
- **fix**: the "add an AOT model" code fix now generates the model as `internal` (a fixer should not
  add to the public surface) and inside the project's namespace (or the anchor contract's), rather
  than a `public` type in the global root
- **fix**: `PBN0022` ("should declare `IsRequired`") no longer fires for collection members —
  `List<T> Lines { get; } = [];` is the standard pattern, an empty collection has no wire presence
  to force, and `IsRequired` is only observable for value-type scalars anyway

## 3.3.0

- **compile-time serializers, for native AOT and trimming** ([docs](https://protobuf-net.github.io/protobuf-net/aot)):
  opt in with `[ProtoModel] partial class MyModel : TypeModel`, seeded by `[ProtoSerializable(typeof(...))]`;
  the generator builds the serializers at compile time, so publishing native AOT works and cold start
  improves even on an ordinary JIT build. The trigger attributes are `[Experimental]` (`PBN9001`)
  while the shape settles. `[ProtoSurrogate]` on the model or assembly is the compile-time
  `SetSurrogate`, and `[ProtoContract(IsScalar = true)]` lets a hand-written serializer state its
  category where the generator cannot see its `Features`
- **the build tools now ship inside the protobuf-net package** (analyzers and generators; previously
  the separate `protobuf-net.BuildTools` package): installed by default, declined entirely with
  `<ProtoBufDisableBuildTools>true</ProtoBufDisableBuildTools>`
- support of deserializing `ISet<T>` and `IReadOnlySet<T>` (ladeak)
- **fix**: `[ProtoPartialMember(..., OverwriteList = true)]` was silently ignored; the option was read
  from the member's own `[ProtoMember]`, which is necessarily absent when the partial-member path
  runs. It is now honoured, so such a member **replaces** rather than appends when deserializing into
  an existing collection — a behaviour change for anyone who had set it and not noticed it doing
  nothing
- **fix**: merging two *unrelated* sub-types of one base (a payload carrying the same field twice with
  conflicting sub-type markers) recursed without bound and killed the process with a
  `StackOverflowException`, which cannot be caught and was reachable from untrusted input; it now
  throws a catchable `InvalidOperationException` naming both types
- **fix**: `Extensible.AppendValue` discarded the result of the underlying write and reported success
  regardless, so a failure was silent data loss; it now throws if it cannot write
- `Extensible.AppendValue<T>`/`GetValue<T>`/`TryGetValue<T>` now keep `T` rather than boxing to
  `object` and re-resolving by reflection, so they **work under native AOT** at the default
  `DataFormat`; other formats and the legacy `object`-based overload are unchanged

## 3.2.30

- support `DateOnly` and `TimeOnly` (#1100 by @mgravell, fixes #977)
- support Roslyn [DefaultValue] analyzer (#1040 by @deaglegross)

## 3.2.26

- support `IncludeInOutput` on .proto additional files, to exclude from code-gen (#1032, #1046, #1047, #1062 by @kmosegaard and @dxdjgl)
- support `Uri` in schema-generation as `string` (#1072 by @Matti-Koopa)

## 3.2.16

- implement `[NullWrappedCollection]`, usage [as here](https://protobuf-net.github.io/protobuf-net/nullwrappers#null-collections) (#1044)
- support `nint` (`IntPtr`) and `nuint` (`UIntPtr`) with layout per `long`/`ulong` (#1043; fixes #1042, fixes grpc 282)

## 3.2.12

- fix bug with default values not including literal suffixes (#1037)
- fix SDKs used by BuildTools[.Legacy]
- expose `IncludeInOutput` for files generated by BuildTools (#1030)

## 3.2.8

- add support for deserializing into uninitialized `IReadOnlyDictionary<TKey, TValue>` (#1022 by ladeak)
- fix missing namespace in `[CompatibilityLevel]` in code-gen (#1026)

## 3.2.0

- implement `[NullWrappedValue]` (compile-time annotation for `ValueMember.SupportNull`, see 3.1.26)
- add `[NullWrappedValue]` support in schema tools in both directions (#1001 by DeagleGross)
- allow nullable primitives for optional values (#856 by dxdjgl, #855, #1016)
- update dependencies
- drop netcoreapp3.1 (still supported indirectly via netstandard2.1)
- (build) switch to central package management

## 3.1.33

- fix issue with comment-parsing in .proto schemas (#1010 / #1011)

## 3.1.31

- support `Memory<byte>`, `ReadOnlyMemory<byte>` and `ArraySegment<byte>` as `bytes` payloads
- add reader/writer API for `Span<byte>`/`ReadOnlySpan<byte>`
- add support for full assembly metadata (bring forward #998 by mihaicodrean)
- update `protoc` and Google reference proto files

## 3.1.26

- reinstate `ValueMember.SupportNull` (from v2) for handling `null` values in lists

## 3.1.25

- fix issue with non-supported features in tuple-types (#964)
- add `MessageType` and `EnumType` on `FieldDescriptorProto` (#971)
- expose fully qualified name for `DescriptorProto` and `EnumDescriptorProto` (#974)

## 3.1.22

- fix schema parsing bug with semicolon-delimited options (#833)
- fix schema parsing bug with escape characters in custom options (#931 / #933)
- fix ProtoSyntax type-forwarding (#953)
- update TFMs (#946)

## 3.1.17

- add .NET 6 TFM for protogen  (#928)
- fix protobuf-net.BuildTools usage with duplicate filenames  (#925)
- fix protobuf-net.BuildTools issue with gRPC/WCF detection not working correctly

## 3.1.4

- allow `OverwriteList` to work with properties declared as `IEnumerable<T>` (as a special-case) even if the existing value is a non-null, non-clearable collection

## 3.1.0

- enforce maximum model depth (`TypeModel.MaxDepth`) during serialize and deserialize

## 3.0.131

- support unknwon/extension fields on models that involve inheritance (via either `Extensible` or `ITypedExtensible`)
- detect Google.Protobuf types and provide guidance (#722)
- don't throw if `EnumPassthru` is explicitly set to `true` (#881) from code compiled against v2
- fix #479 (also backported as 2.4.7)

## 3.0.62

- add .NET 5 TFM and support for related features such as record-types
- split `protobuf-net.ServiceModel` into a separate package to reduce the dependency tree for most users
- fix .proto schema generation when an enum name is overridden
- attempt to declare dynamically-accessed members for linker compatibility
- fix init-only fields in IL-generation


## 3.0.52

- add new protobuf-net.NodaTime package that adds direct support for NodaTime primitives (note: this may be relocated to a NodaTime library)
- fix #700 - new APIs to allow surrogates to be defined externally, and to be implemented over primitive backing types
- fix #703 - new options on MSBuild targets (via Konstantin Sharon)
- fix #693 - new `IgnoreUnknownSubTypes` API on `[ProtoContract(...)]` and `MetaType`; serializes the types it *does* understand, and silently ignores the unknown sub-types
- fix #695 - JIT error when serialization callbacks declared at types other than the inheritance root
- fix #713 - work correctly with arrays (etc) of nullable enum types
- fix #697 - improve error reporting for invalid end-group markers
- fix #668 - additional non-generic APIs
- fix problem with protogen website not allowing imports

## 3.0.24

- fix bug in `SchemaGenerationOptions` (inverted input/output)

## 3.0.18

- add new `SchemaGenerationOptions` API for schema generation; this allows service generation
- tweaks to reflection services for gRPC (#617 via mholo65)

## 3.0.13

- add support for deserializing directly from `ReadOnlySpan<byte>`
- allow using open generic surrogates (#446 via ocoanet)
- add netcoreapp3.1 target (#670 via iamcarbon)
- add new `protobuf-net.AspNetCore` package with input/output formatter support

## 3.0.2

- reworked fix from 3.0.1 (same behavior, different implementation)

## 3.0.1

- fix bug with pre-measured objects and non-root `<T>` ([gRPC #100](https://github.com/protobuf-net/protobuf-net.Grpc/issues/100))

## 3.0

- first deploy of v3; everything as below
- [additional release notes](https://github.com/protobuf-net/protobuf-net/blob/main/docs/3_0.md)

## v3.0.0-alpha

- **breaking change** if you are using `new ProtoReader(...)` - you must now use `ProtoReader.Create(...)`
- **breaking change** by necessity, `ProtoBuf.Serializer+TypeResolver` has moved to `ProtoBuf.Serializer`; this is a rarely used API, but comsumers will need to be recompiled against the new type
- **breaking change** - mapped enum values are no longer supported; all enums are treated as pass-thru, in line with "proto3" semantics
- **breaking change** - dynamic typing (i.e. storing the `Type` metadata) and reference-tracking (`AsReference`, `AsReferenceDefault`, `DynamicType`) are not implemented/supported; this is partly due to doubts over whether the features are adviseable, and partly over confidence in testing all the scenarios (it takes time; that time hasn't get happened); feedback is invited
- **breaking change** - non-generic list-like APIs like `IList` or `ICollection` are no longer supported; there is a new API for processing custom collection types

- new state-based reader/writer API (works with streams, buffers, etc)
- entire new custom serializer API
- new `CreateForAssembly(...)` API (various overloads) for working with precompiled (at runtime) type models (faster than `RuntimeTypeModel`, but less flexible)

Some features are currently incomplete; this may restrict usage for some scenarios:

- serialization callbacks on inheritance models are currently only supported at the root type; workaround: `virtual` / `override`
- tuple-based types and types with surrogates cannot currently be used in inheritance chains - mostly because I need to figure out what that even *means*
- null-item retention in lists/arrays is not currently implemented
- custom default types for collection initializers are not yet implemented; a simple workaround is to initialize the collection in the type

There are some additional changes that are *technically* breaks, but which are simply bizarre things that probably
never should have been allowed; these changes should not impact most people!

- it is no longer valid to attempt to configure `object`
- it is no longer valid to define an inheritance involving value-types
- undeclared inheritance base-types are no longer supported; meaning: if you serialize a `Foo : FooBase` **as a `FooBase`**, but only tell the serializer about `Foo` (never mentioning `FooBase`), it will fail
- all APIs that take `int key` referring to `Type` are deprecated; user code should not be directly using these APIs, so no impact is expected
- the `TypeModel` API surface (for implementing custom models) has changed; user code should not be directly using these APIs, so no impact is expected
- the default .proto syntax has been changed from `Proto2` to `Proto3`; if this is a problem, either specify it explicitly, or there is a global option for the default syntax

Other changes:

- in line with the Google implementation, the serializer now optimally chooses when to use "packed" encoding, rather than taking the user too literally
- empty lists/arrays are no longer serialized (as empty payloads) when "packed" (they aren't serialized when not "packed", so this improves consistency)
- as a consequence of the above, the "setter" may not be invoked (to an empty array) when previously it might have been; this again is consistent with how non-"packed" works
- common stacks (`Stack<T>`, `ConcurrentStack<T>`) now preserve order correctly

## 2.4.8

- add support for full assembly metadata (#998 by mihaicodrean)

## 2.4.7

## 2.4.6

- apply #603 to 2.4 branch
- apply #611 to 2.4 branch

## 2.4.5

- Move TypeModel.Create (#609)
- add ApplyFieldOffset API (#608)

## 2.4.4

- mark `DiscriminatedUnion*` types as `[Serializable]`

## 2.4.2 / 2.4.3

- add `IProtoInput<T>` / `IProtoOutput<T>` APIs for discovering input/output capabilities (this is to allow testing for 3.0 features)

## 2.4.1

- fixes for .NET Core 3, thanks @szehetner
- (this build deliberately *does not* update package dependencies, to reduce impact)

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

- add codegen support for C# 3.0; C# 6.0 is still the default, but can be overridden via CLI or .proto options; see [#343](https://github.com/protobuf-net/protobuf-net/issues/343)
- updated Google "protoc" tooling on the web-site
- better exception messages when inheritance problems are detected; [#186](https://github.com/protobuf-net/protobuf-net/pull/186) via TrexinanF14
- add switch to allow the string cache code to be disabled; [#333](https://github.com/protobuf-net/protobuf-net/pull/333) via solyutor

## v2.3.4

- fix [#341](https://github.com/protobuf-net/protobuf-net/issues/341) - dictionaries with nullable types

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

- critical bug fix [#256](https://github.com/protobuf-net/protobuf-net/issues/256) - length-based readers are failing; if you are using 2.2.0, please update as soon as possible (this bug was introduced in 2.2.0)
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
