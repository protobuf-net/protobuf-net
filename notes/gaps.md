# Gaps and their decisions

**One reviewable place.** Every known gap, with a *decision* against it rather than just an
absence — an unrecorded gap reads as an oversight, and gets re-discovered and re-argued.

Detail lives in the linked documents; this file is the index and the verdict. Status meanings:

| status | means |
| --- | --- |
| **won't do** | decided against, with a reason. Reopen only on new evidence (a consumer asking) |
| **deferred** | wanted, not now; the reason is about ordering or risk, not merit |
| **open** | needs a human decision, and is blocking nothing until it gets one |
| **next** | agreed, unstarted |

This file lives on **`v4`** — the writer/schema stack collapsed onto it on 2026-08-14 — and on
whatever sub-branch is currently in flight off it.

*(It has now asserted the wrong branch **twice** — first `writer-buffer-core`, then
`aot-schema-model` after the stack collapsed — and `AGENTS.md`'s index repeated the error
independently. Recorded rather than quietly corrected, because a document stating where it lives,
wrongly, is precisely the staleness this file exists to prevent. **If you move this file or cut a
branch, this paragraph is the one to re-read.**)*

---

## A. What the AOT generator cannot serialize

Two, both niche, both **decided: keep omitting** (Marc, 2026-08-14). Each is refused with a
diagnostic naming the reason, so a consumer meeting one is told rather than left guessing.

| gap | status | reasoning |
| --- | --- | --- |
| a **collection as a map key** — `Dictionary<List<int>, string>` | **won't do** | Arguably invalid rather than merely unsupported: the BCL immutable collections have no *intrinsic structural equality* (`ImmutableArray<T>` compares its underlying array **by reference**, `ImmutableList<T>`/`ImmutableHashSet<T>` do not override at all), so such a dictionary misses an equal-but-distinct key **before serialization enters into it**. protobuf-net's own *compiled* path throws on it too; only the reflection path handles it. Detail: `AGENTS.md` → "Not yet supported" |
| a **hand-written serializer as a map key or value**, where its category is scalar or unknown | **won't do** | **Zero occurrences in a 1,392-contract corpus.** The unary and collection forms both defer the category to the serializer at run time and a map plausibly could too — `MapSerializer` calls `InheritFrom` on each side exactly as the repeated one does — so it is unbuilt rather than impossible, and is a morning's work if anyone asks |

Everything else the generator refuses is a **match** with protobuf-net rather than a shortfall —
it throws for those shapes too. That table is in `AGENTS.md` → "Telling our gaps from
protobuf-net's", and the distinction matters: those refusals lower the coverage sweep's "%
emitted" while *raising* correctness.

## B. Writer arc — parked work

The reasoning is here rather than in `notes/nano-writer.md`, which keeps the *findings* (what was
tried, measured and reverted) but no longer carries a to-do list.

### B1. Packed writes, and the empty-collection disagreement — **next**

The sharpest item, because it is the only one suspected of being a **live bug** rather than
absent work. An empty packed collection appears to emit a zero-length field where ref-emit writes
nothing. Found while adding enum support to the schema front-end, but it is **not enum-specific**:
protogen marks a repeated enum `IsPacked = true`, and per `AGENTS.md` the symbol path has never
supported that argument — *"that named argument is not supported yet, so we always emit the
disabled form"* — so the packed raw-write arm is largely undriven and the schema path is simply
the first thing to reach it.

Packed repeated writes generally belong with it: the write needs the zero-length-header model
option and a per-element measure, the `MemoryMarshal` block trick for fixed widths is recorded in
`notes/nano-writer.md`'s checklist, and `IMeasuringSerializer` is already implemented by
measurable contracts, which is what the packed engine keys on.

**It is also the one thing that would reopen the varint/tag micro-optimisation line**, since
packed measures per *element* rather than per message — five flat results there assumed the
current op mix. The strategy matrix in `src/NanoBench/VarintMeasureResults.md` is already built,
so that re-test is cheap.

### B2. Skip the depth check on leaf contracts — deferred

A contract with no message-typed members cannot recurse, so its `if (--depth < 0)` can never
usefully fire. Correct in principle — the check bounds unbounded recursion rather than enforcing
an exact depth, and every level that *can* nest still checks.

Deferred on ratio, not merit: it removes one predictable never-taken compare, five consecutive
micro-optimisations in this area have measured flat, and **a wrong "can this contract reach
itself" predicate is a process-killing stack overflow rather than a wrong byte** — which was a
real bug on this branch, fixed earlier in the arc. Measure the ceiling by hand first, on a
leaf-heavy measure-only row; run `MeasureRecursionTests` explicitly if it is ever built.

### B3. Pooled-state retention: audit the rest of Core — deferred

The length caches established a pattern that recurs: pooled state that is `Clear()`ed but whose
**capacity** is either discarded (churn — was 22 KB/op and 7–12%) or kept (hogging ~10× the
payload forever). Leads already found:

- `NetObjectCache`'s `FEAT_DYNAMIC_REF` collections are `Clear()`ed with **no** `TrimExcess`, i.e.
  already on the hogging side;
- `StreamProtoWriter.ioBuffer` grows to hold an entire payload because `flushLock` forbids
  flushing mid-sub-item, then goes back to `ArrayPool` — check it is not above the bucket limit,
  where it would be *dropped* rather than pooled;
