# Compile-time serializers, for native AOT and trimming

> **Preview.** The attributes described here are marked `[Experimental]`, which is a compile *error*
> until you suppress it — see [Opting in](#opting-in). The shape may still change.

Normally, protobuf-net works out how to serialize your types at *runtime*: it reflects over your
contracts and emits IL for them on the fly. That is fast and flexible, and it is fundamentally
incompatible with **native AOT**, where there is no IL emitter, and awkward under **trimming**, where
the members it wants to reflect over may already have been removed.

It also costs you on **every cold start**, which is why this is worth a look even if you never publish
native. That reflection and IL emission happens on first use of each contract. Measured over
`descriptor.proto`'s contracts — time to *first* serialize, median of 30 process launches:

| | time to first serialize |
| --- | ---: |
| runtime model (what you have today) | 50.6 ms |
| a generated model, same ordinary build | 16.9 ms |
| a generated model, published native AOT | 0.43 ms |

Ratios travel better than the milliseconds — one machine, one payload — but the shape holds: roughly
**3× on an ordinary build**, and ~100× once native. A generated model still pays JIT for its own code
on an ordinary build, which is why it is not nearer zero there.

The AOT generator builds those serializers at **compile time** instead. Your model becomes ordinary
C# in your own project — code you can read, step through, and that ILC can compile like anything
else.

## Opting in

Declare a partial class deriving from `TypeModel`, and tell it what to serialize:

``` c#
using ProtoBuf;
using ProtoBuf.Meta;

[ProtoModel]
[ProtoSerializable(typeof(Order))]
public partial class MyModel : TypeModel { }
```

The generator fills in the other half of the class. Use it like any other `TypeModel`:

``` c#
// `Instance` is generated for you; a TypeModel is a cache, so build it once and reuse it
MyModel.Instance.Serialize(stream, order);
var clone = MyModel.Instance.Deserialize<Order>(stream);
```

You only need to name your **top-level types** — the ones you serialize directly. Everything
reachable from those is included automatically: member types, collection elements, map keys and
values, `[ProtoInclude]` sub-types.

The trigger attributes are `[Experimental]` with the id `PBN9001`, so you must suppress it:

``` xml
<PropertyGroup>
  <NoWarn>$(NoWarn);PBN9001</NoWarn>
</PropertyGroup>
```

If you want none of this — no analyzers, no generators — one property turns off everything
`protobuf-net.BuildTools` does, and is checked before any work happens:

``` xml
<ProtoBufDisableBuildTools>true</ProtoBufDisableBuildTools>
```

## Requirements

- **C# 12 or later.** Below that the generator reports `PBN2000` and emits nothing, rather than
  emitting code that will not compile. Note `netstandard2.0` and `net4x` projects default to C# 7.3,
  so those need an explicit `<LangVersion>12.0</LangVersion>`.
- **net8.0 or later** for a few member shapes — `init`-only setters, non-public setters, getter-only
  properties, non-public constructors and private fields all need
  [`[UnsafeAccessor]`](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.unsafeaccessorattribute),
  which is net8.0+.

  Below net8.0 you get a **smaller model, not a broken build**: each such contract is dropped with a
  warning naming the shape and saying that net8.0 would fix it. Everything else still works.

## The model is closed at compile time

This is the most important thing to understand, and the reason the diagnostics matter.

The generated model **never consults the runtime model**. If the generator cannot handle a contract,
it omits it and tells you why — it does *not* silently fall back to reflection, because that would
defeat the entire point under AOT.

A contract that was dropped throws when you use it:

```
InvalidOperationException: No serializer for type Foo is available for model MyModel
```

So **the build warnings are your inventory**. If you want the model to be complete or the build to
fail, escalate them:

``` xml
<WarningsAsErrors>$(WarningsAsErrors);PBN2001;PBN2002;PBN2003;PBN2004</WarningsAsErrors>
```

| id | meaning |
| --- | --- |
| `PBN2000` | the language version is below C# 12; nothing was generated |
| `PBN2001` | a contract was dropped because of one of its members |
| `PBN2002` | a contract was dropped because of how it is declared |
| `PBN2003` | a contract was dropped because of a protobuf-net option not yet supported |
| `PBN2004` | a contract was dropped because something it references was dropped |
| `PBN2010` | a call site still goes through the runtime model — see below |
| `PBN2011` | a call site takes its contract type as a value, so nothing can check it |

`PBN2004` matters more than it looks: dropping cascades. A contract whose member type was dropped
cannot be emitted either, so one unsupported type can take a subtree with it. Fix the ones that are
*not* `PBN2004` first, and the cascade usually clears.

### Not every refusal is a gap

Many of these diagnostics describe shapes **protobuf-net itself refuses** — a contract with no
parameterless constructor, a member type that is not a contract, `[NullWrappedValue]` on something
that is not a scalar. Those would throw at runtime under the reflection-based model too, so there is
nothing being lost.

The diagnostics say which is which, and quote what the runtime model says, so you can tell "this
never worked" from "this is not supported here yet".

## Turning it on does not move your existing code

This is the part that surprises people, so the generator's analyzer says it out loud.

`Serializer.Serialize(...)` — and `Deserialize`, `DeepClone`, `Measure`, and the rest of that static
facade — go through `RuntimeTypeModel.Default`, which builds serializers **by reflection**. Adding a
`[ProtoModel]` does not change that. And those call sites keep working on an ordinary JIT runtime, so
nothing goes wrong until you publish for native AOT, a long way from the change.

Once your project declares a `[ProtoModel]`, `PBN2010` flags each such call and names your model:

``` c#
Serializer.Serialize(stream, order);   // PBN2010
model.Serialize(stream, order);        // what it is asking for
```

Nothing is reported if you have no `[ProtoModel]` — the runtime model is a perfectly good way to use
protobuf-net, and this has nothing to say to anyone using it. Calls on a model *you* named, including
a `RuntimeTypeModel.Create()` you configured deliberately, are left alone.

`PBN2010` comes with a code fix. It offers anything of your model's type already in scope, and
otherwise the generated shared instance:

``` c#
Serializer.Serialize(stream, order);        // PBN2010
MyModel.Instance.Serialize(stream, order);  // what the fix writes
```

Every generated model gets a `public static MyModel Instance { get; }` for this — a `TypeModel` is a
cache, meant to be built once and reused, so there is one obvious place to reach for rather than a
`new MyModel()` at each call site. If you declare your own `Instance`, or your model has no
parameterless constructor, it is not generated and your own arrangements stand.

**`new MyModel()` will not compile**, for the same reason: if you declare no constructor, the
generator emits a non-public one (`private` if your model is sealed, `protected` otherwise), which
replaces the implicit public one and points you at `Instance`. Two consequences worth knowing:

- **reflective creation stops working** — `Activator.CreateInstance(typeof(MyModel))`, or a DI
  container asked to construct the type. Use `Activator.CreateInstance(type, nonPublic: true)`, or
  register the instance: `services.AddSingleton<TypeModel>(_ => MyModel.Instance);`
- **declaring any constructor opts out entirely.** `public MyModel() { }` in your half of the partial
  and everything behaves as before — the generator only does this when you have expressed no
  intention about construction.

`PBN2011` is the other half, and it has no mechanical fix: the non-generic APIs take the thing to
serialize as an `object` or a `Type`, so neither the analyzer nor the generator can tell what will be
serialized. Under AOT that call will take the reflection path. If it needs to work when published,
move it to a generic overload.

## Things that catch people out

### `.proto`-generated DTOs need their own project

If you generate DTOs from a `.proto` using
[protobuf-net.BuildTools](https://protobuf-net.github.io/protobuf-net/contract_first), you **cannot**
put `[ProtoModel]` in the same project. Source generators all run against the same input compilation
and never see each other's output, so the model finds nothing to serialize.

Put the `.proto` and its generated DTOs in one project, and reference it from the project holding the
model. The generator reports `PBN2002` with this explanation if it cannot resolve a type you listed.

### Two references that declare the same type

If two assemblies you reference declare the same full type name, C# can only tell them apart with an
[`extern alias`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/extern-alias),
which you set on the reference in your project file.

Watch the syntax, because it is **not** the same for the two reference kinds. A `PackageReference`
needs `Aliases` as a child *element* — as an attribute it is ignored, and you find out via `CS0430`
at the point of use:

``` xml
<PackageReference Include="Some.Package" Version="1.2.3">
  <Aliases>some</Aliases>
</PackageReference>

<ProjectReference Include="../Other/Other.csproj" Aliases="other" />
```

The generator honours an alias that exists — you do not need to do anything else. If *neither* of a
colliding pair is aliased, no C# syntax can name the type at all, so the contract is refused with
`PBN2002` naming both assemblies. Aliasing **one** of them is enough.

### Types you do not own

You cannot put `[ProtoContract]` on `System.Uri` or a type from someone else's package. Two options:

- many such types are already supported directly (`Uri`, `DateTime`, `Guid`, `decimal`,
  `DateOnly`/`TimeOnly`, `IPAddress` with the option below…);
- otherwise declare a surrogate on the model:

  ``` c#
  [ProtoModel]
  [ProtoSerializable(typeof(Job))]
  [ProtoSurrogate(typeof(DateTimeOffset), typeof(MySurrogate))]
  public partial class MyModel : TypeModel { }
  ```

  `[ProtoSurrogate]` can also be declared at **assembly** level, which is how a package ships
  surrogates for types it supports — a consumer referencing that package gets them automatically.
  That is how `protobuf-net.NodaTime` works.

### Parseable types are opt-in

A type with a `ToString()` and a `static T Parse(string)` can go on the wire as a string, but only if
you ask — matching `RuntimeTypeModel.AllowParseableTypes`, which is also off by default:

``` c#
[ProtoModel(AllowParseableTypes = true)]
```

## Checking it really works

Two things worth doing before you trust it:

1. **Publish for native AOT and run it.** Everything else runs on a JIT runtime where the reflection
   path still exists, so it can hide a problem that only appears once ILC has trimmed.

   ``` sh
   dotnet publish -c Release -r win-x64
   ```

2. **Read the trim/AOT warnings.** `<PublishAot>true</PublishAot>` enables that analysis at ordinary
   *build* time too, so you do not have to pay for a native publish to see them.

## What is supported

Broadly, the code-first surface: contracts and members (properties *and* fields), inheritance via
`[ProtoInclude]` including interface roots, enums, nullables, `[DefaultValue]`, `DataFormat` and
`IsRequired`, collections (arrays, `List<T>`, sets, queues, stacks, the immutable and concurrent
families), dictionaries and `[ProtoMap]`, `CompatibilityLevel` and the BCL types it governs,
null-wrapping, extensible contracts, serialization callbacks, `ShouldSerialize`/`Specified`,
`ImplicitFields`, `[ProtoPartialMember]`/`[ProtoPartialIgnore]`, surrogates, hand-written serializers,
auto-tuples, and closed generic contracts.

An **open** generic contract cannot be supported: the generated services type is a single non-generic
class, so there is nowhere to put the type parameter.

The best current statement of what works is the test corpus: every `[ProtoContract]` in protobuf-net's
own test projects, plus DTOs generated from a large `.proto` corpus, is serialized by both models and
compared byte-for-byte on every CI run.
