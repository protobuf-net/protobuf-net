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

### B1. Packed writes — **LANDED on the raw path (3.4×–32×); two DataFormat arms remain**

**2026-08-15, second update.** The work moved to where it belongs and the numbers changed
completely. `RepeatedSerializer`'s fast paths were **backed out** (classic is the control - see
AGENTS.md, "Don't improve the legacy library or ref-emit"), and the packed primitives are now a raw
`ProtoWriter.State` surface the generator calls directly. Against a pristine classic baseline:
bool **32×**, floating point **13.6×**, enum **7.8×**, unsigned varint **4.0×**, signed **3.4×**.

The larger prize was never the throughput, though: `IsPacked` used to make a member
**measure-blocked**, which by the fixed-point rule removed its whole contract - and every contract
referencing it - from measure-first. That cascade is closed for these shapes.

**Both `DataFormat` arms have since landed too**, so all seven categories are on the raw path -
`FixedSize` at **13.0x** and `ZigZag` at **1.5x**. (The earlier note here recorded those two as
~6-10% *slower* under the raw model; that was an artefact of their being the only non-raw contracts
inside a raw model, and it disappeared when they joined. Nothing was ever wrong with the raw
writer.)

**What is left**, in order: a zigzag write blit (its measure is already vectorised, but the write is
still per element, which is the whole of the gap between 1.5x and the others); B21 tier 2, writing
without per-element room checks now that the length is known exactly; and the **narrow kinds**
(`sbyte`/`byte`/`short`/`ushort`/`char`) - which this previously called "the narrow *varint* kinds",
implying the non-varint ones were done. They are not, and the two cases differ:

| | wire form | why it is out | tractable? |
| --- | --- | --- | --- |
| narrow, default/zigzag | varint | a span pun reinterprets bytes, so a 1-2 byte element cannot become a 4-byte one; and a varint needs per-element encoding regardless, so there is no blit to reach for | least |
| narrow, `FixedSize` | **`Fixed32`** | this is a **widen**, not a pun: 2 CLR bytes become 4 wire bytes | **most** - `Vector.Widen` is the exact inverse of the `Vector.Narrow` the blit already uses, and is in the same portable `Vector<T>` family |

`FixedSize` on these is legal and not obscure: `byte`, `sbyte`, `short` and `ushort` all pass
**width 32** to `ValueMember.GetIntWireType`, so they land on `Fixed32`. **`char` is the exception** -
it hard-codes `WireType.Varint` and ignores `DataFormat` altogether, so there is no non-varint
`char` to support.

So the narrow `FixedSize` column is a widen-then-blit, which is a smaller piece of work than the
narrow varint column and worth doing first if either is done. Sign matters and `Vector.Widen`
handles it by overload: `short` sign-extends to `int`, `ushort` zero-extends to `uint`.


**Status, 2026-08-15.** Tier 1 of B21 has landed for all four varint element types (see B21), and
the two remaining premises recorded here were both checked rather than carried forward:

- **"enums are never packed, because `EnumSerializer` is not an `IMeasuringSerializer`" — FALSE.**
  They are packed, and always were; `PackedBlockCopyTests.PackedEnumsAreActuallyPacked` pins the
  bytes. `TypeHelper.CanBePacked` returns true for `IsEnum` outright, and the *concrete*
  `EnumSerializer<TEnum, TRaw>` implements `IMeasuringSerializer<TEnum>` — only the public abstract
  base does not, and the gate tests the instance. The real gap is smaller: a packed enum does not
  reach the fast varint arms (they match `typeof(T) == typeof(uint)` and an enum is none of them),
  so it pays an enumerator step and a virtual write per element — 2.54 ns/element against 1.74 for
  `uint32`. Routing it through the underlying-type pun is the fix, and it is worth doing.
- **"~1 µs per member blocks further packed work" — RETRACTED.** It is ~10 ns; the figure was a
  total divided by a member count. See `notes/packed-writes.md`, which carries the measurement and
  the methodology lesson.


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

### B16. Locals in the emitted bodies — **`lengths` and `len` done (2026-08-14, corrected 2026-08-16); `tmpN` folding still open**

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

#### Correction, 2026-08-16: "no local at all" was applied to ONE of the two sites

The sentence above said *Applied: no local at all*, flatly, and that was **not true of the whole
emitter** — it described the straight-line unary-message site only. A second site survived in
`EmitRawRepeatedWrite`, hoisting `var lengths{number} = state.RawLengths;` once per repeated
*message* member, and it was **missed rather than reasoned**: `83b9886f` talks exclusively about
"per-site" locals and never mentions the loop case, though both sites arrived together in the
writer arc (`89b535f7`). Marc found it by reading the emitted protogen serializer, where it
accounted for **26** locals.

It is gone now, per Marc: *"we don't even need to declare `lengths`; literally use
`state.RawLengths` at every site."* One rule, no exceptions, nothing to go stale — the argument
that decided the first site decides this one too, and fewer locals is the actual objective.

**The `lenN` family went with it**, which is the part that needed thought rather than deletion: a
sub-message length is an `out` target, so unlike the cache reference it cannot be eliminated — but
it need not be *duplicated*. All such temporaries are `long`, and each is assigned then consumed
immediately, so they fold onto one local with no type map and no lifetime overlap.

**Hoist only what is used more than once** (Marc): a body with a single length site keeps its
declaration **at** the site (`out var len`), because hoisting one use to the top widens its scope
and buys nothing. Two or more get `long len;` once. `AppendFoldingLengthTemp` decides this by
inspecting what was actually emitted rather than by restating the eligibility predicate — a second
copy of that predicate would be free to drift from the first — which also keeps an unused local out
of the consumer's build, where it would be CS0168.

In `Measure_` bodies the shared temp is `sub`, since `len` is already the accumulator there.

**Three emit paths needed it, not one**, and the two extra ones are why the first attempt emitted
`CS0103` across nine fixtures: besides `RawWrite_`, both the classic `ISerializer.Write` body and
the sub-type write body call `EmitWriteMembers` with `raw:` possibly true. The golden tests caught
it because they *compile* their output (`Assert.Equal(0, result.ErrorCount)`) rather than only
diffing it.

Measured on the committed protogen serializer:

| | before | after |
| --- | ---: | ---: |
| `lengths{n}` locals | 26 | **0** |
| `len{n}` locals | 72 | **0** (4 folded declarations, the rest scoped single-site) |
| worst `RawWrite_` body | 25 locals | **13** |

#### B16a. DEBUG-only length-drift detection (Marc, 2026-08-16)

Emitted alongside the folding, because the two touch the same statements. A measured length that
disagrees with the bytes actually written is a **corrupt stream**, and it is the one failure this
arc can produce that nothing else catches: the classic buffer-writer path already validates
(*"Length mismatch; calculated 'x', actual 'y'"*) and the raw path validated nothing.

```csharp
state.WriteRawVarint64((ulong)len);
DebugCapturePosition(ref state, ref before);
RawWrite_Foo(ref state, item2, depth);
DebugAssertPosition(ref state, before + len, "Fields");
```

**`Position64` is safe to lean on, and it is the exception rather than the rule.** The raw path
deliberately does not maintain most writer state - `RawWrite_` threads its own depth budget and
never touches `writer.Depth`. Position needs no maintaining because it is **derived**:
`_position64 + GetUncommitted(in state)`, and both real backends override `GetUncommitted` with the
live buffer offset (`state.OffsetInCurrent`, `Pending(in state)`) that a raw write is already
advancing. A stream flush mid-body is fine too - `_position64` gains exactly what `Pending` loses.

