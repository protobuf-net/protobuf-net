# Release Notes

Packages are available on NuGet: [protobuf-net](https://www.nuget.org/packages/protobuf-net)

[![Donate](https://liberapay.com/assets/widgets/donate.svg)](https://liberapay.com/protobuf-net/donate)

## v3.* (future plans)

see: [protobuf-net: large data, and the future](http://blog.marcgravell.com/2017/05/protobuf-net-large-data-and-future.html)

## v2.3 (work in progress)

- proto2/proto3 DSL processing tools to make a resurgance

preview: [https://protogen.marcgravell.com/](https://protogen.marcgravell.com/)

## v2.2.1

- critical bug fix [#256](https://github.com/mgravell/protobuf-net/issues/256) - length-based readers are failing

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