- `ReadBufferT` does the same dance on the READ side, which this arc has never measured.

Whatever policy the length caches got should be applied once, to all of them — one mechanism, not
five.

### B4. `Pool<T>` and `BufferPool` want dedicated investigation — deferred

Marc's read is that both are early code that never evolved, and a first look supports it.
`Pool<T>` is the multiplier under every other retention question: a **`[ThreadStatic]`** slot
holds one instance *per thread, forever* — so a thread that serialized once keeps a writer, its
`NetObjectCache` and both length caches for that thread's life — plus a `Queue<T>` capped at a
magic `POOL_SIZE = 20` under a plain lock, **never trimmed**. Any per-writer retention is
therefore *(threads + 20)* copies that nothing reclaims.

`BufferPool` is now a thin `ArrayPool<byte>.Shared` wrapper, where three things stand out:
`ArrayPool` does not pool arrays above 1 MB at all (allocates on rent, drops on return), so the
`ioBuffer`-grows-to-hold-the-payload case is pure churn above that; `Return` is called without
`clearArray`, so buffers come back with stale bytes; and `GetCachedBuffer(...) ?? new byte[...]`
is a dead fallback, since `Rent` never returns null.

### B5. Counting mode for mixed contracts — deferred

Legacy-mode members measured via the classic body against the null writer, landing in the same
length cache — which now lives on `NetObjectCache`, shared with the sidecar and the `MeasureState`
hand-off, so the landing spot is already right.

### B6. Maps measure-first — deferred

Entry = one KV sub-message; both sides already have measure forms for the native kinds.

### B7. The presized lease (buffer core step 3) — parked

Built, measured neutral in both directions on every destination, and parked. The only route that
knows the total up front is the one path where that knowledge is already paid for many times over.

### B8. `ProtoFileGenerator` is not incremental — deferred

An `ISourceGenerator` with an empty `Initialize`, so it re-parses every schema on **every**
compilation — continuously, while typing. Fixing it would remove that *and* make one shared parse
feeding both the DTO and model emissions possible, which is the only way to remove the schema
path's double parse (see C9).

### B9. Callback context shapes in the AOT generator — deferred

Closes a gap this arc created: `ISerializationContext` works on the runtime paths, but the
generator accepts only "no parameter" or `StreamingContext`, so a contract whose callback takes
`ISerializationContext` is **dropped** — denying `ProtoWriter.IsMeasuring` to exactly the AOT
consumer who wants it. Should cover mixed models (different contracts in one model using
different shapes; one contract whose four callbacks differ) and cross-check that the validator,
the reflection invoker, the ref-emit path and the generator agree on the accepted set. They
demonstrably disagreed once already.

### B10. The reflection callback path allocates three times per invocation — deferred

For any callback with a parameter: `GetParameters()` returns a fresh array every call, plus the
`object[]` args, plus a box for `StreamingContext`. The shapes are fixed at registration, so it
resolves to a cached plan or delegate. No-emit path only; ref-emit and generated are already clean.

### B11. Direct emit — deferred

The remaining native-AOT warnings need the reflective paths not to exist on the AOT route at all.
**The warning count is now a poor motivation**: those warnings are correct, about code that does
reflect. The real arguments are one less layer of indirection on the generated path, and possibly
size — get a size estimate first.

### B12. An intermittent `Examples` failure on net472 — unresolved, not closed

Seen twice in full-traversal runs, never reproducible standalone, never captured by name.
Everything points at `PEVerify.AssertValid`: it shells out to `PEVerify.exe` with a **20-second
timeout** (`src/Examples/PEVerify.cs`), it is inside `#if !COREFX` so it is net472-only, and a
subprocess timeout is exactly what contention in a full run would trip. Tied to
`Compile(name, path)`, so no AOT path can reach it.

### B13. ~~`ThrowUnexpectedSubtype` on every write~~ — **settled: it costs ~0.6%**

Raised by Marc, 2026-08-14: the generated writer emits
`global::ProtoBuf.Meta.TypeModel.ThrowUnexpectedSubtype(value)` for **every non-sealed reference
contract, on every write**, and that looks like convenience rather than good code. It is the
widest single call in the emitted output.

It is also unavoidable for schema-sourced models as things stand: **protogen emits `partial class`
and never `sealed`** — the `partial` is the consumer's extension point — and structs, `sealed` and
`IgnoreUnknownSubTypes` are the only three elisions. So the entire descriptor tree pays it.

`src/NanoBench/SubtypeCheckBenchmarks.cs` measures both shapes. 4,096 checks, net10.0.

**The exact case** (no hierarchy — and per Marc, this is the *only* case `.proto` traffic takes,
since proto has no inheritance):

| arm | mean | per op |
| --- | ---: | ---: |
| shipped `ThrowUnexpectedSubtype(value)` | 1.570 µs | 0.383 ns |
| inline `GetType() != typeof(T)`, null-tolerant | 1.542 µs | 0.376 ns |
| inline, no null guard | 1.182 µs | 0.289 ns |

