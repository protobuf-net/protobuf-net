# Build-time gRPC proxies: findings and handover

Working notes for the gRPC half of the AOT story, in the same spirit as `aot-findings.md`. The
serializer half is `aot.md` (user-facing) and `aot-findings.md` (working notes).

> **Handover** (2026-08-17). Branch `grpc-aot-generator`, draft PR
> [#1282](https://github.com/protobuf-net/protobuf-net/pull/1282). Validated on Windows:
> `dotnet test src/BuildToolsUnitTests` (**345** pass), a JIT run of `src/AotGrpcSmoke`, and a
> `win-x64` native publish of the same (all five checks pass). `protobuf-net.BuildTools.Legacy` builds
> green.
>
> **Since the first handover:** every diagnostic is fixtured (`Grpc/Data/Diagnostics/`, twelve of them,
> plus one recording a hole that is now `PBN4009`); `PBN4000`, `PBN4002` and `PBN4003` are fixed;
> closed generic contracts are supported; the service-naming port is verified against the real runtime
> and three silent wire-name bugs are gone; and the two structural generator rules (incremental
> caching, no Roslyn references in the model) now have tests, both verified able to fail.
>
> **Landed elsewhere, and this branch depends on it:**
>
> | | |
> | --- | --- |
> | protobuf-net.Grpc **1.3.6** (released) | everything this branch needs; `[ProtoGrpc]`/`[ProtoService]`, both `RuntimeTypeModel` roots gone, `BinderConfiguration.Binder` public |
> | protobuf-net.Grpc #365–#372 (merged) | the above, plus the Actions CI + trusted-publishing pipeline, the release-tag fix, and the `get-version` scripts |
> | protobuf-net.Grpc #369 (open) | not ours — a contributor's `[SubService]` metadata fix, under discussion |
> | protobuf-net #1284 (open) | `[Experimental]` help links + the shared `docs/exp/PBN9001.md` page |
> | docs | `grpc.protobuf-net.dev` is live; protobuf-net's is `docs.protobuf-net.dev` |
>
> **Done here:** the generator-owned trigger attributes are gone, the package floor is `1.3.6`, and
> the golden no longer contains an emitted attributes file. The unit tests stub the attributes in
> `Grpc/Data/_ContractSurface.cs` deliberately — matching is by full name, and the real ones are
> `[Experimental]`, i.e. an *error* by default, so a stub also keeps fixtures free of suppressions.
>
> **`src/AotGrpcSmoke` is verified against the genuine 1.3.6 from nuget.org** — build, JIT run and a
> `win-x64` native run, all five checks green, **4 IL warnings and 14,480,384 bytes** (against 100
> and 15,019,520 on 1.3.0). **All four are per-assembly `IL2104`/`IL3053` rollups; nothing is
> attributed to generated code.**
>
> **Next steps, in order:**
>
> 1. **Interceptors** — the zero-code-change story for `CreateGrpcService<T>`. The mechanism is proven
>    (see below); it is a separate feature rather than a gap in this one.
>
> Seeding, the diagnostics, and the `Empty`/`[SubService]` coverage are **done** — see the sections
> below and the git history on this branch. **Compile-time endpoint metadata was investigated and is
> closed rather than pending**: the runtime list cannot be reproduced exactly (see "Compile-time
> metadata" below), and the reflective call it would have replaced has been made correct for
> overloads instead.

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

### Metadata parity, and protobuf-net.Grpc#369

We deliberately delegate metadata to `GetMetadata` rather than computing it, so whatever the runtime
does, we do. **That equivalence is the thing to re-check if
[protobuf-net.Grpc#369](https://github.com/protobuf-net/protobuf-net.Grpc/pull/369) lands**, because
it changes what `GetMetadata` returns for operations inherited from a `[SubService]` base.

As proposed it *swaps* which type-level attributes are collected — sub-service interface instead of
top-level service contract — and the likely landing shape is a **union of both**, plus the
implementation method's. Either way the set moves.

Two consequences, in order of how easily they are missed:

1. **While we delegate, we inherit the change for free** — including any bug in it. Nothing here needs
   to change, but a fixture with a `[SubService]` base and an attribute at each level would pin it,
   and there is currently no such fixture.
2. **The moment compile-time metadata is built (next-steps item 3), we own the reconstruction**, and
   the union rule has to be reproduced exactly. Attributes on an interface do **not** inherit —
   `GetCustomAttributes(inherit: true)` does not walk base interfaces, probed rather than assumed —
   so "just ask the contract type" silently loses one side of the union. The check to run before
   trusting a compile-time implementation is a differential: for a contract with a `[SubService]`
   base, compare our reconstructed list against `binder.GetMetadata(...)` item by item, on the same
   contract, at all three levels (top-level interface, sub-service interface, implementation method).

That comparison is cheap and mechanical, and it is the only thing standing between "we match the
runtime" and "we quietly diverge on authorization metadata", which is the failure mode worth fearing:
`[Authorize]` going missing produces no error, just a more permissive endpoint.

## Interceptors (proven mechanism, not yet used)

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
parameter, not the receiver (use `ReducedFrom`), and the v1
`[InterceptsLocation(path, line, char)]` form is gone — you need
`SemanticModel.GetInterceptableLocation()` plus `GetInterceptsLocationAttributeSyntax()`, which is
**Roslyn 4.11+**. That is a genuine reason to raise the baseline (`aot-findings.md`'s rule is "only
if we need a modern API we cannot work around" — this qualifies), and Legacy must stay pinned.

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
