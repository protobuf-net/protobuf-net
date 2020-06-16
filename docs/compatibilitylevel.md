# What is `CompatibilityLevel`?

In short, this determines the conventions that protobuf-net *actively prefers and advocates* at a given point in time.
This means things like "how is a `DateTime` stored?". This has evolved over time as "well known" types have become
available and shared by the community. Using the "well known" types is hugely beneficial for cross-platform
compatibility.

Because this knowledge has changed over time, protobuf-net can't blindly just use the "new shiny" - instead, *by default*
it assumes that your code is legacy, and applies all the conventions that have been there since forever. Specifically,
this means that `DateTime`, `TimeSpan`, `Guid` and `Decimal` follow legacy conventions as defined
in [bcl.proto](https://github.com/protobuf-net/protobuf-net/blob/main/src/Tools/bcl.proto), that are awkward to
use from other platforms. Changing the conventions being used is a fundamentally breaking change, and protobuf-net wants
to ensure that you can always deserialize the data you've stored, so it can't change this unilaterally.

`CompatibilityLevel` defines a snapshot of the preferred conventions at a given point in time.

If you're starting a new green-field project, it would be great to add:

``` c#
[module: CompatibilityLevel(CompatibilityLevel.LevelNNN)]
```

for the highest `NNN` defined at the time. This means you'll be using all the current recommendations.

At the current time, the levels defined are:

- `200` - uses `bcl.proto` for `DateTime`, `TimeSpan`, `Guid` and `Decimal`.
- `240` - like `200`, but uses [`.google.protobuf.Timestamp`](https://github.com/protocolbuffers/protobuf/blob/main/src/google/protobuf/timestamp.proto) for `DateTime` and [`.google.protobuf.Duration`](https://github.com/protocolbuffers/protobuf/blob/main/src/google/protobuf/duration.proto) for `TimeSpan`.
- `300` - like `240`, but uses `string` for `Guid` (in big-endian hyphenated UUID text format, 36 bytes; a 16-byte `bytes` variant is also available by additionally specifying `DataFormat.FixedSize`) and `Decimal` (invariant "general" format).

## Can I change levels?

The convention change is *fundamentally breaking* to the data affected. If you don't need to deserialize old data: fine! Otherwise, no - at least not without your code
migrating the data too. You aren't stuck *entirely* at this level, though; the compatibility level can be specified:

- at the assembly
- at the module
- at the individual type being serialized (noting that it *is* inherited)
- at the individual field/property
- at the `RuntimeTypeModel`, `MetaType` and `ValueMember`, if you are building runtime-models - see below

This gives you lots of options for configuring an evolving system - exploting new conventions when available, but without breaking old data. For example:

``` c#
[module: CompatibilityLevel(CompatibilityLevel.Level300)] // all new types should use the new conventions

[ProtoContract, CompatibilityLevel(CompatibilityLevel.Level240)]
public class SomeOldType // except for this pre-existing type, that should use some older conventions
{
    // not shown: ... some existing fields that will use Level240

    [ProtoMember(42), CompatibilityLevel(CompatibilityLevel.Level300)]
    public DateTime DateOfBirth {get;set;} // added later, we can use newer conventions here
}
```

## That sounds like `DataFormat.WellKnown`

Yup. But `DataFormat.WellKnown` only conveyed *one* change of conventions - we can't update it to mean *more* recent conventions without breaking your data.
If a member resolves as level `200` **and** is annotated to use `DataFormat.WellKnown`, then that member is treated as level `240`.

## Why is this even necessary?

The recommeded options change over time because *new conventions* evolve over time. When `bcl.proto` was created, there *was* no `timestamp.proto` or
`duration.proto`. Likewise, decimals. For guids... frankly, `bcl.proto` just made a bad choice there - not least because of the unusual endianness
that is used. It is a lot easier to exchange guids as strings. Decimal is also awkward, and is complicated by [this possible future option](https://github.com/protocolbuffers/protobuf/pull/7039),
but for now: using a regular string is a far more reasonable option than `bcl.proto` presents.

## Configuring compatibility in custom models

Note: these are all advanced topics that do not impact most users.

There are multiple ways of configuring compatibility levels if you are configuring models at runtime:

1. the `RuntimeTypeModel.DefaultCompatibilityLevel` instance property can be specified; this is used when a type or module don't define a level via attributes etc; this can only be done before types are added to the model
2. the `MetaType.CompatibilityLevel` instance property can be specified to explicitly control a particular type; this can only be done before fields are added to the type, so the `RuntimeTypeModel.BeforeApplyDefaultBehaviour` event is a good place to do this
3. the `RuntimeTypeModel.Add` method (for declaring new types) now optionally takes a compatibility level for new types; if specified, and a type already exists, it is *checked* - if it does not match, an exception is thrown
4. the `ValueMember.CompatibilityLevel` instance property can be specified to explicitly control individual data fields

If you are using the `ISerializer<T>` API, there is a new `TypeModel.GetInbuiltSerializer<int>(...)` API that optionally takes a `CompatibilityLevel` and `DataFormat`, and returns the appropriate serializer, if one exists for `T` - or `null` otherwise.