**The hypothesis was refuted, and it is worth recording why it was plausible.** The helper is
generic and `where T : class`, so every reference instantiation shares one body over `__Canon`;
in shared code `typeof(T)` is a generic-dictionary lookup, which is exactly what defeats RyuJIT's
folding of `obj.GetType() == typeof(Constant)` into a method-table compare. If that were
happening, shipped would be far worse than **1.8%** off the inline form. It is not: the JIT
inlines the helper and recovers the exact type. The shipped call is already ~1.3 cycles.

The *only* real difference is the **null guard** — and that is a semantic question, not a codegen
one. `ThrowUnexpectedSubtype` deliberately returns quietly for a null.

**The hierarchy case**, and every alternative loses at realistic data:

| arm | 0% sub-types | 25% | 100% |
| --- | ---: | ---: | ---: |
| shipped (`IsSubType` + `is` chain) | **1.747 µs** | **3.986 µs** | 8.415 µs |
| exact-type `if`/`else if` chain | 3.953 (2.27×) | 5.208 (1.31×) | **6.719 (0.80×)** |
| branchless `\|` membership | 7.029 (**4.03×**) | 7.150 (1.79×) | 7.474 (0.89×) |
| `switch` + `when` (exact) | 4.688 (2.69×) | 5.962 (1.50×) | 7.876 (0.94×) |
| static table scan | 4.743 (2.72×) | 6.518 (1.64×) | 7.139 (0.85×) |

The shipped shape wins because **`IsSubType` short-circuits**: one exact-type test and the chain
is skipped entirely. Every alternative pays the whole matrix up front, and the non-short-circuit
`\|` form pays it hardest — 4× the baseline on the data real models actually have. They only win
at 100% sub-types, which nothing looks like. **Decision: leave the hierarchy shape alone.**

(The `is` chain's correctness does depend on ordering — `is` is subtype-inclusive, so a
most-derived-first order is load-bearing and invisible locally. That is a *readability* argument
for the exact forms, not a performance one, and it is not worth 2.3×.)

#### So the lever is `sealed`, which deletes the check rather than shaving it

Two routes, both Marc's, and they compose:

1. **A protogen option to emit `sealed`** — there are already custom options and a pre-registered
   official options key to hang it on. Opt-in, so it breaks nobody.
2. **Seal our own generated descriptor DTOs**, on the grounds that this is a major.
   **Checked: nothing in the repo derives from any of them.** The hand-written `partial` halves in
   `Parsers.cs` add *interfaces* (`ISchemaObject`, `IType`, `IMessage`, `IReserved<,>`), which is
   fully compatible with `sealed`. The exposure is external consumers who subclass — real, but a
   major is where that is paid.

**Expected size, stated as the upper bound it is:** ~600 messages in the descriptor payload
(the census counts 608 length prefixes) × 0.383 ns ≈ **230 ns against a ~10.3 µs serialize, so
~2%**. Worth having, not transformative — and this arc has now had **six** micro-wins that did not
transfer, so it is not real until it is measured in situ.

#### `IgnoreUnknownSubTypes` is already fully encoded (Marc, 2026-08-14)

Asked whether the concept exists rather than assuming: **it does, end to end, and nothing needs
building.** `ProtoContractAttribute.IgnoreUnknownSubTypes` (`TypeOptions` flag 512) ->
`ProtoModelGenerator.Parse.cs:529` -> `ProtoContractPlan.IgnoreUnknownSubTypes`
(`AotPlans.cs:681`) -> the emit guards (`Emit.cs:1209/1265/1945/1954`). When set the emitter emits
**nothing at all** - not even a null check.

`SchemaPlanBuilder` simply never sets it, so schema-sourced contracts emit the throw. Turning it
on there is **one argument**, justified by construction rather than by preference: proto has no
inheritance, so a schema-sourced contract can never have sub-types.

The three routes are NOT equivalent, and the difference is on the error path:

| route | cost | what breaks |
| --- | --- | --- |
| `sealed` | zero | subclassing stops compiling. Semantically identical elision - a subclass *cannot exist*, so nothing observable changes |
| `IgnoreUnknownSubTypes` | zero | nothing at compile time, but a subclass instance is **silently serialized as the base** instead of throwing |
| plan-only flag in `SchemaPlanBuilder` | zero | as above, **plus** the generated model then diverges from `RuntimeTypeModel`, which still throws - protogen does not emit the attribute |

#### The in-situ measurement, and a finding about the benchmark itself

**`src/NanoBench/DescriptorNano.cs`'s 25 DTOs are already `sealed`** - so the descriptor serialize
benchmark, the headline number for this entire writer arc, **has never paid this check at all**.
It measures a best case that protogen consumers (non-sealed `partial class`) never get. That is
the "whatever it does not cover is not fine, it is unmeasured" rule biting the *benchmark*.

Un-sealing all 25, to price what those consumers actually pay:

| | sealed (no check) | un-sealed (check emitted) |
| --- | ---: | ---: |
| `NanoGenerated` | 10.274 us | 10.352 us |
| `Utf8Floor` (gauge) | 2.797 us | 2.667 us |

**Inconclusive, and recorded as such**: the gauge drifted 4.6% between runs, which is larger than
the effect. Raw that is +0.76%; gauge-normalised it is +5.7%. Those disagree, so neither is a
result - it needs a re-run on a quiet machine. Note the normalised figure is *above* the
micro-benchmark's ~2% prediction, which would be consistent with the method-table load being cold
in situ where a tight loop keeps it hot. Suggestive, not shown.

