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
| a **collection as a map key** — `Dictionary<List<int>, string>` | **won't do** | Arguably invalid rather than merely unsupported: the BCL immutable collections have no *intrinsic structural equality* (`ImmutableArray<T>` compares its underlying array **by reference**, `ImmutableList<T>`/`ImmutableHashSet<T>` do not override at all), so such a dictionary misses an equal-but-distinct key **before serialization enters into it**. protobuf-net's own *compiled* path throws on it too, so the reference behaviour to copy — if it is ever built — is the **reflection** path. Note the scenario is not automatically a mistake: a composite identity key is ordinary in generator-shaped code, and this very codebase keys its incremental cache on an `EquatableArray<T>` for exactly that reason. Supplying an `IEqualityComparer` does not rescue a round-trip either, because protobuf-net **constructs the collection itself** (`ActivatorCreate`), so the comparer is not carried across; the only shape that really works is a key type whose own equality is structural |
| a **hand-written serializer as a map key or value**, where its category is scalar or unknown | **won't do** | **Zero occurrences in a 1,392-contract corpus.** The unary and collection forms both defer the category to the serializer at run time and a map plausibly could too — `MapSerializer` calls `InheritFrom` on each side exactly as the repeated one does — so it is unbuilt rather than impossible, and is a morning's work if anyone asks. **The *collection* form was refused on a wrong premise, which is worth remembering:** the claim was that an element cannot defer because its wire type is baked into the collection's features at the call site — it is baked in only because we chose to bake it in. `WriteRepeated`/`ReadRepeated` both call `features.InheritFrom(serializer.Features)`, which fills in category and wire type *precisely when they were not specified*, so stating no wire type and passing the element serializer defers exactly as `WriteAny` does. Emitting the message form regardless is what produced `Invalid wire-type String` on `Issue1083`'s `List<WrappingStruct>` |

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

### B6. ~~Maps measure-first~~ — **scalar/string sides 2026-08-21, ENUM sides 2026-08-22; message values blocked on B43**

**Built.** A map's size is arithmetic even though its write stays on `MapSerializer.WriteMap` —
measure and write eligibility are independent, as they already are for a repeated BCL member. So a
map member stops blocking, and its contract keeps measure-first rather than dragging itself and
every referrer onto write-to-count.

The shape, read off `KeyValuePairSerializer.Write` rather than assumed:

```
per pair: memberTag + varint(entry) + entry
where     entry = (key non-trivial   ? 1 + keyBody   : 0)
                + (value non-trivial ? 1 + valueBody : 0)
```

