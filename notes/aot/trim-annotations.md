# Trim/AOT annotations: the full history

Moved verbatim from `AGENTS.md` on 2026-10-19, when that file hit its 150k-char limit. `AGENTS.md`
keeps the standing rules; this is the complete record of the measurements and the mistakes behind
them. The raw numbers and the tracing recipe are items 4/4b and A2 of `notes/aot/findings.md`.

---

`[DynamicallyAccessedMembers]` says *"someone will reflect over this T"*. That is a property of **how
a serializer is obtained** (the reflection-based `RuntimeTypeModel` builds one by inspecting the
contract), **not of what a serializer is**. It was originally declared on `ISerializer<T>` and its
siblings, so every consumer paid the runtime model's cost — including generated models, which never
reflect at all. `PrimaryTypeProvider` implements `ISerializer<Type>`, and since `System.Type` is
saturated with `RequiresDynamicCode` members, that single instantiation produced ~180 warnings.

Removing it from the serializer interfaces took the `AotSmoke` publish from **200 warnings to 33**.
Do not reintroduce it there; annotate the reflection entry points instead.

**The same mistake was hiding one interface family over, and cost 808 KB.** `IProtoInput<TInput>`,
`IProtoOutput<TOutput>` and `IMeasuredProtoOutput<TOutput>` carried `DynamicAccess.ContractType` on
the **transport** type — `Stream`, `byte[]`, `ReadOnlySequence<byte>`, `IBufferWriter<byte>` — of
which `TypeModel` implements nine instantiations. Nothing reflects over a stream; the contract is
`T` on `Deserialize<T>`, and that one was never annotated. Because the mask includes nested types,
ILC kept **1738 framework members** reflectable (`Task` 520, `Array` 160, `Enum` 99, `Stream` 72, …)
and *no* protobuf-net members. Removing the three annotations: **34 → 20** warnings and
**3.52 MB → 2.73 MB**, i.e. **22.4%** of the native binary. Item 4 of `notes/aot/findings.md` has the
measurement and the tracing recipe.

So the standing rule is: **before annotating a type parameter, ask what would reflect over it.** A
transport, a buffer, a destination — nothing does. `TInput` survived the `ISerializer<T>` cleanup
purely because the name reads like a contract type.

**The rule cuts both ways, and the other direction is a live bug rather than dead weight.**
`TCollection` on the repeated/map serializers had *never* been annotated, yet
`TypeModel.ActivatorCreate<TCollection>()` constructs the collection when a member arrives null —
so ILC trimmed the constructors and every `HashSet<T>`/`Queue<T>`/`ConcurrentQueue<T>` member threw
`MissingMethodException` on **deserialize** (item 4b). The answer to "what reflects over it" was
"`Activator` does", and the fix is `DynamicAccess.Activated` —
`PublicParameterlessConstructor | NonPublicConstructors`, and deliberately nothing else, since the
collection is constructed but never inspected. Applied to `ActivatorCreate<T>`, the concrete
serializers that call it, and the public factories that name them.

Note `List<T>` worked throughout, purely because application code elsewhere calls `new List<T>()` —
so the long-standing `List<T>` coverage in `AotSmoke` was evidence about nothing.

`Requires*` attributes were tried on the dynamic helpers and **reverted** — they do not remove
warnings, they relocate them to callers, and the callers here are
`ProtoReader.State.DeserializeRootImpl<T>`, `TypeModel.CreateInstance<T>` and
`TypeHelper<T>..cctor()` — i.e. the *generic paths generated models use*. The reflective fallbacks
are entangled inside the AOT-safe paths rather than sitting behind a boundary, so `Requires*` has
nowhere clean to terminate. Fixing that properly means **restructuring** (the generic path must not
reference the fallback at all), not annotating.

**...but a feature switch does terminate cleanly, which `Requires*` could not**, and that is now
`TypeModel.ResolveSerializer<T>`. Gating a fallback on `RuntimeFeature.IsDynamicCodeSupported` lets
ILC substitute a constant and eliminate the arm *before* trim analysis, so the demand disappears
rather than moving to the caller. A generic utility ending `serializer ??= GetSerializer<T>(model)`
must otherwise declare `DynamicAccess.ContractType` on its own `T`, and every instantiation inherits
that — including generated models, which pass a serializer and never reach the fallback.

Applied in two passes, measured separately: `RepeatedSerializer<TCollection, TItem>` took **49 → 45**
(`IL2091` 11 → 7), and the writer/reader cluster took **45 → 34** (`IL2091` 7 → 2). Every annotation
named below is gone as a result.

**The cluster is one unit and cannot be done piecemeal.** `WriteAny`, `WriteWrapped`, `WriteMessage`,
`WriteGroup`, `WriteWrappedItem`, `ReadMessage`, `ReadAny`, `ReadWrapped` and `ReadRepeatedCore` all
hand `T` to each other, so half a treatment relocates instead of removing — which is precisely what
the `RepeatedSerializer` pass did, surfacing two fresh warnings at `RepeatedSerializer.Write`. What
makes it safe is that the *callers* already supply the serializer, so the annotation was only ever
serving a fallback they never take. `MapSerializer<TCollection, TKey, TValue>` and the two
`External*Serializer` bases followed, on the same reasoning.

Deliberately excluded: the root entry points (`DeserializeRoot`/`SerializeRoot`/`ReadAsRoot`/
`WriteAsRoot`) and the two `GetSerializer<T>()` accessors — an explicit resolution request is not a
fallback, so the annotation is honest there.