#### Marc's shared-helper shape, and it wins

A non-generic `AssertExpectedType(object value, Type expected)` doing the `GetType()` and the
throw internally - proposed on the grounds that it is more *honest* than a generic call whose name
describes only its failure mode. Priced against the rest:

| arm | mean | vs shipped |
| --- | ---: | ---: |
| shipped `ThrowUnexpectedSubtype(value)` | 1.565 us | - |
| inline `GetType() != typeof(T)` | 1.540 us | 0.98 |
| **shared helper** | **1.243 us** | **0.79** |
| shared helper, no inlining hint | 1.245 us | 0.80 |
| inline, no null guard | 1.181 us | 0.75 |

**21% faster than shipped while KEEPING the null check** - almost exactly where dropping the null
check gets you. The inlining hint makes no difference (0.79 vs 0.80), so it is not an inlining
effect.

**The mechanism is not established, and that blocks acting on it.** It is the same code shape as
the inline form, which is slow, and passing `expected` as an argument should if anything *prevent*
the method-table fold. One confident mechanism in this very investigation (shared generics
defeating that fold) was already refuted by measurement, so this gets a disassembly check before
it is believed. If it holds, the shape preferred on readability grounds is also the fast one.

#### The quiet-machine re-run settles it: ~0.6%, below the noise floor

Four alternating runs, 12 iterations each, decision rule fixed BEFORE the numbers were seen.

| run | config | `NanoGenerated` | gauge |
| --- | --- | ---: | ---: |
| A1 | sealed | 10.643 us | 2.677 |
| B1 | un-sealed | 10.201 us | 2.673 |
| A2 | sealed | 10.193 us | 2.675 |
| B2 | un-sealed | 10.316 us | 2.674 |

**The gauge is steady to 0.15%**, so the machine is quiet - which makes the noise floor the A1/A2
spread of **4.4%**: between-run variance on the *same* configuration, where within-run StdDev is
only ~0.5%. The sealed/un-sealed effect is **~0.6%**, far below it. B1 - un-sealed, i.e. MORE code
- measured faster than A1, which is the same statement made bluntly.

**This refutes the +5.7% reading and the cold-method-table hypothesis with it.** That figure was
gauge drift, and the gauge was the thing that moved. Worth keeping as method rather than as fact:
the drift was only visible by ALTERNATING the configurations, and a single before/after pair
produced a confident wrong answer twice running on this one question.

#### Decision

- **Seal our own descriptor DTOs** - not for the 0.6%, but because it is free, tidy, a
  *semantically identical* elision (a subclass cannot exist, so nothing observable changes), and
  it lets the JIT devirtualise everywhere rather than only here. Verified: nothing in the repo
  derives from them, and the hand-written `partial` halves add interfaces only.
- **Offer `IgnoreUnknownSubTypes` to consumers via a schema option read by BOTH protogen (which
  emits the attribute) and `SchemaPlanBuilder` (which sets the plan flag)**, so ref-emit and the
  generated model cannot disagree. Not plan-only.
- **Change nothing about the check itself, in either shape.** Non-hierarchy it is ~1.3 cycles and
  worth 0.6%; hierarchy the shipped short-circuit BEATS every alternative at realistic data by
  2.27x, the non-short-circuit `|` form worst at 4.03x.
- **The shared-helper 21% is moot as a performance question** - 21% of 0.6% is 0.13%. The honesty
  argument for `AssertExpectedType(value, typeof(Foo))` over a method named only for its failure
  mode stands on readability alone, and should be decided on that.
- The disassembly drops from blocker to curiosity: worth knowing why an argument-passed `Type`
  beat a foldable `typeof`, but it decides nothing.

**Still owed, now low priority:** deep-graph impact as a deliberate axis (the descriptor payload
covers depth incidentally, at ~600 nested messages).


### B14. ~~Groups defeat measure-first~~ — **done 2026-08-14; write-side depth guard added with it**

Marc, 2026-08-14: *"group basically avoids the whole 'measure before you write' — it boosts write
perf hugely at the cost of reads needing to watch for a sentinel."* Exactly so, and that is what
makes the current behaviour backwards.

A grouped sub-message is framed by a start-group tag and an end-group tag, with **no length
prefix** — so its size is never needed. It is the one shape that should cost *nothing* to write.

Instead: `RawMemberMeasureBlocked` blanket-blocks on `member.DataFormat != ProtoDataFormat.Default`,
and a blocked member **removes its whole contract from the `measurable` set** — computed to a
fixed point, so the exclusion cascades to every referrer. A single grouped member therefore drops
its containing tree onto the classic write-to-count path, which is the slow one measure-first
exists to replace.

So a consumer who reaches for groups *because* they are faster gets the opposite. Two things to
do, and the first is much smaller than the second:

- **Stop blocking on it.** `Group` on a message member is perfectly measurable when its target is:
  the size is `startTag + Measure_(body) + endTag`, all known. The blanket `DataFormat != Default`
  test is right for the formats that genuinely need the engine (`FixedSize`, BCL kinds, wrapping)
  and wrong for this one.
  The tag lengths fold, since the field number is a compile-time constant — so a grouped member's
  measure is literally `Measure_(body) + <two constants>`.

