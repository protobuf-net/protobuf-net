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

### B1. Packed writes — **premise is STALE; re-verify before doing anything (2026-08-14)**

This was "the sharpest item, the only one suspected of being a live bug". Three checks say the
premise does not hold up, and none of them needed new code:

**1. `IsPacked` IS supported, contrary to `AGENTS.md`.** `ListOptions.input.cs` carries five
`IsPacked = true` members, and we emit `WireTypeVarint` **without** `OptionPackedDisabled` for
them — byte-identical to `ListOptions.reference.cs`, and the differential has been comparing them
all along. The claim that "we always emit the disabled form" was simply out of date.

**2. Packing is the WRITER'S FREE CHOICE** (Marc), and the spec says so in as many words —
[the encoding guide](https://protobuf.dev/programming-guides/encoding/#packed), read 2026-08-14:
*"Protocol buffer parsers must be able to parse repeated fields that were compiled as `packed` as
if they were not packed, and vice versa."* **Declining to pack is therefore never a wire bug** —
which removes the whole category this item was filed under.

Three further points from that page, all load-bearing somewhere:

- **only repeated primitive numeric types may be packed** — those using `VARINT`, `I32` or `I64`,
  which includes enums and excludes `string`/`bytes`;
- **parsers must accept *multiple* packed records for one field and concatenate the payloads** —
  which is what `RepeatedFieldOccurrencesMergeIdentically` already exercises by concatenating
  sample payloads;
- **the page is silent on empty/zero-length packed fields.** So `WriteZeroLengthPackedHeader` is a
  permitted choice rather than a requirement, and either form is readable — meaning the original
  "disagreement" would be a byte difference from ref-emit, not an interop problem. That is a much
  weaker thing than the item claimed.

**3. protobuf-net packs only when it can cheaply size the elements.**
`RepeatedSerializer.Write` takes the packed branch only when
`serializer is IMeasuringSerializer<TItem>`. `EnumSerializer<TEnum>` implements
`ISerializer<TEnum>` and `ISerializer<TEnum?>` but **not** the measuring interface — so a repeated
enum is *never* packed by protobuf-net, on either path, even though `TypeHelper.CanBePacked`
returns true for enums. Both paths fall through to the unpacked loop identically, so there is
nothing to disagree about.

So the original observation — "an empty packed collection emits a zero-length field where ref-emit
writes nothing" — needs reproducing before it is believed. It may have come from one specific
configuration rather than being general, and the attribution to `IsPacked` being unsupported is
definitely wrong.

**What genuinely remains** is the *opportunity*, not a bug: making enums (and other kinds) packable
by giving them measuring serializers, which is a size win on the wire and the thing B19's
vectorised sizing would serve. That is worth doing on its own merits — but it is optional
behaviour, not a correctness fix, and it should be measured rather than assumed.

Note also that `descriptor.proto` **does** use `[packed = true]` — on `SourceCodeInfo.Location`'s
`path` and `span` — but source info is normally stripped, so the benchmark payload carries none.
Any packed work needs a **bespoke payload** for both correctness and performance (Marc); the
census already predicts the descriptor set would show nothing.

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
  measure cheap. A length is only ever needed by a *length-prefixed* parent, so a grouped tree can
  be written without a measure pass. That is not a faster measure, it is no measure: the measure
  leg is 3.76 µs of the gRPC shape's 15.45 µs (`docs/aot.md`), and a grouped tree deletes it
  rather than shrinking it.

  **With one condition I first stated too strongly** (Marc): it holds only while **nothing above it
  needs a length**. A grouped subtree hanging under an ordinary length-prefixed parent is still
  walked, because the parent's own length includes it. So the property belongs to the **path from
  the root**, not to the member — "a grouped tree needs no measure" is true of a tree grouped *all
  the way up*, and false of a grouped subtree. `CallbackPassesFollowTheNearestLengthPrefixedAncestor`
  pins the distinction:

  ```
  group at root,        stream / buffer-writer : [false]  / [false]
  group under a length, stream / buffer-writer : [false]  / [true, false]
  ```

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

### B16. Locals in the emitted bodies — **`lengths` done 2026-08-14; `tmpN` folding agreed, for the STACK**

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

#### Measured: three arms, and the per-site local is the worst of them

`src/NanoBench/LengthCacheAccessBenchmarks.cs`, 8 sites, mirroring the real three-load chain with
real `TryGetValue` calls between the sites — without those the JIT hoists everything and all three
arms measure identically, which would have been a meaningless pass.

| arm | mean | ratio |
| --- | ---: | ---: |
| per-site local (as it was) | 43.14 ns | 1.00 |
| one hoisted local | 38.21 ns | **0.89** |
| no local, read inline | 38.53 ns | **0.89** |

Hoisting and reading inline are a **dead heat**, so the JIT inlines the property chain perfectly
well: what costs is materialising a distinct local per site. **Applied: no local at all** — same
speed as hoisting, simpler to emit, and it removes the staleness hazard a hoisted local would have
carried across an `InitializeFrom` swap.

**The `tmpN` locals are a different case and must stay** (Marc): `value.Something` is *consumer*
code, so reading it twice is a **correctness** risk, not merely a cost. `state.RawLengths` is ours
and known to be a field chain, which is exactly why it needs no local. The distinction is "do we
know this is cheap and stable", not "is it a property".

#### `tmpN` folding by type — agreed, and the reason is the STACK, not throughput

Marc: *"I am still inclined to do the reuse-thing, if only for the stack problem."* Agreed, and the
arithmetic supports it over any throughput argument:

- `TypeModel.DefaultMaxDepth` is **512**;
- a 1000-local body is roughly an **8 KB frame**;
- 512 × 8 KB ≈ **4 MB**, against a 1 MB default thread stack.

So for large contracts the depth guard **does not bound anything** — the stack dies around 128
frames, long before 512. `MaxDepth` silently assumes small frames, and the generated bodies for
big contracts break that assumption. That makes this correctness-adjacent rather than a
micro-optimisation, and it is the argument for doing it.

Folding by type also brings a 1000-local body back inside RyuJIT's tracked-local limit, past which
locals are neither enregistered nor lifetime-merged — so the throughput effect, whatever its size,
points the same way.

**Not a union, though.** Marc floated overlapping the non-reference temporaries (refs cannot be
unioned — the GC must track them). Legal, but likely counterproductive: a union member is
address-taken and therefore memory-resident, which **defeats enregistration entirely**, making
every small body worse to fix the large ones. Folding by type gets most of the reduction while
leaving ordinary locals the JIT can still keep in registers.

#### Attempted and REVERTED: scoping is not folding

I substituted a cheaper mechanism for Marc's idea — wrap each member's temporaries in a `{ }`
block and let Roslyn's slot allocator reuse the slot, on the reasoning that the compiler knows the
exact types and cannot mis-key them. Measured before/after on IL local counts
(`MethodBody.LocalVariables`), Release:

| body | before | after |
| --- | ---: | ---: |
| `RawWrite_Lists_Repeated` | 50 | **50** |
| `Measure_Lists_Repeated` | 40 | **40** |
| `RawWrite_Generic_Holder` | 18 | **18** |

**Byte-identical. Reverted**, rather than keeping churn in every generated body for a hypothesis
that did not show.

**But the null result does not test Marc's idea — it tests my substitute for it.** Scoping asks
the compiler to fold; *folding* means emitting genuinely fewer distinct locals, from a
generator-side name map. Those are different changes and only the first has been measured. The
original idea remains open and untested, and the same applies to the `lenN` temporaries Marc
raised alongside — same family, same mechanism, same open question.

Two methodological notes worth keeping, both mine to own:
- **Roslyn reuses slots in Release only** — Debug keeps locals alive for the debugger. A first
  measurement taken under `dotnet test` (Debug by default) reported 70 and meant nothing.
- The probe test that produced these numbers was **deleted**: its threshold was invented, it
  failed in the default Debug configuration for an implementation-detail reason, and the
  hypothesis it was written to defend turned out false.

#### The case for folding stands anyway, and does not rest on a benchmark

Marc: *"if nothing else, it'll make the compiler's job easier."* Right, and that argument is
untouched by the null result above, because neither thing I measured tests it — I measured
**scoping** (not folding) on a **~50-local** body (not the 1000-local one that motivated this).

Emitting genuinely fewer distinct locals reduces, by arithmetic rather than by hypothesis:

- **IL size** and Roslyn's own analysis work on very large bodies;
- **RyuJIT tracked-local pressure** — past its limit locals are neither enregistered nor
  lifetime-merged, and 1000 is comfortably past it;
- **`.locals init` zeroing**, which the *consumer's* assembly pays because
  `[module: SkipLocalsInit]` is on protobuf-net's assemblies and not on theirs, and the per-method
  form is unusable (it requires `AllowUnsafeBlocks` in their project);
- **frame size**, which is the one that matters: `MaxDepth` of 512 only bounds recursion if frames
  are small.

So the right instrument is **the local count on a large contract**, not a nanosecond benchmark —
and the honest status is that the idea is agreed and unimplemented, not that it was tried and
failed. The same applies to the `lenN` temporaries.

### B17. Callbacks and measure-first — **answered; a `BeforeSerialize` contract silently loses measure-first**

Marc asked how we stand. Checked rather than assumed, and the answer is a real limitation that was
not written down anywhere:

- **`RawMeasurableShape` requires `!HasCallback(contract, BeforeSerialize)`.** So a contract with a
  before-serialize callback is **excluded from measure-first entirely** and drops to the classic
  write-to-count path — the very path measure-first exists to replace. That is correctness-driven
  rather than an oversight: a before-serialize callback may *mutate the object*, so anything
  measured before it ran would be measuring the wrong state, and `Measure_` has nowhere to run it.
- **`AfterSerialize` is fine** and is emitted at the end of `RawWrite_`.
- **The measure pass invokes no callbacks at all**, and cannot: `Measure_(value, depth, lengths)`
  holds no `ISerializationContext`.
- `ProtoWriter.IsMeasuring(context)` exists, but it serves the **classic** path, where measuring
  *is* writing to a null writer and callbacks therefore fire on the counting pass as well as the
  real one. On the measure-first route there is nothing to disambiguate, because nothing fires.

**So the cost of a callback today is not "the callback runs" — it is that the whole contract, and
by cascade everything referencing it, falls off the fast path.** That is worth knowing before
recommending callbacks to anyone.

#### What `ref state` would buy, and what it would cost

Marc's proposal: give `Measure_` `ref state` instead of `(depth, lengths)`. His own observation
settles the cost side — **`ref state` is one pointer, exactly what the `lengths` reference already
costs** — and `depth`/`lengths` would then come *off* the state, so the signature gets **shorter**,
not longer. There is no performance argument against it.

It would let callback-bearing contracts join measure-first, which is the actual prize. Two things
to settle first, and only the second is hard:

1. **Double-fire is REQUIRED, not merely acceptable** — settled by prior art, not open. Marc
   recalled it; `CallbackMeasurePassTests` now pins it against the runtime model:

   | route | `BeforeSerialization` fires | `IsMeasuring` |
   | --- | ---: | --- |
   | plain `Serialize` | **once** | `false` |
   | `Measure` + `Serialize` | **twice** | `true`, then `false` |

   The reasoning is in `df529277`: *"the measure pass is a real write to a counting writer, so a
   serialization callback runs once per pass — correct for the common shape (populate a member
   that is then serialized: both passes MUST see the same object). Suppressing wholesale is worse
   than the doubling, because the two passes would then see different objects and the length check
   would throw."*

   So "fire once before the measure, not in the write" — which I first called the cleaner
   alternative — is the **broken** one. If `Measure_` ever fires callbacks it must fire them per
   pass, exactly as the classic path does, with `IsMeasuring` distinguishing them.

   This also makes the current refusal **load-bearing rather than conservative**: firing
   `BeforeSerialize` only in `RawWrite_` would let the object change between measuring and writing
   and commit a wrong length. `RawMeasurableShape`'s exclusion is the only thing keeping that
   correct today — so relaxing it is not "let callbacks through", it is "fire them per pass in
   `Measure_` too", which is now a settled design rather than an open question.

   **And the doubling is not confined to the explicit `Measure` API — it is already per-backend**,
   which Marc identified and `CallbackMeasurePassTests` now pins. For a *nested* contract, with
   nobody asking to measure:

   | route | fires | `IsMeasuring` |
   | --- | ---: | --- |
   | `Serialize` to a **stream** | **once** | `false` |
   | `Serialize` to an **`IBufferWriter`** | **twice** | `true`, then `false` |

   The stream writer reserves, writes and back-fills the length (shuffling bytes when the varint
   width changes), so it crawls once; the buffer-writer computes the length, writes the prefix,
   writes for real and then **validates** (`Length mismatch; calculated 'x', actual 'y'`), so it
   crawls twice. A consumer's callback side-effect therefore already depends on which output they
   chose — undocumented until now.

   **Decision (Marc, 2026-08-14): make twice the consistent normal for both**, rather than leaving
   the stream as the exception. It is also where measure-first leads anyway, since that *is*
   measure-then-write.

   **But "always twice for nested" is not the rule, and cannot be** (Marc). The count is set by the
   nearest **length-prefixed ancestor**, not by the member's own framing: a *grouped* member needs
   no length, so it is visited once — **unless** something above it does need one, at which point
   the parent's measure walks straight through it. Pinned:

   | shape | stream | buffer-writer |
   | --- | --- | --- |
   | group at the root | `[false]` | `[false]` |
   | same group under a length-prefixed parent | `[false]` | `[true, false]` |

   So the target is "**once per pass over this node**", where the number of passes is a property of
   the path from the root — not "twice, always". This is also why B14 and B17 cannot be settled
   independently: making grouped trees measure-free changes how often callbacks inside them fire.

   **The fair demand is "at most twice"** (Marc), for a node not duplicated in the tree — and
   **the length cache already buys it**. Every length-prefixed ancestor needs a length for
   everything beneath it, so a naive measure-by-writing would re-walk the innermost node once per
   ancestor, i.e. exponentially in depth. Memoising a sub-message's measured length by reference
   collapses that to one measure pass plus one write pass. **Verified at depth 3: two calls, not
   eight** — `AtMostTwiceHoweverDeepTheNesting`.

   That is the invariant worth defending, and it is now guarded: if that test ever reports more
   than two, the cache has stopped working and the cost has gone exponential in depth rather than
   linear — which would be a far bigger problem than the callback count that exposed it.

   The cost to weigh when doing it: on the **classic** stream path, replacing one back-fill shuffle
   (a memmove only when the varint width changes) with a full second crawl is a real slowdown. On
   the **measure-first** path it is not, because the measure is arithmetic rather than a write —
   which is an argument for converging the two as measure-first widens (see B1, B5) rather than
   changing the classic stream writer on its own.
2. **Coupling.** `Measure_` is today a pure arithmetic walk with no writer in sight, which is what
   makes it trivially testable and independently callable. Taking `ref state` ties it to a live
   writer. That is the real trade, and it is a design call rather than a measurement.

Note this also interacts with **B1** (packed writes) and **B5** (counting mode for mixed
contracts), both of which want the measure path to reach further into shapes it currently declines.

### B18. ~~`ClassicEmit` is never exercised by any gate~~ — **closed 2026-08-14**

Marc, 2026-08-14: *"remember to cross-check classic-emit in all cases... classic-emit should be
functionally equivalent, if slower."* Checked, and **no test project sets
`[ProtoModel(ClassicEmit = true)]`** — not one fixture, not the conformance model, nothing. So the
generator's classic path is emitted by nobody and compared against nothing.

That matters more than a normal coverage gap, because `ClassicEmit` is the **escape hatch** we
point people at (it is named in `ProtoWriter.State.Raw.cs`'s own error text for the
non-seekable-stream case). It is the fallback that is least tested.

Everything on this branch — proto2 `required`/defaults, groups taking the raw path, the write-side
depth guard, dropping the per-site length local — was verified **only on the raw path**. The
differential proves generated-vs-`RuntimeTypeModel`; it does not prove
generated-classic-vs-generated-raw.

**Closing it is nearly free, with one obstacle.** `DifferentialTests.DiscoverModels()` enumerates
every `[ProtoModel]` type in the assembly, so a twin model declaring `ClassicEmit = true` over the
same seeds is picked up and compared automatically — no new harness. The obstacle is that
`GetSamples` resolves samples **by name convention**: `<Stem>Model` → `<Stem>Samples.Values`. A
twin therefore needs either a name that still resolves, or a small change to that resolver (strip a
known prefix/suffix before looking up the samples class). That is the whole job.

**Closed.** Five `ClassicEmit` twins now live in `src/AotConformanceTests/ClassicEmitTwins.cs`,
chosen for where the paths could plausibly diverge rather than for breadth: groups (raw path, no
measure), callbacks (which disqualify a contract from measure-first), repeated members (where B1
lands), nesting (length prefixes and the cache), and scalars as the control. They are declared in
the test project rather than beside the fixtures, which are shared with the golden tests and
`AotRefGen`.

`DifferentialTests` picks them up automatically and compares each against `RuntimeTypeModel`:
**1445 → 1544 cases**. And per Marc — *nothing precludes two type models over the same domain, one
classic and one not* — `ClassicVsRawTests` compares the twins against their raw siblings
**directly**, in one build. That is sharper than two separate comparisons against ref-emit, which
two models diverging the same way would both pass.

**Verified to be a real second path, not a silently-ignored flag**: `GroupedElementsModel` emits 3
`RawWrite_` and 3 `Measure_` bodies; its classic twin emits none of either.

### B1 addendum: what the packed code actually says (2026-08-14)

Gathered while starting B1, and it may **invert** what B1 states above — flagged rather than
corrected, because it has not been verified end to end yet.

`RepeatedSerializer.WritePacked` accepts `Fixed32` (count×4), `Fixed64` (count×8) and
`Varint`/`SignedVarint` (measured per element), and throws for anything else. So the packable set
is the protobuf one — **enums pack**, being varint, which is why protogen marks a repeated enum
`IsPacked = true` (Marc asked; the answer is yes).

The decision site reads:

```csharp
if (TypeHelper<TItem>.CanBePacked && !features.IsPackedDisabled()
    && (count == 0 || count > 1) && serializer is IMeasuringSerializer<TItem> measurer)
{
    if (count == 0) WriteZeroLengthPackedHeader(ref state, fieldNumber);
    else WritePacked(...);
}
```

So the runtime **does** write a zero-length header for an empty *packed* collection. B1 above says
we emit a zero-length field "where ref-emit writes nothing" — that looks backwards: the difference
is more likely that **we never enable packing at all** (`AGENTS.md`: the `IsPacked` argument "is not
supported yet, so we always emit the disabled form"), so our empty collection writes nothing while a
genuinely packed one writes the header. Same disagreement, opposite attribution — and the fix is
"support `IsPacked`", not "stop writing a header".

**Verify before building.** Both directions are cheap to test and the wrong attribution would send
the work the wrong way.

### B19. Vectorised sizing for a packed varint span — **measured: 1.8×-6.6×, the first non-flat result in this arc**

Marc, 2026-08-14: for a large span of integers — `CollectionsMarshal.AsSpan` on a `List<T>`, or an
array directly — is there anything SIMD can do to *size* them, given more than a vector's width?

**First, the scoping, which cuts most of it away.** `RepeatedSerializer.WritePacked` already handles
the fixed widths in O(1): `Fixed32` is `count * 4`, `Fixed64` is `count * 8`. So floats, doubles
and the fixed integer forms need **no sizing at all** — the opportunity is exactly the
`Varint`/`SignedVarint` arm, which is the one that measures per element.

**Second, it needs no leading-zero intrinsic**, which is what makes it plausible. A varint length is
a threshold ladder, and thresholds vectorise trivially:

```
len(v) = 1 + (v >= 2^7) + (v >= 2^14) + (v >= 2^21) + (v >= 2^28)      // uint32
```

A vector comparison yields all-ones (`-1`) per lane, so the total for a block is
`count - Σ(mask₇ + mask₁₄ + mask₂₁ + mask₂₈)` — four compares and four adds per vector, no
horizontal work until the very end. At `Vector<uint>`'s 8 lanes that is roughly **one instruction
per element**, against a branchy scalar loop today. `uint64` is the same shape with nine thresholds.

The wrinkles, none fatal:

- **`int32`/`int64` sign-extend**: a negative value is always the 10-byte form, so compute the
  unsigned ladder and blend `10` in where the lane is negative;
- **`sint32`/`sint64`** zigzag first — `(v << 1) ^ (v >> 31)` is itself vectorisable — then take the
  unsigned ladder;
- **the span has to exist.** Arrays and `List<T>` via `CollectionsMarshal` are fine; anything only
  enumerable is not, so this is a fast path beside the loop rather than a replacement for it.

**And there is a free correctness net**: `WritePacked` already validates the measure against what
was actually written (*"packed encoding length miscalculation for … expected X, got Y"*), so a
vectorised sizer that disagreed with the scalar writer would be caught immediately rather than
producing a corrupt payload.

#### Measured (`src/NanoBench/PackedSizeBenchmarks.cs`) — and it is the first non-flat result in this arc

| count | distribution | scalar | vectorised | ratio |
| ---: | --- | ---: | ---: | ---: |
| 4096 | mixed | 3,481 ns | **530 ns** | **0.15×** |
| 4096 | small | 948 ns | **534 ns** | 0.56× |
| 256 | mixed | 144 ns | **32.7 ns** | 0.23× |
| 256 | small | 63.4 ns | **32.7 ns** | 0.52× |
| 16 | mixed | 8.39 ns | **3.19 ns** | 0.38× |
| 16 | small | 3.73 ns | **3.18 ns** | 0.85× |

**It wins at every size and distribution, including 16 elements.** Seven consecutive
micro-optimisations in this arc measured flat; this one does not, and the reason is structural
rather than clever — the work per element genuinely drops.

**The branch-free scalar `Ladder` arm LOSES everywhere** (1.13× to 4.27× *worse* than scalar), and
that is why it was included: it isolates the cause. The win is SIMD, not branch-avoidance —
removing the branches without vectorising is a **pessimisation**, because the early-exit ladder is
already cheap on the small values that dominate real data, and the branch-free form always pays
all four comparisons.

**Vectorised is nearly flat across distributions** (530–534 ns at 4096) because it does the same
work regardless of the values, while scalar swings 948 → 3,481 ns on the same count. So the edge is
*smallest* on the small-value data most columns actually contain (1.8×) and largest on mixed or
wide data (6.6×). Quote the 1.8× when deciding, not the 6.6×.

#### The API shape: three methods, and the rest puns (Marc)

Marc's read, and it holds: the surface is
`MeasureVarint(ReadOnlySpan<uint>)`, `MeasureVarint(ReadOnlySpan<ulong>)` and
`MeasureVarint(ReadOnlySpan<int>)`. Everything else reaches one of those by `MemoryMarshal.Cast`,
which is free — same element width, no copy.

| CLR type | puns to | why |
| --- | --- | --- |
| `uint` | *itself* | 1–5 bytes |
| `ulong` | *itself* | 1–10 bytes |
| `int` | **needs its own** | **negatives sign-extend to 10 bytes**, so it is emphatically not `uint` |
| `long` | **`ulong`** | a negative `long` reinterprets as a large `ulong`, and both encode as the same 64-bit two's-complement varint — identical length, so this one is free |
| enum | `int`/`uint`/`long`/`ulong` | by underlying type; same width, same sign rule |
| `bool` | — | always one byte: the answer is `count` |
| `sint32`/`sint64` | zigzag, then pun | `(v << 1) ^ (v >> 31)` is itself vectorisable |
| `float`/`double` | — | fixed width; `WritePacked` is already O(1) for these |
| `short`/`sbyte` | **cannot pun** | narrower elements, so a `short[]` is not castable to `int[]`. Code-first only — no `.proto` produces them |

**The int/uint asymmetry is the load-bearing part**, and it is confirmed in our own emitted code
rather than assumed: an `int32`-family member measures as
`MeasureRawVarint64(unchecked((ulong)(long)value))` — sign-extended to 64 bits — while `uint32`
uses the 32-bit form. This is protobuf's long-standing quirk (*"if you use int32 or int64 as the
type for a negative number, the resulting varint is always ten bytes long"*), and it is exactly why
`int` cannot share the `uint` ladder: the branchless form has to **blend 10 in where the lane is
negative** rather than running four thresholds.

**Where it plugs in, and it is better placed than expected**: protogen emits `T[]` for every
packable scalar (`string`/`bytes`/message become getter-only `List<T>` and are not packed at all),
so the array-backed `RepeatedSerializer` override *is* the packed sizing path for schema-first
consumers — and an array yields a span natively on every TFM, with no `CollectionsMarshal` and no
new API. `List<T>` would need `CollectionsMarshal.AsSpan` (net5+) and keeps the scalar loop
down-level, which costs little given the shape protogen actually emits.

**What this does NOT say.** It measures *sizing in isolation*. Sizing is one part of packed writing
— the encode still has to happen — and packed columns are in turn a fraction of most payloads;
`DescriptorPayloadCensus.md` has this repo's reference payload at 71.5% string/bytes, which is why
the descriptor set would show none of this. An in-situ number needs a packed-numeric payload that
does not exist yet, and that payload is the real prerequisite, not the algorithm.

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

### C4. ~~Enum in a `repeated`, or as a map value~~ — **done 2026-08-14**

Both were refused, and neither refusal survived checking. The repeated-enum one claimed *"the
packed write arm disagrees with ref-emit on an empty collection"*; every clause of that failed (see
B1). The enum-map-value one was parked *"alongside the repeated enum, to keep the two moving
together"* — it had no reason of its own, and the enum map **key** was already supported.

Lifted, and held by the **byte gate** instead of by a refusal — `conformance.proto` gains
`repeated Grade grades` and `map<string, Grade> ranks`, with samples covering the **empty** case
specifically, since that was the shape the original disagreement was reported against. 1559
conformance cases pass, byte-identical to `RuntimeTypeModel`.

**Corpus: 241 → 261 of 268 schemas, 1369 → 1597 contracts, refusals 27 → 7.** The remaining 7 are
all `group` (C6), which is now the only genuinely missing schema feature.

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

### C14. protobuf **editions**, and refreshing `descriptor.proto` — **DEFERRED to 4.1 (Marc, 2026-08-14)**

Marc, 2026-08-14. Editions replace the proto2/proto3 split with per-feature settings, and the one
that matters here is **`features.message_encoding = DELIMITED`** — which *resurrects group
encoding*, deprecated in proto3 and now back as a first-class choice. Marc's note: this is
substantially what he recommended to the protobuf team 15+ years ago, and it is what protobuf-net
has implemented throughout as `DataFormat.Group`.

So we are unusually well placed: the wire form is already implemented on both paths, and the
`DataFormat.Group` plumbing is the same plumbing editions needs. What is missing is the *schema*
half — recognising an `edition = "2023"` file and mapping its feature set onto the options
protobuf-net already has.

**Two features map onto options we already have**, per
[the encoding guide](https://protobuf.dev/programming-guides/encoding/#packed) (read 2026-08-14,
because "things keep changing"):

| edition feature | protobuf-net equivalent |
| --- | --- |
| `message_encoding = DELIMITED` | `DataFormat.Group` |
| `repeated_field_encoding = PACKED` / `EXPANDED` | `IsPacked = true` / default |

Note **Edition 2023+ packs by default**, which inverts the proto2 posture — so an editions
front-end has to treat "packed" as the default and `EXPANDED` as the opt-out, rather than the
reverse. Worth knowing before the mapping is written, since getting the polarity wrong is silent.

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