**`MapSerializer` was excluded once on the grounds that it "produces no warning today", and that
reasoning was wrong** — `AotSmoke` simply had no map member, so the whole family was *unmeasured*
rather than harmless. Adding three map members moved the baseline 20 → 29 and `IL2091` 2 → 11, all
of it previously invisible. "No warning" is only evidence if the code is on the measured path.

Note the two arms are the *same resolution*, deliberately: a generated model resolves a repeated enum
through it — we pass no serializer and rely on `ISerializerProxy<TEnum>` — so an arm that threw would
break exactly the shapes `AotSmoke` covers.

**Only a native publish exercises the AOT arm at all**; a JIT run takes the other one, so the
differential suite says nothing about this. That is why `AotSmoke` carries `List<Status>`,
`List<Customer>`, and the three map members — the repeated enum and the repeated *map value* being
the sharp cases, since resolution has to find the proxy through the model with nothing passed in.

**The element type is not a contract type either**, and this was the last big one. The public
repeated and map factories annotated their *element* — `T`, `TKey`, `TValue` — with
`DynamicAccess.ContractType`, so every collection or map member demanded a fully-reflectable element.
Nothing inside those serializers reflects over it: the element's serializer is passed in by the
caller or resolved through the `IsDynamicCodeSupported`-gated arm, which ILC substitutes away. The
*class-level* annotations were removed when that gate went in; the **public factories that name them
were missed**, and those are exactly what generated code calls. Removing them: **39 → 25 warnings and
−385 KB (−8.9%)**, with nothing relocated.

`TCollection` keeps its `DynamicAccess.Activated` on the same methods, and that is the rule working
rather than an exception to it — `ActivatorCreate<T>()` genuinely constructs the collection. Two
parameters of one method, opposite answers, because the question is always "what would reflect over
*this* one".

**The enum connection is worth knowing, because it disguises the cause.** Reflection returns
*inherited statics*, so `typeof(SomeEnum).GetMethods()` includes `Enum.GetValues` and `Enum.Parse` —
both `RequiresDynamicCode`. So any `PublicMethods` demand that lands on an enum type parameter emits
`IL3050` naming `Enum.GetValues`, which reads as an enum-serialization problem and is really an
annotation-scope problem. It was the element annotation on `List<Status>`, not the enum member.

**`ThrowUnexpectedSubtype<T>` was the last one, and it was the widest**, because the generated code
emits that call for *every* non-sealed reference-type contract — so its `ContractType` demand applied
to the whole model. Nothing in it inspects `T`: `IsSubType<T>` is `typeof(T) != value.GetType()`, and
the pair then goes to the `Type`-based overload, whose parameters carry no demand. Removing it:
**25 → 19 warnings and a further −277 KB (−7.0%)**.

That is also where the `Enum.GetValues` reports came from, and the route is worth following once
because no part of it is guessable: `ContractType` includes `PublicNestedTypes`; a `.proto` DTO nests
its enums *inside* the message (`FieldDescriptorProto.Label`, `FieldOptions.CType`, …); so the demand
kept those enums reflectable, and an enum's reflectable members include the statics it inherits from
`System.Enum`, which are `RequiresDynamicCode`. Four contracts, six warnings, one annotation.

**Do not try to fix an `Enum.GetValues` report by switching to `Enum.GetValues<T>()`.** The advice in
that warning's text is boilerplate from the attribute and is aimed at callers; there is no call here
at all — protobuf-net contains no `Enum.GetValues` call in any shipped assembly. The lever is always
the annotation that demanded the metadata.

The count is now **20** on win-x64 (it was 19 before the `Dictionary<int, List<Customer>>` member
went in for #1337, and 21 before the `.proto` DTO tree was added — none of these are comparable with
each other, since the count tracks fixtures). What is left looks structural: 7
`IL2067` and 3 `IL2070` on the runtime-model, `DynamicStub` and auxiliary paths, 5 `IL3050`
(`MakeGenericType`/`MakeArrayType`, same paths), 1 `IL2057`, 1 `IL2055`, and the spent `IL2091` trio —
`CreateInstance` ×2 (whose fallback is genuinely live) and `SubTypeState<T>.Cast` (which would need
the annotation on every consumer).

**Watch bytes as well as warnings — they do not move together.** Two changes of identical shape:
removing the transport annotation was −14 warnings and **−808 KB**; removing the `MapSerializer`
family's was −8 warnings and **exactly zero bytes**, because a map's `TKey`/`TValue` are contract
types the generated model's `GetSerializer<T>` override annotates regardless. A sweep of every
remaining `[DynamicallyAccessedMembers]` in Core found one further redundancy —
`SerializerCache<TProvider, T>`'s `T`, which nothing in that class reflects over — and removing it
was measured as **byte-for-byte identical**, so it was left in place. `TProvider` there is the
load-bearing one.

**Split them by source location, not by id.** A warning with a `file:line` is a reflective call in
our source; one attributed to a bare type name with no location is a member kept *reflectable* by a
`DynamicallyAccessedMembers` demand and never called — which is a metadata-size problem wearing a
warning's clothes, and is where the 808 KB above was found. There are currently none of the latter.

**Measure with a publish rather than reasoning about them** — clear `obj`/`bin` first, since the
publish is incremental and a second run reports nothing at all, and re-measure the *baseline*
whenever a fixture changes, since the count tracks fixtures. **Watch the binary size too**, not just
the count: the two do not move together, and the largest win so far was invisible in the count.
`notes/aot/findings.md` A2 no longer quotes a floor — every estimate so far was beaten by the next
measurement.

When a warning needs attributing, get the graph rather than guessing —
`/p:IlcGenerateDgmlFile=true`, then walk *incoming* edges from the offending member and read each
edge's `Reason` attribute, which names the responsible generic parameter. Two confident hypotheses
were wrong before that was tried; item 4 records both.
