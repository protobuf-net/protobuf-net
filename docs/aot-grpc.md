# Build-time gRPC proxies: findings and handover

Working notes for the gRPC half of the AOT story, in the same spirit as `aot-findings.md`. The
serializer half is `aot.md` (user-facing) and `aot-findings.md` (working notes).

> **Handover** (2026-08-17). Branch `grpc-aot-generator`, draft PR
> [#1282](https://github.com/protobuf-net/protobuf-net/pull/1282). Validated on Windows:
> `dotnet test src/BuildToolsUnitTests` (**400** pass), the full traversal build, `AotConformanceTests`
> (667), `AotDifferential` (**3080 contracts compared, 0 differing**), a JIT run of `src/AotGrpcSmoke`,
> and a `win-x64` native publish of the same — **twelve checks, 4 IL warnings, 14,585,856 bytes**.
> `protobuf-net.BuildTools.Legacy` builds green. **`linux-x64` has not been measured on this branch.**
>
> **This is feature-complete for the routes people actually use.** Every consumer entry point is covered
> — a direct `CreateGrpcService`, an intercepted one, a DI-registered client, and the server — and
> anything unsupported says so, differently when AOT has been asked for. Specifically done:
>
> | | |
> | --- | --- |
> | proxies + server bindings | all five method shapes, `[SubService]`, disposable bases, void/`Empty`, overloads, closed generics, WCF markers |
> | payloads | seeded from `[ProtoService]`, so a gRPC-only `[ProtoModel]` needs no `[ProtoSerializable]`; checked instead when the model is in a referenced assembly |
> | call sites | interceptors (opt-in), `PBN4016` + code fix when not; DI clients via `TryAddSingleton` in the generated `AddXxx`, `PBN4017` otherwise |
> | diagnostics | `PBN4000`–`PBN4018`, every one exercised by a fixture or test |
> | version | `version.json` revved to **3.4**; `get-version` computes `3.4.0`, which is what the docs now claim |
>
> **Landed elsewhere, and this branch depends on it:**
>
> | | |
> | --- | --- |
> | protobuf-net.Grpc **1.3.6** (released) | everything this branch needs; `[ProtoGrpc]`/`[ProtoService]`, both `RuntimeTypeModel` roots gone, `BinderConfiguration.Binder` public |
> | protobuf-net.Grpc #373, #374, #375 (merged) | the AOT docs page: brought current, led with interceptors, and given the protobuf-net 3.4.0 floor |
> | protobuf-net.Grpc #369 (merged) | a contributor's `[SubService]` metadata fix. **Needed no change here** — our emit already passes the declaring interface's `MethodInfo`, which is what it keys on; see the parity section below |
> | protobuf-net #1284, #1287 (merged) | `[Experimental]` help links; editions. #1287 merged in here cleanly with no file overlap, and the corpus differential read 3080/0 afterwards |
>
> **What is left, in the order I would take it:**
>
> 0. **Compile-time endpoint metadata — measured, and *not* blocking.** The premise was that reflective
>    metadata would not survive AOT. It does: `AotGrpcSmoke` now carries attributes at all three levels
>    plus a real `[Authorize]`, and a `win-x64` native publish delivers every one of them. So this is an
>    optimisation, not a correctness fix — and one with a cost, since a constructed list silently ignores
>    a consumer's overridden `GetMetadata`. `AttributeRenderer`, `MetadataGather` and the
>    `src/AotGrpcMetadataDiff` oracle are all built and green; the emit is **deliberately not done**. See
>    the section below.
> 1. **`linux-x64` native publish.** Unmeasured here; the serializer side historically matched win-x64
>    warning-for-warning, so this is confirmation rather than discovery. Byte sizes are not comparable
>    across RIDs.
> 2. **Server reflection.** `protobuf-net.Grpc.AspNetCore.Reflection` builds schemas at run time and is
>    untouched — almost certainly reflective. Note it resolves a **`BinderConfiguration`** from DI, which
>    is the second and last DI seam in protobuf-net.Grpc, and a `[ProtoGrpc]` type converts to one
>    implicitly, so the trick that fixed the DI client path should apply.
> 3. **Nested/generic `[ProtoGrpc]` declarations** are refused (`PBN4014`) rather than supported.
>    Deliberate — "does not work today, continues not to work" — and `ProtoModelGenerator` has the same
>    case open as a TODO, so if it is ever built it should be built for both.
>
> **Closed rather than pending**, so nobody re-opens them by accident:
>
> - **vendoring `xxHash128`** to synthesise interceptor locations. ~2,000 lines from Roslyn's own copy;
>   reflection into the host is 80. The synthesis route is proven and recorded as the fallback.

## Plan forward

Written for a cold start: this section plus the Handover above should be enough to act without
re-deriving anything. `AGENTS.md` has the rules of the road; this is the queue.

### 0. Compile-time endpoint metadata — measured, and parked

**The premise was wrong, and measuring it is what settled it.** The claim was that `[Authorize]` had to
be constructed at compile time because the reflective route would not survive AOT. It survives: see
"Compile-time metadata: measured" below. The gather, the renderer and the oracle are built and green;
the emit is not done, and should not be done without a reason better than tidiness — replacing
`__cfg.Binder.GetMetadata(...)` with a constructed list silently ignores a consumer's overridden
`GetMetadata`, which is the exact bug the comment at that emit site exists to prevent.

If it is ever picked up, what remains is `PBN4019` (for anything `AttributeRenderer` refuses, naming the
attribute and the operation) and the emit itself — and the emit should keep the reflective call for a
non-default binder rather than replacing it outright.

### 1. `Stream` payloads — the widest capability gap

The largest difference between "works on JIT" and "works published". protobuf-net.Grpc has a documented
streams feature; we refuse those contracts (`PBN4002`), and under AOT a refused contract does not
degrade, it throws. `IObservable<T>` and `Grpc.Core`'s own call types are the same shape of gap but far
rarer. Start from `IsRuntimeOnlyPayload` in `GrpcProxyGenerator.ParseContract.cs`, and from
`docs/streams.md` in the protobuf-net.Grpc repo for what the runtime actually does.

### 2. Verify on `linux-x64`

Never published there on this branch. The serializer side historically matched win-x64
warning-for-warning, so this is confirmation rather than discovery. Byte sizes are **not** comparable
across RIDs; warning counts are.

### 3. Release 3.4.0

`version.json` is revved and `get-version` computes `3.4.0` — verified by squash-probing onto
`origin/main`, not by arithmetic. The gRPC docs already claim that floor, so the two are consistent.

### 4. Server reflection

`protobuf-net.Grpc.AspNetCore.Reflection` builds schemas at run time and is untouched. It resolves a
**`BinderConfiguration`** from DI — the second and last DI seam — and a `[ProtoGrpc]` type converts to
one implicitly, so the fix that covered `AddCodeFirstGrpcClient` should apply.

### Declined, with reasons — do not reopen without new information

- **nested/generic `[ProtoGrpc]` declarations** (`PBN4014`): refused cleanly; "does not work today,
  continues not to work". `ProtoModelGenerator` has the same case open as a TODO with a quieter failure,
  so if it is ever built, build both.
- **open generic contracts**: impossible, not declined — the emitted proxy is a non-generic type.
- **vendoring `xxHash128`**: ~2,000 lines against 80 for reflecting into the host. Synthesis is proven
  and recorded as the fallback if that reflection ever stops working.

### Where to be suspicious

**The interceptor goldens.** That area has produced two false-confidence bugs in one session: the
"expected errors" escape hatch silently recorded a `CS9137` failure as intended output, and a golden
embedded a machine-specific checksum that passed locally and failed CI. Both are fixed, but treat green
there as weaker evidence than elsewhere — `src/AotGrpcSmoke` is what actually proves that path, because
it runs.

### Standing verification recipe

Anything touching the generators should clear all of these before being called done:

``` sh
dotnet test src/BuildToolsUnitTests/BuildToolsUnitTests.csproj      # goldens rewrite then assert: run twice on new fixtures
dotnet build src/protobuf-net.BuildTools.Legacy/protobuf-net.BuildTools.Legacy.csproj
dotnet build Build.csproj -c Debug                                  # the traversal
dotnet test src/AotConformanceTests/AotConformanceTests.csproj
dotnet run --project src/AotDifferential/AotDifferential.csproj     # gates CI; must read 0 differing
dotnet run --project src/AotGrpcMetadataDiff/AotGrpcMetadataDiff.csproj    # gates CI; must read 0 failing
dotnet publish src/AotGrpcSmoke/AotGrpcSmoke.csproj -c Release -r win-x64   # then RUN the exe
```

Last known-good: 400 tests, 3080 contracts compared / 0 differing, twelve native checks, 4 IL warnings.

## What this is

An alternative to [#1255](https://github.com/protobuf-net/protobuf-net/pull/1255) and
[protobuf-net.Grpc#364](https://github.com/protobuf-net/protobuf-net.Grpc/pull/364) by Victor Irzak
(@virzak). Those are excellent work and the contract-shape classification is reused close to
verbatim, credited in the files that borrow it — including the three bugs their port surfaced
(explicit interface implementations must name the *declaring* interface; only `[SubService]` bases
are bound; `IObservable<T>` / `Stream` / `Grpc.Core`'s own call types must be refused rather than
treated as ordinary payloads).

What changed is the *shape*, and the reason is not taste.

## The finding that drove it

**A registry leaves the payload marshallers resolving by `Type` at run time, and that is exactly
what native AOT removes.**

`MarshallerCache.CreateMarshaller<T>` gates on `MarshallerFactory.CanSerialize(typeof(T))`, which for
protobuf-net is `TypeModel.CanSerialize(Type)` → `DynamicStub.CanSerialize` →
`Type.MakeGenericType(ConcreteStub<T>)` inside a `try { } catch { return NilStub.Instance; }`
(`src/protobuf-net.Core/Internal/DynamicStub.cs:124`). `NilStub.CanSerialize` returns **false**.

So under ILC every payload is reported as unserializable and the host dies at startup:

```
System.InvalidOperationException: Failed to bind 'SayHelloAsync' on IGreeter:
  No marshaller available for AotGrpcSmoke.HelloRequest
   at ProtoBuf.Grpc.Internal.MarshallerCache.GetMarshaller[T]()
```

That is a real native publish of `src/AotGrpcSmoke`, taken deliberately before the fix. **The proxies
being static does not help**: they can be perfectly AOT-safe while the bytes they carry are still
built by reflection. This is the half neither source PR closes, and it fails only at publish time.

Recorded because it is the kind of thing that reads as settled once fixed: the JIT run passed
happily at every stage, and `PublishTrimmed` would also have passed, because `MakeGenericType` still
works when a JIT is present. Only a native publish shows it.

## The shape

The consumer declares a type; the generator fills in the other half. Same idea as `[ProtoModel]`,
deliberately, so a consumer meets one concept rather than two.

``` c#
[ProtoGrpc(Model = typeof(MyModel))]
[ProtoService(typeof(IGreeter), typeof(GreeterService))]
public sealed partial class MyServices : ClientFactory { }
```

``` c#
var greeter = channel.CreateGrpcService<IGreeter>(MyServices.Instance);   // client
builder.Services.AddMyServices();                                        // server (generated)
app.MapGrpcService<GreeterService>();
```

Nothing resolves by `Type` at run time: no registry, no `[ModuleInitializer]`, no
`MakeGenericMethod`.

### There is no protobuf-net version matrix here, and that is structural

protobuf-net.Grpc references protobuf-net **2.4.8**, which NuGet reads as a floor rather than a pin, so
nothing holds a consumer on v2. BuildTools ships inside protobuf-net v3+ rather than separately, so the
generator only arrives with a modern protobuf-net in the first place.

But the reason no guard is needed is better than that, and worth keeping because it is easy to
re-litigate: **the surface gates itself**, in both directions.

- **With a `Model`**, the emitted marshaller uses `IMeasuredProtoOutput<IBufferWriter<byte>>` and
  `IProtoInput<ReadOnlySequence<byte>>`, which are v3-only. Reaching that emit at all requires the
  consumer to have written `[ProtoModel]` on a type and named it - and `[ProtoModel]` only exists in the
  protobuf-net that ships it. The gate is the consumer's own source, so it cannot be missing at the
  moment the generated code depends on it.
- **Without a `Model`**, the emitted code references **no protobuf-net API whatsoever** - not
  `IMeasuredProtoOutput`, not `IProtoInput`, not even `TypeModel`. It is `Grpc.Core` plus
  protobuf-net.Grpc's own `BinderConfiguration.Default`. Check `Diagnostics/NoModel.output.cs`: it
  compiles against any protobuf-net that protobuf-net.Grpc itself accepts.

So there is nothing to probe for and no down-level emit shape to maintain, unlike the C# language floor
(`PBN4000`), where a down-level consumer genuinely does reach code we would otherwise fail to emit.

### Facts verified against the real assemblies, not assumed

- **`ClientFactory` is already the seam.** It declares no constructor of its own, so an abstract
  class gets an implicit `protected` one and is externally derivable; `BinderConfiguration` and
  `CreateClient<TService>(CallInvoker)` are both `abstract`; and
  `CreateGrpcService<T>(this CallInvoker, ClientFactory?)` already accepts it. So the client half
  needs **no** protobuf-net.Grpc change at all.
- **`AddCodeFirstGrpc` registers its provider with `TryAddEnumerable`**, and
  `MapGrpcService<TImpl>()` calls *every* registered `IServiceMethodProvider<TImpl>`. A generated
  provider is therefore simply added alongside — no new runtime API. The generated registration calls
  `AddGrpc()` rather than `AddCodeFirstGrpc()`, so the reflective provider is never registered and
  there is nothing to double-bind against.
- **The three-arity server-method delegates are `Grpc.AspNetCore.Server.Model`'s, not
  `Grpc.Core`'s** — `Grpc.Core`'s own are two-arity, with no `TService`. Probed with reflection over
  the real package; the first emitter got this wrong.
- **`BinderConfiguration.Binder` was `internal`**, so generated code could not reach the metadata
  pipeline through it and had to use the public `ServiceBinder.Default` — which silently ignores a
  consumer's custom binder. It is **public from 1.3.6**, and the emitter now uses `__cfg.Binder`.
- **`BinderConfiguration.SetMarshaller<T>` is public**, and `GetMarshaller<T>` checks the cache
  first. That is what lets the generator sidestep `CanSerialize(Type)` entirely.
- **protobuf-net.Grpc 1.0.21 predates the `ClientFactory` shape** — its `CreateClient<TService>` is
  not even virtual, and the abstract member is
  `CreateClient<TBase, TService, TChannel>(TChannel)`. `Directory.Packages.props` was pinned there;
  the floor is **1.3.6** now. Found within minutes of building against the real package, and not
  findable from the surface snapshot — which is the argument for `AotGrpcSmoke` existing at all.

### Why naming the implementation matters

`[ProtoService(typeof(IGreeter), typeof(GreeterService))]`. `IServiceMethodProvider<TService>` is
generic in the **implementation**, so naming it closes the server generics at compile time. Without
it there is nothing to instantiate the provider with, which is why the registry approach needed
`MakeGenericMethod(typeof(TService))` with `IL3050` and `IL2060` suppressed. A client-only project
omits the implementation and simply gets no server bindings.

### Why the marshallers are pre-registered

`__CreateConfiguration()` calls `config.SetMarshaller<T>(...)` for every payload type, with
marshallers built directly on the model's `IMeasuredProtoOutput<IBufferWriter<byte>>` and
`IProtoInput<ReadOnlySequence<byte>>` (both public, both generic). This is **load-bearing, not an
optimisation** — it is the fix for the finding above.

Two details: the buffer-writer probe mirrors `ProtoBufMarshallerFactory`'s (the managed gRPC
implementation does not implement `GetBufferWriter`), and `Empty` is excluded because
`MarshallerCache` pre-registers it and it is not a contract in anyone's model.

**Do not pre-register onto `BinderConfiguration.Default`.** It is shared process-wide; when no model
is named the generated config *is* `Default`, so the pre-registration is skipped in that branch.

### Why the attributes were generator-owned (historical)

**They ship in protobuf-net.Grpc 1.3.0 now, and the emission is gone.** Kept because two parts of the
reasoning are still live: the post-init constraint, and why the transition was safe in both
directions.

`[ProtoGrpc]` / `[ProtoService]` used to be emitted per consuming assembly as `internal`, from
`RegisterPostInitializationOutput`. They **had** to be post-init rather than ordinary output, because
`ForAttributeWithMetadataName` can only see a generator's own post-init sources — that constraint is
permanent and worth remembering if a third trigger attribute is ever added.

This is the path `[ProtoModel]` took, and it is what kept this work off the critical path of a
protobuf-net.Grpc release. Dropping it is safe in both directions: an `internal` copy in the
consumer's own assembly wins name resolution over a `public` one in a referenced assembly, so a
consumer on an older BuildTools with a newer protobuf-net.Grpc — or the reverse — still compiles.

The generator matches these by **full name**, not by symbol, and that must stay: the unit-test
harness declares its own stubs (dodging the `[Experimental]` gate), exactly as the AOT serializer
generator's trigger attributes do.

## Layout

| file | what |
| --- | --- |
| `Generators/GrpcProxyGenerator.cs` | trigger, capabilities probe, the post-init attributes |
| `Generators/GrpcProxyGenerator.Parse.cs` | the model-level parse (new) |
| `Generators/GrpcProxyGenerator.ParseContract.cs` | contract/operation shape classification (borrowed, credited) |
| `Generators/GrpcProxyGenerator.Emit.cs` | emit; per-operation bodies borrowed, placement new |
| `Generators/GrpcProxyGenerator.Diagnostics.cs` | PBN40xx |
| `Internal/Grpc/GrpcContractModel.cs` | shape models (borrowed, credited) |
| `Internal/Grpc/GrpcModelPlan.cs` | the plan for one declared partial (new) |
| `BuildToolsUnitTests/Grpc/` | goldens + the runtime surface snapshot |
| `AotGrpcSmoke/` | the end-to-end proof |

Same rules as the serializer generator apply: **nothing in `Internal/Grpc/` may hold a Roslyn
reference** (equality would become reference-based, defeating caching, and the model would pin the
compilation graph alive), and the models are hand-written equatable values rather than records for
the same reason.

`Internal/**` is **globbed** by `protobuf-net.BuildTools.Legacy`, so `Internal/Grpc/` is explicitly
`Compile Remove`d there — the same trap `UseAotModelCodeFixProvider` hit. Generators are listed by
name in Legacy, so `GrpcProxyGenerator*.cs` is excluded naturally. Build Legacy after touching either.

## Running things

``` sh
dotnet test src/BuildToolsUnitTests --filter FullyQualifiedName~GrpcProxyGeneratorTests
dotnet run --project src/AotGrpcSmoke -c Debug

# the only thing that proves the actual goal
dotnet publish src/AotGrpcSmoke/AotGrpcSmoke.csproj -c Release -r win-x64
./src/AotGrpcSmoke/bin/Release/net8.0/win-x64/publish/AotGrpcSmoke.exe
```

`vswhere.exe` must be on `PATH` (`%ProgramFiles(x86)%\Microsoft Visual Studio\Installer`) or ILC's
link step fails with a mangled command line naming `link.exe`, which is misleading. Clear
`obj`/`bin` first — the publish is incremental and a second run reports nothing.

The goldens **rewrite themselves on every run** and then assert, so a new fixture fails on its first
run; re-run and review `git diff`. Don't hand-edit a golden to make a test pass.

`Grpc/Data/_ContractSurface.cs` is a hand-maintained snapshot of the runtime surface, compiled into
the goldens' input compilation. It cannot be a package reference: BuildTools *compiles in*
protobuf-net.Core's sources, so referencing protobuf-net.Grpc would make every type in Core
ambiguous. `ServiceContractAnalyzerTests` takes the same approach for the same reason. **Drift in the
snapshot is caught by `AotGrpcSmoke`, which uses the real packages** — and it has already caught one.

## The service name is bug-compatible on purpose

`GetDefaultServiceName` is a port of `ServiceBinder.GetDefaultName`, and it has to agree character for
character: the name is the wire contract, so a generated client that computes it differently from a
reflection-bound server does not find the service, and the failure surfaces as an
unimplemented-method error at call time with nothing pointing at the generator.

Every rule below was checked by calling `ServiceBinder.Default.IsServiceContract` against the real
1.3.6 package, not by reading the source — and three of them were wrong here until then:

| contract | binds as | notes |
| --- | --- | --- |
| `IGreeter` | `Ns.Greeter` | the ordinary case |
| interface named `Item` | `Ns.tem` | the "I" is stripped by a bare `StartsWith("I")` |
| `IBox<Request>` | `Ns.Box_Request` | arity suffix cut, arguments appended with `_` |
| `IBox<Renamed>` | `Ns.Box_the_payload` | argument names come from `GetDataContractName`, so `[ProtoContract(Name)]` wins |
| `[Service("tmpl.{0}.svc")]` on a generic | `tmpl.the_payload.svc` | an explicit name on a generic contract is a **format string** |
| a contract in the global namespace | `.GlobalNs` | `Type.Namespace` is null and is concatenated regardless |

The first and last look like slips in the runtime and are reproduced deliberately — "more correct"
here would only mean "does not interop". `ServiceNaming.input.cs` pins all four of the awkward ones.

## Known gaps

- **Seeding is manual.** `AotGrpcSmoke` lists `[ProtoSerializable(typeof(HelloRequest))]` by hand.
  The design settled on is below.

### Seeding, in both directions

The principle, which is worth keeping wider than the payload set: **if a consumer says "use this
model", we should check that we think it is going to work.** Naming a model is a claim about
something in another file - possibly another assembly - and every part of that claim is checkable at
compile time. The failure it prevents is the one this whole feature exists for: a build that succeeds,
a JIT run that succeeds, and a native publish or a first call that does not.

`ProtoModelGenerator` cannot read this generator's *output* — no generator sees another's — but
`[ProtoGrpc]` and `[ProtoService]` are ordinary symbols, so it can re-derive the payload set itself: a
second `ForAttributeWithMetadataName` on `[ProtoGrpc]`, filtered to declarations whose
`Model = typeof(...)` names this model, then the unique set of request and response types across their
contracts' operations. Both generators are in **one assembly**, so the payload extraction can and
should be *shared code* — if the two ever disagree, the proxy calls `SetMarshaller<T>` for a `T` the
model does not have, and the build stays green while the marshaller goes reflective.

Two directions, depending on where the model lives:

- **In this compilation** — infer the seeds, i.e. behave as though `[ProtoSerializable(T)]` were
  written for each payload type; and warn if the named model is not marked `[ProtoModel]` at all,
  which today fails as a bare CS0117 on the generated `Instance`.
- **In a referenced assembly** — nothing can be added to it, so *check* it instead, and warn per
  payload type it cannot serialize.

**For the second direction, read the emitted `ISerializer<T>` set, not `[ProtoSerializable]`.** A
generated model contains a nested `private sealed class ProtoBufGeneratedServices : ISerializer<T1>,
ISerializer<T2>, …`, and Roslyn sees private nested types through metadata. That set is what the model
*can actually do*, where the attribute is only what it was *asked* to do — the two differ in both
directions, and the attribute gets both wrong:

- a seed that was dropped (`PBN3001`) still carries its attribute, so an attribute check passes for a
  type that will throw at run time;
- a payload reached transitively as a *member* of another seed needs no attribute of its own, so an
  attribute check **false-positives** on a model that is completely fine.

Match on the interface rather than the type name, since `ProtoBufGeneratedServices` is a private
implementation detail; and accept `ISerializerProxy<T>` too, which is what enums and hand-written
serializers get instead. Stay silent when the model carries no `[ProtoModel]`, since a hand-written
`TypeModel` cannot be inspected this way and is not ours to judge; warn when it has `[ProtoModel]` but
no services type, which means the generator did not run there.

`Empty` is excluded from all of this, per the gap noted above.

A further point in the interface check's favour, and the reason the `[Conditional]` question below
turned out to be moot: **it takes no dependency on attribute retention.** An attribute-based check
would have needed `[ProtoSerializable]` to survive into metadata, and would then have quietly relied
on that forever. Reading the emitted interfaces relies only on the code the generator actually wrote.

For the record, since it was asked and the answer is easy to mis-remember: neither
`ProtoModelAttribute` nor `ProtoSerializableAttribute` is `[Conditional]`, so both do reach metadata —
but **nothing in this design relies on that**, and no comment in the source claims otherwise. The one
place metadata survival buys anything is `[ProtoModel]`, used to tell "a hand-written `TypeModel`,
not ours to judge" from "a generated model whose generator did not run" — and if that ever stopped
being visible, the fallback is to stay silent in both cases, losing one warning rather than
misreporting.

`PBN4012` and `PBN4013` are the two ids.

**Built, and the proof is `src/AotGrpcSmoke` having no `[ProtoSerializable]` at all.** Its model is
seeded entirely from `[ProtoService]`, and the native publish still passes all five checks at 4 IL
warnings and 14,480,384 bytes - both unchanged, so seeding costs nothing. A JIT run would not have
proved it; if seeding regressed, the marshallers would fall back to the reflective model and only the
native leg would fail.

Two things about the implementation worth keeping:

- **The compilation walk is gated on `[ProtoGrpc]` existing at all**
  (`compilation.GetTypeByMetadataName(...) is null` returns immediately), so a consumer who has never
  heard of protobuf-net.Grpc - which is most of them - pays one lookup. Same principle as
  `ProtoBufDisableBuildTools`.
- **It walks only this assembly's types.** A `[ProtoGrpc]` in a referenced assembly names its own model
  and has nothing to say about this one, so the walk is bounded by the project rather than by its
  dependency graph.

**The golden fixtures lit up, exactly as predicted, and that is the check working.** Every `Grpc/Data`
fixture declares a hand-written stand-in model, because only `GrpcProxyGenerator` runs in that harness -
so `PBN4012` fired on all thirteen of them. Resolved per fixture rather than wholesale: `Basic.input.cs`
**keeps** its warning, as the demonstration on the canonical shape, and its golden `.txt` records the
message; every other fixture marks its stand-in `[ProtoModel]`, so its goldens stay about whatever it is
testing. Marking the stand-in is enough on its own - the check is on the attribute, so the serializer
generator does not need to have run.

Note the seeding side is invisible to *both* golden harnesses - the AOT goldens run
`ProtoModelGenerator` with no protobuf-net.Grpc surface in the compilation, and the gRPC goldens run
`GrpcProxyGenerator` against a stand-in model. Seeding is the seam between them, hence
`GrpcSeedingTests` and `GrpcReferencedModelTests`; the latter needs genuinely separate compilations,
since the whole question is what can be discovered about a model through metadata.
- **One reflective call left on the server path**:
  `__cfg.Binder.GetMetadata(typeof(IFoo).GetMethod(name, parameterTypes)!, …)`, needed to preserve
  `[Authorize]`-style endpoint metadata. It survives the native publish. **Replacing it with a
  compile-time reconstruction is not merely unbuilt — it cannot be done exactly**; see below.
- ~~Neither of the two rules `ProtoModelGenerator` is held to is enforced here.~~ Both are now:
  `GrpcModelPlanShapeTests` and `GrpcProxyGeneratorIncrementalTests`. Two things worth keeping from
  writing them:

  - the shape test **found something**. `Internal/Grpc` was holding a `DiagnosticDescriptor` and a
    `Location` where `Internal/Aot` holds neither. Neither was actually harmful — descriptors are
    static singletons, and the location was detached at construction — but "harmless for reasons you
    have to reconstruct" is what drifts, so the model now stores a `GrpcDiagnosticKind` and a
    `PlanLocation` like the serializer generator's, and the test needs no exceptions.
  - the incremental pair was **verified able to fail**, in both directions, by sabotaging
    `GrpcModelPlan.Equals`: always-false breaks the cached test, always-true breaks the not-cached one.
    Worth redoing if that file changes shape, and note the first attempt at the sabotage was a no-op
    (`if (other is null) return true` leaves the real comparison intact) and both tests passed — which
    is exactly the false reassurance the exercise is meant to catch.
- ~~A contract with no recognised operations is dropped silently.~~ Now `PBN4009`, which took the gap
  in the middle of the block. It fires only where nothing was *refused* either, since a contract with a
  bad member already reports the more specific `PBN4002`.

  The run-time message went with it: `CreateClient<T>`'s throw used to say "add
  `[ProtoService(typeof(X))]` to it", which is the worst possible advice for the consumer who did
  exactly that and had the contract dropped - it sends them to inspect the one thing that is fine. It
  now names both causes and points at the build log.
- **`PBN4002` is per-method but takes the whole contract**, which the message says; what it cannot yet
  say is anything about *fixing* the method beyond naming why the shape was refused.
- **4 IL warnings** on a native publish against protobuf-net.Grpc 1.3.6, down from 100 — see "The
  two `RuntimeTypeModel` roots" below for why it was all-or-nothing. All four are per-assembly
  rollups; nothing is attributed to generated code. If more ever appear, use
  `/p:IlcGenerateDgmlFile=true` and walk *incoming* edges rather than guessing, per
  `aot-findings.md`.
- **No interceptors yet.** See below.

### The marshaller's two arms, and the SerializationContext state machine

The generated `__Serialize<T>` measures once, calls `SetPayloadLength`, then tries `GetBufferWriter()` -
writing straight into it and calling `Complete()` if it gets one, or serializing to an array and calling
`Complete(byte[])` if it does not.

**Calling `SetPayloadLength` on both arms is valid, and the reason is worth recording** because it looks
wrong: `SetPayloadLength` is legal only from `Initialized`, and `Complete(byte[])` is *also* legal only
from `Initialized`. It works because `SetPayloadLength` records the length and **does not advance the
state** - only `GetBufferWriter()` does, to `IncompleteBufferWriter`, which is in turn the only state
`Complete()` accepts. Checked against both grpc-dotnet implementations, which have identical state
machines: `Grpc.Net.Client`'s `GrpcCallSerializationContext` and `Grpc.AspNetCore.Server`'s
`HttpContextSerializationContext`.

The order also cannot be otherwise: `GetBufferWriter()` consults `_payloadLength` to decide whether
direct serialization is possible, and where it is, writes the header *immediately* from that value. So
the length has to be set before asking for the writer, and the bytes written afterwards must match it
exactly - which they do, since both come from the same `Measure` call.

**Every real context supplies a buffer writer, so the array arm never runs in the smoke test's actual
calls, on either side.** It is driven directly instead, by a `SerializationContext` that throws
`NotImplementedException` from `GetBufferWriter` - which is what the base class does, and is the reason
the generated code catches it. That fake also throws from `Complete()`, so the check fails loudly if the
wrong arm is taken rather than passing quietly.

### Compile-time metadata: closed, not pending

This was on the next-steps list twice, deferred each time on the grounds that
[#369](https://github.com/protobuf-net/protobuf-net.Grpc/pull/369) was still moving. That was the wrong
reason. Probing `ServiceBinder.Default.GetMetadata` against the real 1.3.6 package — rather than reading
its source — shows the list **cannot be reproduced exactly by generated code at all**:

```
IThing.GetAsync / IThing / ThingService  (8)
    ProtoBuf.Grpc.Configuration.ServiceAttribute
    NotInheritedAttribute
    MarkAttribute                                    == Mark(contract-iface)
    MarkAttribute                                    == Mark(contract-method)
    System.Runtime.CompilerServices.NullableContextAttribute      <-- the blocker
    MarkAttribute                                    == Mark(impl)
    MarkAttribute                                    == Mark(impl-method)
    System.Runtime.CompilerServices.NullableContextAttribute
```

`NullableContextAttribute` is **synthesised by Roslyn into each assembly as an `internal` type**, so
generated code in the consuming assembly cannot construct the contract assembly's copy. It appears
whenever a member carries nullable reference annotations, i.e. in almost all modern code. The metadata
list holds attribute *instances*, so "emit the list" means emitting `new SomeAttribute(...)` for each
one — and that is impossible for this one, and for any attribute whose type or constructor is not
accessible to the consumer.

Gating on constructibility would technically work and is not worth building: the gate would fail on
nearly every real operation, so the reflective call would remain on the path it was meant to leave.

Two further things the probe settled, both of which a compile-time version would have had to reproduce
and neither of which is guessable:

- `GetCustomAttributes(inherit: true)` on an **interface** does not walk base interfaces —
  `IThing : IAudited` yields `Mark(contract-iface)`, `NotInherited`, `Service`, and *not*
  `Mark(sub-service-iface)`. So the four sources are genuinely four, and "ask the contract type" loses
  one.
- on the **implementation** side it resolves through the interface map and then honours virtual
  inheritance: for the sub-service operation the list carries `Mark(base-virtual)` from the overridden
  base method, and not `Mark(impl-method)`.

**What came of the investigation instead** is a bug fix. The call was
`typeof(IFoo).GetMethod(name)`, and `Type.GetMethod(string)` throws `AmbiguousMatchException` as soon as
a contract carries two operations of the same method name — legal, since `[Operation]` gives them
distinct names on the wire. That compiled cleanly and failed at server startup. It now passes the
parameter types, which `GrpcOperationModel` already carried. `Overloads.input.cs` pins it, along with
the reason the typeof list cannot reuse the signature rendering: `typeof(Foo?)` is CS8639 for a
reference type, while the annotation is part of the type for `Nullable<T>`.

### Compile-time metadata: measured, and parked

Previously recorded here as closed-because-impossible. That was wrong, and the correction matters:
endpoint metadata is **how authorization is enforced** in gRPC for .NET, so `[Authorize]` arriving as a
real instance is a security property, not a fidelity nicety. "Cannot be reproduced exactly" is the wrong
place to stop; the bar is **reproduce what carries meaning, and be loud about the rest**.

`Internal/Grpc/AttributeRenderer.cs` is step 0 and is **done** - it renders an `AttributeData` to
constructing source, or returns a reason.

**Then it was measured, and the reason for building it went away.** `AotGrpcSmoke` carries attributes at
all three levels — contract method, implementation class, implementation method — plus a real
`[Authorize(Roles = "admin")]`, and a `win-x64` native publish delivers **every one of them**, with
`[Authorize]` arriving as a constructed instance carrying its arguments. 4 IL warnings, unchanged. ILC
keeps them because the generated binding already roots the declaring interface and the implementation
with `typeof(...)`, and attribute metadata on a rooted type is preserved.

So this entry has now been wrong in both directions: first closed as impossible, then reopened as
required. What it actually is: an **optimisation** that removes one startup-time reflective call. And it
has a real cost, which is why it is parked rather than merely deprioritised — `ServiceBinder.GetMetadata`
is `virtual` and `BinderConfiguration.Create` accepts a custom binder, so emitting a constructed list
silently ignores a consumer's override. That is precisely the bug the comment at the emit site was
written to prevent, and reintroducing it to avoid a reflective call that demonstrably works is a bad
trade.

Two smaller things the measurement settled, both worth keeping:

- **The metadata genuinely reaches ASP.NET Core.** The first JIT run failed with *"Endpoint gRPC -
  /AotGrpcSmoke.Greeter/SayHello contains authorization metadata, but a middleware was not found that
  supports authorization"* — i.e. the framework saw and enforced the `[Authorize]` that arrived through
  the generated binding. The fixture now puts it on a **bound but never invoked** operation, since
  binding is when metadata is collected and invoking it would need an auth stack irrelevant to the test.
- **`NullableContextAttribute` really is in the list** (the oracle reports two, and now says so rather
  than filtering silently) — so the old *fact* was right even though the conclusion drawn from it was
  not. They are unconstructable from another assembly and ASP.NET Core does not consume them, so
  dropping them loses nothing; that was already the renderer's deliberate position.

**What was built before the measurement is kept**, and is not wasted: the oracle is the only precise,
executable record of what the runtime's metadata semantics actually are, it gates CI, and it found three
things nobody would have derived by reading (below). If the emit is ever wanted, it is now checkable.

**Steps 1 and 2 are now done**, and the gather still has no caller in the generator - so the branch
emits the reflective call and behaviour is unchanged. What exists:

1. **The differential harness** - `src/AotGrpcMetadataDiff`, described in its own section below. It
   reconstructs each endpoint's metadata from symbols, **compiles and runs it**, and compares the
   resulting objects against `ServiceBinder.GetMetadata` property by property. It gates CI.
2. **The gather** - `Internal/Grpc/MetadataGather.cs`, reproducing `GetMetadata`'s four sources **in
   order** (contract type, contract method, service type, service method), because the consumer treats
   *later as higher priority*, so the order is semantic and not just the set. Both traps are handled and
   both are now pinned by the harness rather than by reading:
   - `GetCustomAttributes(inherit: true)` **does not walk base interfaces**, but does walk base classes
     and overridden methods; `[AttributeUsage(Inherited = false)]` opts out; non-`AllowMultiple`
     attributes dedup most-derived-wins.
   - the implementation side resolves through `FindImplementationForInterfaceMember`, keyed on the
     **declaring** interface - which is what protobuf-net.Grpc#369 taught the runtime, so the two agree.
3. **`PBN4019`** for anything `AttributeRenderer` reports as unsupported, naming the attribute *and* the
   operation. This is what converts today's silent risk into a build warning.
4. **Emit**, replacing `__cfg.Binder.GetMetadata(...)` with the constructed list. The harness is what
   makes this checkable, and it is in place, so this is now unblocked.

Skipped silently, deliberately: the compiler-synthesised `NullableContext` family. Unconstructable from
another assembly by construction, not consumed by ASP.NET Core, and present on nearly every annotated
member - so warning about them would train a reader to ignore `PBN4019`.

### The endpoint-metadata oracle (`src/AotGrpcMetadataDiff`)

The differential for metadata, and the counterpart of `AotDifferential`: that one proves the generated
serializer agrees with ref-emit **on bytes**, this one proves the reconstructed endpoint metadata agrees
with the runtime binder **on objects**.

It compares live instances, not rendered strings. The compile-time side is gathered from symbols,
rendered by `AttributeRenderer`, **compiled and executed**, and the resulting objects compared to
`ServiceBinder.GetMetadata`'s property by property. Nothing weaker tests the gather and the renderer
together; a string comparison would pass on an expression that does not compile, and a set comparison
would pass on a list in the wrong order — which is the case that matters, since the consumer treats
later as higher priority.

The fixture is compiled **twice, deliberately**: once by the SDK, giving the live types the reflective
side needs, and once by Roslyn in-process, giving the symbols the compile-time side needs. That is why
`Fixtures/*.cs` is both `<Compile>` and `<Content>`.

Three things about the plumbing are load-bearing, and all three are the same trap wearing different
clothes — BuildTools compiles protobuf-net.Core's sources in, so it collides with the real Core that
protobuf-net.Grpc brings:

- the project reference to BuildTools carries **`Aliases="buildtools"`**. `AotDifferential` avoids the
  collision by loading BuildTools reflectively and talking to it only through Roslyn's interfaces; we
  cannot, because we call `MetadataGather` and `AttributeRenderer` directly. An `extern alias` is the
  other answer, and this repo already knows the machinery (see "Identifiers" in `AGENTS.md`);
- the **run-time** reference set drops `protobuf-net.BuildTools.dll` for the same reason. An alias is
  not available there, since those references are assembled from `TRUSTED_PLATFORM_ASSEMBLIES`;
- the harness's **own** assembly is excluded when compiling the fixtures for symbols (it already
  contains those types, so every one would be ambiguous with the copy being compiled) and included when
  compiling the rendered metadata, which has to name them.

**It is proven able to fail**, by mutation rather than by observing green:

| mutation | caught as |
| --- | --- |
| drop the per-group reversal | `item 0 is ServiceAttribute, expected SingletonAttribute` — note the *multiset is identical*, so only an ordered comparison catches it |
| ignore `[AttributeUsage(Inherited = false)]` | `expected 12 item(s), reconstructed 16` |

Three things it established that were not obvious, each recorded where it applies:

- **`[Authorize]` cannot go on a contract interface at all.** Its `AttributeUsage` is `Class | Method`,
  so `[Authorize]` on a `[Service]` interface is CS0592. The routes that exist are the contract
  *method*, the implementation *class* and the implementation *method* — worth knowing, since the
  obvious mental model of "annotate the service contract" does not compile.
- **The `Inherited = false` rule is load-bearing for every class-implemented endpoint**, not merely for
  types that use it: the class walk ends at `System.Object`, which carries `[TypeForwardedFrom]`,
  `[ComVisible]` and `[ClassInterface]`, and only their `Inherited = false` keeps all three out of the
  metadata. Found by disabling that test and reading what appeared, which is also the +4 above.
- **#369 is in no released package.** 1.3.6 predates it, so the pinned package still misses
  implementation-method attributes for an operation declared on a `[SubService]` base. We target
  `main`'s behaviour deliberately — it is about to be released — so the harness reports that difference
  as an *explained divergence* rather than failing it, while anything unexplained exits non-zero. The
  old behaviour is asked of the gather itself (`resolveInheritedImplementation: false`) rather than
  filtered out of its output, so a wrong model of it fails rather than quietly agreeing with itself.

Run it with `dotnet run --project src/AotGrpcMetadataDiff/AotGrpcMetadataDiff.csproj`. Last known-good:
**2 operations compared, 0 failing, 1 explained divergence.**

### Metadata parity, and protobuf-net.Grpc#369 — landed, and we needed nothing

We deliberately delegate metadata to `GetMetadata` rather than computing it, so whatever the runtime
does, we do. The thing to re-check was
[protobuf-net.Grpc#369](https://github.com/protobuf-net/protobuf-net.Grpc/pull/369), which changed what
`GetMetadata` returns for an operation inherited from a `[SubService]` base. **It merged on 2026-08-17
and required no change here** — but the reason is worth keeping, because it is a property of our emit
rather than luck.

What it actually landed as is narrower than the version reviewed earlier, and better: it touches only
`GetMethodImplementation`, changing `GetMap(contractType, serviceType)` to
`GetMap(serviceMethod.DeclaringType ?? contractType, serviceType)`. `GetMetadata`'s
`contractTypeAtt = contractType.GetCustomAttributes(...)` is untouched, so implementation attributes are
**added** rather than the contract type's being swapped away — which was the regression worth fearing.
`Issue330.cs` pins it: all three expectations begin with `ContractType`.

**Why our emit is already correct, on both axes.** The fixed lookup keys on the method's
`DeclaringType`, and the generated bindings pass exactly that, while keeping the *top-level* contract as
`contractType`:

``` c#
var __meta = __cfg.Binder.GetMetadata(
    typeof(IAudited).GetMethod("WhoAmIAsync", …)!,   // the DECLARING interface
    typeof(IThing), typeof(ThingService));            // the top-level contract, and the implementation
```

Pass the declaring type as `contractType` instead and the top-level attributes would be lost; pass the
top-level type as the method's owner and the map would not resolve. Both are right, and
`SubService.output.cs` is what pins them — every emitted `GetMethod` in the golden set uses its declaring
type, checked across all fixtures rather than for the one that motivated it.

The package floor stays at **1.3.6**: we do not depend on the fix, we benefit from it, so a consumer who
wants implementation-level attributes on sub-service operations wants whatever release carries it, and
everyone else is unaffected.

One deliberate limit, recorded so it is not mistaken for a bug: the sub-service interface's own
*type-level* attribute is still not collected. That is consistent with
`GetCustomAttributes(inherit: true)` not walking base interfaces — probed rather than assumed — so it is
existing semantics rather than something #369 introduced.

**This union is what compile-time metadata has to reproduce exactly**, and the differential described
above now runs precisely that comparison: a contract with a `[SubService]` base carrying an attribute at
each level, reconstructed and compared against `binder.GetMetadata(...)` item by item. The failure mode
is why it was built before the emit rather than after: `[Authorize]` going missing produces no error,
just a more permissive endpoint.

### Built, and where it can be tested

The interceptor half is now emitted. Shape as designed: one generic method per *receiver overload*, one
`[InterceptsLocation]` per call site, and the body is the runtime overload's own line with our factory
substituted. `AotGrpcSmoke` is the proof - it enables the namespace and calls
`channel.CreateGrpcService<IGreeter>()` with **no factory argument**, which under a native publish can
only work if the call was rewritten, since the reflective default is what ILC removed. Eight checks
became ten, still 4 IL warnings, 14,494,720 bytes.

Two things fell out that are worth keeping:

- **The factory is the `[ProtoGrpc]` type, not the serializer model it names.** These are different types
  - one derives `ClientFactory` and has `CreateClient`, the other is a `TypeModel` - and conflating them
  is `CS0019` on the `??`, which is exactly how it was caught on the first run. Emitted whether or not a
  `Model` was named, since pointing the call at the generated factory avoids `ProxyEmitter` either way;
  whether that factory has a good marshaller source is `PBN4010`'s business.
- **The receiver overload is detected, and both are needed.** `GrpcChannel` derives from `ChannelBase`, so
  the everyday call takes that overload and the emitted body has to add `CreateCallInvoker()`. A test that
  only exercised `CallInvoker` would have missed the shape people actually write.

**Which route: reflection, not synthesis - and the distinction is worth being precise about.** Proving the
encoding by hand settled the question that mattered, which was whether the *shipped* Roslyn reference had
to move: it does not. The implementation then calls `GetInterceptableLocation` reflectively off the host
anyway, because it is ~80 lines against ~2,000 for a vendored `xxHash128`, and because it tracks whatever
encoding the compiler prefers rather than pinning version 1. Synthesis stays the recorded fallback, proven
to work, if that ever fails.

The one cost of reflection was testability - `GetInterceptableLocation` is Roslyn 4.11+, and
`InterceptableLocations.IsSupported` is false below it, so the unit tests saw no interceptor however they
were configured. The lever for that is the **test** project's own Roslyn override, which already existed
purely so the tests could parse C# 12; it is now 4.11.0. The shipped baseline is untouched, which is the
whole point of going through reflection.

So the interceptor path *is* golden-tested, by `Intercept.input.cs` plus a `.interceptors` sidecar holding
the namespace list - the same convention as `.langver`, and for the same reason: the switch is
per-project, so it cannot be expressed inside the fixture. The golden pins the two things that matter:
two methods, one per receiver overload with `CreateCallInvoker()` on the `ChannelBase` one, and **two**
`[InterceptsLocation]` attributes across three call sites, because the one already passing a factory is
left alone. It also *compiles*, which is the check that the namespace and feature detection agree with the
compiler.

The snapshot gained `GrpcClientFactory` and `ChannelBase` to make that possible - and deliberately both
overloads, since a snapshot carrying only `CallInvoker` would let a generator that never matched real code
pass its goldens.

### All five method shapes now run natively

The goldens *compile* unary, server-streaming, client-streaming, duplex and void; for a while
`AotGrpcSmoke` only *ran* three of them. Client-streaming and duplex each reach their own `Reshape`
helper on the client and their own `AddXxxMethod` on the server, so neither was known-good under ILC -
merely unmeasured, which is the distinction `aot-findings.md` insists on for `AotSmoke` and which applies
just as much here.

Both are now exercised, with three distinct values in the request stream so that a shape which dropped or
duplicated one shows up in the totals rather than passing plausibly: `a+b+c / 6` for the client-streaming
sum, `2,4,6` for the duplex echo. Twelve checks, still 4 IL warnings, 14,585,856 bytes.

## PBN4015: you asked for AOT and have not squared the circle

The counterpart of the serializer generator's `PBN3012`, and worth having separately because the gRPC
failure is *further* from the developer: switching on `PublishAot` changes nothing at build time, the JIT
run keeps working, and the first sign of trouble is a native publish or a startup that cannot bind. Both
halves reach reflection without a model - the proxies through `ProxyEmitter`, the payloads through
`MarshallerCache` - so there is nothing to fall back to.

**The trigger is consumer-side *usage*, not the presence of service contracts, and that is load-bearing.**
Shipping `[Service]` interfaces in a shared package is the recommended layout, and such a package needs no
`[ProtoGrpc]` of its own - its consumers do. Triggering on declarations would nag hardest at exactly the
project that is laid out correctly. So the two things that mean "this is a client or a server" are what
count: a **plain `CreateGrpcService<T>`** (one already passing a factory is left alone - that consumer has
done what we would ask), and the server's **`AddCodeFirstGrpc`**. The server case is the one that
justifies the diagnostic on its own, since a server-only project has no client call site to flag.

A warning rather than an error, for `PBN3012`'s reason: switching on `PublishAot` should not break a build
on the spot.

The "does this project ask for AOT" probe is now shared with `AotMigrationAnalyzer`
(`Utils.AsksForAot`) rather than duplicated - it is a list of four MSBuild properties that would drift the
moment the SDK grew a fifth. Note that writing this found `PBN3012`'s own AOT-configured path had **never
been tested**: `AnalyzerTestBase` had no way to supply build properties at all, so those code paths could
be read but not run. It has one now.

## PBN4018: a drop means something different under AOT

Every drop diagnostic says "the runtime proxy will be used", which is accurate and proportionate when
there *is* one: on a JIT build the contract keeps working, just reflectively. Under `PublishAot` there is
no runtime proxy - `ProxyEmitter` needs ref-emit and the marshallers need `MakeGenericType` - so the same
contract does not degrade, it throws on first use. `PBN4018` says that, per dropped contract, only when
the project asks for AOT or trimming.

Three choices in it worth keeping:

- **a separate id rather than raising the drop's severity.** Severity belongs to the descriptor and cannot
  vary per report, so escalation is not available anyway - but the separate id is better regardless, since
  it can be suppressed on its own by someone who knows a particular contract is never reached on the AOT
  path;
- **it adds rather than replaces.** The original `PBN400x` still fires and still says *why* the contract
  was dropped; this one says what that now costs. Both, because they answer different questions;
- **anchored on the `[ProtoGrpc]` declaration**, not the contract - that is the file the consumer has to
  change, and the drop diagnostic is already sitting on the contract explaining itself.

Mechanically it is a set difference: the parse knows which contracts were *named* and which reached the
plan, so the rest are dropped whatever the reason - which means new drop reasons are covered without
touching this. The AOT question cannot be answered there, though, because build properties are a different
incremental input; so the names travel on the candidate as plain strings and the diagnostics output, which
has the capabilities, decides. `Utils.AsksForAot` is shared with the two migration analyzers rather than
re-listing the four property names, and the test covers all four.

## PBN4017: the DI client path, where the seam is the container

`AddCodeFirstGrpcClient<T>` - the mainstream ASP.NET Core way to get a gRPC client - was the one route
with no coverage *and* no warning, which is the worst combination we had. It turns out to need neither an
interceptor nor a per-call-site fix, because of what it does:

``` c#
// ConfigureCodeFirstGrpcClient<T>, which all six AddCodeFirstGrpcClient overloads funnel through
=> clientBuilder.ConfigureGrpcClientCreator(
    (services, callInvoker) => CreateGrpcService<T>(callInvoker, services.GetService<ClientFactory>()));
```

It resolves the factory **from the container**, so one registration covers every client in it. And that is
the *only* place protobuf-net.Grpc resolves a `ClientFactory` from DI - checked, not assumed - so there is
exactly one lever:

``` c#
services.AddSingleton<ClientFactory>(MyServices.Instance);
```

**The generated `AddMyServices()` now does it for you**, with `TryAddSingleton`. Two details there:
`TryAddSingleton` and not `TryAddEnumerable` - this is one service, where the `IServiceMethodProvider`
registrations beside it are a set, and using the collection form for a single service is a semantic
mistake that happens to compile; and `Try` rather than `Add`, so a consumer who registered their own
choice keeps it. A server project therefore gets DI-registered clients onto the build-time proxies with
no second step to remember.

`PBN4017` covers the client-only project, which has no `AddMyServices()` to call. It fires on
`AddCodeFirstGrpcClient<T>` where a model covers `T` and no registration is visible, and its suppression
check is worth knowing the shape of, because it is a dynamic question answered statically:

- **it is biased toward silence.** Anything that looks like a `ClientFactory` registration anywhere in the
  compilation quiets it. A registration in another assembly, or built by a helper, is invisible - so it can
  miss, which is much better than nagging someone who has already done it;
- **it skips generated trees**, and that is load-bearing rather than tidy: the generated `AddMyServices()`
  *contains* a registration, so counting it would suppress the suggestion for every project - including one
  that never calls `AddMyServices()`;
- **the registration methods are named explicitly rather than matched by prefix.** The first cut matched
  `Add*`/`TryAdd*` and suppressed the very diagnostic it was guarding: `AddCodeFirstGrpcClient` starts with
  "Add" and, fully qualified, contains the string "ClientFactory" - because the namespace *is*
  `ProtoBuf.Grpc.ClientFactory`. The test caught it; reading would not have.

Note for the parked server-reflection item: `protobuf-net.Grpc.AspNetCore.Reflection` resolves a
**`BinderConfiguration`** from DI, which is the second and last DI seam. A `[ProtoGrpc]` type converts to
one implicitly, so the same trick should apply when that is picked up.

## PBN4016 and its fixer: the ordinary-C# equivalent

The counterpart of `PBN3010` for the gRPC half, and it exists for the identical reason: declaring a
`[ProtoGrpc]` does not move existing call sites onto it, and a plain `CreateGrpcService<T>` keeps working
through `ProxyEmitter`, so nothing complains until a publish.

Two things distinguish it from `PBN4015`:

- **no AOT request is required.** A project that has built a proxy for this contract and is not using it
  is paying ref-emit for nothing, whatever it publishes as;
- **it is silent once interceptors are enabled**, because then the generator has taken the call site over
  and there is nothing to ask for. That check reads the syntax tree's own parse options, which is where
  the feature lands.

The fix is one argument - `CreateGrpcService<T>()` becomes `CreateGrpcService<T>(MyServices.Instance)` -
and that is the point rather than a limitation: the explicit form and the interceptor produce the *same
program*, so the fixer is exactly what interception would have done, available to anyone who has not
enabled the feature. Which is also what keeps the magic honest.

Three details worth keeping, two of them caught by the test rather than by design:

- **The diagnostic carries two spellings of the same names.** The `factory` property is fully qualified,
  because the fixer parses it into an expression and cannot know what is in scope at the call site; the
  *message* uses the readable form, because `global::MyServices` in prose is noise a reader has to
  mentally strip. The first cut leaked the qualified form into the message.
- **The fixer appends unless there is an explicit `null` to replace**, which covers all four shapes the
  call can take. The first cut assumed the reduced (extension) form and replaced the argument list
  wholesale - so `GrpcClientFactory.CreateGrpcService<T>(invoker)`, the static form, lost its receiver.
- **It is `Compile Remove`d from `protobuf-net.BuildTools.Legacy`**, because `CodeFixes/**` is a glob
  there while analyzers are listed by name - the asymmetry that made `UseAotModelCodeFixProvider` a build
  break. `GrpcMigrationAnalyzer` is not in Legacy, so its fixer must not be either.

## Interceptors

The plan for the zero-code-change half. Probed with a scratch generator before proposing, and every
clause below was verified rather than recalled:

- a **generic extension-method call can be intercepted** with the type argument fixed at the site —
  the interceptor must match arity and signature (receiver included), and at that one site `TService`
  is the concrete contract;
- interception is **per call site**, so two contracts and the same contract twice each get their own;
- a site whose type argument is an **open `T`** (from an enclosing generic) is not interceptable, and
  falls through to the runtime path — the natural filter is `arg is not INamedTypeSymbol`;
- **`InterceptorsNamespaces` can be supplied from our own `.props`**, so it really is zero consumer
  change;
- `InterceptsLocationAttribute` can be `file`-scoped and generator-declared, so the interceptor half
  needs **nothing** from protobuf-net.Grpc.

Two gotchas that cost time: `m.Parameters[0]` on a *reduced* extension method is the first real
parameter, not the receiver (use `ReducedFrom`), and the `[InterceptsLocation(path, line, char)]` form
is gone — replaced by `[InterceptsLocation(int version, string data)]`, produced by
`SemanticModel.GetInterceptableLocation()` + `GetInterceptsLocationAttributeSyntax()`, which are
**Roslyn 4.11+**.

### ...but the baseline does not have to move

Recorded here because the first read of this was "raise the Roslyn reference", which would give up what
keeps `protobuf-net.BuildTools.Legacy` serving old SDKs.

**The version-1 `data` encoding is fully specified, and was reproduced exactly.** Per
[`docs/features/interceptors.md`](https://github.com/dotnet/roslyn/blob/main/docs/features/interceptors.md#interceptslocationattribute)
it is base64 of three fields, and
[`InterceptableLocation1`](https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Utilities/InterceptableLocation.cs)
writes them in order: the 16-byte checksum, an `int32` position, then the UTF-8 display file name.
Verified against the real API by building both and comparing strings, which pinned the two details the
spec leaves implicit — and got both wrong on the first attempt:

| field | what it actually is |
| --- | --- |
| checksum | `xxHash128` of the text as **UTF-16 code units**, *not* UTF-8 — "content checksum of the file" reads like bytes-on-disk and is not |
| position | the **name node's** `Position` (i.e. `FullSpan.Start`), not the invocation's — for `"x".Trim()` it is `Trim`'s position, and the invocation's is 4-12 characters earlier |
| display name | `Path.GetFileName(tree.FilePath)`, checked across an absolute, a nested and a relative path |

Only the **checksum and position** have to be right: the compiler decodes the data and matches on those,
using the display name for diagnostics alone. And a wrong value is a *compile error* in the consumer's
project, not a silent miss — an unpleasant failure but a loud one.

`xxHash128` is the only piece Roslyn 4.3.1 does not give us, and **Roslyn vendors its own copy** —
`src/Compilers/Core/Portable/Hashing/XxHash128.cs` with `XxHashShared.cs` and `XxHash64.State.cs`, MIT,
already building for netstandard-targeting assemblies. Lifting those three is the "borrow a few lines"
route. `System.IO.Hashing` would supply the same algorithm from a package, and is the wrong shape here:
NuGet packages and analyzers do not mix well - the dll has to be shipped inside the analyzer folder and
loaded in the analyzer load context, which is the pain that made BuildTools compile Core's sources in to
begin with. Note **Roslyn made the same call**: the compiler could reference that package and instead
carries its own copy, which is about as good a corroboration as this decision gets.

**Better still, reflection is probably sufficient, and the version numbers are why.** Interceptors need
a modern compiler regardless, and the `(version, data)` attribute form and
`GetInterceptableLocation` arrived *together* in 4.11 — so any host that would accept what we emit
already has the API. The analyzer binds to the **host's** Roslyn at run time (the same reason
`ProtoModelGenerator` spells `LanguageVersion.CSharp12` numerically), so it can call the new API
reflectively when present and fall back to synthesis otherwise. Reflection at build time costs nothing
and carries none of AOT's constraints.

That ordering matters for risk: calling the API tracks whatever encoding the compiler prefers, whereas
synthesis pins us to v1 and bets on v1 remaining readable. Versioning exists to let the format evolve,
and v1 strings are baked into shipped assemblies' metadata, so that bet looks safe for a long time — but
it is a bet, and the reflective route does not take it.

### Proven end to end, by hand, on SDK 10.0.302

A scratch project settled the remaining questions by *doing* it rather than by reading: a call site, and
an interceptor whose `data` was synthesised by the encoder above with **no Roslyn interceptor API
involved at all**. It printed `INTERCEPTED: hello world`. So the synthesis route works, and the three
failure modes are all worth knowing because each is loud:

| situation | result |
| --- | --- |
| correct data, `<InterceptorsNamespaces>` set | intercepted |
| **feature not enabled** | **error CS9137**, naming `<InterceptorsNamespaces>` in the message |
| **stale/wrong checksum** (file edited after generation) | **error CS9234**, "a matching file was not found in the compilation" |

Two consequences for the design:

- **`InterceptorsNamespaces` is the current spelling, non-preview** — the compiler's own CS9137 text
  names it. `InterceptorsPreviewNamespaces` was the older form (DapperAOT's getting-started still shows
  it), and accepting either is the gracious thing to do when detecting enablement.
- **Enablement has to be detected, not assumed.** CS9137 is an *error*, so emitting an interceptor into
  a project that has not opted in breaks the build - which is exactly why DapperAOT checks. So the
  behaviour splits: with interceptors configured, intercept; without, emit **no** interceptor and instead
  warn, with a fix that rewrites the call to pass the factory explicitly - the same shape as
  `UseAotModelCodeFixProvider` does for `Serializer`/`RuntimeTypeModel.Default`, and the warning should
  link a docs page saying what to add.

### The emit shape, pinned against the compiler

Probed with a scratch project mirroring `CreateGrpcService`'s real signature -
`static TService CreateGrpcService<TService>(this CallInvoker client, ClientFactory? clientFactory = null)
where TService : class`, in `ProtoBuf.Grpc.Client.GrpcClientFactory`, with a `ChannelBase` overload
alongside. The interceptor below was accepted and ran, so this is the shape to emit rather than a guess:

``` c#
namespace ProtoBuf.AOT
{
    internal static class GrpcInterceptors
    {
        // one attribute per intercepted call site, however many contracts they name
        [global::System.Runtime.CompilerServices.InterceptsLocation(1, "<site 1>")]
        [global::System.Runtime.CompilerServices.InterceptsLocation(1, "<site 2>")]
        public static TService CreateGrpcService<TService>(
            this global::Grpc.Core.CallInvoker client,
            global::ProtoBuf.Grpc.Configuration.ClientFactory? clientFactory = null)
            where TService : class
            => (clientFactory ?? global::Some.MyServices.Instance).CreateClient<TService>(client);
    }
}
```

**The interceptor may be generic, and that is the shape to emit.** Checked by intercepting two call
sites - `IGreeter` and `ICalculator` - with a *single* `<TService>` method carrying two
`[InterceptsLocation]` attributes, and having the body report `typeof(TService).Name`: it printed
`INTERCEPTED via IGreeter` and `INTERCEPTED via ICalculator`, so each site got its own substitution.

That is worth more than the code it saves, because it removes three problems rather than one:

- **one method per model instead of one per contract**, with `AllowMultiple = true` doing the work;
- **the per-model naming question disappears** - there is one method, named after the intercepted one,
  so nothing has to be uniquified and the `CS0101` caveat evaporates;
- **the body needs no substitution at all**: `CreateClient<TService>(client)` is generic already, and our
  generated `CreateClient` dispatches on `typeof(TService)` internally. So the emitted line is
  *identical* for every consumer and every contract - there is exactly one shape to review, and it is
  the runtime overload's own body with one identifier swapped.

An earlier note here said the return type had to be the substituted one; that was true of the
non-generic form I probed first, and is not a constraint.

**And because `TService` is constrained `: class`, every instantiation shares one body.** Reference-type
generics canonicalise, so `CreateGrpcService<IGreeter>` and `CreateGrpcService<ICalculator>` are one
method at run time rather than two - and ILC canonicalises the same way, so the native image carries one
body plus the small per-type dictionaries, not a copy per contract. The one-method-per-contract shape
would have put N distinct bodies in the image, each rooted, for no benefit.

The cost that remains is the `typeof(TService)` chain inside the generated `CreateClient`, which shared
generics make a real lookup rather than a compile-time pick - but that was always there, it is a handful
of reference comparisons, and it runs once per client construction rather than per call.

Four details that had to be checked rather than assumed, because getting any of them wrong is a
compile error in the consumer's project:

- the interceptor is **extension-shaped** (`this` on the receiver), matching how the call site binds;
- a **generic** interceptor is legal and preferred (above); the non-generic substituted form also works,
  which is what the first probe established. An *open* `T` at the call site still cannot be intercepted,
  since there is no location to name;
- the **optional parameter is retained** even though the call site omits it;
- an `internal static class` at namespace level is fine - extension methods need a non-generic
  non-nested static class, and `internal` satisfies the call site without `file` scoping.

For the `ChannelBase` overload the body adds `client.CreateCallInvoker()`, which is exactly what the
runtime overload does - keeping the rule that an interceptor only ever swaps the factory and never
reimplements anything.

**The `??` is a real null check, not defensive noise, and it is there instead of a `Debug.Assert`.** The
matcher only intercepts call sites that pass no factory, so `clientFactory` is provably null at every
site we emit for - which makes an assert a check on *our own* matcher rather than on anything a consumer
did. The coalesce is strictly better: it costs one token, needs no `DEBUG` conditional, mirrors the
runtime overload's own `(clientFactory ?? ClientFactory.Default)`, and makes the dangerous failure
impossible rather than merely detectable. That failure being: silently discarding a factory the consumer
explicitly configured, which is exactly what a widened matcher plus a forgotten argument would do.

### The namespace: `ProtoBuf.AOT`

What the consumer types into `<InterceptorsNamespaces>`, so it is effectively public API and wants to be
short, memorable and stable. `ProtoBuf.AOT` mirrors `Dapper.AOT`, which is the same author's precedent
and the shape anyone who has done DapperAOT already recognises.

**It matches by prefix, which is what makes it an umbrella rather than one name among many.** Probed the
cheap way, exploiting the two error codes: declare an interceptor in `ProtoBuf.AOT.Grpc` with a
deliberately bogus `data`, enable only `ProtoBuf.AOT`, and see which error comes back - `CS9137` would
mean the namespace was not enabled, `CS9234` means it *was* and only the location was bad. It reported
**CS9234**.

So one line in the consumer's project file covers `ProtoBuf.AOT` and everything under it: a future
`ProtoBuf.AOT.Grpc`, or interceptors for protobuf-net's own APIs, need no second opt-in. That argues for
scoping the umbrella at the family (`ProtoBuf.AOT`) rather than at this feature
(`ProtoBuf.Grpc.AOT`), even though the generated code today is entirely gRPC's - the generator lives in
`protobuf-net.BuildTools`, which ships with protobuf-net, so the family is the honest scope.

Inside it, an `internal static class` holding the `[InterceptsLocation]`-attributed methods. One caveat to
settle when it is built: two `[ProtoGrpc]` models mean two generated files, so the holder needs a
per-model name (or `static partial`, or `file` scoping) or the second one is `CS0101`. `file` scoping is
proven to work for interceptors - the hand-built proof used it, with the call site in another file
entirely - so all three options are open.

That second point is also what keeps the feature honest: the interceptor is an optimisation of a call
the consumer could always have written by hand, and when we cannot apply it we say so rather than
silently leaving them on the reflective path.

**The design rule, which is what keeps "zero magic" true where it counts:** an interceptor may only
ever swap the factory argument — never inline a proxy, never do anything the explicit form does not.
The body is one line, `=> MyServices.Instance.CreateClient<TService>(c)`, which is exactly what the
code fix would have written. So the magic is compile-time, inspectable, and always has an
ordinary-C# equivalent one keystroke away.

Decline and diagnose rather than intercept when: the consumer already passed a `ClientFactory`; two
`[ProtoGrpc]` partials claim the same contract; or the type argument is not a concrete named type.

`AddCodeFirstGrpc` is deliberately **not** intercepted — it is a one-time call in `Program.cs`,
easily changed by hand, and intercepting it would fight a consumer who had configured a
`BinderConfiguration` in the lambda.

## The two `RuntimeTypeModel` roots, and how they were measured

Removing the reflective serializer machinery from a gRPC consumer's AOT graph needed **two** changes
in protobuf-net.Grpc, and this is the single most useful number in these notes:

| gated | IL warnings | bytes |
| --- | ---: | ---: |
| nothing (baseline) | 100 | 15,015,936 |
| `ProtoBufMarshallerFactory.Default` + `.Create` (#365) | 100 | 15,019,520 |
| `BytesValue.SlowParse` (#368) | 100 | 15,017,984 |
| **both** — i.e. shipped 1.3.6 | **5** | **14,480,384** |

**An AND, not an OR.** Either alone keeps the other's graph alive, so each change measured in
isolation looks worthless — and the natural conclusion would have been to drop it. The residual four
are per-assembly rollups, with nothing attributed to generated code.

The second root is the surprising one: `MarshallerCache` pre-registers `BytesValue.Marshaller` in a
**field initialiser**, so every consumer — including one that never touches a `Stream`-shaped
operation — made `RuntimeTypeModel.Default` reachable through `BytesValue.SlowParse`.

Two things about `BytesValue` worth not re-deriving:

- **it is `repeated MessageThatHasBytes`, not `repeated bytes`.** A `Stream` operation becomes a gRPC
  *streaming call* whose message type is `BytesValue` (the frozen well-known wrapper, one `bytes`
  field). The repetition is frame-level; there is no protobuf `repeated` field involved. That is why
  #368 can take **replace** semantics for a duplicated field 1 with no legacy opt-out — `Serialize`
  writes field 1 exactly once per frame, so the divergence from protobuf-net's concatenating
  `AppendBytes` is unreachable from anything we produce;
- **an empty payload reaches the slow path.** A defaulted `BytesValue` is legally zero bytes on the
  wire; `TryFastParse` reads four zero bytes, matches no case, and declines. Normal outcome, not an
  error.

**Open question, deliberately not answered:** protobuf-net.Grpc pins `ProtoBufNet2Version = 2.4.8`
with no comment saying why, and *that pin is the only reason #368 hand-rolls a parser* — v3's
`ProtoReader.State` would read raw fields directly. The runtime already copes with both (the old
`SlowParse` literally probed `if (model is IProtoInput<ReadOnlySequence<byte>> v3)`), so the pin is
about letting consumers on protobuf-net 2.x use protobuf-net.Grpc. If that constituency is still
real, hand-rolling is correct; if not, raising the pin dissolves this class of problem more cheaply
than routing around it.

## Measuring: traps that cost time

All four are process, not code, and none is discoverable from the repo.

- **A local `dotnet pack` will not be picked up twice.** NB.GV derives the version from the commit,
  so two local packs of different code can share a version string, and NuGet never re-extracts.
  **Identical byte counts between runs are a red flag, not a null result** — that nearly got recorded
  as "this change does nothing".
- **The packages folder here is `C:\Code\NugetPackageCache`, not `~/.nuget/packages`.** Clearing the
  wrong one silently does nothing. Delete
  `C:\Code\NugetPackageCache\protobuf-net.grpc\<version>` between packs.
- **Git Bash mangles `/p:Foo=bar` into a path**, so MSBuild reports the baffling
  `MSB1008: Only one project can be specified`. Use `-p:` locally; the GitHub workflows run under
  `cmd`/`pwsh`, where `/p:` is fine.
- **Run the protobuf-net.Grpc tests in Debug as well as Release.** `BytesValue.FastPassMiss` is a
  process-wide `#if DEBUG` counter that one test asserts against, so a Release run compiles the
  assertion out and reports green over a real failure.

Also: clear `obj`/`bin` before an AOT publish (it is incremental, and a second run reports nothing),
and `vswhere.exe` must be on `PATH` or ILC's link step fails with an error naming `link.exe`.

## Versioning and release traps (protobuf-net.Grpc)

Recorded because both cost time and neither is visible from the code.

- **`publicReleaseRefSpec` must match how the repo actually tags.** protobuf-net.Grpc has always
  tagged releases *unprefixed* (`1.3.0`, `1.2.5`, …) but its spec listed only
  `^refs/tags/v\d+\.\d+`, so a release tag was never a "public release" and NB.GV appended a
  `-g<commit>` suffix. Invisible for years, because AppVeyor published from `main` — which *does*
  match — and nothing ever evaluated the version at a tag. `release.yml`'s guard is the first thing
  that does. Fixed in #367; protobuf-net's spec accepted both forms all along.
- **NB.GV resets commit height only when the `version` field changes.** Editing any other property
  of `version.json` does not reset it. So a follow-up commit that touches only
  `publicReleaseRefSpec` is height *n+1*, and `versionHeightOffset` has to absorb it — which is why
  that repo went `-1` → `-2` to keep the first release at `x.y.0`. Corollary: **a version-bump PR
  must be squash-merged and stay one commit**, or the offset is wrong again.
- `nbgv tag`'s default is `v{version}`; `release.tagName` is set to `{version}` so it emits the
  unprefixed form the repo uses.

## Diagnostic IDs

`PBN40xx` is this generator's block. The blocks, as of #1283:

| block | owner |
| --- | --- |
| `PBN0xxx` | `DataContractAnalyzer` |
| `PBN1xxx` | `ProtoFileGenerator` (schema errors) |
| `PBN2xxx` | `ServiceContractAnalyzer` — the gRPC *contract* checks |
| `PBN3xxx` | the AOT serializer generator and its migration analyzer |
| `PBN4xxx` | this generator |
| `PBN9001` | the `[Experimental]` gate on the trigger attributes |

**`AnalyzerReleases.Unshipped.md` is the only register of which ids are taken** — check it before
adding one, and add the id to it. This branch originally claimed `PBN30xx`, chosen to dodge a
collision between `ServiceContractAnalyzer` and the AOT generator that turned out to already exist;
#1283 fixed that by moving the *AOT* diagnostics into `PBN3xxx`, which then collided with this
branch. Renumbering to `PBN4xxx` on the merge is how that was settled.