- **Then skip the measure entirely**, which is the actual prize and is bigger than making the
  measure cheap. A length is only ever needed by a *length-prefixed* parent, so **an entire
  grouped tree can be written without a single measure pass**. That is not a faster measure, it is
  no measure: the measure leg is 3.76 µs of the gRPC shape's 15.45 µs (`docs/aot.md`), and a
  grouped tree deletes it rather than shrinking it.

  The trade is on the read side, and it is real: a length prefix lets a reader skip a sub-message
  without parsing it, while a sentinel has to be scanned for. That is the choice protobuf made in
  the other direction for proto3, and the reason this is a *format* decision rather than a
  free win.

Worth measuring rather than assuming, as ever — but unlike the seven flat micro-experiments this
one is a *structural* removal of work, not a cheaper way of doing it.

### B15. Depth is not synced on the raw → stateful transition — **open, narrow**

`RawWrite_` now carries a **remaining depth budget**, seeded from `state.RawDepthBudget` where the
stateful world hands off to the raw one, and never touches `writer.Depth` — the "raw API does not
maintain all the members" convention, and Marc's read of how the two worlds should meet.

The gap is the **reverse** transition. Where a raw body falls back to the stateful engine mid-way
(`state.WriteMessage(...)` for a member the raw path does not handle), the engine measures depth
from `writer.Depth`, which was last set at the *outer* boundary — so a deep raw chain that then
goes stateful under-counts, and the effective cap is larger than `MaxDepth`.

Narrow, and not a correctness hole in the sense B14 was: the cap still exists at both ends, it is
just not additive across a boundary. The fix is to push `writer.Depth` at the point of transition
rather than to thread anything further. Recorded rather than done because it wants a fixture that
actually crosses the boundary deeply, and no existing one does.

### B16. The emitted bodies declare one local per member, and large contracts have a lot of them — **open**

Marc, 2026-08-14, on two shapes in the generated code:

```csharp
var lengths2 = state.RawLengths;   // repeated per message member
var tmp7 = value.Something;        // one per member, whatever the type
```

**Scale first, because it changed the answer.** Sampling the test fixtures gave 8–11 locals per
body and the conclusion "the JIT already handles this". Marc pointed at a real model, and the
fixtures are not representative:

| body | locals | source |
| --- | ---: | --- |
| `RawWrite_TestEnormousDescriptor` | **1000** | corpus |
| `RawWrite_FileOptions` | 21 | an ordinary protogen DTO |

`state.RawLengths` appears 1613 times in the differential model, 33 in protogen's own serializer.

**Why the scale matters rather than just being a bigger number.** RyuJIT tracks a bounded number
of locals for liveness; beyond that limit locals become *untracked*, and untracked locals are
neither enregistered nor lifetime-merged. So "the register allocator already reuses slots by
liveness, which beats folding by type" is true for a ten-local body and stops being true long
before a thousand. Marc's suggestion — fold the temporaries **by type**, via a map in the emitter
— would cut a 1000-local body to roughly one local per distinct type, back inside the tracked set.

**A second cost, not previously connected.** `[module: SkipLocalsInit]` is applied to
protobuf-net's own assemblies, but **the generated model lands in the consumer's assembly**, which
has no such attribute — so every one of those locals is zero-initialised on each call, in their
build rather than ours. Per-method `[SkipLocalsInit]` is the targeted fix and is **not usable**:
it requires `AllowUnsafeBlocks` in the consumer's project, which a serializer has no business
demanding.

**The `lengths` half is separate and simpler.** `state.RawLengths` is three dependent field loads
(`state._writer` → `writer.netCache` → `netCache._rawLengths`); Roslyn does not CSE across
statements, and the JIT cannot hoist the two heap loads across the intervening `TryGetValue` /
`Measure_` / `RawWrite_` calls. One method-level local removes N−1 of them.

That hoist is **safe today but needs a comment saying why**: `_rawLengths` is genuinely reassigned
— `NetObjectCache.InitializeFrom` swaps it for the measure→write hand-off, and `ClearAndMaybeTrim`
may replace it — but both happen at the boundaries, never mid-body. If the hand-off ever moved,
a hoisted local would go stale silently.

**Unmeasured, and deliberately so far.** Nothing in this arc has targeted *dependent load chains*
or *local-count pressure*, so unlike the seven flat micro-experiments there is no census result
predicting the outcome. Worth measuring on a large contract rather than on the descriptor payload,
since the effect scales with member count and `descriptor.proto`'s messages are mostly small.

## C. Schema front-end (`[ProtoSchema]`)

The feature lands on the **`aot-schema-model`** branch; the design and the findings are in
`notes/aot-schema-model.md` there. The gap list is here, so there is one to consult.

**Already verified on bytes** against `RuntimeTypeModel` in both directions: every scalar width
including the `sint`/`fixed` spellings, `string` with protogen's proto3 `[DefaultValue("")]`
guard, `bytes`/`bool`/`float`/`double`, enums as members, nested messages and enums, `repeated` in
both protogen shapes, maps including the `bool`-key case, `import`, and the naming rules
(pluralisation, collision avoidance, package → namespace).

### C1. ~~Point the existing schema corpus at this path~~ — **done, 2026-08-14**

