# Build-time gRPC proxies: findings and handover

Working notes for the gRPC half of the AOT story, in the same spirit as `aot-findings.md`. The
serializer half is `aot.md` (user-facing) and `aot-findings.md` (working notes).

> **Handover** (2026-08-16). Branch `grpc-aot-generator`, draft PR
> [#1282](https://github.com/protobuf-net/protobuf-net/pull/1282). Validated on Windows:
> `dotnet test src/BuildToolsUnitTests` (327 pass), a JIT run of `src/AotGrpcSmoke`, and a `win-x64`
> native publish of the same (all five checks pass). `protobuf-net.BuildTools.Legacy` builds green.
>
> **Landed elsewhere, and this branch depends on it:**
>
> | | |
> | --- | --- |
> | protobuf-net.Grpc **1.3.0** | ships `[ProtoGrpc]` / `[ProtoService]`, the `RuntimeTypeModel` gates, and `BinderConfiguration.Binder` made public |
> | protobuf-net.Grpc #366 | GitHub Actions CI + trusted-publishing release pipeline (AppVeyor retired) |
> | protobuf-net.Grpc #367 | release-tag fix; see "Versioning traps" below |
> | protobuf-net #1284 | `[Experimental]` help links + the shared `docs/exp/PBN9001.md` page |
> | docs | `grpc.protobuf-net.dev` is live; protobuf-net's is `docs.protobuf-net.dev` |
>
> **Next steps, in order** — the first two are the ones with a decision attached:
>
> 1. **Switch to the shipped attributes.** Bump `protobuf-net.Grpc` to `1.3.0` in
>    `src/Directory.Packages.props` (currently `1.2.2`), then delete `TriggerAttributesSource` and
>    the `RegisterPostInitializationOutput` call from `GrpcProxyGenerator.cs` — see "Why the
>    attributes are generator-owned", which becomes historical once this is done. Regenerate the
>    golden (it currently contains the emitted attributes file).
> 2. **`BytesValue.SlowParse`** in protobuf-net.Grpc — the other half of the AOT warning reduction,
>    and the larger of the two. See "The measurement" below; blocked on a semantics decision.
> 3. **The one real `IL2091`**, on the generated `__Serialize<T>` — ours, and small.
> 4. **Seeding**: teach `[ProtoSerializable]` to accept a `[Service]` interface.
> 5. **Compile-time endpoint metadata** — now unblocked by `Binder` being public.

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
- **`BinderConfiguration.Binder` is `internal`**, so generated code cannot reach the metadata
  pipeline through it — hence the public `ServiceBinder.Default`. See "Known gaps".
- **`BinderConfiguration.SetMarshaller<T>` is public**, and `GetMarshaller<T>` checks the cache
  first. That is what lets the generator sidestep `CanSerialize(Type)` entirely.
- **protobuf-net.Grpc 1.0.21 predates the `ClientFactory` shape** — its `CreateClient<TService>` is
  not even virtual, and the abstract member is
  `CreateClient<TBase, TService, TChannel>(TChannel)`. `Directory.Packages.props` was pinned there;
  bumped to **1.2.2**. Found within minutes of building against the real package, and not findable
  from the surface snapshot.

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

### Why the attributes are generator-owned (historical — being removed)

**They now ship in protobuf-net.Grpc 1.3.0, so this is the state to migrate off, not to preserve.**
Kept because the reasoning explains why the migration is safe.

`[ProtoGrpc]` / `[ProtoService]` are currently emitted per consuming assembly as `internal`, from
`RegisterPostInitializationOutput`. They **must** be post-init rather than ordinary output, because
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

## Known gaps

- **Only one fixture.** `Basic.input.cs` covers all five method shapes plus client and server. The
  diagnostic fixtures (nested, generic, non-`[SubService]` base, not partial, not deriving
  `ClientFactory`, no model named) are **not written**, so PBN3001–PBN3011 are unproven code paths.
- **Seeding is manual.** `AotGrpcSmoke` lists `[ProtoSerializable(typeof(HelloRequest))]` by hand.
  Teaching `[ProtoSerializable]` to accept a `[Service]` interface and enqueue its payload types is
  the obvious next step; `GrpcOperationModel` already carries them as strings.
- **One reflective call left on the server path**:
  `ServiceBinder.Default.GetMetadata(typeof(IFoo).GetMethod(name)!, …)`, needed to preserve
  `[Authorize]`-style endpoint metadata. It survives the native publish, but it is a `GetMethod` over
  an interface and wants a non-reflective route.
- **100 IL warnings on the native publish**, against `AotSmoke`'s 19. All from
  `RuntimeTypeModel` / `DynamicStub` / surrogate / tuple paths, reachable because
  `ProtoBufMarshallerFactory.Default` statically names `RuntimeTypeModel.Default`. Road not taken,
  but unattributed — use `/p:IlcGenerateDgmlFile=true` and walk *incoming* edges rather than
  guessing, per `aot-findings.md`.
- **No interceptors yet.** See below.

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