**`ref` rather than `out` is forced, not preferred:** `out` on a conditional member is **CS0685**,
precisely because the call may vanish and leave the target unassigned.

**It costs a Release consumer nothing at all**, measured rather than assumed - the concern being
that a capture local per site would re-add what the folding above just removed:

| build | IL locals | IL bytes | warnings |
| --- | ---: | ---: | --- |
| Release | **0** | 2 | 0 |
| Debug | 2 | 27 | 0 |

Roslyn elides the local entirely once both conditional calls are gone, and emits no CS0219 for the
assigned-never-read `long before = 0;`. The bodies are `#if DEBUG`'d as well (Marc), so even an
explicit call costs nothing there.

**`Debug.Fail` TERMINATES THE PROCESS on .NET Core** - it is not a logged warning; it is an
uncatchable `FailFast` printing *"Process terminated. Assertion failed."* That makes it a real gate
and also means a **false positive would kill every consumer's Debug build**, so the no-false-alarm
side needed proving broadly rather than spot-checking: `AotConformanceTests` in Debug (1592, with
**44 live call sites across 19 models** - verified by `-p:EmitCompilerGeneratedFiles=true`, since a
plain build writes nothing to disk and an empty `generated` folder looks identical to a generator
that emitted nothing) and `AotSmoke -c Debug`, whose `.proto` descriptor tree is the deepest nesting
available.

**Proven to fire**, per the precedent set by the services-constructor `IsScalar` assert - by
perturbing `Measure_` to `return len + 1`, i.e. the real bug class rather than a tautology, then
reverting:

```
Process terminated. Assertion failed.
Length drift writing 'Customer': measured length and bytes written differ by -1.
```

Note what it does **not** cover: the root length, which is checked by the classic engine's own
validation via `TryMeasureRaw`. Between the two, every length-prefixed node is covered. Grouped
members carry no length and so get no check.

`tmpN` folding remains open and is now the whole of the remaining work here — it is much the
largest family (337 in that same file, 21 in one body) and the only one needing a type-keyed map.

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

### B18b. Packed writes are per-element even where a block copy would do — **the biggest packed win, and it needs no SIMD**

`RepeatedSerializer.WritePacked` writes **every** packed element through an enumerator and a
virtual serializer call, whatever the type — there is no `MemoryMarshal`, no `AsBytes`, no bulk
path anywhere in the file. So a packed `float[]` → `repeated float`, which on a little-endian
machine is a **pure `memcpy`**, is emitted one float at a time through an interface dispatch.

Sizing is already O(1) for those types (`count * 4` / `count * 8`); it is only the write.

This outranks every vectorised idea below it: block copy is portable, needs no intrinsics, is
trivially correct behind the `IsLittleEndian` guard the codebase already uses, and covers the whole
matching-fixed-width family. **`notes/packed-writes.md` has the full scenario matrix** and the
ranking; B19–B21 are all downstream of this one.

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
| `sint32`/`sint64` | **two more methods**, not three - see below | there is no unsigned zigzag, and the transform needs no shift instructions |
| `float`/`double` | — | fixed width; `WritePacked` is already O(1) for these |
| `short`/`sbyte` | **cannot pun** | narrower elements, so a `short[]` is not castable to `int[]`. Code-first only — no `.proto` produces them |

**The int/uint asymmetry is the load-bearing part**, and it is confirmed in our own emitted code
rather than assumed: an `int32`-family member measures as
`MeasureRawVarint64(unchecked((ulong)(long)value))` — sign-extended to 64 bits — while `uint32`
uses the 32-bit form. This is protobuf's long-standing quirk (*"if you use int32 or int64 as the
type for a negative number, the resulting varint is always ten bytes long"*), and it is exactly why
`int` cannot share the `uint` ladder: the branchless form has to **blend 10 in where the lane is
negative** rather than running four thresholds.

#### Zigzag: two more methods, no shifts, and a BIGGER relative win (Marc asked)

Marc asked whether zigzag needs a separate SIMD implementation for the extra shifts. Two answers,
both measured rather than reasoned:

**It needs no shift instructions at all.** `zigzag(v) = (v << 1) ^ (v >> 31)`, and neither shift is
required: `v << 1` is `v + v`, while the arithmetic `v >> 31` is *exactly* what
`Vector.LessThan(v, Zero)` produces — all-ones for a negative lane, zero otherwise. So the
transform is **add, compare, xor**, using only the down-level-safe `Vector<T>` surface. That
matters: `Vector.ShiftLeft` is .NET 7+, so a shift-based version would have no vector path on
netstandard2.0 or net4x. The identity is cross-checked in the benchmark's setup, which throws if
the two forms disagree.

**And it is two methods, not three** — `sint32` and `sint64`; there is no unsigned zigzag. Zigzag
also *simplifies* the ladder rather than complicating it: the result is unsigned, so there is no
10-byte blend, and `sint32` is always 1–5 bytes where `int32` can be 10.

**Measured, and the shape is the interesting part:**

| | plain | zigzag | overhead |
| --- | ---: | ---: | ---: |
| vectorised, 256 mixed | 32.65 ns | 34.36 ns | **+5%** |
| vectorised, 256 small | 32.40 ns | 34.36 ns | **+6%** |
| scalar, 256 mixed | 129.7 ns | 175.7 ns | +35% |
| scalar, 256 small | 63.2 ns | 112.8 ns | **+78%** |

The transform amortises across lanes, so **zigzag is a bigger relative win for SIMD than plain
varint is** — the scalar path pays the shifts per element, the vector path pays them per block.
`sint32` columns are therefore the best case for this work, not the awkward one.

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

### B21. SIMD for the packed varint WRITE — **tiers 1 and 2 LANDED; tier 3 still open**

Marc, 2026-08-14: can SIMD do something clever for the write — measure a block at once, then
"scatter/gather, switch, something"? Yes, but it separates into tiers that differ enormously in
cost and reach, and the honest answer is that the cheapest one carries the most traffic.

**Tier 1 — the homogeneous-block fast path. Portable, and the dominant case.**
If every value in a block is `< 128`, every varint is exactly one byte and there are no
continuation bits to interleave — so the encode collapses to **narrow-and-store**. Detect with
`Vector.GreaterThanAny(v, 127)`, then `Vector.Narrow` twice (`uint`→`ushort`→`byte`) and blit.
`DescriptorPayloadCensus.md` puts 99.7% of tags and most field values in the single-byte class, so
this arm would carry most real traffic — and it needs only `Narrow`, the same portable primitive
B20 wants. **This is the one to try first.**

**Tier 1 is now LANDED**, 2026-08-15, for all four varint element types — `uint32`, `int32`,
`uint64`, `int64` — on both the array path (every TFM) and the `CollectionsMarshal` span path
(net5+). `PackedVarintMeasure.WritePackedUInt32` / `WritePackedInt32` / `WritePackedUInt64`.
Measured in isolation by `PackedWriteBenchmarks`, 999 elements:

| element | scalar | tier 1 | where it fires | where it cannot |
| --- | --- | --- | --- | --- |
| `uint32` | 1449 ns | **169 ns** | **8.6×** | +3% |
| `int32` | 1448 ns | **170 ns** | **8.5×** | +6% |
| `uint64` | 1445 ns | **217 ns** | **6.6×** | +5% |