`SchemaSourcedCorpusProbeTests` runs all 268 schemas of `protobuf-net.Reflection.Test/Schemas`
through the front-end and tallies the verdicts. It is a **measurement, not a gate**: it asserts
only that a verdict is reached for every schema rather than thrown, so the numbers are reviewed
rather than defended.

```
schemas: 268   built: 241 (1,369 contracts)   refused: 27   unparseable: 0
     16  repeated enum          (the packed arm, B1)
      5  enum as a map value    (C4, same cause)
      6  proto2 `group`         (C6)
```

**It found a crash on its first run**, which is the whole argument for doing this before working
the list: an imported type reference (`.google.protobuf.Timestamp`) threw
`KeyNotFoundException` **out of the plan builder** — in a real build, an unhandled source-generator
crash taking the consumer's compilation with it. Two causes, both fixed: the type index skipped
files that were not `IncludeInOutput`, so imported types were never indexed; and the unary member
path indexed the dictionary directly instead of `TryGetValue`. Every lookup now refuses with a
diagnostic instead.

It also found a **spurious diagnostic**: 14 schemas were refused for "no messages", which is not a
failure — a schema of only enums, services or extensions legitimately contributes no contracts.
That was a warning telling the consumer off for something entirely valid.

**What it says about the remaining list**: 21 of the 27 refusals are the *single* parked item
(B1), so unblocking that recovers three quarters of them. The only genuinely missing *feature* in
268 real schemas is `group`. Everything else on the C list — extensions, well-known types, schema
options, cross-schema references — did not refuse a single schema, which is a much better basis
for ordering than the one I invented.

### C2. ~~`oneof`~~ — **done, 2026-08-14**

protogen emits it as **ordinary members with `ShouldSerialize{Name}()`**; the
`DiscriminatedUnion32Object` is a private field behind the property. The plan already models that
as `WriteCondition`, which emits `if (value.ShouldSerializeA())`. A couple of lines, not a feature.

### C3. ~~proto3 `optional`~~ — **done, 2026-08-14**

The same shape: a `ShouldSerialize{Name}()` over a private nullable backing field. Also just a
`WriteCondition`.

### C4. Enum in a `repeated`, or as a map value — blocked on B1

The proxy is **free**: naming the enum on the plan is all `EmitEnumProxies` needs, and that was
built and working. What blocks it is the packed-empty disagreement in B1. Note an enum map **key**
cannot occur at all — proto forbids it (*"invalid map key type (only integral and string types are
allowed)"*).

### C5. ~~A map value that is itself a map~~ — **not a gap**