The guards are the non-obvious half. **The entry is always emitted, even when empty** (`tag + 0x00`)
— it is the pair's *contents* that are conditional, not the pair. Each side is tested
**independently** via `HasNonTrivialValue`, so a default key or value gives a half-entry. And
**`string` is non-trivial when merely non-null** — an empty string *is* written ("we write `""` for
compat") — so it measures as a present zero-length field. Both inner tags fold to one byte, the
field numbers being 1 and 2.

**Scoped**: scalar and string sides, default formats. Enum sides (the underlying scalar needs a
cast) and message values (a nested `Measure_` with a **null** slot buffer, since the write consumes
no slots) are follow-ups, not obstacles.

**The corpus caught the one real mistake instantly**: `ValueKind` is the *element's* kind for a
repeated value, so `Dictionary<int, List<int>>` reported `Int32` and the measure emitted
`pair.Value != 0` against a `List<int>`. `ValueSerializerFactory` is the marker for a model-resolved
value and now excludes it.

`MapMeasure.input.cs` exists because `Map.input.cs` structurally cannot cover this — it carries a
message-valued map, which is unmeasurable, and one blocked member takes the whole contract, so no
map measure was ever emitted there. The corpus exercises the new path 62 times; the fixture makes it
reviewable and gives it a fast guard.


Entry = one KV sub-message; both sides already have measure forms for the native kinds.


#### Enum map sides — DONE 2026-08-22; message VALUES investigated and blocked

An enum side is the underlying scalar on the wire plus a cast, which is what the old comment here
predicted. One thing it did not predict, and it is the same trap the lone null-wrapped enum sprang:

**an enum map side is written even when ZERO.** Probed — `{1:None}` is `0A-04-08-01-10-00`, and
`{None:1}` is `0A-04-08-00-10-01`, where a plain `int` of 0 is omitted from the entry entirely.
`KeyValuePairSerializer.Write` asks `HasNonTrivialValue`, and `EnumSerializer` supplies no
`IValueChecker`, so the "non-null is non-trivial" default applies. So the enum arm carries **no
guard at all**, and reusing the scalar guard would have measured short for exactly the zero case.

**Message VALUES were built and then backed out**, and the evidence is worth keeping because the
shape looks trivially adjacent to the enum one:

- with message values admitted, the corpus went from **0 mismatches to 13**, every one of them a
  wrong LENGTH PREFIX rather than wrong content: same total payload, one byte different at a
  prefix. Removing message values alone took it back to 0, so the enum half is independently clean;
- the reproduction is a **null map value**. In a fixture, `Dictionary<int, Leaf>` with `[3] = null`
  has the reference emitting `4A-02-08-03` (key only) and the generated model emitting
  `4A-04-08-03-12-00` — a present, empty value message. That is a **write-side** divergence, and it
  is B43's subject: *null is not representable in a map*;
- **measuring is what turns that latent disagreement into a broken payload.** While such a contract
  was unmeasurable its length prefix came from writing-to-count, which necessarily matched whatever
  the writer did, right or wrong. An arithmetic measure does not, so a pre-existing disagreement
  stops being invisible and starts being a corrupt frame.

So message-valued maps stay out **until B43 is settled**, which is the honest ordering: the fix is
not in the measure. `MapMeasure.input.cs` carries the enum pair, with a zero on each side.

Still out beyond that: a message **key** (the write path refuses one too), and any non-default
key/value format.

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

### B14. ~~Groups defeat measure-first~~ — **done 2026-08-14; write-side depth guard added with it. BUT SEE B35** (2026-08-19): a benchmark says delimited writes are 5.7× slower than prefixed, which this entry says should be impossible**

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

> **Superseded in part by B38 (2026-08-21).** Everything below about `state.RawLengths` is a
> record of what was true then: the dictionary it describes is gone from the generated path,
> replaced by a positional `long[]`, so the access-shape measurements here no longer apply to
> current output. The *reasoning* is why it stays — the three-shapes comparison (per-site local
> vs hoisted vs inline) and the rule that `tmpN` must remain a local because `value.Something`
> is consumer code both still hold. The `tmpN` half of this entry is still open.


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

### B17. Callbacks and measure-first — **the GENERATED half is BUILT (2026-08-21/22); the classic STREAM path is what remains**

> **Resolution, read this first — the analysis below is the design argument and several of its
> statements are now history.** A `[ProtoBeforeSerialization]` contract is measurable, and the
> callback fires in **both** passes with `ProtoWriter.IsMeasuring` telling them apart. Three
> sentences below are therefore no longer true and are left in place only as the record:
> "`RawMeasurableShape` requires `!HasCallback(...)`", "the measure pass invokes no callbacks at all,
> and cannot", and "this also makes the current refusal load-bearing rather than conservative".
>
> Two things about how it was actually done differ from the proposal below, and both are
> improvements:
>
> - **`Measure_` takes an `ISerializationContext`, not `ref state`.** That answers point 2's
>   coupling objection outright rather than accepting it: the measure stays a pure arithmetic walk,
>   independently callable, and gains one reference argument. `ref state` was never needed.
> - **the context it hands the callback is a MEASURING one** — a wrapper for which `IsMeasuring`
>   answers `true` (`RawLengthBuffer.AsMeasuring`). The classic backend answers that question by
>   *being* a counting writer; the raw measure has no writer to be. Firing twice while answering
>   `false` both times would have been worse than the old refusal.
>
> Also note "**the length cache already buys it**" below describes the **classic** path only. The
> generated raw path measures arithmetically and visits each node once by construction; what it
> needs is *transport*, which is `RawLengthBuffer` — see B38, and the same correction in `AGENTS.md`.
>
> **What is still open is the other half of Marc's 2026-08-14 decision**: on the *classic* engine, a
> nested contract's callback still fires **once** to a stream and **twice** to an `IBufferWriter`.
> Making the stream path twice is the remaining work, and B14 is still coupled to it. See
> `notes/gaps.md` B42 for the generated side.

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

### B26. ~~Span unrolls: every collection SHAPE done; element KINDs mostly done, BCL level variants remain~~ — **CLOSED 2026-08-19: all four items done**

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

1. ~~**`DataFormat.FixedSize` on `DateTime`/`TimeSpan`**~~ — **DONE 2026-08-19.** The flat eight-byte
   form under a `Fixed64` header, so the whole member folds to `len += tagLen + 8` — one literal, no
   length prefix, no body local. `BclFixedWidth` is the single decision point; `BclMeasurable` and the
   blanket `DataFormat != Default` gate in `RawMemberMeasureBlocked` both consult it, so the carve-out
   sits beside the ones for `Group` and packed rather than duplicating their shape. Ref-emit's own
   output (`BclFixedSize.reference.cs`) independently shows the `WireType.Fixed64` header the constant
   assumes, which is the check worth having — the arithmetic is otherwise self-confirming.

   **The tuple arm of this is defensive and unreachable**, and that is worth stating so nobody
   "tests" it: the third measure site fires on `contract.IsTuple`, for a tuple's own synthesised
   members, which carry no attributes and therefore no `DataFormat`. The arm is written anyway,
   because the failure mode when a path is missed is `len += 1 + ;` rather than a wrong number.
2. ~~**level 240+ `Timestamp`/`Duration`**~~ — **DONE 2026-08-19.** `MeasureSecondsNanos` sits beside
   `WriteSecondsNanos` on `PrimaryTypeProvider` and mirrors it exactly: both fields are omitted when
   zero, so a default `Timestamp`/`Duration` has an **empty body**, and negatives sign-extend to the
   ten-byte varint form (which `ProtoWriter.MeasureInt64`/`MeasureInt32` already account for).

   **`NormalizeSecondsNanoseconds` has to run first, with the same `isTimestamp` flag** — it is what
   decides the final pair, so measuring the un-normalised values would agree on ordinary inputs and
   disagree at every boundary. That is why `BclLevel240.input.cs` carries sub-second, exact-second and
   negative samples rather than a couple of ordinary dates.

   As predicted by item 3's restructuring, this needed **no generator change** beyond letting
   `BclMeasurable` through: `BclSuffix` already picked `Timestamp`/`Duration` for the writer, and
   `BclMeasureBody` follows it. `BclMeasurable` is now level-agnostic for all four kinds.
3. ~~**level 300 `GuidString`/`GuidBytes`/`DecimalString`**~~ — **DONE 2026-08-19.** All three stay
   length-prefixed, so the emitted shape is the usual `tag + varint(len) + len` and only the body
   measure differs: `GuidHelper.Measure` is a constant 16 or 36 (and **0** for `Guid.Empty`, which
   the writer short-circuits to an empty payload), while `MeasureDecimalString` formats for real with
   the same `Utf8Formatter` call as the writer — the only honest way to agree with it.

   **The structural change is the one worth keeping**: `BclMeasureBody` is now keyed on
   `BclSuffix`, *the same selector the writer uses*, instead of on the member kind. The level and
   format pick `Guid`/`GuidString`/`GuidBytes` and `Decimal`/`DecimalString`, and deriving measure and
   write from one function is what makes them unable to drift. It also means item 2 below needs no
   generator change at all beyond `BclMeasurable` — just the two new measures.

   `RawMemberMeasureBlocked`'s blanket format gate now asks `BclMeasurable` rather than carrying a
   second special case, so `FixedSize`-on-`Guid`-at-300 (the only format that reaches these) is
   admitted in one place.
4. ~~**repeated BCL elements**~~ — **DONE 2026-08-19.** The ordering trap was real and is exactly
   why nothing admitted them: the repeated branch of `RawMemberMeasureBlocked` runs **before** the
   BCL arm, so a single `List<DateTime>` dropped its whole contract back to write-to-count.

   **The key realisation is that measure and write eligibility are INDEPENDENT**, and already were:
   a *unary* BCL member is measurable while being written statefully (`WriteFieldHeader` +
   `BclHelpers.WriteX`). So there is no need for a raw repeated BCL *write* — the member keeps the
   stateful `RepeatedSerializer` path, and only the measure has to predict its bytes:
   `tag + varint(body) + body` per element, the unary shape in a loop.

   `RawRepeatedBclMeasurable` is the predicate. Two details: `RawScalarWireBits` does not know these
   kinds are length-prefixed (it answers for the raw *scalar* kinds), so the tag is forced to wire
   type 2; and the per-element body takes its own `bcl{n}` local rather than the shared `sub`, which
   is reserved for sub-message lengths and is declared by matching on `sub = Measure_`.

   Nullable elements are excluded — protobuf-net rejects null elements outright, so there is no wire
   form to agree with.

**B26 is closed**: every collection shape and every element kind now measures, at every compatibility
level and for the formats that reach them.

**The trap to expect, because it has now happened three times:** the measure emitter reaches BCL
kinds from **three** places — the nullable path, the tuple path, and the main switch — and each
asks `RawScalarMeasure`, which returns null for these and is dereferenced with `!`, emitting
`len += 1 + ;`. Landing `DateTime`/`TimeSpan` exposed the nullable one; widening to `Guid`/`decimal`
exposed the tuple one, via an *unrelated* fixture (`Diagnostics/TupleLevels`). Anyone adding the
level variants should expect a fourth. The goldens catch it; review did not.


#### The unary formatted scalars — DONE 2026-08-22

`FixedSize` and `ZigZag` on a unary integer scalar now measure, which is the tail this entry and B30
both pointed at as "the easiest arms, being constant-width". `FixedSize` folds to a literal;
`ZigZag` is a varint over the zig-zagged value, using the same shift pair `WriteRawZigZag32/64`
apply, inline rather than as a call.

Two things had to be right together, and only the second is obvious:

- **the tag's wire bits are not derivable from the KIND.** `ZigZag` is wire type 0 like any varint —
  `SignedVarint` is a protobuf-net distinction, not a wire one — while `FixedSize` is 5 or 1 by
  width. The measure sites took `RawScalarWireBits(member.Kind)`, which would have tagged a fixed32
  as a varint; they take `ScalarWireBits(member)` now;
- **it is deliberately UNARY only.** A repeated member with a format is either packed (which vets
  its own format and has its own measure) or blocked; admitting one here would reach
  `EmitRawRepeatedMeasure`, whose element wire type is still computed from the kind alone. The write
  is untouched throughout — it stays on `WriteFieldHeader` + `WriteInt32`/`WriteInt64` — which is
  the usual measure-and-write independence.

All three scalar branches were updated together (nullable, tuple, main switch), the trap `AGENTS.md`
names by number. `Formats.input.cs` gained a `Sized` contract because the existing `Formatted` one
carries an unpacked repeated ZigZag member, which is blocked and therefore takes the whole contract
— it could say nothing about whether a formatted scalar measures. That is the third time in this
session a new arm was green while never firing; **check the emitted output, not just the gates.**

### B27. ~~`AotDifferential` loads the generator from **Debug first**, whatever it was built as~~ — **FIXED 2026-08-19**

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

**Fixed 2026-08-19.** `LoadGenerator` now tries the `Configuration` const first — the same one
#1264 gave the corpus scan — and only then the other, announcing it when it falls back. Verified
three ways rather than assumed: with a stale `bin/Debug` planted, a Release run now picks Release
and matches 3058 at 100% (it previously failed with *"the generated model does not compile"*); with
only Release present, no note fires; and with only Debug present, the note **does** fire, so it is
not dead code.

**A staleness check was written and then removed**, which is worth recording because it looked
obviously right: "warn if the generator is older than the library it will be compared against". It
fires on a **three-second build-ordering gap** — i.e. on every ordinary build — and a warning that
cries wolf is worse than none, as B32's sixteen standing warnings demonstrate. Any threshold that
silenced it would have been invented, which is the objection that removed an earlier probe test in
B16. The configuration fix alone addresses the recorded failure.

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

### B29. ~~`ProtoReader.cs` cites a `PORTING.md` that does not exist~~ — **FIXED 2026-08-19**

The "museum bridge" comment — the one that explains liquify/resolidify and is the best short
statement of how the legacy reader relates to `State` — ends *"Museum API, museum prices - see
PORTING.md"*. There is no such file anywhere in the repo (`git ls-files` finds nothing).

**Repointed rather than deleted**, since the pointer earns its place — it is what someone follows
when they wonder why the instance API is slow. It now names `notes/nano-core.md`, which actually
holds the arc and its cuts, and says outright that there is no `PORTING.md` so nobody goes looking
again. Writing a real porting guide is still worth doing if the museum API ever needs a migration
story for consumers; that is a documentation decision, not a dangling-link bug.

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
5. ~~**`decimal` + `ZigZag` is a live JIT/AOT divergence**~~ — **FIXED 2026-08-19.** `decimal` is now
   exempt from the ZigZag refusal, and the fixture proves the premise rather than asserting it:
   `DecimalZigZag.reference.cs` shows ref-emit emitting `WriteFieldHeader(1, WireType.String)` +
   `WriteDecimal` for the ZigZag member — **byte-identical to the plain one at field 2** — and
   `AotConformanceTests` compares our bytes against exactly that, so the comparison could not pass if
   the runtime refused the shape. Original note, retained because the reasoning is the reusable part:

   **PRE-DATES this feature.** The generator
   drops any BCL-kind member with `ZigZag`; the runtime *ignores* the format for `decimal` entirely
   (`ValueMember.cs`'s `ProtoTypeCode.Decimal` arm sets `WireType.String` unconditionally and calls
   `DecimalSerializer.Create(compatibilityLevel)` with no `dataFormat` argument). So the runtime
   model serializes and the generated model drops the contract and cascades. Reachable today with a
   plain `[ProtoMember(1, DataFormat = ZigZag)]`; `[ProtoDataFormat]` only widens the aperture.
   AGENTS.md already calls the refusal "a small deliberate over-reach" — the one-line fix is to
   exempt `decimal` from it.

### B31. ~~An EXTERNAL serializer takes its member off measure-first~~ — **NARROWED 2026-08-22 to a non-measuring one**

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

**Not a defect in the feature, and not fixable in GENERAL** — the whole point of a hand-written
serializer is that we do not know what it emits. But "in general" is doing a lot of work in that
sentence, and the 2026-08-22 Core audit narrowed it considerably: `Guid`, `decimal`, `DateTime`,
`TimeSpan`, `Duration`, `Timestamp` and `Empty` all measure now, so the serializers a real consumer
is most likely to meet through `[ProtoSerializer]` **do** answer. Two things could narrow it
further, the first now much more worthwhile than when it was written:

- an **`IMeasuringSerializer<T>` external serializer could be asked**, exactly as the classic engine
  already asks one (`ProtoWriter.cs`'s `OptionTrySkipWritingWhenMeasuring` interception). A
  hand-written serializer that implements the measuring interface is telling us it can size itself
  arithmetically; the raw path currently ignores that and blocks anyway;
- the **info diagnostic** proposed in B26 would cover this case too, since it is the same question
  from the consumer's side: *why did my model leave the optimised path?*

#### Narrowed and BUILT, 2026-08-22

Marc: *"presumably this logic for surrogates also applies to item 4 on our list?"* — it does, and the
first bullet above was already the answer; a ranked-list entry summarising this gap had meanwhile
claimed the opposite (*"unknowable at compile time by definition, so probably correctly out"*), which
is the summary being worse than the note it summarised.

**The size never needed to be knowable at compile time.** The measure runs at run time; all it has
to be is *arithmetic rather than a traversal*, and a serializer implementing
`IMeasuringSerializer<T>` is saying precisely that. So the member-level block is now
`SubSerializerMeasurable`, and the emitted measure delegates:

```csharp
sub = ((IMeasuringSerializer<Gauge>)SerializerCache.Get<GaugeSerializer, Gauge>())
    .Measure(context, WireType.String, tmp1);
len += 1 + MeasureRawVarint64((ulong)sub) + sub;
```

Four conditions, each for its own reason:

- **the serializer must implement the interface** for that exact target — an ordinary Roslyn check
  against `AllInterfaces`. Note this works for the **inbuilt** provider too: `GetSubSerializer`
  returns the expression `"null"` there because `PrimaryTypeProvider` is internal and a consumer
  cannot *name* it, but un-nameable is not un-**inspectable** — the symbol is right there. The
  measure names it through the public `TypeModel.GetInbuiltSerializer<T>(default, default)`;
- **the category must be KNOWN** (`SubSerializerIsScalar`/`SubSerializerDynamic` both refused). An
  undetermined category defers framing to `WriteAny` at run time, so the measure cannot tell whether
  a length prefix is in play — and a scalar one is framed by the serializer's own wire type, which
  is a different sum;
- **default `DataFormat` only** — `Group` on a unary message is otherwise allowed through, and is a
  different framing again;
- **no slot is reserved.** The write hands this member to the stateful engine, which computes its
  own length, so reserving here would shift every later length in the payload (gap B38's rule).

**This pays off much more since the same day's Core audit**, which made `Guid`, `decimal`,
`DateTime`, `TimeSpan`, `Duration`, `Timestamp` and `Empty` measuring — those are exactly the
serializers a real consumer meets through an inbuilt type or a `[ProtoSerializer]` declaration.

##### The surrogate half, and a formal DECISION on the rest

**Decided (Marc, 2026-08-22): a custom serializer that does not measure is a FALLBACK scenario, not
a gap.** It falls to the classic write-to-count path, which is correct and is what protobuf-net did
before v4; the cost is throughput, and the remedy is entirely in the consumer's hands — implement
`IMeasuringSerializer<T>` and the member, its contract and every referrer come back onto
measure-first. Nothing further is owed here, and this line exists so it is not re-argued as an
absence.

`RawMeasurableShape` now admits a delegating surrogate on the same test, so
`[ProtoContract(Surrogate = …)]` where the surrogate carries its own measuring serializer is
measured by converting and asking it. Verified end to end on **`AotNodaTimeSmoke`**, which is the
shape this exists for: `Instant` → `WellKnownTypes.Timestamp`, whose serializer became measuring in
the same day's Core audit. All four NodaTime types now emit a `Measure_`, **and so does the
consumer's own `Appointment` contract** — the cascade arriving, which is the actual payoff.

Two things about it are load-bearing:

- **measurable does NOT imply raw-writable here.** Such a contract has a `Measure_` and no
  `RawWrite_` — its body is the delegation. So `RawNativeMessageTarget` and
  `RawRepeatedMessageTarget` both exclude it (`DelegatesMeasure`), leaving a referring member on the
  stateful write and, critically, reserving **no slot**. Without that the member would call a static
  that was never emitted, breaking the consumer's build;
- **the fixed point must skip its members.** For these contracts `memberSource` stays the
  *underlying* type, so `Members` holds properties that are never serialized — the surrogate's
  serializer writes the whole body. Testing them would decide measurability on irrelevant shapes,
  and would have done so silently in either direction.

`ExternalSerializer.input.cs` grew `GaugeSerializer`/`Gauge` (measuring, message category) and a
`Panel` contract holding one. `Panel` is deliberately separate from `Holder`, which carries the
NON-measuring `Thing`: a measuring member there would have proven nothing, since one blocked member
takes the whole contract. The serializer's body is fixed-width so a wrong prefix is unmissable, and
a zero-reading sample is included because that body is written regardless.

**It also found a third spelling for `AppendFoldingLengthTemp`.** That helper declares the `sub`
local by matching assignment text, and knew `sub = Measure_` and `sub = state.RawSlots.Next()`. The
delegating form matched neither, so the first build emitted an undeclared `sub` — **in the
consumer's compilation**, which is where that class of mistake always lands. Same failure as when
the buffer spelling was added.

### B32. ~~`PublicAPI.Shipped.txt` has drifted~~ — **24 of 28 symbols fixed 2026-08-19; the residue is `#if DEBUG` API, which no tracking file can satisfy**

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

**Investigated properly on 2026-08-19, and the first diagnosis above was too narrow.** A clean
per-configuration build shows **28 untracked public symbols**, and only four of them are the
signature problem described above:

- **24 were simply never declared**, and 20 of those are **our own**: the eight `WriteRawPacked*`
  and eight `MeasureRawPacked*` members from the packed arc, and the four `BclHelpers.Measure*`
  from B26's level-200 tier. The other four are `ProtoDataFormatAttribute` from #1276. All added
  public API and none added a tracking entry. **Fixed**: Debug now reports **zero**.
- **4 are `#if DEBUG`-conditional public API**, which is the real B32 and cannot be fixed by editing
  a file: `TypeModel.ForwardsOnly` exists only in DEBUG (a test hook), and
  `TypeModel.GetInbuiltSerializer<T>` has **two different signatures** — without default arguments in
  DEBUG (*"I always want these explicitly specified in the library code; so: enforce that"*) and with
  them in Release. One tracked signature cannot match both configurations: declaring the Debug form
  makes Release report `RS0016` + `RS0017`, and declaring both makes each configuration complain
  about the other.

  So the options are all design changes, not bookkeeping: drop the `#if DEBUG` split and enforce
  explicit arguments another way; suppress `RS0016`/`RS0017` project-wide (which loses the coverage
  that just found 24 real omissions); or accept the noise. **Left for Marc** — it is his deliberate
  mechanism, and the third option is what has been happening.

The lesson that generalises: those 24 omissions hid *inside* the 192-warning noise, which is exactly
the failure this entry predicted — "16 standing warnings are how a new one goes unnoticed" — except
the number was larger and the new ones were ours.

### B33. ~~Move the working notes into per-arc sub-folders~~ — **`main` DID IT, 2026-08-20; one open question left**

Marc, 2026-08-17; closed on the `main` merge of 2026-08-20.

**`main` performed this independently, and in the shape proposed here**: `notes/aot/coverage.md`,
`notes/aot/differential.md`, `notes/aot/findings.md` — sub-folder per arc, redundant prefix dropped,
alongside the `notes/editions/` that already existed. It also added `notes/readme.md` stating the
`docs/` vs `notes/` split, which is now the reference for it; `AGENTS.md` points at that rather than
restating it.

**The predicted conflict is exactly what happened.** This entry called a rename against a concurrent
edit "the one conflict class worth not inviting", and the merge produced three `rename/rename`
conflicts — the same file renamed two ways from the same base. Resolved to `main`'s layout
throughout. Git carried the content merge to *both* paths, so resolution was: keep `main`'s path,
delete ours, strip the markers. Cheap because both sides were only ever a self-referencing path
inside the file.

**What is still flat, and the one decision that needs re-taking:** `gaps.md`, `nano-core.md`,
`nano-writer.md`, `packed-writes.md` and `aot-schema-model.md` are untouched, since `main` never had
them.

- `aot-schema-model.md` → `notes/aot/schema-model.md` is uncontroversial and follows the convention
  exactly;
- `nano-*.md` / `packed-writes.md` want a folder of their own (`notes/nano/`?) — they are the
  reader/writer engine, not the AOT generator;
- **`gaps.md` stays at `notes/gaps.md`, and the question that looked open is answered.** This entry
  sent it to `notes/aot/gaps.md` on the grounds that "its content is entirely this arc"; the merge
  made that look wrong (C14 is *editions*, and `main` now has a `notes/editions/`). Sorting the file
  settled it, because it forced the sections to be read for what they are rather than for what their
  headings suggest: **A and C are the AOT generator** (C is `[ProtoSchema]`, the generator's schema
  front-end — C14 sits there because editions changed *that*, not because the file is about
  editions), while **B is the v4 engine** — the nano reader/writer, which is not the AOT arc at all.
  So no single sub-folder owns this file, and a top-level `notes/gaps.md` indexed from `AGENTS.md`
  is right. A split would also cut B35 in half, which reasons across the boundary by design.

The sweep cost stands at ~148 references across ~25 files, in `.cs` and `.proto` comments as well as
markdown, so whichever way it goes it is a sweep and not a `git mv`.

**Sorting the file turned up a worse filing problem than the folders, 2026-08-20.** Six entries —
**B22, B23, B23 (original), B24, B25, B26**, some 335 lines — were sitting *under the `## C. Schema
front-end` heading*, having been appended to the end of the file rather than to their section. They
are packed writes, span unrolls, `Serialize<object>` cost and doc links: nothing to do with
`[ProtoSchema]`. Anyone reading section C for "what is left in the schema front-end" got six
writer-arc entries, and anyone scanning section B for B24 did not find it.

Both series are now in numeric order (`B1`–`B37`, `C1`–`C14`), with `B1 addendum` and `B23 (original
entry)` riding directly behind their parents. The move was proven content-preserving by sorting the
old and new files line-by-line and diffing — **identical**, so it is pure reordering.

How they got there is not recoverable — the history is squashed and `--follow` does not reach past
the `docs/` → `notes/` move — so no mechanism is claimed here. What is worth keeping is the failure
mode: **the entries were individually correct and correctly numbered, so nothing about them looked
wrong**; only the heading above them was, and a heading is the one thing you do not re-read when
appending under it. A section heading is invisible from inside an entry. Check which one you are
under before adding to this file, and note that numeric order is now a property worth preserving —
it is what made this visible at all.

### B34. ~~**BLOCKER: PR #1282 (build-time gRPC proxies) lands before #1277**~~ — **MERGED 2026-08-20; the measurement held**

**Closed.** #1282 merged to `main` as `427e3e80` and was merged into `v4` on 2026-08-20. Every prediction below was made from a trial merge on 2026-08-17 and is recorded as written; what actually happened is at the end of this entry.


Marc, 2026-08-17. `grpc-aot-generator` → **`main`**, **+11,500 / −85 across 105 files**, and it will
land first. So #1277 absorbs it, not the other way round. Trial-merged against `schema-breadth` on
2026-08-17 rather than guessed at; the estimate below is from that merge, not from the diff size.

**The headline is that it does not go where we go.** Zero changes to `protobuf-net.Core` or
`protobuf-net` — no library changes at all, as intended. Its own weight is a new gRPC generator
(19 files under `protobuf-net.BuildTools`), its own test corpus (`Grpc/Data`, ~57 goldens) and a new
`AotGrpcSmoke` project.

**Nine conflicted files, of which one is interesting:**

| file | shape |
| --- | --- |
| `ProtoModelGenerator.Emit.cs` | **the same fix, twice** — see below |
| `docs/aot.md`, `AnalyzerReleases.Unshipped.md`, `PublicAPI.Unshipped.txt` ×3 | list appends |
| `Reflection/Descriptor.cs`, `Reflection/Internal/CustomProtogenSerializer.cs` | **generated output**; regenerate, never hand-merge — the latter is CI's "Generated model drift" gate |
| `src/version.json` | trivial |

**The `Emit.cs` conflict is not divergent work.** Both branches carry the *same* fix — a
`WriteCondition` (`Specified`/`ShouldSerialize`) must replace the `[DefaultValue]` guard rather than
compose with it — because it was made on `v4` and independently on `main`. **Ours is a superset**: we
also gate on `!member.IsRequired`. Resolution is take-ours, then check `#1282`'s new
`ConditionalDefault` fixture still says what it means to.

**The one shared file that matters auto-merges**: `ProtoModelGenerator.Parse.cs` gains **+17/−0**, a
call to `GrpcProxyGenerator.CollectPayloadsForModel` that seeds the model from `[ProtoGrpc]`
declarations. That is the entire functional coupling between the two arcs.

**Golden churn is 1 file, not 57.** Only `ConditionalDefault.{input,output,reference}.cs` touch
`Aot/Data`, and its `.output.cs` was generated on `main`'s emitter, so it will be stale against ours
(depth guard, folded length temps, drift asserts). Its ~57 `Grpc/Data` goldens contain no
`RawWrite_`/`RawRead_`/`Measure_` at all — checked — so our emitter changes cannot touch them.

**No diagnostic-id collision, and this is worth noting as evidence rather than luck**: #1282 takes a
fresh `PBN4000`–`PBN4018` block and *references* `PBN3000`–`PBN3013`, i.e. it was written against a
`main` that already had #1283's renumber. The owner table in `AGENTS.md` did its job first time out.

**Estimate**: well under an agent-session — five list-append resolutions, one take-ours, two
regenerations and one golden. The gate battery dominates the wall-clock, not the merging.

**Two things to watch, neither large:**

- the path is `#1282 → main → v4 → schema-breadth`, two hops, and the `Emit.cs` duplicate-fix
  conflict surfaces on the **v4** hop rather than ours;
- `CustomProtogenSerializer.cs` is committed generator output, so it churns on *every* hop; regenerate
  at each, and expect the final shape to be ours.


**What actually happened, 2026-08-20.** The shape was right and the estimate was right; two things
were missed, both additions from `main` rather than errors in the measurement:

- **the conflicts were nine, but not the nine listed** — the two hops predicted here collapsed into
  one, because #1277 had already squashed into `v4`. `Emit.cs` did not conflict at all (the
  duplicate `WriteCondition` fix resolved on the earlier hop, exactly as predicted), and neither did
  `PublicAPI.Unshipped.txt`. What conflicted instead was `AGENTS.md` (13 hunks, 12 of them the
  `notes/` rename from B33) and `docs/aot.md` (3, all additive — both sides added sections in the
  same places and both are kept).
- **`main` brought two new gates with it**, neither of which this entry anticipated, and both bit:
  - **release tracking is now enforced** (`RS2000`/`RS2001`/`RS2002` as errors). It failed the build
    on `PBN1900`. Not a collision — this entry was right that the id blocks do not collide — but a
    rule change about how ids must be *declared*. See the commit and the `AGENTS.md` note: the
    tracker discovers ids from `DiagnosticDescriptor` declarations, not from `SupportedDiagnostics`;
  - **`ReferenceProvenanceTests`** stamps every `*.reference.cs` with its input's sha256 and failed
    13 of our fixtures. Twelve were merely unstamped; one, `DynamicCategory`, was **genuinely stale
    and short a member**. A gate arriving and immediately finding a real defect is the argument for
    it.

**The gate battery after the merge, all green:** traversal build 0 errors; `BuildToolsUnitTests`
624/624 (Debug *and* Release); `AotConformanceTests` 1709/1709; `protobuf-net.Test`, `Examples` and
`protobuf-net.Reflection.Test` on net8.0 *and* net472; the corpus differential **3123 compared, 0
differ** (up from 2988 — the merge widened the corpus, 1392 → 1448 seedable, 93% emitted);
`AotSmoke` native win-x64 **19 IL warnings, matching the recorded baseline**, passing in both Release
and Debug; and `main`'s two new legs, `AotGrpcSmoke` (native) and `AotGrpcMetadataDiff`, both passing.

### B35. ~~Delimited (`DataFormat.Group`) writes did NOT get the optimized emit~~ — **DIAGNOSED AND FIXED 2026-08-19; delimited is now 4.5× FASTER than length-prefixed**

Marc, 2026-08-19, from `marc/bench-delimited-v4` (branched off `schema-breadth`). **Captured, not
started.**

**Provenance, and why it raises the stakes:** this came out of a write-up of the **editions**
feature — hence "delimited" rather than `Group`. That is not a naming footnote. **C14** records that
editions' `features.message_encoding = DELIMITED` *resurrects group encoding as a first-class
choice*, and that protobuf-net is "unusually well placed" because the wire form is already
implemented on both paths. B35 puts that premise in question: the framing editions promotes is the
one that got **none** of the v4 write optimisation, and is now 5.7× slower than the alternative it
used to beat.

**Corrected 2026-08-19, having first written the opposite:** editions is not pending. It **shipped**
— #1287, GA in **3.3.21** on 2026-08-17 — so "fix this before editions ships" was already too late
when it was written. The real shape is worse and more specific:

- **today, on 3.3.21**, a consumer who sets `features.message_encoding = DELIMITED` gets the
  *faster* framing — 14% at depth 512, 27% at depth 64;
- **on v4** the same consumer gets 81,270 ns against length-prefixed's 14,258. Choosing the
  spec-blessed option would cost them **5.7×**, and leave them 4% slower than the release they
  upgraded from.

So this is a **regression for users of a GA feature, arriving at the v4 upgrade** — not a scheduling
preference. It is worth weighing as a v4 release-blocker candidate on that basis: v4's whole pitch is
that everything gets faster, and this is a now-standard encoding choice, on the one wire form
protobuf-net backed 15+ years before the spec caught up, where it would not.

**C14 predicted it five days early** and the prediction is why the diagnosis has a favourite: that
entry warned on 2026-08-14 that groups defeating measure-first "would make an editions-delimited
payload slower rather than faster until it is fixed". B14 was marked done the same day. B35 measures
the predicted slowness anyway — so **"B14 regressed, or its fix never covered this shape"** outranks
the other two candidates below.

**The finding.** Length-prefixed framing got the optimized write emit; delimited did not. On
`schema-breadth`, delimited is now the slower framing in *every* serialize cell, by up to **5.7×**.
Serialize to `Stream`, deep chain of 512, ns, via the compile-time model, same source file on both
branches:

| framing | 3.3 | `schema-breadth` | |
| --- | ---: | ---: | --- |
| prefixed | 90,798 | 14,258 | **6.4× better** |
| delimited | 77,941 | 81,270 | **4% worse** |

Every other cell carries the same signature — prefixed improved 1.2–1.7×, delimited did not move.
**The two framings swapped places**: on 3.3 delimited was 14% faster at depth 512 and 27% faster at
depth 64; here it is 5.7× and 1.6× slower.

**The read path is fine** and is not implicated: deserialize improved on both framings (deep-512
prefixed 14,842 → 5,930, delimited 14,968 → 6,336) and now beats Google.Protobuf in all six cells.
Write emit only.

**Ruled out before filing** (Marc): not a runtime-model fallback — a generated model is closed over
compile-time types and would throw rather than reflect; not a payload difference — `Setup` asserts
byte-equality against Google.Protobuf for all four payloads, passing on both branches; not noise —
5.7× at StdDev under 1%.

**Repro**: `--filter *DelimitedEncoding*` on that branch. `SerializeDeep_ProtobufNet_Delimited` at
`Size=512` against its prefixed sibling is the single cell to watch.

#### DIAGNOSED 2026-08-19: a **repeated** grouped member disqualifies its whole contract

Not the stated hypothesis, and not a B14 regression. Established by emitting the benchmark's own
generated code (`-p:EmitCompilerGeneratedFiles=true` on `src/Benchmark`) and counting:

| contract | `Measure_` | `RawWrite_` |
| --- | ---: | ---: |
| `LengthPrefixedNode` | 6 | 4 |
| `DelimitedNode` | **0** | **0** |

Then the counterfactual, which is what makes it proof rather than correlation: **delete the one
repeated grouped member** and rebuild — `DelimitedNode` immediately emits **3 `Measure_` + 3
`RawWrite_`**. (The build then fails because the wide benchmarks reference `Children`; the generator
runs first, so the evidence is complete regardless.)

The benchmark's contract is:

```csharp
[ProtoMember(2, DataFormat = DataFormat.Group)] public DelimitedNode Child { get; set; }
[ProtoMember(3, DataFormat = DataFormat.Group)] public List<DelimitedNode> Children { get; set; }
```

`RawMemberMeasureBlocked`'s carve-out is `DataFormat == Group && Kind == Message &&
Repeated.Factory is null && Map.Factory is null` — so `Child` passes and **`Children` fails on
`Repeated.Factory is null`**. `RawRepeatedMessageTarget` independently requires `DataFormat ==
Default`, so a grouped repeated message is excluded from the raw repeated path in both directions.

**Two things make the effect much larger than the cause looks.** Measurability is a *static*
property of the contract, so the deep benchmark pays in full even though it never populates
`Children`; and the exclusion cascades to a fixed point, so a self-referential contract like this one
is removed entirely. That is the whole 5.7×: `Child` — the member the benchmark actually uses — would
have been emitted perfectly, and never gets the chance.

**B14 did not regress; its carve-out never covered the repeated case**, and says so: *"Restricted to
a unary message member: a grouped COLLECTION or MAP frames each element, which is a different sum and
is not attempted here."* What nobody drew out is that declining the *sum* costs the *contract*.
That is the general lesson worth keeping — in a fixed-point exclusion, "we do not attempt X" is never
local.

#### FIXED, and measured: 81,270 ns → 3,047 ns

All four sites landed together. Re-measured on the reporting branch, deep chain, `Size=512`,
`SerializeDeep_ProtobufNet_*`:

| | 3.3 | `schema-breadth` before | after the fix |
| --- | ---: | ---: | ---: |
| length-prefixed | 90,798 | 14,258 | 13,769 |
| **delimited** | 77,941 | 81,270 | **3,047** |

So delimited is **~27× faster than it was an hour ago**, and now runs at **ratio 0.22 against
length-prefixed — 4.5× faster**, having been 5.7× slower. The same shape holds at the other sizes:
0.22 at 64, 0.49 at 8. It also allocates nothing.

**That is the outcome B14 and C14 both predicted and neither had ever been able to show**: a
fully-grouped tree writes with **no measure pass at all**, so once it is admitted to the raw path it
must beat length-prefixed, which pays a measure plus a write. C14's claim that protobuf-net is
"unusually well placed" for editions is now true of the write performance as well as the wire form —
`features.message_encoding = DELIMITED` is, on v4, the fast choice.

The generated-code check that started this now reads: `DelimitedNode` **0 → 4 `Measure_` + 4
`RawWrite_`**, matching its length-prefixed twin.

#### The fix, scoped: four sites, and it must be done in one go

The arithmetic is easy — a grouped element is `start-tag + body + end-tag`, and both tags fold to the
same constant because the field number is shared and only the wire type differs (3 vs 4). It is the
same shape as `MeasureAddGroup`'s `len += 2 * tagLen + payload`, applied per element.

1. `RawRepeatedMessageTarget` — admit `Group` as well as `Default`;
2. `RawMemberMeasureBlocked` — drop `Repeated.Factory is null` from the carve-out, letting the
   repeated branch decide;
3. `EmitRawRepeatedWrite` — emit start/end tags per element instead of the `| 2` tag plus length
   prefix (it currently hard-codes wire type 2 for message elements);
4. the repeated measure arm — `len += 2 * tagLen + sub` per element instead of
   `tagLen + MeasureRawVarint64(sub) + sub`.

**All four together or none**: AGENTS.md's standing warning is that widening one of these predicates
without a matching measure arm makes the generator *throw*, surfacing as every model in the
compilation losing its `Instance` accessor with the real message buried in a `CS8785`. That trap has
bitten three times.

Worth a decision before starting, because it is a **widening** rather than a repair: the alternative
is to leave the sum unattempted and instead stop it costing the whole contract — but there is no
obvious way to do that, since a member either measures or it does not.

#### The stated hypothesis is contradicted by the code, which makes it MORE interesting

Marc's hypothesis, explicitly flagged unconfirmed: *the optimized emit declines group-encoded members
and leaves them on the classic bodies*. It fits the numbers exactly — v4-delimited ≈ 3.3-delimited
while v4-prefixed pulled 6.4× ahead.

But the emitter as written says the opposite, in two places, and **B14 is recorded as done**:

- `ProtoModelGenerator.Emit.cs` has a dedicated raw grouped-write arm whose own comment is *"THE
  point of a group, and the whole of gap B14: framed by a start/end tag pair rather than a length
  prefix, so there is no length to compute and the measure call is not merely cheap — it is ABSENT.
  A fully-grouped tree therefore writes without a single measure pass."*;
- `RawMemberMeasureBlocked` carves `Group` out of the blanket non-default-format block, for exactly
  this shape: `DataFormat == Group && Kind == Message && Repeated.Factory is null && Map.Factory is
  null`.

So a deep chain of grouped message members *should* be fully raw **and** measure-free — i.e. strictly
less work than the prefixed path, which pays Measure_ plus write. That is the reverse of what the
benchmark reports. Three ways that can be true at once, and they are what the diagnosis has to
separate:

1. the contracts are being dropped from the measurable/raw set **upstream**, for a reason unrelated
   to `Group` — the cascade runs to a fixed point, so one unrelated member would do it;
2. the carve-out is **not being reached** — e.g. the benchmark's chain member is not
   `ProtoMemberKind.Message` as the predicate requires;
3. **B14 regressed** since 2026-08-14 and nothing caught it, which would make this the first evidence.

#### Two cheap confirmations, neither needing a benchmark

- **Count the methods.** `-p:EmitCompilerGeneratedFiles=true` on the bench project, then count
  `Measure_`/`RawWrite_` for the delimited contract. This is the diagnostic `AGENTS.md` already
  prescribes — *"a model with zero of them is not exercising the raw writer at all, whatever its name
  says"* — and it distinguishes all three cases above in seconds. Note the trap recorded with it: an
  absent or empty `generated/` folder is indistinguishable from a generator that emitted nothing.
- **A `ClassicEmit` control on the same branch.** `[ProtoModel(ClassicEmit = true)]` for the
  delimited fixture: if v4-delimited ≈ v4-classic-delimited, that confirms it is on the classic
  bodies. This is a *better* control than the 3.3 comparison because it isolates emit mode without
  crossing branches or packages — and B18 exists precisely so that comparison is available.

#### Two things the report does not pin down

- **What the six cells are.** The read-path claim is "all six cells" and the write signature is
  "every other cell", but the matrix is never stated — a reader cannot reconstruct which
  depths/shapes/framings were run. Worth one line in the PR body.
- **Terminology.** The report says *delimited* throughout and the cause is `DataFormat.Group`; these
  are the same thing (proto2 groups, renamed "delimited" by editions), but the codebase says `Group`
  everywhere and a cold reader will be searching for the wrong token. Worth stating the synonym once,
  and it will matter more as C14 (editions) lands.

### B36. ~~`DataFormat.Group` vs editions' `DELIMITED`~~ — **DONE 2026-08-19: `Delimited` is the preferred spelling; `Group` kept, but hidden from IntelliSense**

Marc, 2026-08-19, agreeing the terminology gap is real: **at a minimum change the IntelliSense** on
the enum member so the link is explicit, and **consider** an alias `DataFormat.Delimited` with
`[Obsolete]` on `Group`.

**Landed as the suggested shape**, per Marc ("synonym sounds good… we'll call it a v4 addition"):
`DataFormat.Delimited = Group`, both members documented in terms of the other, and **`Group` is not
obsoleted**. Two risks were checked rather than assumed before doing it:

- **protogen's output cannot change.** The `{dataFormat}` interpolations in both code generators
  interpolate a **string** (`out string dataFormat`, assigned from `nameof(DataFormat.Group)`), not
  the enum — so the duplicate value can never make `ToString()` pick the other name in generated
  code. That was the one way a synonym could have done harm silently.
- **the public-API analyzers do track enum members** (`ProtoBuf.DataFormat.Group = 4 -> …` is in
  `PublicAPI.Shipped.txt`), so `Delimited` is declared in `Unshipped.txt` for Core *and* for
  protobuf-net's forwarded surface. RS0016/RS0017 are live here — see B32 — so omitting it would
  have added to that noise rather than being silently fine.

**`Group` is additionally hidden** — `[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]`
(Marc's call; I had argued against it, and he is the one who has to answer the support questions).
It matches the house pattern, though note every *existing* use of that pair here also carries
`[Obsolete]` — `RepeatedSerializer.CreateReadOnySet`, and three members of `TypeModel`. Hiding
without deprecating is new to this codebase.

Facts established by probe before doing it, since two of the three were guesses otherwise:

- **it compiles cleanly on an enum member** — no warning, and `EditorBrowsable` produces no build
  diagnostic of any kind, so nothing in our own tree or a consumer's starts complaining;
- **`ToString()` still returns `"Group"`.** For duplicate-valued members the *first declared* name
  wins, so the hidden name is the one the runtime reports. Harmless here — nothing stringifies a
  `DataFormat` (both code generators interpolate a `string` obtained from `nameof`) — but worth
  knowing before anyone reads a log and cannot find the member;
- **`[Browsable(false)]` alone would do nothing for this**: it targets the designer property grid.
  `EditorBrowsable` is the one that reaches IntelliSense, and only for *referenced* assemblies, which
  a consumer of protobuf-net satisfies.

**Unverified, and worth checking in a real IDE**: whether VS honours `EditorBrowsable` on enum
members specifically. It is honoured for referenced assemblies generally; enum-member support has
historically been patchy across versions, and nothing here can test IDE completion.

**protogen still emits `Group`** — deliberately, per Marc, for compatibility: emitting `Delimited`
would make newly-generated DTOs require a protobuf-net new enough to have it. So generated code
references a member that IntelliSense will not offer, which is the accepted cost.

Still true, and the reason `Group` stays: obsoleting it would raise warnings in `protogen`-generated
files consumers cannot edit.

The problem is now permanent rather than cosmetic: editions is **GA** (3.3.21) and the spec's word
for this encoding is `DELIMITED`, while the codebase says `Group` everywhere. Anyone arriving from
the editions documentation — including whoever writes the next bug report, as B35 shows — greps for
the wrong token and finds nothing.

**The doc change is unambiguous and free.** `DataFormat.Group`'s XML summary should name
`features.message_encoding = DELIMITED` outright, so IntelliSense closes the gap at the point of use.
Same for `[ProtoMember(DataFormat = …)]` and the `[ProtoInclude]`/`[ProtoMap]` overloads that take
one.

**The alias is a compatibility question, not a naming one**, and that is the part worth knowing
before deciding:

- `nameof` saves us from the obvious trap. Duplicate-valued enum members make `ToString()` pick
  unpredictably between the two names, but the codegen path uses `nameof(DataFormat.Group)`
  (`CSharpCodeGenerator.cs:979`, and the VB twin), which is compile-time and unambiguous. So an alias
  would *not* silently flip generated output.
- **`[Obsolete]` on `Group` is the disproportionate part.** protogen writes that token into consumer
  source — `tw.Write($", DataFormat = global::ProtoBuf.DataFormat.{dataFormat}")` — so obsoleting it
  makes every contract-first consumer's **generated** DTOs raise warnings in code they did not write
  and cannot edit. Fixing that means flipping protogen to emit `Delimited` in the same release, which
  then makes newly-generated DTOs require the newer protobuf-net. A rename would be paying a
  compatibility cost for a vocabulary problem.
- a `case DataFormat.Group: case DataFormat.Delimited:` pair is **CS0152** (duplicate label), so any
  consumer switching on the enum has to pick one. Minor, but it is the kind of thing that surfaces
  after the fact.

**Suggested shape, if it is wanted at all**: add `Delimited` as a documented synonym, do **not**
obsolete `Group`, and leave protogen emitting `Group` until there is a reason to break that. New
code and editions-driven users get to write what the spec says; nobody's build starts warning. The
XML doc alone may be enough — it costs nothing and fixes the discoverability problem, which is the
actual complaint.

### B37. Incoming from `main`: new analyzers will fire on existing fixtures

Marc, 2026-08-19, as a heads-up rather than a defect: new analyzers on `main` (the
`[DefaultValue]`/implicit-default family — see the open `marc/analyzer-*` PRs) will trigger against
**pre-existing tests whose default-value setups conflict**. Expect noise on the next `main` → `v4`
merge that is nothing to do with whatever change is being merged.

**Suppression in the affected fixtures is fine** (Marc), because those fixtures exist to pin a
*known state* — several deliberately encode contradictory or degenerate declarations precisely so
the behaviour is nailed down. `Partial.input.cs` already sets this precedent, suppressing `PBN0008`
and `PBN0010` with `#pragma` rather than the analyzer being changed: pinning a precedence rule
requires a contradiction to resolve, so there is no version of that test the analyzer would allow.

The thing to avoid is the reflex of "a warning appeared, soften the analyzer". Where a fixture is
deliberately contradictory, suppress at the fixture and say why in a comment; where it is *not*, the
analyzer has found something and the fixture is wrong.

### B38. ~~Length-prefixed writes lag Google.Protobuf on wide graphs~~ — **BUILT 2026-08-21: positional length transport, 1.3×–2.3×, and wide-512 now leads Google**

Marc, 2026-08-21: *"iirc there's still some scenarios in the tables where we lag Google.Protobuf -
from memory, serialize prefixed?"* Correct, and `docs/delimited.md` concedes it in its own closing
bullet: *"Google still leads the length-prefixed writes of wide graphs."*

**Which cells, exactly** (`BufferWide`, re-measured on `v4` at 4270ee79, unchanged from the doc):
every **length-prefixed** serialize of a *wide* graph, plus the shallow `deep, 8` pair — Google by
1.5×–2.3×. Every delimited cell and every deserialize cell is ours, and the deep prefixed cells are
ours by 4×–56× (Google's `CalculateSize()`-per-level goes quadratic).

**It is not B35 repeating.** The first check was the B35 diagnostic — count `Measure_`/`RawWrite_` in
the benchmark's generated model. Both contracts emit them; the prefixed path *is* on measure-first.

**The tell is internal, and needs no comparison with Google at all.** Measure-first should cost about
**two passes**, so prefixed ought to land near 2× delimited. It was landing at **3.6× (wide)** and
**6.3× (deep)**. That excess is the whole story.

**What it is:** `state.RawLengths` is a `Dictionary<object, long>` with a custom
`IEqualityComparer<object>` (`NetObjectCache.RawLengthComparer`), so every operation is an interface
call plus `RuntimeHelpers.GetHashCode`. The delimited writer performs **none**, which is exactly why
it is the floor.

**The count per node is 2 or 3, depending on shape, and the difference matters** — an earlier
version of this entry said "three" flatly, which is wrong for the wide case. The emitted write site
is `if (!TryGetValue(x, out len)) { len = Measure_(x); lengths[x] = len; }`, i.e. **lazy**: the probe
*is* what triggers the measure, so there is no separate lookup afterwards.

- **wide** (leaf children): probe-miss + insert = **2**. `Measure_` on a leaf touches the dictionary
  not at all.
- **deep**: the first write site's miss triggers one measure walk that inserts every descendant
  (miss + insert each), and the write then descends hitting each = **3**.

**Isolated by construction rather than by inference** — `src/Benchmark/LengthCarrierBenchmarks.cs`,
which holds writer, graph and output bytes constant and varies *only how a measured length reaches
its write site*: the generated statics (dictionary) against a hand-written pair carrying the same
lengths in an append-only `long[]` consumed by index. Both are checked **byte-for-byte** against the
model in `[GlobalSetup]`, and the hand-written pair carries the same depth guard,
`ThrowUnexpectedSubtype` and null-element checks the generated one does, so the comparison is fair.

| shape, n | dictionary (today) | ordered array (eager) | delimited (floor) | gain | Google | vs Google after |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| wide, 8 | 187 | 129 | 86 | 1.44× | 93 | 1.39× behind |
| wide, 64 | 1,155 | 678 | 334 | 1.70× | 641 | 1.06× behind |
| wide, 512 | 9,058 | 5,226 | 2,514 | 1.73× | 5,258 | **level** |
| deep, 8 | 219 | 107 | 81 | 2.05× | 108 | **level** |
| deep, 64 | 1,624 | 618 | 291 | 2.63× | 6,776 | **11× ours** |
| deep, 512 | 13,967 | 5,125 | 2,233 | 2.73× | 787,929 | **154× ours** |

**The change takes every cell Google currently leads to level or ahead, bar the two smallest wide
payloads** — and what is left there is fixed per-call cost, not framing. **B39 measures it**: 28 ns
of that is opening and shutting a writer with nothing written at all, which is 30% of Google's entire
93 ns at wide-8, and another ~28 ns is `Serialize<T>` dispatch that a typed overload removes. Neither
is the length machinery, and neither is touched by this entry.

**A caveat on the Google column, since it applies to every table above:** those figures are Google's
*whole public path* (`value.WriteTo(bufferWriter)`), while the array prototypes called
`ProtoWriter.State.Create` directly and so skipped `TypeModel.Serialize<T>`. Like-for-like at the
public API, add B39's dispatch cost back to our side until the typed overloads exist — which is
exactly why B39 is worth doing alongside this.

**The right-hand columns are the proof, not the left.** Removing the dictionary moves
prefixed-over-delimited from 3.6×/6.3× to **2.1×/2.3×** — which is what two passes *should* cost.
The anomaly is fully accounted for; what remains is the irreducible price of needing a length.

**Against Google this closes the gap but does not beat it**, and that correction matters: at
wide-512 it is 5,235 against Google's 5,258, i.e. **level**, not ahead. At wide-64 (677 vs 641) and
wide-8 (130 vs 93) we would still be a little behind, on fixed cost. The deep cells were never in
question.

**Why the cache is expensive here is a design point, not a bug.** In the generated raw path the
`Measure_` statics are a single recursive arithmetic traversal, so within one measure pass every node
is visited once *by the shape of the recursion*. What the dictionary buys is **lazy** measurement:
`RawWrite_` measures a child on cache-miss at the write site, and the entries that measure deposits
are what stop the next level re-measuring — without it, lazy measurement is O(n²) in depth. So the
dictionary is not redundant; it is what makes the *current* scheme linear.

An ordered array is therefore **not a drop-in replacement for the container** — it is a different
scheme: **measure eagerly at the raw boundary, then write**, with position as the correlation. That
is already what the `IMeasuredProtoOutput` path does, so it is not a new idea in the codebase.

**Do not confuse this cache with the classic one.** `NetObjectCache` holds two: `_knownLengths`
(keyed by `ObjectKey`) serves the **classic** measure-by-writing path, where memoisation genuinely
prevents 2^depth crawls and `AtMostTwiceHoweverDeepTheNesting` pins it — note that test builds a
`RuntimeTypeModel`, so it does not exercise `_rawLengths` at all. `AGENTS.md`'s "AT MOST TWICE"
paragraph explains the raw cache using the classic cache's rationale, which is how this sat
unexamined. The invariant itself is unaffected: eager-measure-then-write is still exactly two passes.

**EAGER measure is not a detail of the prototype, it is REQUIRED — proven by getting it wrong.**
The obvious refinement is to keep the generator's existing *lazy* shape (measure triggered from the
write site) and just swap the container: mark the cursor, measure, rewind, consume. It is faster on
wide graphs — 4,084 ns against eager's 5,226 at wide-512, because a leaf child is measured inline
immediately before being written instead of the child list being walked twice. **And it is O(n²) on
deep ones: 586,745 ns at depth 512, against 5,125 eager.** Every level re-measures everything below
it, which is precisely Google.Protobuf's 787 µs shape.

The reason is the one this entry already records and the author still walked into: **the dictionary's
probe is what makes lazy linear.** Take the probe away and the only thing that can stop the
re-measure is having measured the whole subtree up front. So the choice is not "eager or lazy" — it
is "dictionary + lazy" or "array + eager", and the array pays a second traversal of every child list
(~30%) as its entry fee. It still wins by 1.7×–2.7× regardless, because zero hashing is worth more
than one extra traversal.

Recorded because the wide-only measurement looks like a free further win and is a trap. A refinement
does exist — measure lazily where the contract statically cannot contain another length-prefixed
sub-message — but it does **not** fire for the common shape: `Node` *declares* sub-message members
and merely holds null in them at run time, so the static test says no. Not worth building on that
evidence.

**What it would cost to build, and the risk to weigh** (not started — decision owed):

- an ordered array is **positional**, where a dictionary is self-correcting by identity. It is sound
  only while `Measure_` and `RawWrite_` visit sub-messages in the same order — true by construction
  (one plan, identical guard conditions, slot taken pre-order before recursing) but a real coupling.
  `DebugAssertPosition` already catches a desync in DEBUG; a Release-cheap "the write consumed
  exactly as many slots as the measure produced" check is worth having too;
- **mutation between the passes gets worse, not merely equally bad.** Today a stale length corrupts
  one field; positionally it shifts every subsequent field. Both are already why
  `[ProtoBeforeSerialization]` disqualifies measure-first, so this narrows no supported scenario;
- **the shape that would genuinely break positional is UNSTABLE ENUMERATION, not aliasing** — a
  member whose second traversal yields different items (a lazily-evaluated `IEnumerable<T>`, or a
  derived collection whose `GetEnumerator` is a hiding redeclaration). **That is already closed, for
  an unrelated reason.** `RawRepeatedMessageTarget` admits only `CreateList`/`CreateVector` with
  `!TakesCollectionType`, and `RawRepeatedWritable` adds `CreateImmutableArray` on the same terms —
  i.e. `List<T>`, `T[]` and `ImmutableArray<T>` **exactly**, all traversed as a span over a backing
  array. Derived lists are already excluded with the comment *"foreach binds to the DECLARED type's
  GetEnumerator, which could be a hiding redeclaration"*. So the conservatism that protects the
  write already supplies the determinism positional indexing needs, and nothing reaching the raw
  path can enumerate differently twice. Worth re-checking if those predicates are ever widened;
- **aliasing does NOT break it, and does not even flip the verdict** (Marc raised this: one instance
  used several times in parallel, not recursively). Correctness holds *because* the array does not
  dedupe — each occurrence takes its own slot and its own recursive slot-run, and both passes
  traverse every occurrence, so they stay in lockstep. Only the memoisation is lost: the dictionary
  measures a shared subtree once and the array measures it per occurrence, bounded at 2× the total
  traversals (`m + k·m` visits against `2·k·m`). Measured on a root holding the **same** 9-node
  subtree instance *n* times, bytes asserted identical:

  | n | dictionary (today) | ordered array | delimited | gain |
  | ---: | ---: | ---: | ---: | ---: |
  | 8 | 908 | 609 | 404 | 1.49× |
  | 64 | 6,182 | 4,469 | 2,874 | 1.38× |
  | 512 | 48,855 | 35,725 | 22,548 | 1.37× |

  So the array wins even where the dictionary is at its best: re-measuring a small shared subtree is
  cheaper than the hashing that avoiding it costs. 1.37× is the **floor** of this change, against
  1.74×/2.77× on ordinary trees;
- the array must ride the same lifecycle the dictionary does in `NetObjectCache` — the null-writer
  sidecar and `MeasureState`→`Serialize` hand-off both share it today;
- **a positional cursor cannot cross the classic-interop boundary, and this is the one real design
  constraint.** `TryMeasureRaw` hands the cache out so the classic engine's measure hook
  (`ProtoWriter.Measure` → `IMeasuringSerializer<T>.Measure`) can fill it, and a *later*
  `ISerializer<T>.Write` → `RawWrite_` consumes it. Those are separate calls with arbitrary work
  between them — the engine may measure several objects before writing any, which is what
  `SetKnownLength`/`TryGetKnownLength` exist for. Identity spans that; a cursor does not. Three ways
  out, in preference order:

  1. **a boundary map** — an identity map used *only at crossings*, `object → cursor start`.
     `Measure_` entered through the hook records where its run began; `RawWrite_` entered through
     `Write` looks it up **once** and consumes positionally from there. One hash per crossing, zero
     per node, and "at most twice" holds everywhere;
  2. use the array only when the whole model is raw-measurable — a compile-time property the
     generator already has — and keep today's dictionary otherwise;
  3. accept that a classic-interop subtree measures twice and writes once (three passes).

  **Decided (Marc, 2026-08-21): 1, falling back to 2; 3 is out.** The ordering turns on one
  distinction that is easy to miss — and did get missed once here, which is why it is spelled out:
  **3 is the only option that makes anything WORSE than today.** A mixed model goes from two passes
  to three. 2 merely declines to *improve* mixed models, leaving them exactly where they are, so the
  worst case is a missed opportunity rather than a regression — and it turns "be fully raw" into an
  incentive instead of turning "not fully raw" into a new penalty. Marc: *"having the entire trick
  only good on full raw mode is fine: it is a reason to drive full raw."*

  Note this does not touch the measurements above: `DelimitedModel` is fully measurable, so the hook
  never fires on the measured path.


**There is NO cheap partial. Both candidates were tried and both fail** — recorded because each
looks obviously right until checked, and the first was offered here before it was.

- **`CollectionsMarshal.GetValueRefOrAddDefault` is unusable in this position.** The idea was to
  collapse the measure side's probe-then-insert into one hash. But the `ref` it hands back is
  invalidated by *any* later mutation of the dictionary, and the recursive `Measure_` sitting between
  the probe and the store inserts every descendant into **that same dictionary** — so by the time the
  child's length is written through the ref, the ref points at the old bucket array. Demonstrated
  rather than reasoned: take a ref, insert 64 entries, write 42 through it, read back — **0**. The
  value is lost silently, which is the worst available failure. Reordering to measure-then-add
  removes the hazard but also removes the probe, which is the next bullet.

  It would also have needed a TFM condition: `GetValueRefOrAddDefault` is **net6+**, and
  netstandard2.0/net472 have no `CollectionsMarshal` at all (verified by compiling). The existing
  `listAsSpan` probe would not have covered it — `AsSpan` is net5+, so the capabilities differ by a
  version and need separate probes, exactly as `immutableArrayAsSpan` already does for its own reason.
- **"drop the measure-side probe" cannot be done either, because in the lazy scheme the probe IS the
  mechanism.** Without it, `RawWrite_` re-measures each child's whole subtree at every level, which
  is O(n²) in depth — precisely the shape that makes Google.Protobuf's prefixed deep case 787 µs.
  Measured in the eager framing where it *is* expressible: 11,936 ns → 10,162 ns at wide-512, still
  far worse than the array's 5,235 and worse than today's lazy 9,087.

**The ordered array needs no framework API whatsoever**, so unlike either partial it carries **no TFM
condition** and helps a netstandard2.0 or net472 consumer exactly as much as a net10 one. Given the
partials are dead, it is this or nothing.

**BUILT AND VERIFIED, 2026-08-21.** `ProtoBuf.RawLengthBuffer` replaces the dictionary on the
generated raw path. Measured on the **public** path (`model.Serialize` to an `IBufferWriter`), so
like-for-like with Google's `value.WriteTo`:

| shape, n | was | now | gain | Google | vs Google |
| --- | ---: | ---: | ---: | ---: | --- |
| wide, 8 | 214 | 168 | 1.28× | 97 | 1.73× behind |
| wide, 64 | 1,173 | 696 | 1.68× | 664 | 1.05× behind |
| wide, 512 | 9,091 | 5,150 | 1.77× | 5,179 | **ours** |
| deep, 8 | 226 | 146 | 1.55× | 112 | 1.30× behind |
| deep, 64 | 1,636 | 757 | 2.16× | 6,174 | **8× ours** |
| deep, 512 | 14,044 | 6,034 | 2.33× | ~788,000 | **130× ours** |

The remaining small-payload gap is **B39**, not this: fixed writer setup plus `Serialize<T>`
dispatch, neither of which the length machinery touches.

**Three things were got wrong on the way, every one caught by a gate rather than by reading**, and
none of them predictable from the design. They are the durable content of this entry:

1. **Measure-eligibility is WIDER than write-eligibility.** The measure arm takes anything in
   `measurable`; the write arm additionally refuses a nullable member, a non-default `DataFormat`,
   and more. Under a dictionary that asymmetry was free — an entry nobody read cost nothing.
   Positionally, a slot nobody consumes shifts every later length. A sub-tree the write will not
   walk is now measured with a **null buffer**, which suppresses reservation all the way down.
2. **A contract can be raw-WRITING without being MEASURABLE**, in which case nothing has filled its
   slots and each site must measure on demand — exactly what the dictionary's `TryGetValue` *miss*
   did ("the miss arm serves a root write"). Removing the probe removed that arm. It is a
   compile-time question, so the emitter asks whether the *enclosing* contract is measurable.
3. **A grouped sub-message consumes no slot**, and a fully-grouped tree consumes none at all, so it
   must not be given a measure prologue. Emitting one unconditionally cost **2,453 ns → 3,934 ns**
   on a 512-wide grouped graph — quietly undoing B35. The prologue is gated on a transitive
   slot-consumer set.

Points 2 and 3 are why **the slot belongs to the CALL SITE, not to the contract being measured**: a
slot is reserved exactly where the write calls `Next()`, one for one, in order. The first draft had
`Measure_` claim a slot for itself, which cannot express "measured but not read".

**Gates:** `AotConformanceTests` 1709/1709; corpus differential **3123 compared, 0 differ**; goldens
624/624; `protobuf-net.Test`, `Examples` and `protobuf-net.Reflection.Test` on net8.0 *and* net472;
the "Generated model drift" CI gate clean; native publish **19 IL warnings, the recorded baseline**,
passing; and `AotSmoke -c Debug`, where `DebugAssertPosition` checks every length-prefixed write
against the bytes actually written — the sharpest available test of a positional scheme.

**One measurement still owed:** delimited reads ~4% (wide) and ~7% (deep) above its pre-change
figure. The grouped path emits identical code, so this is most likely run-to-run variance between
sessions rather than a regression — but it was not measured in the same session, so it is recorded
as unconfirmed rather than dismissed.

**The edit list, enumerated from the source rather than estimated** (14 sites; the slot layout above
is what keeps it this small):

*Core* — `NetObjectCache` gains `long[] _rawSlots`, an append high-water `int`, a read cursor, and
the boundary map `Dictionary<object, int>` (reusing the existing `RawLengthComparer`). All four ride
the lifecycle `_rawLengths` already has: cleared by `ClearAndMaybeTrim`, and **swapped** by
`InitializeFrom` — that swap is the `MeasureState`→`Serialize` hand-off, deliberately O(1) rather
than a copy (a copy once cost 22 KB and 11%), so the count and map swap with the array and the read
cursor resets. `ProtoWriter` exposes it, `ProtoWriter.State.Raw.cs` carries the API
(`Reserve`/`Next`/`Mark`/`SeekTo`/`RecordBoundary`/`TryEnterBoundary`), and **every new member takes
`[Experimental("PBN9002")]`** — the policy in `AGENTS.md`, which is easy to skip because the repo
`NoWarn`s it and nothing here fails. `TryMeasureRaw` hands out the slot buffer in place of the
dictionary. New members go in `PublicAPI.Unshipped.txt` with the `[PBN9002]` prefix.

*Generator* — `Measure_` swaps its `Dictionary<object, long>` parameter, claims `int self =
slots.Reserve()` on entry and writes `slots[self] = len` before returning; its four sub-message sites
lose the probe-and-insert and simply recurse. `RawWrite_`'s four sub-message sites lose the
probe-and-insert and read `state.RawSlots.Next()`. **The two struct arms collapse into the general
one** — today they are special-cased ("a struct has no reference identity to key on") and call
`Measure_` directly; positionally a struct child is an ordinary slot, so that carve-out disappears
rather than needing a new branch. The entry points do the eager measure and the seek, and
`IMeasuringSerializer<T>.Measure` records the boundary.

*Gates* — every `.output.cs` golden churns (regenerate in **Debug**, review the diff); no
`.reference.cs` moves, since ref-emit is untouched. Add the Release-cheap check that the write
consumed exactly as many slots as the measure produced, to sit alongside `DebugAssertPosition`.

**A methodological note, because the harness caught the author out.** `DictBaseline` — a hand-written
copy of the current scheme, added to prove the harness reproduces `Generated` before trusting deltas
from it — came in at **11,936 ns against `Generated`'s 9,087**. It was not faithful: it measured
*eagerly* and then wrote, where the generator measures *lazily* at the write site. That is a second
full traversal of every child list, and it costs 31%. Two things follow: the "3 hashes everywhere"
claim above was wrong, and **eager-vs-lazy is itself worth ~30% independently of the container** — so
the ordered array pays that traversal cost and still wins by 1.74×, rather than being flattered by it.

**A secondary, smaller finding fell out of making the comparison fair:** the per-node guards
(`ThrowUnexpectedSubtype` plus the null-element test) cost ~745 ns across 513 nodes, ~1.45 ns/node —
14% of the prefixed time at wide-512. `sealed` elides `ThrowUnexpectedSubtype` and Google's generated
DTOs *are* sealed while this benchmark's are not, so a slice of the residual wide gap is that rather
than framing. `docs/aot.md` already records the effect at ~0.6% on a message-dense payload; on a
graph this node-dense it is larger.

### B39. ~~The `Serialize` entry path costs ~28 ns a call~~ — **typed overloads BUILT 2026-08-21; they recover 17 ns of the 28, and the writer-setup floor is now the larger half**

Marc, 2026-08-21, prompted by B38's small-payload residue: *"is our Serialize path wrong? what
happens if we add per-root-type typed (non-generic) Serialize methods that take overload priority
away from the existing setup code... maybe experiment with a manually added single example rather
than write the generator code?"* Done exactly that way — one hand-written overload on the
benchmark's model, not a generator change.

**First, a correction to how B38's comparison was framed.** Google's side of that benchmark is
`value.WriteTo(bufferWriter)` — one interface call — while the array prototypes called
`ProtoWriter.State.Create` directly and so **skipped `TypeModel.Serialize<T>` entirely**. Comparing
our path-minus-dispatch against Google's whole path flattered us at the small end. Everything below
is like-for-like.

| n | `StateOnly` | via `Serialize<T>` | typed overload | saved | array + typed | Google |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 8 | 28 | 216 | 188 | **28** | 129 | 93 |
| 64 | 28 | 1,178 | 1,152 | **26** | 679 | 641 |
| 512 | 28 | 9,127 | 9,089 | **38** | 5,272 | 5,258 |

**The typed overload works and recovers essentially all of the dispatch.** A non-generic
`Serialize(IBufferWriter<byte>, TRoot)` on the model beats `TypeModel.Serialize<T>` at any call site
naming a concrete type, and lands within noise of calling the generated static directly (188 against
190). What it skips, per call, is a `TypeHelper<T>.ValueChecker.IsNull` indirection,
`TryGetSerializer<T>` resolution, two `CheckClear`s, two `GetPosition`s and `WriteAsRoot`'s feature
dispatch — all of it re-deciding at run time what a typed overload knows at compile time. Note
`FEAT_DYNAMIC_REF` is documented in the csproj but **never actually defined**, so `SetRootObject` is
compiled out and a typed path does not have to reproduce it.

**Second, and larger at small sizes: `StateOnly` is 28 ns and does not vary with payload.** That is
`State.Create` + `Close` + `Dispose` with *nothing written*. At wide-8 the whole cost decomposes as:

| | ns | |
| --- | ---: | --- |
| fixed writer setup | 28 | **30% of Google's entire 93 ns** |
| write work | 56 | (delimited 84 less the setup) |
| measure work | 45 | (129 less delimited 84) |

So our **write alone** (28 + 56 = 84) is 90% of what Google spends doing measure *and* write *and*
setup. At tiny payloads the gap is not framing, not the length cache, and — once the typed overload
lands — not dispatch either: it is that a protobuf-net writer costs 28 ns to open and shut.

**Where that leaves B38's headline:** with both changes, level at 512, 1.06× behind at 64, 1.39×
behind at 8. The array is what closes the large end; the remaining small-end gap is this entry, and
the two are independent — either can land without the other.

**BUILT, and measured on the shipped emission rather than on the prototype** — which matters,
because the two are not the same and the difference is the whole design decision:

| n | `Serialize<T>` | typed overload | saved | | `StateOnly` |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 8 | 159 | 143 | **17** | 10.5% | 30 |
| 64 | 687 | 669 | **18** | 2.6% | 30 |
| 512 | 5,100 | 5,196 | −97 | −1.9% | 30 |

**It recovers 17 ns of the prototype's 28, and that shortfall is deliberate.** The prototype called
`RawWrite_` directly, skipping `SerializeRoot` *and* `WriteAsRoot`. The shipped one stops at
`SerializeRoot`, because `CheckClear` and `Abandon` are internal and generated code cannot reproduce
the root sequence's guarantees — so it recovers the `TypeHelper<T>.ValueChecker` indirection and
`TryGetSerializer<T>` resolution, and keeps paying `WriteAsRoot`'s feature dispatch. **~11 ns is the
price of not reimplementing root semantics in generated code, and it is worth paying.**

The 512 row is **noise, not a regression**: 5,196 against 5,100 with errors of ±98 and ±56, which
overlap. Expected, since the whole saving is a fixed per-call cost — it is 10.5% of a small payload
and arithmetically invisible on a large one.

**So the honest summary is a real but modest win**, worth having because it is free at run time and
zero-risk, but not the headline the 28 ns figure implied. **The larger half of the small-payload gap
is now `StateOnly`**: 30 ns to open and shut a writer with nothing written, which is 21% of the
entire typed call at n=8 and untouched by anything here.

**Still open** — two candidates, in order:

- **the ~30 ns writer-setup floor**, now the bigger prize at small payloads and untouched by both
  B38 and the overloads. Unmeasured beyond the total — whether it is the writer pool, the buffer
  lease or `Close`/`Dispose` is not yet known, and should be measured before anything is designed;
- **a typed `Deserialize`**, which cannot use overload resolution at all (there is no instance to
  bind on) and is therefore B40's problem rather than this one's.

### B40. Steering call sites onto the fast entry points, and the read path — **captured, not started** (Marc, 2026-08-21)

B39 shows a typed non-generic `Serialize` recovers ~28 ns a call. This entry is the follow-on
question Marc raised with it: *how do consumers actually end up on it*, and what the equivalent is
for reading. **Logged deliberately without starting**, so the write-path sweep is not interrupted.

**The shared constraint, which shapes every option below:** both a non-generic overload and a
`new`-hiding generic bind only when the call site's **static receiver type is the generated model**.
`TypeModel.Serialize<T>` is **not virtual** (checked — only `GetSerializer<T>`/`GetSerializerCore<T>`
are), so a model cannot override it; anyone holding a `TypeModel` reference keeps the slow path
whatever we emit. That is the same problem `PBN3010`'s fixer already solves by swapping the receiver
to `Model.Instance`, so the machinery exists.

**On `[Obsolete]` versus a diagnostic — the diagnostic, and this is already settled policy here.**
`[Obsolete]` is `CS0618`, which is *global*: a consumer suppressing it to keep one legitimate generic
call site loses the warning everywhere, including on genuinely obsolete API. A custom id is
suppressible on its own. That is the standing preference and it applies cleanly here.

But the decisive argument is Marc's, and it is about *precision* rather than harshness:

> the advantage of the diagnostic approach is that we can decide in the analyzer whether they're
> using generics **at the call site**

`[Obsolete]` cannot tell `Serialize<Order>(...)` from `Serialize<TValue>(...)` inside a generic
method — it fires on both, and the second has no typed overload to move to, so the warning would be
unactionable and would train people to suppress it. An analyzer reads the type argument: **concrete
→ report and offer the fixer; still-open type parameter → say nothing.** That distinction is the
whole feature, and only an analyzer can make it. It also gets a code fix, which `[Obsolete]` never
does.

**The read path is harder, and the reason is structural:** there is usually no instance to pass, so
overload resolution has nothing to bind on — `Deserialize<Order>(source)` and
`Deserialize(typeof(Order), source)` both name the type rather than supplying a value. Options as
raised, none evaluated:

- **named methods per root** — `DeserializeOrder(...)`, with the analyzer redirecting both spellings
  to it and a fixer applying it. Works, but puts a generated name per contract into the model's
  surface, and reads oddly next to `Serialize(...)` overloads that need no suffix;
- **make the generic path itself cheaper**, which would help every call site including the ones no
  analyzer can reach (`TypeModel`-typed receivers, open type parameters). B39 already itemises what
  the write side spends per call; the read side wants the same treatment before choosing;
- a **`new`-hiding generic** on the model, dispatching through a generated type switch. Same
  receiver-type constraint as the overloads, and still pays generic dispatch — probably dominated by
  the other two.

**Do not build the analyzer before the entry points exist**, and check `AnalyzerReleases.Unshipped.md`
plus the owner table in `AGENTS.md` for a free id when it does — `PBN3010`–`PBN3013` is
`AotMigrationAnalyzer`'s block and this belongs beside it.


### B41. A HIERARCHY is off measure-first entirely, and `[ProtoSubType]` makes that far easier to hit

Found on the `main` merge of 2026-08-21, by running the "count the methods" diagnostic over the
fixture that arrived with #1317. `OutOfBandSubType.output.cs` emits **zero** `Measure_` and **zero**
`RawWrite_` — against `Simple.output.cs`'s two of each — while still emitting `RawRead_`. So reads
are optimised and **writes are entirely classic** for that model.

That is not new and not a merge fault: `RawMeasurableShape` has always required
`RootTypeName is null && SubTypes.Count == 0`, so any contract in a hierarchy is excluded, and
exclusion cascades to a fixed point through every referrer — one hierarchy can take a whole model
off the raw write path. **What is new is the reachability.** `[ProtoSubType]` exists precisely to
add a hierarchy to types you do *not* own, from beside the model, so a consumer can now turn a
fully-measurable model into a fully-classic one by adding an attribute somewhere else entirely, with
no diagnostic and nothing at the contract to look at.

Two things make this worth an entry rather than a shrug:

- **the failure is silent and non-local.** The contract that loses measure-first is not the one
  carrying the attribute, and the cascade means the loss is not even confined to the hierarchy;
- **the write path is where v4's gains are.** B38 measured 1.3×–2.3× on length-prefixed writes and
  wide-512 overtaking Google.Protobuf; a model in this state gets none of it, while its reads still
  look fast, which is exactly the shape that reads as "v4 didn't help much" in a consumer's
  benchmark.

**ESTABLISHED 2026-08-21: it is "never taught", not "cannot be done"** — read off the emitted
hierarchy rather than reasoned from the predicate.

Two things the golden shows immediately. **A layer's own members are already written raw**
(`state.WriteRawTag((1 << 3) | 2); state.WriteRawString(tmp1);`), so the member half needs nothing
new. And **the only stateful part is the sub-type dispatch** — `state.WriteSubType(100, sub100,
this)` — which is structurally just a nested length-prefixed frame, the same thing a sub-message
member already measures today:

```
Measure_Shape(value, depth, slots):
    len = 0
    if (TypeModel.IsSubType(value)) {
        if (value is Circle c)      { slot; sub = Measure_Circle(c, ..); len += 2 + varint(sub) + sub; }
        else if (value is Square s) { sub = Measure_Square(s, ..);       len += 2 + sub + 2; }  // StartGroup
    }
    ... this layer's own members, arithmetic exactly as today ...
```

The tag widths fold, as everywhere else — field 100 is a constant. A `StartGroup`-framed sub-type
(`[ProtoInclude(.., DataFormat = Group)]`) carries no length, so it takes **no slot**, which is the
same carve-out B38 already needed for grouped members.

**The positional invariant holds without new machinery**: both passes dispatch on `value`'s runtime
type via the identical `is` chain, and that cannot differ between them — mutation between the passes
is already unsupported for exactly this reason — so the branch taken, and therefore the slot order,
agrees by construction.

**STARTED AND BACKED OUT, 2026-08-21 — and the reason is the part worth keeping.** The two
predicate changes are easy and were made in minutes: drop the hierarchy clause from
`RawMeasurableShape`, and add a `HasRawWrite(contract)` guard to `RawNativeMessageTarget` /
`RawRepeatedMessageTarget` so nothing emits a direct call to a body a hierarchy does not have
(measurable and "has a `RawWrite_`" stop being the same question). Both compiled first time.

What stopped it is the **interaction with B38's positional slots**, which reading the predicate does
not reveal and reading the emitted hierarchy does:

- `EmitSubTypeContract` writes a layer's own members with `raw: rawWrite`, so **a layer's members are
  already written raw** — including `len = state.RawSlots.Next()` for a measurable sub-message
  member. That write is reached from `WriteSubType`, i.e. from the *stateful* path;
- so a hierarchy's slots would be produced and consumed across the classic-interop boundary, which
  is exactly the case B38's boundary map exists for — but the boundary there is per *contract*, and
  a hierarchy's measure spans several layers with the marker frames interleaved between them;
- and a sub-type marker **can be** length-prefixed — not always is (Marc): `[ProtoInclude(..,
  DataFormat = Group)]` and `[ProtoSubType(.., isGroup: true)]` both make it **delimited**, which
  carries no length and so takes no slot, exactly as a grouped member does under B38. So a
  hierarchy's slot pattern depends on each link's framing, and the two cases have to coexist within
  one chain. Where a marker *is* length-prefixed it either takes a slot — and the stateful
  `WriteSubType` must be taught to consume it, which it currently cannot — or it does not, and the
  measure must pass a null buffer down, suppressing the layer's members' slots too, which changes
  what those members' *writes* expect.

That is a genuine design question about where the measure/write boundary sits in a hierarchy, not a
plumbing detail, and getting it wrong produces **wrong bytes rather than a build error**. Backed out
with the tree green rather than half-landed.

**The shape to design first, before touching the emitter again:** a leaf's `ISerializer.Write`
delegates to the **root's** `WriteSubType` (`=> ((rootSub)this).WriteSubType(ref state, value)`), so
`Measure_Circle` must equal `Measure_Shape` — two functions per layer, not one:
`MeasureSubType_{layer}` for the layer's own contribution (its marker chain plus its own members),
and `Measure_{layer}` delegating to `MeasureSubType_{root}`. Decide the slot question against that
shape rather than against a single contract.

**What that leaves as the real work**, none of it novel: emit a `Measure_` per layer mirroring
`WriteSubType`'s chain; relax `RawMeasurableShape`'s `RootTypeName is null && SubTypes.Count == 0`;
and keep the all-or-nothing rule the hierarchy already has for dropping (one unmeasurable layer must
take the whole hierarchy, since every type routes through the root). The `is` chain in the measure
must be emitted from the *same* plan data as the write's, or the two can diverge — which is the
B38 lesson restated, and the reason to generate both from one place rather than two.

Related: `notes/gaps.md` B13 already measured `ThrowUnexpectedSubtype` in both shapes and decided to
leave the hierarchy *check* alone; that is about the per-write type test, not about measurability,
and does not answer this.


### B42. **THE MEASURE-FIRST EXCLUSION LIST** — every way a contract loses the raw write path, in one place

Marc, 2026-08-21: *"how many other big gaps do we have? inheritance? callbacks? do we have a list?"*
Not really, until now: the authoritative list is the two predicates themselves, and `gaps.md` covered
only some of them. This entry is the index; it is **derived from the source, not remembered**, and
should be re-derived rather than trusted if the predicates move.

**Why it matters more than the count suggests:** exclusion is computed to a **fixed point**, so one
blocked member removes its *whole contract*, and that removes every contract that references it. A
single awkward member can take an entire model off measure-first, and the only symptom is that
writes are slow — reads still look fine, because read eligibility is separate and much wider.
The diagnostic is to count methods in the generated output: **zero `Measure_`/`RawWrite_` means the
model is entirely on the classic write path.**

#### Contract-level (`RawMeasurableShape`) — six exclusions

| what | gap? | tracked |
| --- | --- | --- |
| **inheritance** (`RootTypeName`, `SubTypes`) | **yes, and the biggest** | **B41** |
| ~~surrogate (`SurrogateTypeName`)~~ | **DONE 2026-08-21 — also "never taught"** | below |
| surrogate with a NON-MEASURING serializer | **fallback, not a gap — DECIDED 2026-08-22** | below |
| ~~external serializer~~ (`[ProtoContract(Serializer=)]`, `[ProtoSerializer]`) | **DONE 2026-08-22 when it MEASURES** | **B31** |
| ~~`[ProtoContract(IsGroup = true)]`~~ | **DONE 2026-08-21 — it was "never taught"** | below |
| ~~`[ProtoBeforeSerialization]`~~ | **DONE 2026-08-21 — I was wrong that it was load-bearing** | below |

**The callback one — I said "not a gap, must not be fixed", and that was wrong** (Marc, same day:
*"does it have the State? could the State gain the context?"*). The correct statement is narrower:

- firing **only** in `RawWrite_` would indeed be wrong, since the object could change between the
  passes and the measured length would not match the bytes;
- firing in **neither** is what happens today — the contract is refused outright;
- firing in **both** is correct, and is **exactly what the classic buffer-writer path already
  does**: `IsMeasuring` true, then false. AGENTS.md records that the doubling there is *required*,
  not incidental, and Marc's 2026-08-14 decision was that twice becomes the consistent normal.

So the refusal is a consequence of the **signature**, not of the semantics. `Measure_(value, depth,
slots)` simply was never given a context — and one is in scope at both entry points already:
`state.Context` (public) from `ISerializer<T>.Write`, and the `ISerializationContext` argument
directly from `IMeasuringSerializer<T>.Measure`. `ProtoWriter.IsMeasuring(context)` is public too,
which is how a consumer's callback already tells the passes apart.

**The cheap shape**: thread the context only into the `Measure_` of contracts that *have* a
before-serialize callback, so the common case keeps its current signature and pays nothing.

**The threading cascades, and that is the point rather than the cost** (Marc): with
`Foo → Bar → Blap` and the callback on `Blap`, both `Bar` and `Foo` need the parameter purely to
pass it along. So the "needs a context" set is a fixed point over referrers — the same shape as
`measurable` and as B38's `slotConsumers`, and computed with the same machinery.

**What makes that cheap rather than expensive is that the cascade already exists today, in a worse
form.** `Blap`'s callback does not merely refuse `Blap`: exclusion propagates up through referrers,
so `Bar` and `Foo` lose measure-first *entirely* and fall to write-to-count. Threading replaces a
cascade of **exclusion** with a cascade of **parameter passing** — strictly better for exactly the
contracts that are penalised now, and untouched for everything else. A model with no callbacks has
an empty set and sees no change at all. Then
measure fires with `IsMeasuring == true` and the write fires with `false`, which is the behaviour a
consumer already gets from the buffer-writer backend.

**What actually needs settling before building it** — counts, not correctness:
`CallbackMeasurePassTests` pins exact firing counts per route, and generated models would move from
"refused" to "twice". That is the alignment Marc asked for in B17/B14 rather than a regression, but
it is a deliberate behaviour change and the test is the place it has to be argued. The awkward case
is the classic-interop boundary, where `ProtoWriter.Measure` **caches** by identity and may call
`IMeasuringSerializer.Measure` a variable number of times — so "at most twice" needs re-checking
there specifically, not assumed from the pure-raw path.

##### Callbacks — built, 2026-08-21

The plan above survived contact, with **three things it had not anticipated**, each found by a gate
rather than by reading:

- **the context has to be a MEASURING one, and `state.Context` is not.**
  `ProtoWriter.IsMeasuring` was `context is ProtoWriter writer && writer.IsMeasuringPass` — i.e. it
  answers for the classic backend by that backend *being* a counting writer (`NullProtoWriter`).
  The raw measure has no writer at all; it is arithmetic. So handing `state.Context` straight
  through fires the callback twice with **`false` both times**, which is strictly worse than the
  old refusal: the consumer's side-effects double and nothing tells them. `RawLengthBuffer` now
  hands out a wrapper carrying the real context's model and user-state and differing only in that
  one answer, recognised through an internal `IMeasuringPassContext` marker — so the two backends
  give the same answer from different places, which is what that method's own doc always said
  should happen. It is cached against the context it wraps, so a serialize costs one allocation and
  a reused writer costs none; and a model with **no** serialize callback anywhere never emits the
  call at all (`measuresCallbacks`, a model-level flag threaded beside `slotConsumers`).
- **the generator did not accept the one callback signature that can ask.** It took a callback
  taking nothing or a `StreamingContext`; `ISerializationContext` is the only flavour carrying the
  context *object* rather than a copy of its data, and so the only one `IsMeasuring` works on. It
  is now the third accepted shape (`ProtoCallbackArgument`). Without it the feature was
  unusable by construction — and the symptom was the smoke test *dropping the contract*, not
  disagreeing about hooks, which is a much better failure than it sounds.
- **after-serialize fires in the measure pass too.** `TypeSerializer.Write` fires both
  unconditionally, so the classic null-writer pass has always run the pair; a generated measure
  firing only the first would hand the same consumer a *different sequence* on a path the backend
  chose for them. So it is `bs,as,bs,as`, matching classic, rather than `bs,bs,as`.

The classic-interop worry turned out to be unfounded in the direction it was raised: that boundary
caches by identity through `RawLengthBuffer.Enter`/`Leave`, so a repeat crossing reuses the measured
slots rather than re-measuring, and the count stays at two.

Pinned by `MeasurableContractTests.TheMeasurePassIdentifiesItselfToCallbacks` (`bs*;as;bs;as;`, the
`*` being `IsMeasuring`) and by `AotSmoke`, which is the only place a consumer callback runs under
ILC. Verified: conformance 1640/1640, corpus 3131 compared 0 differ, goldens 639/639, smoke passed,
`protobuf-net.Test` 1578/1578 — including `CallbackMeasurePassTests`, whose per-route counts are
unchanged, it being an exercise of the classic backend.

**Note the twin had to be seeded too.** `ClassicEmitTwins.cs` declares a `ClassicEmit` model per
fixture, and a seed added to the fixture model but not to its twin fails as *"Type is not expected"*
from six different test classes at once — which reads like a generator fault and is bookkeeping.

##### The `IMeasuringSerializer` audit, 2026-08-22 (Marc: *"a quick audit that our existing custom serializers aren't unnecessarily omitting it"*)

The suspicion was right and broader than `Duration`. The recent BCL measure work (B26) added the
**arithmetic** for five families so the *generator* could call it, and never exposed any of it
through `IMeasuringSerializer<T>` — so nothing else could ask. Swept and closed:

| serializer | arithmetic | interface, before | now |
| --- | --- | --- | --- |
| primitives, `DateOnly`/`TimeOnly` | yes | **yes** | unchanged |
| `Duration`, `Timestamp` (+nullable) | `MeasureSecondsNanos` | no | **added** |
| `Guid` (+nullable) | `MeasureGuidBody` | no | **added** |
| `decimal` (+nullable) | `MeasureDecimalBody` | no | **added** |
| `ScaledTicks`, `TimeSpan`, `DateTime` (+nullable) | `MeasureScaledTicks` | no | **added** |
| `Empty` (+nullable) | trivially `0` | no | **added** |
| `EnumSerializer<TEnum>` | trivial | no | **deliberately NOT — see below** |

**Adding the interface changes no bytes on any path**, which is what makes this safe rather than a
judgement call, and both halves of that were checked rather than assumed:

- the **classic** engine consults a measure only when the serializer *also* declares
  `OptionTrySkipWritingWhenMeasuring` (`ProtoWriter.Measure`, and the stream writer's equivalent).
  **No library serializer sets that flag** — only generated models do. So the classic path cannot
  reach any of these, and the control stays a control;
- `RepeatedSerializer`'s **packed** branch tests `serializer is IMeasuringSerializer<TItem>` with
  *no* flag gate — but it is guarded by `TypeHelper<T>.CanBePacked`, which is **false** for every
  type above (it admits enums and the numeric/bool/char type codes only).

**`EnumSerializer` is the exception, and it is exactly gap B1's cause.** `CanBePacked` is **true**
for an enum, so giving `EnumSerializer<TEnum>` a measure would immediately flip every repeated enum
member to the packed encoding — different bytes for existing consumers on both paths. That is a
deliberate wire-format decision, not a drive-by, and it is why AGENTS.md's "a repeated enum is never
actually packed" has been true all along. Doing it is a *feature*; this audit only removed the
accidental omissions.

One detail worth keeping, because it is the reason the interface takes a context at all:
**`DateTime`'s measure must ask the model** whether to include the `Kind`
(`TypeModelOptions.IncludeDateTimeKind`), because `ISerializer<DateTime>.Write` does. So it does
**not** reuse `BclHelpers.MeasureDateTime`, which hard-codes the kind-less form — correct for a
generated writer, which never takes that option, and wrong here.

**`IsGroup` at contract level — established and fixed, 2026-08-21.** The suspicion was right: a
grouped contract carries no length prefix of its own, so measuring it is *easier*, not harder, and
the exclusion was "never taught" rather than necessary — the same shape as B35, where blocking
grouped *members* turned out to be backwards.

What made it load-bearing until now is that **two sites decided framing from the MEMBER's
`DataFormat` alone** and did not know the *target contract* was grouped: the raw write's
start/end-tag branch, and the measure's `MeasureAddGroup` branch. Removing the exclusion without
teaching them would have emitted a length prefix over a group-framed body — wrong bytes, not a
build error.

Both now ask `GroupFramed(member, target)`, which honours **both routes to group framing**:
`[ProtoMember(DataFormat = Group)]` on the member, and `[ProtoContract(IsGroup = true)]` on the
target, whose features carry `WireTypeStartGroup` and which `InheritFrom` supplies wherever the
member states no wire type of its own.

Verified: `ContractOptions`'s `Grouped` contract now emits a `Measure_` where it previously emitted
none; conformance 1747/1747, corpus 3123 compared 0 differ, goldens 639/639.

**Surrogates — done, 2026-08-21, and the third "never taught" in a row.** A surrogated contract's
body already *was* raw: the emitted write converts (`var surrogate = (CodeSurrogate)value;`) and
then writes the surrogate's members with `WriteRawTag`/`WriteRawVarint64`. Only the measure was
missing, and only because `EmitMeasureMembers` hard-coded `value` as the instance while
`EmitWriteMembers` had always taken one. Both now emit the same conversion, so they walk identical
members.

**A surrogate carrying its own SERIALIZER stays excluded, and that one is real**: there are no
members to inline — the body is delegated to that serializer — so there is nothing to measure
arithmetically.

**The trap it sprang, twice, is the same one B39 hit**: on a surrogated contract `IsValueType`
describes the **surrogate**, so a member typed as a surrogated *struct* (`Money`, `Ticks`) got
`!= null` emitted against it (CS0019), and its repeated form got `is null` (CS0037). Every null
guard that asks "can this MEMBER be null?" must use `DeclaredIsValueType`; seven sites, and the two
in the repeated-element checks were missed on the first pass because I edited by line number after
the file had shifted. Textual replacement caught them.

Effect: `Surrogate` went from 6 `Measure_` occurrences to 16, `ModelSurrogate` 6 to 18.

#### Member-level (`RawMemberMeasureBlocked`) — what blocks a member, and so its whole contract

| what | gap? | tracked |
| --- | --- | --- |
| **maps** — a MESSAGE value (blocked on B43), a message key, a repeated value, or a non-default format | **yes** | **B6** |
| ~~maps — ENUM sides~~ | **DONE 2026-08-22** | **B6** |
| ~~maps — plain scalar/string sides at the default format~~ | **DONE 2026-08-21** | **B6** |
| ~~null-wrapping — lone, both collection scopes, message elements~~ | **DONE 2026-08-22** | below |
| ~~null-wrapping — maps~~ | **DONE 2026-08-22** (scalar/string values) | below |
| ~~non-default `DataFormat` on a unary scalar~~ | **DONE 2026-08-22** | **B26** |
| non-default `DataFormat` on a REPEATED member, unpacked | yes | **B26**, and **B30** for the ambient-default angle |
| a repeated member that is neither raw-writable, packed, BCL-measurable, nor a measurable message | yes | partly B26 |
| ~~a **nullable struct** message member~~ | **DONE 2026-08-21** — and the park reason was wrong | below |
| ~~a **value-type bytes** member~~ | **DONE 2026-08-21** | below |
| a message member whose target is not itself measurable | **this is the cascade, not a gap of its own** | — |

**Nullable-struct message members — done, and the recorded reason for parking them was not the real
one.** The note said the measure and write each take their own `GetValueOrDefault()` copy and that
the shape was "parked until a fixture proves it rather than reasoned safe" — but `Structs.input.cs`
already carried `Point? MaybeLocation` *with* a present-but-all-default sample, so the fixture
predated the park.

The actual blocker was structural and had nothing to do with copies: **such a member's write stays
stateful** (`state.WriteMessage<Point>(..., this)`), and `RawWrite_` is a **static**, so `this` was
not available — `CS0026`. That is the same wall behind several other exclusions, so it was worth
removing properly rather than working around: the services type now holds a
`private static readonly ... Self = new()` and the write emitter threads a `self` (defaulting to
`this`, so nothing else changes). `SerializerCache<TProvider>.InstanceField` would have been the
natural singleton and is `internal`, unreachable from generated code; a second instance is harmless,
the type being stateless bar a `[Conditional("DEBUG")]` constructor assert.

**And it sprang the trap AGENTS.md documents by name.** The measure emitter reaches scalar kinds from
three branches, and the *nullable* branch runs before the main switch — so the `case
ProtoMemberKind.Message when member.IsNullable` arm I first added was unreachable, and the nullable
branch's `RawScalarMeasure(...)!` turned null into an empty string and emitted `len += 1 + ;`. Third
time that has bitten; the fix belongs beside the BCL arm that was added for exactly the same reason.

Effect: `Structs` 3 → 7 `Measure_` occurrences, `Getter` 6 → 10, `TupleMembers` 12 → 16.

**Value-type bytes — done, 2026-08-21.** `Memory<byte>`, `ReadOnlyMemory<byte>` and
`ArraySegment<byte>` are written **unguarded** (they cannot be null), so the measure must not guard
either or the two disagree on a default instance. The only wrinkle is that `ArraySegment` counts with
`.Count` and the other two with `.Length` — sidestepped entirely, since all three convert implicitly
to `ReadOnlyMemory<byte>`, so one cast serves all of them and no type spelling has to be matched.
(`DeclaredTypeName` is no help: it is populated only for tuple read locals.) `Bytes.output.cs` went
from **zero** `Measure_` occurrences to two — that contract had no arithmetic measure at all.

**Null-wrapping — the LONE form is measurable, 2026-08-22; collections and maps are not.** The
write stays on `WriteAny` and always will — the extra message layer is not expressible as features
on an ordinary write — but the *size* is arithmetic, which is the same measure-vs-write independence
that B6 turned on for maps.

**The wire shape was PROBED, and three of the answers contradict what the ordinary write guards
would predict.** A lone wrapper's inner field follows `IValueChecker<T>.HasNonTrivialValue`, which is
*not* the member's own write guard:

| | bytes | inner field |
| --- | --- | --- |
| `int? 0` | `0A-00` | **omitted** |
| `int? 1` | `0A-02-08-01` | present |
| `string ""` | `0A-02-0A-00` | **present** — protobuf-net writes `""` for compat |
| `byte[] []` | same shape | **present**, same reason |
| `enum? Zero` | `0A-02-08-00` | **present** — `EnumSerializer` supplies no checker, so the default "non-null is non-trivial" applies |
| `int? 0`, `AsGroup` | `0B-0C` | omitted, and no length prefix — a start/end tag pair |

So reusing the member's `!= default` guard would have been right for the numeric kinds and wrong for
enums, in exactly the zero case; and reusing `!= null` would have been wrong for every numeric zero.
`WrappedTrivialTest` mirrors the checker instead, and throws for any kind not on the probed list —
the generator's usual "slipped eligibility" self-check rather than a silent short prefix.

`WrapMeasure.input.cs` exists because `Wrapped.input.cs` **cannot** cover this: it also carries
wrapped collections and maps, and one blocked member takes the whole contract. Its samples put every
wrapper present with every inner value trivial, which is the single case where all three candidate
rules disagree, and a field past 15 so a folded-tag mistake shows up as a two-byte tag.

A `string` needed its own arm: it has no `RawScalarMeasure` entry, and `MeasureRawString` already
includes the payload's own length prefix. Missing that emitted `long wrapN = 1 + ;` — the fourth
place the `!`-turns-null-into-nothing trap has bitten, and again the goldens caught it rather than
review.

Still blocked, and untracked beyond this line: **wrapped collections and wrapped maps**
(`[NullWrappedValue]` on a collection/map, and `[NullWrappedCollection]` in either scope). Those are
pure features composition on the write, so the size is derivable in principle — the element form
adds `OptionWrappedValueFieldPresence`, which means the inner field is written *even when trivial*,
i.e. **the opposite of the lone rule above** and a separate arm rather than a reuse of this one.

**...and the COLLECTION scopes, same day.** Both of them, plus the two composed, on the same probe-
first footing. The shapes, and the reason they are a separate arm rather than a reuse:

| | bytes |
| --- | --- |
| element-wrapped `[1,null,0]` | `0A-02-08-01` `0A-00` `0A-02-08-00` |
| element-wrapped `[""` ,`null`, `"a"]` | `0A-02-0A-00` `0A-00` `0A-03-0A-01-61` |
| element-wrapped group `[1,null,0]` | `0B-08-01-0C` `0B-0C` `0B-08-00-0C` |
| collection-wrapped `null` / `[]` / `[1,0]` | *nothing* / `0A-00` / `0A-04-08-01-08-00` |
| collection-wrapped group `[1,0]` | `0B-08-01-08-00-0C` |
| both, `[1,null]` | `0A-06-0A-02-08-01-0A-00` |

Three facts drive the emitter, and only the first was predictable:

- **an element wrapper always carries its inner field**, zero or not — that is
  `OptionWrappedValueFieldPresence`, and it is the **inverse** of the lone rule recorded above. So
  the collection arm has *no* trivial-value test at all, only a null one, and sharing code with the
  lone arm would have been actively wrong in both directions;
- **a collection wrapper renumbers its contents to field 1**, so the element tag is not the member's
  own — and the elements inside it are unconditional, zero included;
- **the two scopes compose by nesting**, which falls out for free: the element sum accumulates into
  a local and the collection wrapper then measures that local exactly as it would any body.

`null` versus `[]` is the whole point of collection wrapping and is the pair a single rule gets
wrong, so the fixture carries a sample with every collection present and empty, beside one with a
null element in every element-wrapped scope.

**Message elements went in on the same pass**, and cost less than the paragraph parking them
suggested: a wrapped element's payload is `1 + varint(sub) + sub` exactly as a scalar's is
`1 + payload`, so the only new thing is *where* `sub` comes from. It comes from the target's own
`Measure_` reached with a **null slot buffer** — the write leaves a wrapped element to the stateful
engine, which computes its own sub-lengths, so this sub-tree must reserve **nothing** or every later
length in the payload shifts. Same rule as a nullable-struct sub-message.

The sharp sample is an **empty** message element: `0A-02-0A-00`, a present inner field over a
zero-length body, where a *null* element is `0A-00`. A measure that treated "empty" and "absent"
alike would agree with ref-emit on every populated sample and differ only there.

Still out: **maps**, whose wrapping is two-sided — `OptionWrappedValueFieldPresence` rides on the
map while `OptionWrappedValue` rides on the value features, so the two scopes do not compose the way
they do for a collection, and it needs its own probe.

**Maps: probed but NOT built, 2026-08-22 — the evidence is here so the next attempt starts from it.**
Wrapping a map is a third arm rather than a variation, and the probe says so plainly:

| | bytes |
| --- | --- |
| `{1:2}` value-wrapped `int?` | `0A-06-08-01-12-02-08-02` |
| `{1:0}` value-wrapped `int?` | `0A-02-08-01` — **value side absent** |
| `{1:null}` value-wrapped `int?` | `0A-02-08-01` — **identical to the zero above** |
| `{1:""}` value-wrapped `string` | `0A-06-08-01-12-02-0A-00` — present |
| `{1:null}` value-wrapped `string` | `0A-02-08-01` |
| `{0:null}` | `0A-00` — both sides trivial, an empty entry |
| group value `{1:2}` | `0A-06-08-01-13-08-02-14` |
| collection-wrapped `null` / `{}` / `{1:2}` | *nothing* / `0A-00` / `0A-06-0A-04-08-01-10-02` |
| both, `{1:null}` | `0A-04-0A-02-08-01` |

**Wrapped maps — BUILT 2026-08-22, and the probe above is what made it cheap.** Both scopes, for
scalar and string values. The collection scope is a straight reuse of the collection rule (entries
renumbered to field 1, then length-prefixed, so null and empty differ); the value scope is the new
part, and it is *smaller* than the collection element form because the guard does not change: it
still asks `HasNonTrivialValue` of the **unwrapped** value, which is why a wrapped `int?` of 0 and
one of null are byte-identical. The measure mirrors that rather than wishing otherwise, and
`WrapMeasure.input.cs`'s `Ledger` pins it with a sample carrying both.

Excluded, matching the neighbouring decisions rather than by separate argument: an **enum** value
(written even when zero, which is the opposite of the guard the wrapper sits behind) and a
**message** value (out for maps generally until B43).

Two findings worth keeping:

- **a wrapped map value of zero and one of null are BYTE-IDENTICAL** (`0A-02-08-01` for both), which
  is the opposite of what wrapping is for. The entry side is still gated by
  `KeyValuePairSerializer.Write`'s `HasNonTrivialValue` on the *wrapped* value, and for `int?` that
  is `GetValueOrDefault() != 0` — so the wrapper never gets the chance to distinguish them. It is not
  a generator question at all; whether it is a protobuf-net bug is worth a separate look, and it is
  recorded here because a measure written from the docs would produce different bytes and look wrong;
- **collection-scope wrapping composes exactly as it does for a collection** — contents renumbered to
  field 1, then wrapped — so that half is a reuse. It is the *value* scope that is new.

What the build needs beyond the above is the **value scope** only. A first draft of this note claimed
a nullable value spelling (`Dictionary<int, int?>`) would also need work, because `MapSideBody` emits
`(ulong)(long)pair.Value`; that was **wrong**, and checking beat reasoning again. `(long)` on an
`int?` is a legal explicit conversion, and it is guarded by `MapSideGuard`'s `pair.Value != 0`, which
*lifts* and is `false` for null — so the cast never runs on one. `MapMeasure.input.cs` now carries a
`Dictionary<int, int?>` with a null, a zero and a real value, and agrees with ref-emit; the shape was
simply untested rather than broken.

#### Ranked, for "what next"

**As of 2026-08-22 there is one item on this list.** Everything else in the two tables above is
struck through, decided as a fallback, or blocked on a named prerequisite rather than on effort.

1. **inheritance** (B41) — the last real gap, and still the worst blast radius: a whole hierarchy
   leaves the measurable set together, and `[ProtoSubType]` made that reachable from outside the
   contract entirely. It is also the only one where the exclusion may be *necessary* rather than
   never-taught, which is the question to settle first — a sub-type marker is a length-prefixed
   sub-message, but **optionally** delimited, so "one slot per marker" is not a fixed shape, and the
   positional scheme's whole correctness argument is that reservations match `Next()` calls one for
   one, in order. **Needs a decision before code.**

Held behind something else, not behind effort:

- **map MESSAGE values** — built, measured, and backed out: measuring them turns B43's latent
  null-map-value disagreement into a wrong length prefix (corpus 0 → 13). Blocked on **B43**;
- **unpacked repeated members with a non-default `DataFormat`** — the unary half landed; the
  element wire type is still derived from the kind alone, which is the piece to fix (B26/B30).

Settled rather than done, and not to be re-argued as absences: a **non-measuring** custom or
surrogate serializer is a *fallback* (Marc, 2026-08-22) — it takes the classic write-to-count path,
which is correct, and the remedy is the consumer's; the **cascade** row was never a gap of its own;
and **contract-level `IsGroup`** turned out to be backwards in the same way B35 was.

**None of these is a correctness problem.** Every one of them falls back to the classic
write-to-count path, which is what protobuf-net did before v4 and is still correct — the cost is
throughput, and the reason to care is that the write path is where v4's gains are (B38: 1.3×–2.3×,
and wide-512 overtaking Google.Protobuf).


### B43. ~~A null map value diverges~~ — **SETTLED 2026-08-21: null is not REPRESENTABLE in a map; the generator was right all along**

Found by the adversarial samples written for B6's fixture, then diagnosed wrongly twice from the
harness, then settled in twenty lines by running the thing directly. `RuntimeTypeModel` alone, no
generator anywhere near it:

```
write(null value)      -> 0A-02-08-07
read back              -> Map[7] is "" (length 0)      <-- the RUNTIME reader yields ""
write(round-tripped)   -> 0A-04-08-07-12-00
*** NOT ROUND-TRIP STABLE ***

write(empty string)    -> 0A-04-08-07-12-00            <-- identical to the re-write above
```

**The conclusion, and it is not a bug to fix.** `null` and `""` *are* distinguishable on the wire —
an omitted value field versus a present zero-length one — but they **collapse on read**, because the
reader has to produce *something* and produces `""`. So a null map value is **not representable**:
protobuf-net writes it losslessly-looking and reads it back as an empty string, and no change to the
generator can alter that, because the behaviour is entirely in the runtime path.

**Which settles "v3 core or v4 new bits" the other way from my second answer: neither.** It is not a
defect at all, it is what the map encoding can express. And the generated reader's `string v1 = "";`
— the generator's only such seed, everything else using `default` — is **exactly correct**: it
matches what the runtime reader yields. My "fix" to `default` would have made the generated reader
the odd one out, and was backed out.

**Two wrong calls, both from the same mistake**, kept because the reasoning looked sound each time:
first "the generated model writes different bytes" (line 62 compares serializations and *passes*),
then "the generated reader seeds `""` where the runtime gives `null`" (line 71 is the runtime model
reading its *own* bytes, and it fails). Both came from inferring behaviour from **which assertion
fired**. The twenty-line reproduction settled in one run what two rounds of harness archaeology got
backwards. Reach for it sooner.

**Marc's nullability idea survives, but the question it answers is sharper now.** For
`Dictionary<int, string>` the runtime's `""` is already right and already matches. The interesting
case is `Dictionary<int, string?>`, where the consumer has *declared* null expressible and the wire
cannot represent it — so the options are to preserve null in the generated reader (a deliberate,
harness-visible divergence from the runtime model, for a shape the wire cannot round-trip anyway),
or to say so at build time. That is a real design question, and a better one than "which literal
goes in the initialiser". A scope-bound AOT-only configuration attribute remains the escape hatch.

**The B6 fixture excludes the null sample** with a comment pointing here — not because the behaviour
is wrong, but because a sample neither engine can round-trip pins nothing.


## C. Schema front-end (`[ProtoSchema]`)

The design and the findings are in `notes/aot-schema-model.md`; the gap list is here, so there is
one to consult. (This used to say "lands on the **`aot-schema-model`** branch" — that branch is long
gone and the feature is on `v4`. It is the exact error the note at the top of this file warns about,
left in place by the very stack collapse it describes.)

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

### C14. ~~protobuf **editions**, and refreshing `descriptor.proto`~~ — **LANDED (#1287) and GA in 3.3.21, 2026-08-17.** The deferral below is stale; read it as background

Marc, 2026-08-14. Editions replace the proto2/proto3 split with per-feature settings, and the one
that matters here is **`features.message_encoding = DELIMITED`** — which *resurrects group
encoding*, deprecated in proto3 and now back as a first-class choice. Marc's note: this is
substantially what he recommended to the protobuf team 15+ years ago, and it is what protobuf-net
has implemented throughout as `DataFormat.Group`.

**Status, 2026-08-19.** Editions **shipped**: `ff6a6cf4 Implement "editions" (#1287)` is on `main`
and released **GA as 3.3.21** on 2026-08-17. `DELIMITED` is handled through the schema front-end
(`Parsers`, both code generators, and a refreshed `descriptor.proto`), so the "what is missing is the
schema half" paragraph below and the `descriptor.proto` prerequisite are both **done**. Everything
after this block is background, kept because the mapping table and the packed-by-default polarity
warning are still the reference for anyone touching it.

**Caveat RESOLVED 2026-08-19 — see B35.** "Unusually well placed" was, for a few days, true of the
wire form and false of the write performance: a repeated grouped member disqualified its whole
contract from measure-first, so delimited measured 5.7× slower than length-prefixed. Fixed; delimited
now runs **4.5× faster** than length-prefixed, because a fully-grouped tree needs no measure pass at
all. The claim below is now true in both senses. The paragraph that follows is kept as the record of
what was wrong, since the shape of the mistake is the reusable part.

**The caveat as it stood.** "Unusually well placed" is true of the wire form and
currently *false* of the write performance. This is no longer a "fix it before editions ships"
question, because editions has shipped: a consumer can opt into `message_encoding = DELIMITED`
**today, on 3.3.21**, where it is the *faster* framing (14% at depth 512). On v4 the same consumer
gets 81,270 ns against length-prefixed's 14,258 — so choosing the spec-blessed option would cost them
**5.7×**, and leave them 4% slower than the release they upgraded from. That is a regression for
users of a **GA feature**, materialising at the v4 upgrade rather than at some future release.

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

See also **B14** — and note this paragraph **predicted B35 five days early**: groups currently
*defeat* measure-first rather than benefiting from it, which would make an editions-delimited payload
slower rather than faster until it is fixed. B14 was marked done the same day this was written; B35
now measures exactly the slowness predicted here. That corroboration is why "B14 regressed, or its
fix never covered this shape" is the leading hypothesis rather than one of three equals. That is the
ordering constraint between these two items.

## D. Decisions owed by a human

| question | detail |
| --- | --- |
| **manual review of the write-emitted goldens** | **65 changed vs `v4`**, plus **4 new fixtures** (`PackedAll`, `BclMeasure`, `RepeatedBytes`, `GroupedRepeated`) — all four now carry a `.reference.cs`, so ref-emit's own output is available beside every one. Start with `PackedAll.output.cs`: it is the only file showing all six `WriteRawPacked*` variants, three of which appeared in no golden before it existed. **The bodies moved four times since this review began** — B16's local folding, B16a's drift calls, B35's grouped-repeated arm, and the `GroupedElements` contract moving onto measure-first — so anything read before `f69d4baf` is stale. Each diff is uniform and skims quickly. **The one item still open, and the only thing owed by a human in this branch** |
| ~~do the two external PRs land before or after #1277?~~ | **Answered by events, 2026-08-17: both landed first.** #1276 and #1275 are merged into `v4` and forward-merged here. #1282 (build-time gRPC proxies) then landed too, and merged into `v4` on 2026-08-20 — see **B34**, where the measured overlap held. Nothing is now waiting on an external PR |
| ~~the tag ladder: keep or revert?~~ | **Settled 2026-08-14: narrowed, not reverted.** Split by *dynamic population* rather than kept or dropped wholesale — the one- and two-byte arms and the bool fold stay, the folded 3/4/5-byte arms go back to the shipped encoder. Two follow-on micro-ideas (`&` for `&&`, hoisting `RemainingInCurrent`) were answered by inspection and need no measurement; the reasoning is in the comment block |
| ~~when to rewrite `docs/aot.md`'s "needs its own project" advice~~ | **Done**, pre-emptively on `aot-schema-model`, so it arrives with the merge |