64-bit is behind 32-bit because it takes **three** narrowing steps rather than two, so a block is
eight vectors rather than four; the element count per block is 32 either way.

Two puns make four element types into three methods, and both were verified rather than assumed:
`long` rides on `WritePackedUInt64` (a negative `long` reinterprets as a large `ulong`, so it fails
the `< 128` test and is never blitted, and both encode as the same 64-bit two's-complement varint),
while `int32` needs its **own** method — the block test can be the unsigned one, but the scalar
*fallback* must write the 64-bit sign-extended form, so it cannot simply call the `uint32` version.

**The cost side is the number that mattered for the decision**, since the check runs on every block
whether it succeeds or not: +3% to +6% on a distribution where a block is essentially never
uniform. Against 6.6×–8.7× where it does fire, and a census that puts 99.7% of values in the
single-byte class, that trade is not close.

**A mixed block hands the REST to the scalar loop rather than falling back per-block.** That is
deliberate: resuming the vector loop after a wide value would need the tail written first to keep
byte order, and the pathological case (`oneWide`) measures at full speed anyway because a single
wide value in the ragged tail never breaks the block loop at all.

**Two methodology findings came out of this, and both generalise:**

- **The end-to-end harness could not resolve it.** `PackedMatrixBenchmarks` serializes a four-member
  contract, and the run-to-run spread on a single arm (7.26–7.84 µs for identical code) was
  *larger than the effect being claimed*; two successive runs put the same arm on opposite sides of
  its baseline, which would have supported either conclusion. The dilution has two named causes —
  the ~1 µs/member fixed overhead (item 3 in `notes/packed-writes.md`) and, before the 64-bit arm
  landed, half the members having no fast path. An end-to-end number is the right *final* check and
  the wrong instrument for attributing a delta to one loop; `PackedWriteBenchmarks` exists for that,
  at ±0.5% noise.
- **Watch what the control arm does.** Classic-emit tracked raw within 1% throughout, which looks
  like "the optimisation did nothing" and actually means "both models go through the same library
  `RepeatedSerializer`, so both got it". A control that shares the code under test is not a control.

**The test had a hole that only sabotage found.** `PackedBlockCopyTests.PackedVarintBlockMatchesTheEncodingRules`
sweeps 31/32/33/47/64/999 elements against a hand-written LEB128 oracle — but widening the
uniformity threshold from `0x80` to `0x100` left every case passing, because no fixture value lay
in **128–255**, the band the cutoff actually discriminates. With a `justOver` pattern added it
fails as it should. The differential suite is structurally blind here for the usual reason: both
sides go through `RepeatedSerializer`, so a wrong blit is wrong identically on both.

**Tier 2 LANDED 2026-08-15**, for zigzag and for the varint blits' scalar tails. It is worth +25%
on the `spread` distribution and ~nothing on `small` (where tier 1 already took the work), and it is
zigzag's *only* lever since that has no blit — 1.5× → 2.4×. Two things it taught:

- **"nearly free" was wrong about the shape.** The obvious form — measure the payload, check the
  room once, write the whole column unchecked — measured at **exactly zero**, because the writer's
  buffer is `BufferPool.BUFFER_LENGTH` = 1024 and a packed column worth optimising is precisely one
  that exceeds it. The guard could essentially never be true. The working form is **chunked**: stay
  unchecked while the current chunk provably holds one more worst-case element, and hand only the
  boundary element to the checked path.
- **it is bounded by what tier 1 leaves.** A bracket in `PackedWriteBenchmarks` (scalar loop vs a
  no-checks ceiling) put the available win at 2.7× for zigzag and 3.8× for a plain varint column;
  realised is well under that, because the ceiling also drops the writer entirely.

The original sketch follows, for the record:

**Tier 2 — measure, then write without per-element room checks. Nearly free, falls out of B19.**
SIMD-measure the block for its total length, reserve exactly that, then run the scalar encoder with
**no `RemainingInCurrent` test per element**, because the room is already guaranteed. That deletes
a compare-and-branch per element from the current raw path. Unglamorous, portable, and essentially
free once B19 exists.

**Tier 3 — full vectorised LEB128. Real, published, and a big lift.**
The shape is: encode each element into a fixed-width lane, derive a per-block length pattern, then
**compact via a shuffle table** indexed by that pattern (`pshufb`). It is what streamvbyte and
group-varint do. Two things bite:

- **LEB128 is deliberately SIMD-hostile.** Group-varint exists *because* interleaved continuation
  bits resist vectorisation — Google designed a different format rather than vectorising this one,
  and we cannot change the format;
- **the shuffle is not portable.** `Vector128.Shuffle`/`Ssse3.Shuffle` is .NET Core 3.0+/.NET 7+,
  not in the `Vector<T>` API and absent on net472. So tier 3 is a modern-TFM-only path with tiers
  1–2 as the fallback.

**There is no scatter/gather shortcut past this**: variable-width output needs a compaction step,
and compaction needs a shuffle. That is the whole difficulty, and it is why the format-level answer
(group-varint) exists at all.

Ordering: tier 1 is cheap, portable and hits the common case; tier 2 comes free with B19; tier 3 is
a research-shaped piece that should only start once tiers 1–2 have shown what is left to win.

### B20. Cross-width packed columns — **DOWNGRADED: not reachable from `[ProtoMember]`**

Marc, 2026-08-14: protobuf-net supports cross-targeting widths — a C# `double`/`double[]` member
can target a `float`/`repeated float` field. Confirmed rather than assumed:
`ProtoWriter.State.WriteMethods.cs` documents `WriteDouble` as *"supported wire-types: **Fixed32**,
Fixed64"* and narrows with `float f = (float)value;`. That is the schema-first shape — the `.proto`
says `float`, the C# member is `double`.

**First, the thing that decides its priority** (Marc asked whether the floating-point parity
result covered widen/narrow — it did not, and checking why was instructive): **a C# `double` member
cannot be narrowed onto a `float` field by any `[ProtoMember]` option.** `ValueMember` consults
`DataFormat` for a width only through `GetIntWireType`, which is called from the **integer** cases
alone; `Single` and `Double` assign `Fixed32`/`Fixed64` unconditionally. Pinned by
`ADoubleMemberIsAlwaysFixed64_EvenWithFixedSize`.

So `WriteDouble`'s documented `Fixed32` support exists for **reading** a payload whose schema says
`float` into a `double` member, and for models configured by other means — not for anything a
consumer can express with an attribute. That makes this an interop affordance rather than a shape
real code produces, and it drops below everything else on the list.

**It is a WRITE-side opportunity, not a sizing one**, which distinguishes it from B19: a packed
*fixed-width* column needs no measuring at all (`WritePacked` is `count * 4` / `count * 8`). So the
only work is conversion plus store, which is pure throughput.

**And it splits in two, with only half of it interesting:**

- **matching width** (`float[]` → `repeated float`) is already a straight block copy —
  `MemoryMarshal.AsBytes` over the span, which `notes/nano-writer.md` records as the fixed-width
  trick. Nothing to gain;
- **cross width** (`double[]` → `repeated float`, `long[]` → `repeated sfixed32`) converts per
  element today. That is where `Vector.Narrow` fits: it takes **two** source vectors and yields one
  narrowed vector, which is precisely the shape needed. `Vector.Widen` is the read-side mirror.

`Vector.Narrow`/`Widen` live in `System.Numerics.Vector`, so they should be available on every TFM
via `System.Numerics.Vectors` — **worth confirming on net4x before relying on it**, as that is the
same class of assumption that made `Vector.ShiftLeft` unusable in B19.

**Endianness: do NOT assume little-endian, because this codebase does not and it is free not to.**
`fixed32`/`fixed64` are little-endian *on the wire*, so a block or vector store of native memory is
only correct where the CPU agrees. The established pattern here is a `BitConverter.IsLittleEndian`
guard with a `BinaryPrimitives` fallback — `LocalWriteFixed32` already writes through
`BinaryPrimitives.WriteUInt32LittleEndian`, and the raw reader states the reasoning outright: *"…
every little-endian platform (**IsLittleEndian is a JIT constant**), so correctness on BE costs
nothing — and legacy is BE-correct via BinaryPrimitives, so anything less would be"* a regression.

So the vector arm takes the same shape: guard on `IsLittleEndian`, fall back to the existing scalar
path on big-endian. The JIT eliminates the dead arm on LE, so the guard is genuinely free — which
means "assume LE" would buy nothing and lose a correctness property the library currently has.

Note this is **specific to B20**: B19 sizes rather than stores, and a *length* is endian-independent.

**Isolatable, and easily**: a benchmark of scalar convert-and-store versus vectorised
narrow-and-store over a span needs no serializer at all, exactly as `PackedSizeBenchmarks` needed
none. Worth doing *because* it is cheap to answer, but note it ranks below B19 on reach: cross-width
columns are rarer than same-width ones, and the same-width case is already optimal.

### B27. `AotDifferential` loads the generator from **Debug first**, whatever it was built as

Found 2026-08-16 while merging main into v4: a `-c Release` run reported *"the generated model does
not compile"* against `WriteRawPackedVarint`, an API that exists on `schema-breadth` and nowhere
else — while checked out on `v4`, which neither defines nor emits it.

`Corpus.LoadGenerator` walks `new[] { "Debug", "Release" }` and takes the first
`protobuf-net.BuildTools.dll` it finds, so a **stale Debug build left behind by another branch wins
over the Release build the run was asked for**. The harness then compares a generator from one
branch against a library from another and reports the disagreement as a corpus failure, which is
exactly the wrong place to look.

Note this is *not* what #1264 fixed: that made the **corpus scan** follow the configuration the
harness was built as, and the generator load was left on its own Debug-first path. So the file
already contains the right idea, applied to the other half.

It is a sharp edge rather than a wrong answer — the run fails loudly rather than passing falsely,
and `rm -rf src/protobuf-net.BuildTools/bin/Debug` clears it. But it fails *blaming the corpus*, and
the same class of mistake ("a control that shares the code under test is not a control") has already
cost this project weeks once, in the packed arc. The fix is to load the configuration the harness
itself was built in, and to say so when the dll it picks is older than the library it is testing.

**Open.** Not touched in the merge that found it, since a merge commit is the wrong place for it.

### B28. Is `Impl*` dispatch a bottleneck on the CLASSIC write path? — **no on the real write; DEFINITIONALLY yes on the measuring pass**

Marc, 2026-08-16, having read how the two halves differ: the reader's `Impl*` family is **gone**
(zero occurrences in `ProtoReader*.cs`) — it went with the backend classes, replaced by one
`_source switch` in `GetNextBuffer`, i.e. a type test per *refill* instead of a virtual call per
*primitive*. The writer still has all 11, abstract on `ProtoWriter` and overridden three times
(`BufferWriter`, `Stream`, `Null`). Reasonable to suspect that of being a write-path bottleneck.

**The closest available measurement says no.** The `int32`-bypass fix (`notes/nano-writer.md`,
"Two findings from reading a golden") removed exactly that cost — runtime tag encode, `WireType`
set and reset, **and the `ImplWriteVarint32` virtual hop** — for the commonest member shape in
protobuf, and measured **10.192 → 10.207 µs (+0.15%)**, with the Google.Protobuf gauge drifting
2.5% between the same two legs. That was the fifth consecutive write-path micro-optimisation to
measure flat, and the payload census explains it: the descriptor set is **71.5% UTF-8 string
payload**, so per-field overhead is diluted.

**What IS costing the classic writer** is length discovery, not dispatch: `docs/aot.md`'s gRPC
table has the runtime model at **+85%** for measure+serialize over plain serialize (write-to-count
against the null writer, plus buffer-and-patch back-fill), against +51% for the generated model.
That is what measure-first replaces, and it is why B5's counting mode and the writer swap are one
piece of work — `ProtoWriter.Null` exists precisely to implement `Impl*` as "count, don't store".

**Still genuinely untested**: an *unpacked numeric-dense* payload, where there are no strings to
hide behind. The census note is explicit that the descriptor payload cannot speak for one. The
packed results (3.4×–32×) do not answer it either, since packing replaces the loop with a block
write. `PackedMatrix` already builds the columns, so running them unpacked through classic vs raw
would isolate per-field write overhead the way `PackedSizeBenchmarks` isolated sizing.

**But there IS one path where it is a bottleneck by construction: the NULL writer** (Marc, asked
after the above). `NullProtoWriter` never acquires a buffer — `CreateNullProtoWriter` returns
`new State(obj)`, whose constructor is `this = default; _writer = writer;`, and the file contains
**no** `state.Init`, `GetMemory`, `GetSpan` or lease anywhere. So `RemainingInCurrent` is
permanently 0, **every** raw op fails its room check and takes the `[NoInlining]` slow arm, and the
override counts instead of storing (`ImplWriteVarint32` → `Advance(MeasureUInt32(value))`,
`ImplWriteString` → `Advance(expectedBytes)`).

That is elegant — the polymorphism *is* the measuring switch, and no branch anywhere asks "am I
counting?" — and it means the answer to this whole entry sharpens to: **`Impl*` dispatch is not a
bottleneck on the real write, and is definitionally a per-primitive cost on the MEASURING pass**,
where the fast arm is unreachable. That is the `+85%` in the gRPC table, and it is exactly the pass
measure-first deletes. So the dispatch question and B5 are the same question asked twice.

Two details that follow, both checked rather than assumed:

- **the drift check (B16a) stays valid under the null writer**: `GetUncommitted` is overridden only
  by the buffer-writer and stream backends, so the null writer inherits `=> 0` and `Position64` is
  just `_position64`, which `Advance` maintains. A counted length is proven exactly as a written one
  is;
- **generated measurable contracts do not normally reach it**: `ProtoWriter.cs`'s
  `IMeasuringSerializer<T>` + `OptionTrySkipWritingWhenMeasuring` interception calls `Measure(...)`
  and caches the length instead of traversing. The null traversal is the fallback — a
  non-measurable contract, or `TryMeasureRaw` answering -1 for a non-writer context.

**Ranked low deliberately** (Marc): the raw path already demotes `Impl*` from the hot arm to the
overflow arm — every raw primitive is a `RemainingInCurrent` room check with a `[NoInlining]`
`Impl*` fallback, so the virtual call fires per *buffer boundary*, exactly as the reader's type
test does. So the question only characterises the **classic** writer, which is on a deliberate
go-slow as the control and the fallback. It matters for consumers who have not adopted
`[ProtoModel]` — and `docs/aot.md` shows that population got nothing from v4 on serialize
(20.59 → 20.39 µs) while deserialize improved 23%.

### B29. `ProtoReader.cs` cites a `PORTING.md` that does not exist

The "museum bridge" comment — the one that explains liquify/resolidify and is the best short
statement of how the legacy reader relates to `State` — ends *"Museum API, museum prices - see
PORTING.md"*. There is no such file anywhere in the repo (`git ls-files` finds nothing).

Either write it or drop the reference. It is the pointer someone follows when they wonder why the
instance API is slow, so a dangling one costs more than no pointer at all.

### B31. An EXTERNAL serializer takes its member off measure-first — widened by `[ProtoSerializer]`

`RawMemberMeasureBlocked` excludes any member whose sub-serializer is external:

```csharp
if (member.SubSerializer is not null || member.SubSerializerIsScalar
    || member.SubSerializerDynamic) return false;   // ...i.e. not raw-writable
```

and the exclusion runs to a fixed point, so the *containing* contract and everything referencing
it fall onto the classic write-to-count path with it. The comment at the site states the reason
and it is a good one: **a hand-written or deferred-category serializer frames itself and cannot be
second-guessed**, so there is nothing for `Measure_` to do arithmetically.

That has always been true of `[ProtoContract(Serializer = …)]`. What `[ProtoSerializer]` (#1275)
changes is the *reach*: a serializer can now be bound to a type **you do not own** — a BCL type, or
anything whose assembly cannot reference protobuf-net — so a single declaration can attach to a
widely-used value type and demote every contract that contains one. Narrower than B26's blast
radius (only explicitly-bound types, not every member of a scalar kind across an assembly), but the
same shape, and equally **silent**: no diagnostic says a contract left the fast path.

**Not a defect in the feature, and probably not fixable in general** — the whole point of a
hand-written serializer is that we do not know what it emits. Two things could narrow it, neither
attempted:

- an **`IMeasuringSerializer<T>` external serializer could be asked**, exactly as the classic engine
  already asks one (`ProtoWriter.cs`'s `OptionTrySkipWritingWhenMeasuring` interception). A
  hand-written serializer that implements the measuring interface is telling us it can size itself
  arithmetically; the raw path currently ignores that and blocks anyway;
- the **info diagnostic** proposed in B26 would cover this case too, since it is the same question
  from the consumer's side: *why did my model leave the optimised path?*

### B32. `PublicAPI.Shipped.txt` has drifted from the real signature — 16 warnings on every build

Noticed 2026-08-17 while running `AotRefGen`; **pre-dates both external PRs** and is nobody's recent
doing. `TypeModel.GetInbuiltSerializer<T>` produces `RS0016` (symbol not part of the declared API)
and `RS0017` (declared API symbol not found) simultaneously, in `protobuf-net.Core` and again in
`protobuf-net` via the type-forward — 16 warnings on a plain `v4` build. The declared entry has no
default values; the real one does.

Note this **contradicts a claim in `AGENTS.md`**, which says release tracking "is not actually
*enforced* here (the `Microsoft.CodeAnalysis.Analyzers` RS2000 rules are not active), so the table is
documentation rather than a build gate". That is true of the **RS2000** release-tracking rules and
false of the **RS0016/RS0017** PublicApiAnalyzers rules, which are plainly live and shouting. The
sentence should be narrowed when this is fixed.

Cheap to clear (update the two `.Shipped.txt` entries to the current signature), and worth clearing
precisely because 16 standing warnings are how a *new* one goes unnoticed.

### B33. Move the working notes into per-arc sub-folders (Marc, 2026-08-17)

`notes/` is flat and about to gain a second arc — Marc has parallel work on **protobuf editions**
(C14 here, deferred to 4.1). Proposal: `notes/aot/…` for this arc, `notes/editions/…` for that one.
Agreed in principle; **unblocked as of #1275 merging**, which was the reason to wait (it edited
`notes/aot-findings.md`, and a rename against a concurrent edit is the one conflict class worth not
inviting).

Three decisions taken when it happens, recorded so they are not re-litigated:

- **`gaps.md` moves too**, to `notes/aot/gaps.md`. Its content is entirely this arc, and a *global*
  gaps file that mixes arcs gets worse as arcs multiply. `AGENTS.md`'s document table is already the
  real index, so the model becomes per-arc gaps indexed from there. The cost is that AGENTS.md calls
  `notes/gaps.md` "the entry point for what is missing" in more than one place, and those pointers
  must move in the *same commit* — a stale pointer to the file that tells you what is missing is
  exactly the failure this project keeps recording.
- **Drop the redundant prefix while moving** — `notes/aot/findings.md`, not
  `notes/aot/aot-findings.md` — but leave the other names alone (`nano-writer.md`,
  `packed-writes.md`). Renaming path *and* filename for everything doubles the churn for no gain.
- **It is a sweep, not a `git mv`.** There are ~148 references across ~25 files, and not only in
  markdown: `.cs` and `.proto` comments cite these paths (`AotDifferential/Program.cs`, several
  `Aot/Data/*.input.cs`, `SchemaSourcedModelEndToEndTests.cs`, `Schemas/*.proto`).

Best done on a quiet tree, i.e. after #1277 squashes into `v4`.

### B30. `[ProtoDataFormat]` follow-ups, carried over from the #1276 review — **open, none blocking**

Merged into `v4` on 2026-08-17. The feature is sound: **inert when unused** (with no declaration
anywhere the resolver returns `Default` and the emitted bytes are identical, so the blast radius for
existing consumers is zero rather than small), wire-correct where used (AotDifferential 100%, its
fixture covering nullable / repeated-nullable / map exemption / explicit-member-wins / `WellKnown`
promoting 200→240), and in the right layer — `Parse.cs`, the plan rather than the emit, so
`ClassicEmit` and raw both inherit it and neither engine diverges from the other.

Its measure-first cost is recorded in **B26**, which is where the work is. The rest:

1. **Generator-side caching and an early-out.** The runtime helper gained a cache after the
   contributor's own final review; `GetDataFormatDefault` did not. It walks the contract's base
   chain plus module plus assembly attributes **per member**, and builds a `Qualified` key before
   checking whether any declaration exists at all. This is the only cost that falls on compilations
   that never use the feature, which is what makes it grate against the `ProtoBufDisableBuildTools`
   promise that unwanted tooling costs one dictionary lookup. **Unmeasured** — the magnitude is not
   asserted here, only the shape.
2. **A refusal should name the ambient source.** `PBN3001: "DataFormat.ZigZag on a BCL type"`
   reported against a member carrying no format attribute is baffling, and it costs the whole
   contract plus its referrers.
3. **A declaration that can never match is a silent no-op** — `typeof(Guid?)`, `typeof(List<Guid>)`
   — because both sides unwrap the *member* but never the declared type. Analyzer-shaped.
4. **No `GetSchema` test.** The attribute rewrites the emitted `.proto` (`fixed64` vs `int64`,
   `bytes` vs `string` for a `Guid`), which is arguably its most user-visible consequence and is
   covered nowhere.
5. **`decimal` + `ZigZag` is a live JIT/AOT divergence, and PRE-DATES this feature.** The generator
   drops any BCL-kind member with `ZigZag`; the runtime *ignores* the format for `decimal` entirely
   (`ValueMember.cs`'s `ProtoTypeCode.Decimal` arm sets `WireType.String` unconditionally and calls
   `DecimalSerializer.Create(compatibilityLevel)` with no `dataFormat` argument). So the runtime
   model serializes and the generated model drops the contract and cascades. Reachable today with a
   plain `[ProtoMember(1, DataFormat = ZigZag)]`; `[ProtoDataFormat]` only widens the aperture.
   AGENTS.md already calls the refusal "a small deliberate over-reach" — the one-line fix is to
   exempt `decimal` from it.

## C. Schema front-end (`[ProtoSchema]`)

The feature lands on the **`aot-schema-model`** branch; the design and the findings are in
`notes/aot-schema-model.md` there. The gap list is here, so there is one to consult.

**Already verified on bytes** against `RuntimeTypeModel` in both directions: every scalar width
including the `sint`/`fixed` spellings, `string` with protogen's proto3 `[DefaultValue("")]`
guard, `bytes`/`bool`/`float`/`double`, enums as members, nested messages and enums, `repeated` in
both protogen shapes, maps including the `bool`-key case, `import`, and the naming rules
(pluralisation, collision avoidance, package → namespace).

### B22. ~~The raw PACKED path is natively UNMEASURED~~ — **CLOSED 2026-08-15, and it cost zero warnings**

**Found by audit, 2026-08-15, and it is a hole by this repo's own standard** ("whatever `AotSmoke`
does not cover is not fine, it is unmeasured" — AGENTS.md). `grep IsPacked src/AotSmoke/*.cs`
returns nothing, so the entire raw packed surface — six write methods, six measures, the SIMD blit,
the `MemoryMarshal.Cast` puns — has never run under ILC.

The risk is low rather than nil: it is all managed span work with no reflection, so the failure
modes that bit before (`MissingMethodException` from a trimmed constructor, "missing native code or
metadata") do not obviously apply. But `Vector<T>` under ILC and a `MemoryMarshal.Cast` over an enum
span are both unexercised there, and low-risk-but-unmeasured is exactly what the map members were
before adding three of them moved the warning count 20 → 29.

**Fix is cheap**: add packed members to `AotSmoke` covering a varint column, a fixed-width column, a
bool column and an **enum** column (the enum one being the sharp case, since it is the pun). Re-measure
the warning baseline on both sides when doing it, since the count tracks fixtures.

**Done.** Four columns on `Order`, 40 elements each — deliberately over the 32-element block so the
vector path engages *and* leaves a ragged tail; a shorter column would have exercised only the
scalar fallback and proved nothing about the blit:

| member | reaches |
| --- | --- |
| `Readings` (`int[]`) | varint over a span, values straddling 128 so the uniform-block blit **and** the scalar fallback both run |
| `Offsets` (`int[]`, `FixedSize`) | the flat block copy under a `Fixed32` header |
| `Flags` (`bool[]`) | the payload-IS-the-span blit, behind its vectorised canonical scan |
| `Levels` (`Status[]`) | the **enum pun** — `MemoryMarshal.Cast<Status, int>` at the call site, the sharp case, since nothing else here would show it wrong |

`win-x64` native publish **passes**, and the framing is verifiable by eye in the emitted payload:
field 59 is `DA-03-A0-01` — tag, then a length of 160, i.e. exactly 40 x 4 bytes.

**The warning count did not move: 19 before, 19 after**, with the composition unchanged (6 `IL2067`,
5 `IL3050`, 3 `IL2091`, 3 `IL2070`, 1 `IL2057`, 1 `IL2055`). Binary 3.44 MB.

That zero is worth recording rather than shrugging at, because the comparable case went the other
way: adding three *map* members moved the baseline 20 -> 29 on the spot. The difference is that the
raw packed surface is pure span work — no `Activator`, no `MakeGenericType`, no serializer resolved
from the model — so it demands no metadata at all. `Vector<T>` under ILC and a `MemoryMarshal.Cast`
over an enum span were the two genuinely unknown pieces, and both are free.

### B23. ~~Packed columns are limited to `T[]` and `List<T>`~~ — **`ImmutableArray<T>` DONE 2026-08-15; derived lists still a hold**

**`ImmutableArray<T>` now takes the span path**, for packed *and* unpacked, write *and* measure —
so it also stopped blocking measure-first for its contract. Three findings, in the order they
turned up:

1. **`AsSpan()` fails safe on a default instance, on both targeted runtimes.**
   `default(ImmutableArray<T>)` throws on `Length`, the indexer and `GetEnumerator`, but `AsSpan()`
   returns an **empty span** — checked on net472 *and* net8.0 rather than assumed from one, since
   the type comes from a package down-level and the shared framework on modern .NET.
2. **A default serializes exactly as empty**, not as absent: the packed member still writes its
   zero-length header (`12-00`). That follows from `ImmutableArraySerializer.Initialize` mapping
   `IsDefault` onto `Empty`. Together with (1) this means the emit needs **no guard at all** — an
   empty span produces precisely the bytes a default is defined to produce.
3. **...and the guard that was already there was actively WRONG.** `ImmutableArray<T>` declares
   lifted equality over `ImmutableArray<T>?`, so the generated `if (tmp != null)` *compiles* for it
   and evaluates to **false** for a default instance. So a default immutable array was skipped
   entirely. Invisible for an unpacked member (empty writes nothing anyway); for a **packed** one it
   dropped the zero-length header — a real wire divergence, and **pre-existing**, not introduced by
   this work.

That third one is the interesting one, because of how it was found: the corpus differential caught
it (`99% match`, one contract) on a contract that only existed because the *test* for (1) declared
one. Nothing in the fixtures had a packed `ImmutableArray`, so the bug had no way to show. The fix
is `NeedsNullGuard`, keyed on `Repeated.IsValueType`.

**`AsSpan` availability is probed** (`ProtoModelPlan.ImmutableArrayAsSpan`) separately from
`ListAsSpan`, because it is a different capability with a different origin — `CollectionsMarshal` is
a net5+ *framework* type, while this rides on the `System.Collections.Immutable` *package*, so a
net472 consumer can perfectly well have it. Absent it, the member keeps the stateful path.

**Derived lists remain the deliberate hold** described below — still no fixture, still not widened.

### B23 (original entry). Packed columns are limited to `T[]` and `List<T>`

`RawPackedWritable` admits `Factory is "CreateList" or "CreateVector"` only, so of the 28 repeated
factories the generator knows, **two** reach the raw packed path. Two exclusions are worth separating,
because one is a design decision and the other is unfinished work:

- **`ImmutableArray<T>` — unfinished.** The design note in `notes/packed-writes.md` explicitly lists
  it as one of the three shapes that yield a span (`.AsSpan()`), and it never got implemented. It is
  a struct, so it also needs the hazard settled that the design note flags and nobody has checked:
  **`default(ImmutableArray<T>)` is not null and throws on most access**, so whether `AsSpan()`
  survives a default instance decides whether the emit needs an `IsDefaultOrEmpty` guard the array
  case does not.
- **derived lists (`TakesCollectionType`) — a deliberate hold, with the reasoning already done.** The
  unpacked path excludes them because `foreach` binds to the DECLARED type's `GetEnumerator`, which
  could be a hiding redeclaration. `CollectionsMarshal.AsSpan` has no such hazard, so the packed path
  **could** be wider than the unpacked one. It is not, only because no fixture covers a derived
  packed list — and a widening nobody has a test for is not a widening.

Everything else in that table (sets, queues, stacks, the concurrent and immutable families) has no
span at all and is correctly out.

### B26. Span unrolls: every collection SHAPE done; element KINDs mostly done, BCL level variants remain — **and a SHIPPED FEATURE now leans on the remainder**

**Priority note, 2026-08-17.** `[ProtoDataFormat]` merged into `v4` (#1276), and its headline use —
`[assembly: ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]` at level 300 — lands squarely on
the tier this entry still owes. `RawMemberMeasureBlocked` blocks a member on *any* non-default
`DataFormat` (bar a packed column and `Group` on a unary message) and `BclMeasurable` gates on the
default format outright, so an ambient declaration takes members off measure-first and **one blocked
member removes its whole contract, to a fixed point through every referrer**. The feature's own
fixture demonstrates it: `FormatDefault.output.cs` emits `RawRead_` and **zero**
`Measure_`/`RawWrite_`.

That is a gap here, not a defect there — but it changes the ranking twice over. The consequence is
**silent**: a consumer who turns the attribute on loses write throughput with no diagnostic and no
way to discover why, and the symptom is "it got slower". And the arms it needs are the *cheapest*
ones outstanding, because the formats this attribute makes common are constant-width — a level-300
`FixedSize` `Guid` is 16 bytes, `FixedSize` `DateTime`/`TimeSpan` is 8. Doing the constant-width
cases alone would cover the motivating use.

Worth considering alongside them: an **info-level diagnostic** when an ambient format demotes
contracts off measure-first. Nothing else tells the consumer, and "your model got slower for a
reason you cannot see" is precisely the failure this codebase keeps writing notes about.



Marc, 2026-08-15: *"ensure we use span based unrolls when possible for lists/arrays/immutable arrays
for regular non-packed writes and measures of all types."* Half done, and the halves are worth
separating because only one of them is a whitelist.

**Collection shape — done.** `RepeatedSpan` is the single decision point and covers all three:
`T[]` directly, `List<T>` via `CollectionsMarshal.AsSpan` (net5+, probed), `ImmutableArray<T>` via
`AsSpan()` (probed). Both `EmitRawRepeatedWrite` and `EmitRawRepeatedMeasure` go through it, so
wherever the raw path is taken there is no enumerator, packed or unpacked.

**Element kind — still a whitelist.** `RawRepeatedWritable` admits `Bool`, the integer kinds,
`Char`, `Single`, `Double`, `String`, plus `Message` through `RawRepeatedMessageTarget`. Absent, and
therefore still on the stateful path *and* still measure-blocking their contract:

| kind | state |
| --- | --- |
| `Bytes` (`List<byte[]>`) | **DONE 2026-08-16.** Length-prefixed and shaped exactly like `String`; `WriteRawBytes` emits its own length and the measure is `tag + varint(len) + len`. Admitted for the `byte[]` element spelling only. |
| `IntPtr`/`UIntPtr`, `DateOnly`/`TimeOnly` | **DONE 2026-08-16**, scalar *and* repeated. All four are plain varints — `WriteDateOnly` is `WriteInt32(DayNumber)`, `WriteTimeOnly` is `WriteInt64(Ticks)` — so no library surface was needed after all. |
| `DateTime`, `TimeSpan`, `Guid`, `Decimal` | **still open**, and the only ones that genuinely need library work. |

Every kind added stops blocking measure-first for the contract that holds it, and that exclusion
runs to a fixed point — so the reach is wider than the member count suggests.

#### The last four: level 200/240 DONE, the level variants remain

**Landed 2026-08-16** — all four compatibility-level BCL types now measure arithmetically at the
**default format**, which is the common case since level 200 is the default:

| type | measure | shape |
| --- | --- | --- |
| `DateTime`, `TimeSpan` | `BclHelpers.MeasureDateTime` / `MeasureTimeSpan` | one `MeasureScaledTicks`, since both serialize as that message |
| `Guid` | `BclHelpers.MeasureGuid` | constant: empty, or two `Fixed64` fields at one-byte tags = **18** |
| `decimal` | `BclHelpers.MeasureDecimal` | value-dependent: three fields, each omitted when zero, so `0m` has an empty body |

Each measure sits **beside its writer** rather than in `BclHelpers` with the public entry points —
the two must agree field-for-field and adjacency is the cheapest way to keep that true.
`BclHelpers` just forwards. They take no `ISerializationContext`, because a generated `Measure_`
has none.

Proven against **bytes protobuf-net actually wrote** (`BclMeasureTests`, 29 cases): serialize a
one-member contract, read the length prefix back out, compare. Not against a second copy of the
arithmetic, which would agree with itself. Verified able to fail.

**Still open**, in the order worth doing them:

1. **`DataFormat.FixedSize` on `DateTime`/`TimeSpan`** — the flat eight-byte form under a `Fixed64`
   header, so the measure is the constant `8` and there is no length prefix at all. Cheap, but it
   needs the blanket `DataFormat != Default` refusal in `RawMemberMeasureBlocked` relaxing for
   these kinds — the same shared gate that already carries carve-outs for `Group` and for packed.
2. **level 240+ `Timestamp`/`Duration`** — a seconds+nanos message, genuinely different arithmetic
   from `ScaledTicks`; needs its own measure and its own fixture.
3. **level 300 `GuidString`/`GuidBytes`/`DecimalString`** — string and byte forms; `GuidBytes` is a
   flat 16 and `GuidString` a flat 36, so only `DecimalString` is value-dependent.
4. **repeated BCL elements** — `List<DateTime>` and friends. Note the ordering trap found while
   fixturing this: a repeated member is tested for eligibility **before** the BCL arm, so a single
   `List<DateTime>` drops its whole contract back to write-to-count.

**The trap to expect, because it has now happened three times:** the measure emitter reaches BCL
kinds from **three** places — the nullable path, the tuple path, and the main switch — and each
asks `RawScalarMeasure`, which returns null for these and is dereferenced with `!`, emitting
`len += 1 + ;`. Landing `DateTime`/`TimeSpan` exposed the nullable one; widening to `Guid`/`decimal`
exposed the tuple one, via an *unrelated* fixture (`Diagnostics/TupleLevels`). Anyone adding the
level variants should expect a fourth. The goldens catch it; review did not.

### B24. ~~`Serialize<object>` on a `RuntimeTypeModel` costs 41×~~ — **FIXED on main (#1280); the general half remains**

Found while disproving the "~1 µs per member" claim, and it is a real user-facing cliff rather than a
harness artefact — but note it is the **pair**, and neither half alone is slow:

| model | dispatch | per call | allocated |
| --- | --- | ---: | ---: |
| generated | generic | 58 ns | 0 |
| generated | `object` | 76 ns | 0 |
| `RuntimeTypeModel` | generic | 72 ns | 0 |
| `RuntimeTypeModel` | **`object`** | **2951 ns** | **2272 B** |

This is the call shape `PBN2011` already warns about for AOT reasons; it turns out to carry a large
throughput and allocation cost as well, on a shape plenty of pre-generic protobuf-net code still
uses.

**DIAGNOSED 2026-08-15, and it is a `main` bug, not a v4 one** — `RuntimeTypeModel.cs` is
byte-identical between this branch and `origin/main`, so it ships in 3.x today.

**Cause: negative serializer lookups are never memoised.** `RuntimeTypeModel.GetServices<T>` is

```csharp
=> (_serviceCache[typeof(T)] ?? GetServicesSlow(typeof(T), ambient));
```

and `GetServicesSlow` ends with `if (service is not null) { … _serviceCache[type] = service; }`. A
`Hashtable` miss and a cached null are indistinguishable, so a type with **no** service re-runs the
whole slow path on **every call**: a `lock (_serviceCache)`, `TryGetRepeatedProvider`, and
`FindOrAddAuto`. `typeof(object)` is exactly that type, and the object API asks for it every time.

Attributed exactly rather than guessed — `src/NanoBench/ObjectDispatchProbe.cs`, run with
`dotnet run --project src/NanoBench -c Release -- --probe`, using
`GC.GetAllocatedBytesForCurrentThread()` so it needs no profiler:

| | B/call |
| --- | ---: |
| `TryGetSerializer<object>` (never cached) | **2272.0** |
| `TryGetSerializer<Payload>` (cached) | 0.0 |

i.e. **100% of the cliff is that one lookup**. It is perfectly linear from 1 call to 1000, so it is
a fixed per-call cost and not a warm-up, and it is identical to a `Stream` and to an
`IBufferWriter`, so the destination is irrelevant.

**Scale**: 2.2 KB/call is ~22 MB/s of pure GC pressure for a service doing 10k messages/sec through
the non-generic API, for no work.

**Two candidate fixes**, and they are not the same size:

- **A, minimal.** Short-circuit `typeof(T) == typeof(object)` in `TypeModel.TryGetSerializer<T>`;
  `object` can never have a serializer, so nothing is lost.

  **This was first written up as "a JIT-time constant, so it folds away and costs every other `T`
  nothing". That is wrong, and Marc caught it: all reference-type instantiations share a single
  canonical JIT body (`__Canon`).** So for a reference-type `T` — which is every contract that
  matters here — the test does *not* fold; it becomes a runtime type-handle load and compare inside
  the shared code, paid by every caller. It folds only for value-type `T`, which get their own
  instantiation, and those are not the case in question.

  It is probably still fine, because `TryGetSerializer<T>` is called **per root serialize**, not per
  member, and one type-handle compare against ~72 ns of existing work is noise. But "probably fine"
  is a measurement, not an assertion, and the first version of this entry asserted it.
- **B, general and needs care.** Memoise negatives with a sentinel. Wider (it helps *any* repeated
  failed lookup, including the auxiliary-type paths) but it changes behaviour: a type that becomes
  serializable later would stay negative unless invalidated, and `ResetServiceCache` is called from
  only two `MetaType` sites — nothing obviously invalidates when a *new* type is added. The cache is
  also keyed on type alone while `GetServicesSlow` takes an ambient `CompatibilityLevel`; that is
  already true of positives, but caching negatives widens the exposure.

**The `__Canon` correction shifts the balance toward B**, which is worth saying plainly since A was
recommended on a false premise. B adds **nothing at all** to the generic path — the fast path stays
a single `Hashtable` hit — whereas A adds a comparison to every reference-type caller to fix a case
only the object API reaches. B's cost is entirely in invalidation semantics, which is a design
question with a testable answer, rather than a tax on the hot path.

A third option worth weighing with them: cache the negative **only for `typeof(object)`**, inside
`GetServices`/`GetServicesSlow` rather than at the generic call site. That has B's zero-overhead
property and A's zero-risk property — `object` cannot become serializable, so the invalidation
question does not arise — at the cost of being a special case rather than a general fix.

**Fix on a main-facing branch either way** (Marc, 2026-08-15: "we might break tradition and fix it on
a main-facing branch") so 3.x gets it, and let it flow into v4 by merge. Whichever is chosen, the
`--probe` numbers above are the before/after gate.

**FIXED on main — [PR #1280](https://github.com/protobuf-net/protobuf-net/pull/1280)**, 2026-08-15,
by the third option: an early return for `typeof(object)` in `GetServicesSlow`, plus
`ObjectSerializerLookupTests`. **2272 → 0 B/call.**

**Merged to `main` as `afb97b2d` and merged back here**, 2026-08-15, so `--probe` now reports
**0 B/call** on this branch too. The route was: cherry-pick onto a branch off `main`, drop the local
copy so exactly one version existed, then take it back by merge. That avoided reconciling a
duplicate — and, the real reason for the care, avoided any review change to #1280 turning that
duplicate into a conflict. In the event #1280 merged unchanged, so the caution cost nothing and
proved nothing; it was still the right shape for a fix whose review outcome was unknown.

**Still open, and the larger half**: general negative-caching, for the auxiliary-type paths. #1280
fixes only `typeof(object)`, which is the one type where "never has a service" is guaranteed
(`Add(typeof(object))` is refused), so it needs no invalidation. Doing it generally does.

### B25. ~~Doc links in SOURCE still point at `protobuf-net.github.io`~~ — **CLOSED, main #1279**

Main's move to `docs.protobuf-net.dev` swept `docs/` only; roughly ten links remain in source —
`ThrowHelper`, `DataContractAnalyzer`, `AotMigrationAnalyzer`, `AddProtoModelCodeFixProvider` and its
tests, `Issue722`, `NullWrappedValueAttribute`. Two of those are **analyzer `helpLinkUri`s and
exception messages**, i.e. consumer-visible.

Deliberately not fixed on this branch: every one is present in main with the same URL, so it is
main's sweep to finish, and doing it here would put unrelated churn in a merge. Marc is handling it
(2026-08-15). Recorded so it is not lost if that lands after this branch merges — note
`AotMigrationAnalyzer.cs` is the one file both sides touch, so expect a small conflict there.

**Done by main #1279** (`d5324e64`), merged here 2026-08-15. A grep for `protobuf-net.github.io`
across `*.md`/`*.cs`/`*.csproj` now returns only this heading. The predicted conflict in
`AotMigrationAnalyzer.cs` did **not** materialise — both sides touched the file but not the same
lines, so it auto-merged. Leaving it to main was the right call: no unrelated churn in a merge
commit, and the sweep landed as one coherent change rather than a scattered follow-up.

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
ambiguity `PBN3021` reports is only reachable in a project whose DTO generation is already
incomplete. The diagnostic still earns its place: it names the problem where the alternative is a
silent pick.

## D. Decisions owed by a human

| question | detail |
| --- | --- |
| **manual review of the write-emitted goldens** | **63 changed vs `v4`**, plus **3 new fixtures** (`PackedAll`, `BclMeasure`, `RepeatedBytes`). Start with `PackedAll.output.cs`: it is the only file showing all six `WriteRawPacked*` variants, added because three of them (`Bool`, `Fixed32`, `ZigZag`) previously appeared in no golden at all. Was 55 when `int32` moved onto the raw path; the packed arc, `ImmutableArray`, repeated `bytes`, `nint`/`DateOnly`/`TimeOnly` and the BCL measures have all moved shapes since. **The bodies moved twice more on 2026-08-16 after the review began** — B16's local folding (`lengths{n}` gone, `len{n}` folded) and B16a's drift-check calls — so anything read before those two commits is stale; both diffs are uniform and skim quickly. **The one item still open** |
| **do the two external PRs land before or after #1277?** | #1275 and #1276 are retargeted to `v4`, merged, gated and MERGEABLE (see the handover in `notes/nano-writer.md`). Landing them first means #1277's goldens move again; landing #1277 first means both need another merge-forward. Either is fine — but they *both* need one more forward-merge for the `PBN3000+` renumber regardless |
| ~~the tag ladder: keep or revert?~~ | **Settled 2026-08-14: narrowed, not reverted.** Split by *dynamic population* rather than kept or dropped wholesale — the one- and two-byte arms and the bool fold stay, the folded 3/4/5-byte arms go back to the shipped encoder. Two follow-on micro-ideas (`&` for `&&`, hoisting `RemainingInCurrent`) were answered by inspection and need no measurement; the reasoning is in the comment block |
| ~~when to rewrite `docs/aot.md`'s "needs its own project" advice~~ | **Done**, pre-emptively on `aot-schema-model`, so it arrives with the merge |