Withdrawn: proto forbids it. `map<string, map<string, int32>>` does not parse (*"expected
Symbol '>'"*), so the shape cannot occur; the refusal in the code is a defensive branch against
an unresolvable value type, not a feature gap. A map whose value is a **message that happens to
contain a map** is ordinary, and is supported.

Found by a scope test asserting the refusal and getting a plan instead — the third "gap" on this
list to evaporate on contact (with the enum map key, and the enum-proxy plumbing). Worth the
pattern: **a refusal nobody has tried to trigger may be describing an impossibility.**

### C6. proto2 — **`required` and defaults fixed 2026-08-14; `group` still deferred**

Not a deferral on inspection: `required` and `[default = x]` were **silently wrong on the wire**,
and the fix was two lines. `group` remains refused, which is safe.

The bug went both ways, which is why neither showed up as a missing feature:

| sample | ref-emit | us, before |
| --- | --- | --- |
| `new Required()` (all zero) | `08-00 12-00 18-00` | **`(empty)`** — every required member dropped |
| `new Defaulted()` (nothing set) | **`(empty)`** | a full payload — every declared default written |
| `Defaulted` with values equal to the declared defaults | writes them | omits the enum one |

The cause is that protogen presence-tracks proto2 differently from how the plan assumed:
`required` becomes `[ProtoMember(..., IsRequired = true)]`, which **drops** the write guard; and
**every** proto2 `optional` — with or without a declared default — is backed by a nullable field
and a `ShouldSerialize{Name}()`, so presence rather than value decides. We were comparing each
member against its type's zero, and the generated getters return the declared defaults.

So `[default = x]` needs **no handling of its own**: the condition REPLACES the value guard, so
the declared default never reaches a comparison. Two lines: set `IsRequired` from the label, and
extend `WriteCondition` to proto2 optionals.

`proto2`-ness has to come from the **file**, not the field: a proto3 singular field is also
`LabelOptional` in the descriptor, so the label alone cannot tell them apart and getting it wrong
would put a `ShouldSerialize` guard on every proto3 field in the corpus. Note `syntax` is *absent*
in most proto2 files, since it is the default.

**Why this was invisible:** the byte gate was proto3-only, and the corpus probe only asks whether
a plan BUILDS. `SchemaProto2ProbeTests` now reports the plan's view of each proto2 shape, and
`Schemas/legacy.proto` puts them on the byte gate — with samples chosen so the guard can fail: an
all-zero `required`, and values explicitly set EQUAL to their declared defaults.

Still deferred: **`group`** — 6 of the corpus's 268 schemas, and the only genuinely missing
schema feature. It is refused rather than mis-emitted.

### C7. ~~`extend` / extensions~~ — **already works, 2026-08-14**; well-known types and schema options deferred

Probed rather than assumed, and the extension half needed nothing: protogen emits extension
accessors as **static extension methods** (`Extensible.GetValue<T>(obj, 100)` /
`AppendValue<T>(obj, 100, value)`) rather than as members, so they are consumer API and never
reach the plan. The message carrying `extensions 100 to 199` is just an extensible contract, which
the AOT generator has supported throughout — `SchemaPlanBuilder` already marks every
schema-sourced contract `ProtoExtensibleKind.Untyped`, matching protogen's `: IExtensible` on
every message. An `extend` block adds no members and needs no plan.

Worth knowing: those accessors DO work under native AOT, but only for the generic overloads at
the default `DataFormat` — which is exactly what protogen emits. See `docs/aot.md`'s known issues.

Still deferred here: the **well-known types** (`Timestamp`, `Duration`, `Any`), where the
compatibility level starts to matter, and **schema-level options** that can change the contract.

### C13. ~~`ProtoFileGenerator`'s language-version detection stops at C# 9~~ — **fixed 2026-08-14**

`ProtoFileGenerator` maps `context.ParseOptions.LanguageVersion` onto the string protogen's
`GeneratorContext` parses, and the switch ends at `LanguageVersion.CSharp9 => "9"`, with
`_ => null` for everything above. `CommonCodeGenerator.GeneratorContext.Supports(Version)` then
reads null as **"default is highest"** and returns true.

So today **every C# 10+ project reports no language version to the code generator**, and every
`Supports(...)` test passes unconditionally.

**Harmless right now, and not harmless in general.** Nothing protogen emits is newer than C# 9, so
no consumer can currently hit it — but the failure mode is emitting syntax a consumer's compiler
does not accept, i.e. a build break in a project they did not change. Any feature gated on
language version (C12 is the first) is inert until this is fixed, which makes it a prerequisite
rather than a tidy-up.

Found by Marc questioning a claim in C12 that the Roslyn path was version-blind. It is not blind —
it reads the real value — but it discarded everything above C# 9, which has the same effect and is
harder to see.

**Fixed by decoding arithmetically rather than by naming constants.** From C# 8 the enum is
`major*100 + minor` (`CSharp8 = 800`, `CSharp10 = 1000`, `CSharp12 = 1200`), so the mapping names
nothing — which it must not, since this assembly compiles against the Roslyn 4.3.1 baseline where
`LanguageVersion.CSharp12` does not exist. That is the same reason `ProtoModelGenerator` already
spells its AOT floor `(LanguageVersion)1200`, and following that precedent means **C# 15 needs no
change here either**.

Safe to switch on, checked rather than assumed: every existing `Supports(...)` gate in
`CSharpCodeGenerator` is C# 3, 6 or 7.1, so reporting 10+ where nothing was reported before
satisfies all of them and changes no output — 403 BuildTools, 556 Reflection (both TFMs) and 1431
conformance tests pass with no golden drift.

**A correction worth keeping**, since it is the exact failure this file exists to prevent: the
first cut assumed the values were 10, 11, 12 and range-checked `>= 10 and < 100`. They are 1000,
1100, 1200 — so it matched *nothing*, and was a silent no-op that a hand-written test expectation
would have confirmed happily. Marc's aside that "for AOT *we* demand C# 12" is what led to the
existing `(LanguageVersion)1200` constant and the real numbering. `LanguageVersionMappingTests`
therefore **derives** its expectations from the enum rather than restating them, so the same
mistake cannot pass twice.

### C14. protobuf **editions**, and refreshing `descriptor.proto` — **future, likely 4.1**

Marc, 2026-08-14. Editions replace the proto2/proto3 split with per-feature settings, and the one
that matters here is **`features.message_encoding = DELIMITED`** — which *resurrects group
encoding*, deprecated in proto3 and now back as a first-class choice. Marc's note: this is
substantially what he recommended to the protobuf team 15+ years ago, and it is what protobuf-net
has implemented throughout as `DataFormat.Group`.

So we are unusually well placed: the wire form is already implemented on both paths, and the
`DataFormat.Group` plumbing is the same plumbing editions needs. What is missing is the *schema*
half — recognising an `edition = "2023"` file and mapping its feature set onto the options
protobuf-net already has.

**A prerequisite, and one we have already stubbed a toe on:** our `descriptor.proto` needs
refreshing. It is behind upstream — regenerating it during the sub-type work turned up a missing
`FieldOptions.unverified_lazy` that had been silently landing in extension data — and editions add
a good deal more (`Edition`, `FeatureSet`, `FeatureSetDefaults`, and the `features` fields on every
options message). That refresh is worth doing on its own, ahead of and independent of editions
support, since a stale descriptor mis-parses schemas rather than merely lacking features.

See also **B14**: groups currently *defeat* measure-first rather than benefiting from it, which
would make an editions-delimited payload slower rather than faster until it is fixed. That is the
ordering constraint between these two items.

### C12. Extension *properties* for `extend`, via C# 14 extension blocks — **tracked, opt-in when built**

Marc, 2026-08-14. protogen emits extension accessors as static methods (see C7):

``` c#
public static string GetNote(this Base obj) => Extensible.GetValue<string>(obj, 100);
public static void SetNote(this Base obj, string value) => Extensible.AppendValue<string>(obj, 100, value);
```

C# 14 **extension blocks** allow extension *properties*, so the same thing could read as an
ordinary member — `thing.Note = "x"` rather than `thing.SetNote("x")`:

``` c#
public static partial class Extensions
{
    extension(Base obj)
    {
        public string Note
        {
            get => Extensible.GetValue<string>(obj, 100);
            set => Extensible.AppendValue<string>(obj, 100, value);
        }
    }
}
```

Four things to settle before building it, in the order they bite:

- **TWO gates, and they are AND — not either** (Marc). It is API-breaking, so the primary gate is
  an explicit opt-in: another protogen option in the shape of `SubTypes`, honoured on all three
  routes, **off by default**. The language version is a *second* guard on top, never the enabling
  condition.

  **This distinction is the whole point**: nobody should get an API break as a side effect of a
  machine acquiring a newer SDK.

  **PREREQUISITE, found by Marc questioning the claim above.** The build-tools path *does* read
  the language version from the project — `context.ParseOptions.LanguageVersion` in
  `ProtoFileGenerator` — so it is not blind, as this note first said. But **the mapping stops at
  `CSharp9`**: everything newer falls to `_ => null`, and `ctx.Supports(null)` returns *true*
  ("default is highest"). So every modern project currently reports **no** language version, and a
  langver gate would be **inert** — the mechanism would emit C# 14 syntax into a C# 10 project.
  That switch has to be extended before the second gate means anything, and it is a live
  latent bug rather than a new requirement: it is simply unexercised because nothing yet emits
  syntax newer than C# 9.

  The **CLI** is genuinely ignorant unless `+langver=` is passed, so the opt-in has to carry the
  weight there regardless — which is a second reason it, not the version, is the primary gate.

  So: opt-in off → nothing ever changes. Opt-in on, language version too low → a **diagnostic and
  fall back to methods**, not a build break; that is the failure mode the `PBN2000` floor exists
  to avoid elsewhere, and the reason it is a fallback here rather than a hard floor is that the
  consumer asked for a nicety, not for a wall.
- **`repeated` extensions do not fit the shape.** Today they are `GetTags` (returning
  `IEnumerable<T>`) plus `AddTags` — and a property cannot express "append". So this is not a
  blanket swap: either repeated extensions keep their methods (a mixed API, which is ugly but
  honest) or the property returns something with an `Add`, which changes the semantics. **This is
  the part that needs a decision, not just work.**
- **No AOT implication whatsoever.** These are consumer API and never reach the plan — see C7 —
  so nothing in the AOT generator or the byte gate changes. It is purely a protogen codegen
  nicety, which is also why it is safe to defer indefinitely.

### C8. ~~Cross-schema type references~~ — **covered, 2026-08-14**

The corpus run covers this: the type index is built across every file in the set including
imports, and 241 of 268 corpus schemas build — most of which import. It was the *absence* of
import indexing that threw `KeyNotFoundException` out of the plan builder on the first run, so
this is now tested by the sharpest possible route rather than untested.

### C9. The schema is parsed twice — deferred

`ProtoFileGenerator` parses each schema to emit DTOs; `AddSchemas` parses it again to build the
plan. They cannot share it, because sharing means one generator.

Two things make this less urgent than it sounds: the DTO generator is **not incremental** so it
re-parses on every compilation, while the model side re-parses only on change — meaning the *new*
parse is the cheaper one; and the real fix is B8. Worth knowing that the second parse was
initially **wrong** rather than merely redundant (no `IFileSystem`, so `import` failed), which is
the reason to distrust "just parse it again" as a general answer.

### C10. A precedence rule with no reachable scenario — open

`AddSchemas` resolves a name clash between a schema-derived and a symbol-derived contract by
letting the **symbol** win. Plausible — a consumer who wrote the DTO by hand meant it — but no
case has been found where the clash arises, and **a rule with no reachable scenario is a rule
nobody has tested**. It may want to be a diagnostic instead of a silent precedence.

### C11. `ProtoFileGenerator` keys schemas by leaf name — known limit

`Path.GetFileName` then `set.Add(name, …)`, so two same-named `.proto` files in different
directories do not both produce DTOs. Pre-existing and not the model path's, but it means the
ambiguity `PBN2021` reports is only reachable in a project whose DTO generation is already
incomplete. The diagnostic still earns its place: it names the problem where the alternative is a
silent pick.

## D. Decisions owed by a human

| question | detail |
| --- | --- |
| **manual review of the write-emitted goldens** | 55 changed shape when `int32` moved onto the raw path. **The one item still open** |
| ~~the tag ladder: keep or revert?~~ | **Settled 2026-08-14: narrowed, not reverted.** Split by *dynamic population* rather than kept or dropped wholesale — the one- and two-byte arms and the bool fold stay, the folded 3/4/5-byte arms go back to the shipped encoder. Two follow-on micro-ideas (`&` for `&&`, hoisting `RemainingInCurrent`) were answered by inspection and need no measurement; the reasoning is in the comment block |
| ~~when to rewrite `docs/aot.md`'s "needs its own project" advice~~ | **Done**, pre-emptively on `aot-schema-model`, so it arrives with the merge |
