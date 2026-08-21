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

That cost **scales with the number of distinct contracts** you serialize, at roughly 0.2 ms each on
the machine above — so a 400-contract model spends about an eighth of a second before its first byte:

| 400 contracts | time to first serialize |
| --- | ---: |
| runtime model | 129 ms |
| a generated model, ordinary build | 72 ms |
| a generated model, native AOT | **0.9 ms** |

Note the middle row: on an ordinary build the advantage *narrows* as the model grows, because the
generated code is itself JIT-compiled and there is more of it. It is native AOT that changes the order
of magnitude.

The AOT generator builds those serializers at **compile time** instead. Your model becomes ordinary
C# in your own project — code you can read, step through, and that ILC can compile like anything
else.

If you use **protobuf-net.Grpc**, the same applies to your gRPC clients and services — see
[gRPC](#grpc) below.

## And it is about twice as fast

Cold start is the reason to look; throughput is the reason to stay. Same payload — the
`FileDescriptorSet` for `descriptor.proto`, 7,670 bytes — in both directions:

| | serialize | | deserialize | |
| --- | ---: | --- | ---: | --- |
| protobuf-net 3.2.46, as shipped on NuGet | 20.59 µs | `████████████████████` | 22.77 µs | `████████████████████` |
| protobuf-net v4, runtime model | 20.39 µs | `███████████████████▊` | 17.63 µs | `███████████████▍` |
| **protobuf-net v4, generated model** | **10.28 µs** | `██████████` | **9.03 µs** | `███████▉` |
| Google.Protobuf | 13.14 µs | `████████████▊` | 12.66 µs | `███████████` |

*Lower is better; bars are relative to the slowest row in each direction.*

Against the package you have installed today, the generated model is **2.0× faster to serialize and
2.5× faster to deserialize** — and it is **21% / 29% ahead of Google.Protobuf**, on Google's own
schema, with their own generated types and their own serializer. Serialization allocates
**nothing**, against 4,232 bytes for Google.Protobuf.

Three honest notes, because the table invites the questions:

- **the "v4, runtime model" row is the interesting one.** It is barely faster than 3.2.46 at
  serializing, but meaningfully faster at deserializing — the reader rewrite lifts everyone, while
  the writer's gains land almost entirely in generated code. So "upgrade to v4" and "generate your
  model" are two separate wins, and this table is the only place that separates them;
- **the generated-model figures are measured on an ordinary JIT build**, not a native publish. The
  code is identical either way — that is the point of generating it — but these are not native-AOT
  numbers, and a native publish has its own (better) cold-start story above.
- **the benchmark's DTOs are `sealed`, and until recently a `.proto`-first consumer could not get
  that.** protobuf-net emits a per-message unknown-sub-type check that a generated DTO can never
  actually trip, and `sealed` is one of the things that elides it — so this row was, in that one
  respect, a shape contract-first users could not reach. They can now, with
  [`SubTypes="sealed"`](contract_first.md#additional-options). Do not read much into it: the check
  measures at **~0.6%**, which is below this benchmark's run-to-run noise. It is noted because the
  numbers should say what they were measured on, not because it moves them.

### When the length is needed first — the gRPC shape

gRPC frames every message with its length, so the payload has to be **priced before a byte is
written**. That is a harder question than "serialize this", and the two engines answer it very
differently:

| | measure + serialize | plain serialize | cost of needing the length |
| --- | ---: | ---: | ---: |
| protobuf-net v4, runtime model | 38.19 µs | 20.62 µs | +85% |
| **protobuf-net v4, generated model** | **15.45 µs** | 10.21 µs | **+51%** |

The runtime model has no way to price a message except to **write it and count** — to a null
writer, then again for real — so it very nearly serializes twice. A generated model has an
arithmetic `Measure_` per contract, which walks the object graph doing sums instead of encoding:
3.76 µs here, against the 17.6 µs a write-to-count pass costs.

The rest of the difference is that the measure's work is *kept*. `Measure` hands back a state
object, and the sub-message lengths it computed are handed to the write with it — so the write
does not re-derive them. That hand-off is an O(1) swap of the length caches, not a copy; when it
*was* a copy it cost an extra 22 KB and 11%.

So this is the shape where the generated model gains most: **2.5× against the runtime model**,
where plain serialization is 2.0×. If you are on gRPC, that is the number that applies to you.

### What the payload is made of

Ratios travel; absolute microseconds do not. And a single payload cannot speak for yours, so it is
worth knowing what this one is: of its 7,670 bytes, **71.5% is string/bytes payload**, 13.8% field
tags, 8.2% length prefixes and 6.6% varint values. Encoding those strings — the irreducible part of
any serializer's job here — is 2.8 µs on its own, about a quarter of the generated model's serialize
time.

A payload built from packed numeric columns, or from far deeper nesting, would have a different
composition and could land differently. `src/NanoBench/DescriptorPayloadCensus.md` has the full
breakdown and the script that produces it, so the same question can be asked of your own data.

### Reproducing these

Two processes, because the released package and a local build share an assembly name and cannot be
referenced side by side. Same machine, same payload, same BenchmarkDotNet settings; the payload
length is printed by both so a divergence cannot pass unnoticed:

``` bash
# protobuf-net v4 (runtime + generated) and Google.Protobuf
dotnet run -c Release -f net10.0 --project src/NanoBench -- \
    --filter "*DescriptorSerializeBenchmarks*" --job short
dotnet run -c Release -f net10.0 --project src/NanoBench -- \
    --filter "*DescriptorParseBenchmarks*" --job short

# protobuf-net as shipped on NuGet
dotnet run -c Release --project src/ReleasedBench -- --filter "*" --job short
```

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

If you want none of this — no analyzers, no generators — one property turns off all of protobuf-net's
build-time tooling, and is checked before any work happens:

``` xml
<ProtoBufDisableBuildTools>true</ProtoBufDisableBuildTools>
```

### Contract-first: DTOs generated from a `.proto`

If your types come from a `.proto` rather than from hand-written C#, you have nothing to name a
`typeof(...)` for — the DTOs do not exist until the build runs. `[ProtoSchema]` seeds the model from
the schema instead:

``` c#
[ProtoModel]
[ProtoSchema]
public partial class MyModel : TypeModel { }
```

That is the whole thing, and it is the form to reach for by default: no argument means **every
`.proto` in the project**, which is what almost everyone wants. The `.proto` files, the DTOs
generated from them, and the model can all live in **one project**.

Serialization is unchanged — the generated model serializes the generated DTOs like any other
contract:

``` c#
MyModel.Instance.Serialize(stream, new Order { Id = 1 });
```

If you want only some of the schemas, name them:

``` c#
[ProtoSchema("orders.proto")]
[ProtoSchema("shipping/address.proto")]
```

A path is matched from the right, a segment at a time, so `address.proto` matches
`shipping/address.proto` but `billing/address.proto` does not — and an exact match always wins over a
suffix one. `/` and `\` are interchangeable, and matching ignores case.

Imports are followed automatically: naming a schema pulls in the types it imports, so you list what
you *serialize*, not everything it depends on.

You can mix the two forms freely — `[ProtoSerializable]` for hand-written contracts and
`[ProtoSchema]` for generated ones, on the same model.

**Why this exists**: source generators all run against the same input compilation and never see each
other's output, so `[ProtoSerializable(typeof(Order))]` cannot name a DTO that another generator is
producing in the same project — the model would find an error symbol where the contract should be.
`[ProtoSchema]` reads the schema directly, which is the same information the DTO generator works
from. Before this, the answer was to put the `.proto` in its own project and reference it; that still
works and is still a perfectly good layout, but it is no longer required.

## gRPC

protobuf-net.Grpc has the same problem twice over: it builds its client proxies with ref-emit, and their
payloads through the runtime model. Both halves are generated at compile time too, and the shape mirrors
the one above — you declare a partial, and the generator fills it in:

``` c#
using ProtoBuf.Grpc.Configuration;

[ProtoGrpc(Model = typeof(MyModel))]
[ProtoService(typeof(IGreeter), typeof(GreeterService))]
public sealed partial class MyServices : ClientFactory { }
```

``` c#
var greeter = channel.CreateGrpcService<IGreeter>(MyServices.Instance);   // client
builder.Services.AddMyServices();                                        // server (generated)
app.MapGrpcService<GreeterService>();
```

Two things worth knowing here, because they are easy to miss:

- **the `Model` link is the point.** Generated proxies with reflected payloads is the failure that looks
  fine until you publish: the proxies are AOT-safe, the bytes they carry are not, and the build succeeds
  either way. Naming a `[ProtoModel]` is what closes that;
- **you do not list the payload types.** `[ProtoService]` already names the contracts, so the model picks
  up their request and response types automatically — a `[ProtoModel]` used only for gRPC needs no
  `[ProtoSerializable]` at all.

There is more: interceptors that let existing `CreateGrpcService` calls stay as they are, DI-registered
clients, and diagnostics for each way a contract can fall short. That belongs with protobuf-net.Grpc's own
documentation:

> **[grpc.protobuf-net.dev/aot](https://grpc.protobuf-net.dev/aot)**

It needs protobuf-net.Grpc **1.3.6 or later**; earlier versions statically root the runtime model, which
keeps the reflection paths alive however static your own code is.

## Requirements

- **C# 12 or later.** Below that the generator reports `PBN3000` and emits nothing, rather than
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
<WarningsAsErrors>$(WarningsAsErrors);PBN3001;PBN3002;PBN3003;PBN3004</WarningsAsErrors>
```

| id | meaning |
| --- | --- |
| `PBN3000` | the language version is below C# 12; nothing was generated |
| `PBN3001` | a contract was dropped because of one of its members |
| `PBN3002` | a contract was dropped because of how it is declared |
| `PBN3003` | a contract was dropped because of a protobuf-net option not yet supported |
| `PBN3004` | a contract was dropped because something it references was dropped |
| `PBN3010` | a call site still goes through the runtime model — see below |
| `PBN3011` | a call site takes its contract type as a value, so nothing can check it |

`PBN3004` matters more than it looks: dropping cascades. A contract whose member type was dropped
cannot be emitted either, so one unsupported type can take a subtree with it. Fix the ones that are
*not* `PBN3004` first, and the cascade usually clears.

> **Upgrading from 3.3?** These ids used to be `PBN2000`–`PBN2004` and `PBN2010`–`PBN2013`, which
> collided with the gRPC service-contract analyzers in the same package — so silencing an AOT
> warning could also silence a gRPC **error**. The whole block moved to `PBN3000+`, keeping the last
> three digits, so `PBN2001` is now `PBN3001`. Update any `WarningsAsErrors`, `NoWarn` or
> `dotnet_diagnostic.*` entry you copied from the old docs: the old ids still exist and still mean
> something, just not this, so a stale entry fails quietly rather than loudly.

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

Once your project declares a `[ProtoModel]`, `PBN3010` flags each such call and names your model:

``` c#
Serializer.Serialize(stream, order);   // PBN3010
model.Serialize(stream, order);        // what it is asking for
```

Nothing is reported if you have no `[ProtoModel]` — the runtime model is a perfectly good way to use
protobuf-net, and this has nothing to say to anyone using it. Calls on a model *you* named, including
a `RuntimeTypeModel.Create()` you configured deliberately, are left alone.

`PBN3010` comes with a code fix. It offers anything of your model's type already in scope, and
otherwise the generated shared instance:

``` c#
Serializer.Serialize(stream, order);        // PBN3010
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

`PBN3011` is the other half, and it has no mechanical fix: the non-generic APIs take the thing to
serialize as an `object` or a `Type`, so neither the analyzer nor the generator can tell what will be
serialized. Under AOT that call will take the reflection path. If it needs to work when published,
move it to a generic overload.

## Things that catch people out

### `typeof(...)` cannot name a `.proto`-generated DTO

If you generate DTOs from a `.proto` using
[protobuf-net.BuildTools](https://docs.protobuf-net.dev/contract_first), and put
`[ProtoModel]` in the same project, `[ProtoSerializable(typeof(Order))]` will not resolve: source
generators all run against the same input compilation and never see each other's output, so the model
gets an error symbol rather than the contract. The generator reports `PBN3002` naming the fix.

Use [`[ProtoSchema]`](#contract-first-dtos-generated-from-a-proto) instead, which seeds from the
schema rather than from a symbol and works in one project. (Putting the `.proto` in a separate project
and referencing it also still works.)

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
`PBN3002` naming both assemblies. Aliasing **one** of them is enough.

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

### Hierarchies the base type cannot declare

`[ProtoInclude]` goes on the base, so it only works when the base can *name* its sub-types. It cannot
when the sub-type is in a package the base library does not reference, or is a generic construction
like `Tagged<Order>` that the base library could never have written down. At runtime that is what
`AddSubType` is for; the compile-time equivalent is `[ProtoSubType]`:

``` c#
[ProtoModel]
[ProtoSerializable(typeof(Shape))]
[ProtoSubType(typeof(Shape), typeof(Tagged<Order>), 100)]
public partial class MyModel : TypeModel { }
```

The outcome is exactly as though `Shape` had carried `[ProtoInclude(100, typeof(Tagged<Order>))]` all
along — the same wire format, the same sub-type dispatch. Like `[ProtoSurrogate]` it can also be
declared at **assembly** or **module** level, which is how a package ships the linkage for a
hierarchy it extends; anything referencing that package picks it up.

Two things differ from `[ProtoSurrogate]`, both worth knowing:

- **declarations accumulate rather than override.** Two packages each adding a sub-type to one base
  give a hierarchy with both — there is nothing to be more specific *about*. Two declarations that
  genuinely conflict (one field number claimed twice, or one sub-type named twice) are reported with
  `PBN3002` and the hierarchy is dropped;
- **your model stops being determined by your own source.** Adding a package reference can change
  what a base-typed member puts on the wire, and — because a hierarchy is all-or-nothing — a
  sub-type someone else declared can drop a hierarchy rooted at a type you own.

`[ProtoSubType]` is read by the generator only: **the runtime model does not honour it**, exactly as
it does not honour `[ProtoSurrogate]`. If you use `RuntimeTypeModel` as well, keep calling
`AddSubType` there.

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

## Known issues

- **Trim/AOT warnings from protobuf-net itself.** A native publish reports around 19 `IL2xxx`/`IL3050`
  warnings originating in protobuf-net. They come from the *reflection-based* model — the runtime
  model, `DynamicStub`, and the auxiliary/list paths — which is code your generated model never
  executes, but which ILC still analyses because it is present in the assembly. Your model works; the
  warnings are noise from the road not taken. Removing them entirely needs the reflective paths not
  to exist on the AOT route at all, which is a larger piece of work and is tracked.
- **`Extensible.AppendValue`/`GetValue` work under native AOT for the generic overloads at the
  default `DataFormat`** — which is the common case. A non-default `DataFormat`, or the legacy
  `object`-based overload, still resolves by reflection; those now throw rather than silently
  discarding the value. Reading and writing *unknown fields* is unaffected either way — that path only
  copies bytes.
- **A hand-written serializer as a map key or value** is refused when its category is `CategoryScalar`
  or cannot be determined, as is a **collection as a map key**. Both are reported with a diagnostic
  naming the reason rather than silently mis-emitted.
- **`[ProtoSchema]` does not yet cover every schema.** Across protobuf-net's 268-schema test corpus it
  builds 241, and the shortfall is two shapes: a **`repeated` enum** (and an enum as a **map value**),
  and proto2 **`group`**. Both are refused with a diagnostic rather than mis-emitted, and neither
  affects `[ProtoSerializable]`, which handles all three. If you hit one, generate the DTOs in a
  separate project and seed the model with `typeof(...)` as before.
- **`AppendValue` aside, anything the generator cannot handle is refused, not guessed.** If a contract
  is missing from your model there is a `PBN3001`–`PBN3004` warning saying which and why.

## What is supported

Broadly, the code-first surface: contracts and members (properties *and* fields), inheritance via
`[ProtoInclude]` (and `[ProtoSubType]`) including interface roots, enums, nullables, `[DefaultValue]`, `DataFormat` and
`IsRequired`, collections (arrays, `List<T>`, sets, queues, stacks, the immutable and concurrent
families), dictionaries and `[ProtoMap]`, `CompatibilityLevel` and the BCL types it governs,
null-wrapping, extensible contracts, serialization callbacks, `ShouldSerialize`/`Specified`,
`ImplicitFields`, `[ProtoPartialMember]`/`[ProtoPartialIgnore]`, surrogates, hand-written serializers,
auto-tuples, and closed generic contracts. Contracts can be seeded from a `typeof(...)` or, with
`[ProtoSchema]`, straight from a `.proto`.

An **open** generic contract cannot be supported: the generated services type is a single non-generic
class, so there is nowhere to put the type parameter.

For gRPC, the supported surface is the code-first one protobuf-net.Grpc itself binds: unary,
client-streaming, server-streaming and duplex operations, `CallContext` or `CancellationToken`, void
requests and responses, `[SubService]` bases, `IDisposable`/`IAsyncDisposable`, `[Operation]` and
`[ServiceContract]`/`[OperationContract]` naming, and closed generic contracts. Anything else is refused
with a warning rather than half-generated — see [grpc.protobuf-net.dev/aot](https://grpc.protobuf-net.dev/aot).

The best current statement of what works is the test corpus: every `[ProtoContract]` in protobuf-net's
own test projects, plus DTOs generated from a large `.proto` corpus, is serialized by both models and
compared byte-for-byte on every CI run. For the gRPC half it is a native-AOT smoke test with a real
client and a real server over a real socket, in one natively-compiled binary.
