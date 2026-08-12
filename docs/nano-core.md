# Nano-core: rewriting the reader/writer internals

The goal, stated bluntly: the existing `ProtoReader.State` / `ProtoWriter.State` internals are slow —
2–5× behind both Google.Protobuf and a 2022 prototype on every axis that was measured — and they do
not use the ref struct shape for what it is good at. This is a **systematic rewrite of the core**,
not a parallel library: "nano" is the new *implementation* of `ProtoReader.State` and
`ProtoWriter.State`, behind the same surface, so every consumer and every generated serializer
inherits it without change.

## Where the prior art lives

The `v4` branch (still on the remote, last touched 2023-02) contains the prototype, under the
working name "Nano". The parts that matter, all at `origin/v4`:

| path | what |
| --- | --- |
| `src/protobuf-net.Nano/Nano/PrepReader.cs`, `PrepWriter.cs` | the working POC reader/writer (the `Reader.cs`/`Writer.cs` beside them are aspirational stubs) |
| `src/protobuf-net.Nano/Nano/INanoSerializer.cs` | the three-method contract: `Read(ref reader) → T`, `Measure(in T) → long`, `Write(in T, ref writer)` |
| `src/protobuf-net.Nano/Nano/Internal/` | the memory strategies: `RefCountedMemory`, `RefCountedSlabAllocator`, `SimpleSlabAllocator` |
| `src/Benchmark/Nano/Results.md` | the headline numbers (below) |
| `src/Benchmark/Nano/DecodeResults.md`, `EncodeResults.md` | intrinsic-level varint studies with kept tables |
| `src/Benchmark/Nano/HandWritten*.cs` | the direct-code serializer shape, in three memory-strategy variants |
| `src/NanoTestRig/` | client/server harness proving it over real gRPC |

Headline numbers from `Results.md` (net6.0, outputs validated; GBP = Google.Protobuf, PBN = v3):
serialize ~3–4× faster than PBN and ~1.8–2× faster than GBP; deserialize ~2–3× faster than PBN;
**measure ~10–12× faster than either**; deserialize in pool mode allocates **101 B** where GBP
allocates 770 KB. PBN v3 loses to GBP on every row.

## The design principles to carry over

Each of these is visible in the v4 code, and most are the same discipline as SE.Redis's
`RespReader`/`RespWriter`:

1. **Hot state is registers.** The reader is a span/array plus `_index`/`_end` ints. Every hot
   method is bounds-check-plus-arithmetic; slow halves split into `*Slow` methods; throws in
   `NoInlining` helpers.
2. **Measure is pure arithmetic** (that is the 10×) and write then targets a single pre-sized
   region — no capacity checks in the hot path, one flush at the end.
3. **Direct simple code for the common case.** Serializers switch on raw uint tags as compile-time
   constants — `case (2 << 3) | (int)WireType.String:` — with plain `is { Length: > 0 }` write
   guards. There is a `TryReadTag(expected)` fast path for fields arriving in order.
4. **Repeated fields via function pointers**, not interface dispatch, consuming a *run* of same-tag
   fields in one call (`UnsafeAppendLengthPrefixed(list, &Item.Merge, tag)`).
5. **Varint intrinsics are measured, not assumed.** Decode via `tzcnt`-based branchless handling
   (~2× at length ≥4, slightly worse at 1–2 — the `PreferShort` hedge exists for small-value
   streams); measure via `lzcnt` is effectively free (0.23 ns); encode via zero-high-bits /
   shifted-masks ~1.5–2× at length ≥2. The tables are in the two results files; re-measure on
   current runtimes before locking choices.
6. **Memory strategy is a spectrum, not a decision**: plain allocation (best CPU), pooled leaf
   `bytes` with explicit `Dispose` (near-zero alloc), slab allocation between them. v4's conclusion
   was to offer both ends.
7. Supporting details worth keeping: `GC.AllocateUninitializedArray`, `CollectionsMarshal.AsSpan`
   over lists, `Unsafe.As` reinterprets, and the `USE_SPAN_BUFFER` A/B (span-field vs array-field
   buffer representation — benchmark both; v4 kept both compiled).

## Micro-benchmarks: "functionally correct" is step 0

Expect a *lot* of silly micro-benchmarks for minutiae, and treat that as the method, not overhead.
The differential suite gets a change through the door; the benchmark table is the actual review.
Every hot-path method — field headers, integer reads/writes, varint/zigzag, strings, length
prefixes — gets squeezed individually, because this is exactly where v4 found its wins and its
traps:

- **winners flip with the input distribution.** The tzcnt varint decode is ~2× at length ≥4 and a
  *regression* at length 1–2 (see `DecodeResults.md`) — which is why the `PreferShort` hedge exists,
  and why every varint benchmark is parameterised by byte-length *and* buffer offset. A single
  "varint benchmark" number is a lie; the table is the result.
- **the v4 convention is the right one**: per-concern benchmark files
  (`DecodeIntrinsicBenchmarks`, `EncodeIntrinsicBenchmarks`, `StringMaterialization`,
  `MemorySliceBenchmarks`, `ArrayAllocBenchmarks`, `ConstructionBenchmarks`) with the results
  **committed as markdown tables beside them**, so review sees numbers, not claims — the same
  derive-don't-guess rule the AOT work ran on, applied to nanoseconds.
- **2022 numbers are hypotheses, not facts.** Everything in the v4 tables predates net8/net10
  codegen changes and newer intrinsics; re-measure before locking any variant in, and note the
  hardware in the committed table (deltas on one machine, never absolute figures across machines —
  the same rule the AOT binary-size work used).

The micro-benchmarks resurrect under `src/Benchmark` (the project already exists and carried the
v4 Nano suites). The working rule for the hot path: **no change without its table** — a hot-path
PR that says "should be faster" without a before/after is not reviewable.

## What v4 never built — and the rewrite cannot dodge

The POC only ever handles a **single contiguous buffer**: `ReadStringSlow`, `ReadRawByteSlow` and
the multi-segment story are `NotImplementedException`, and `PrepWriter` grabs one 300 KB region and
hopes. The real `State` supports streams and `ReadOnlySequence<byte>`; the rewrite has to decide the
buffer model *first* (contiguous fast path with an explicit refill boundary is the obvious shape —
the same trick the current reader plays, but with the fast path actually fast).

One buffer-model decision is already made: on modern TFMs the reader holds the array root as a
**`ref byte` field** (C# 11 ref fields, .NET 7+) and the position is a byte offset applied with
`Unsafe.Add` — no `MemoryMarshal.GetArrayDataReference` per read, no bounds checks, no pinning
(ref fields are GC-tracked). netfx/netstandard2.0 falls back to `arr[index]` — bounds-checked and
slower, hidden behind a per-TFM accessor pair marked `AggressiveInlining`; the down-level path pays,
and .NET 10 is the optimization target. The micro-benchmark harnesses hoist the root ref once per
batch to approximate the ref-field shape, so the tables measure the modern layout.

Also unbuilt: slab strings (`ReadOnlyMemory<char>`) were sketched only, and the whole POC predates
`SearchValues`/net8 intrinsics — some of the 2022 measurements deserve re-running.

## What "same surface" means, and the escape valve

The shape being preserved is captured in `src/NanoState/` — `ReaderState.cs` and `WriterState.cs`
are **generated from the real types** (`generate-shape.ps1`, re-runnable) as throwing stubs: 82
methods + 8 properties on the reader, 72 + 9 on the writer, with the two members whose signatures
name Core-internal types kept as comments. That is the to-do list; implement by moving members out
of the generated files into hand-written partials, so what remains generated is what remains to do.

Not everything on that surface deserves a fast path. The 42-niche-scenarios machinery — extension
data, groups-as-framing, dynamic types, `IExtensible`, reference-tracking remnants — can sit on a
deliberately-boring implementation *behind the same members*, provided the boundary is explicit and
the hot scalar/message/repeated paths never pay for it. The AOT generator is what makes this viable
now in a way it was not in 2022: the generated serializers are the "direct simple code" consumer,
and the generator knows *at compile time* which contracts stay on the boring path.

**The existing surface is the floor, not the ceiling — the new surface is the point.** It is
absolutely expected and planned that the key optimisations arrive as *new* members, with the
existing API reimplemented over them for compatibility. The archetype is the field header:

- today, `ReadFieldHeader()` decodes the tag varint into a field *number* (returned) and a
  `WireType` (written to state) — shift, mask, two results, and every consumer that wants to
  dispatch reassembles what the wire had already joined;
- the new primitive returns **the raw units**: `ReadTag() → uint`, the tag varint as-is. Generated
  code switches on compile-time constants — `case (2 << 3) | (int)WireType.String:` — one read, no
  decomposition, no state writes, and the switch is a jump table over exactly what came off the
  wire; the old `ReadFieldHeader()`/`WireType` pair becomes a shift-and-mask veneer over it.

The same pattern recurs across the surface: raw-run append for repeated fields, measure primitives
that are pure statics, wire-type-carrying write methods that skip the features indirection. New
members live in hand-written `*.Nano.cs` partials beside the generated shape files, so the split
between "compatibility floor" and "new surface" stays visible in the file layout.

**The "Raw" convention: a dual API, split on who knows the encoding.** Legacy consumers (ref-emit,
existing compiled code) keep the stateful API: `ReadFieldHeader()` sets `FieldNumber`/`WireType`,
typed reads consult the wire type at runtime, and zigzag needs the `Hint` dance — all of which
exists because that API loses the schema knowledge between the header read and the typed read.
AOT-generated code uses the `Raw` family, whose contract is: **no header state, no hints, the
caller states the encoding, and the tag flows through parameters.**

- `ReadRawTag() → uint` (strict-5); typed reads with the encoding in the name —
  `ReadRawVarint32()` (tolerant of 10-byte sign-extended values), `ReadRawZigZag32()`,
  `ReadRawFixed32()`, and friends — selected by the generator at compile time from `DataFormat`,
  turning the legacy per-read wire-type branch into a compile-time decision. v4's `Reader` already
  validated encoding-in-the-name.
- **Nothing is stored, not even "just for errors"**: a tag store is a memory write per field in the
  hottest loop in the library, and the generated code already holds the tag in a local everywhere
  it could be needed — so `SkipTag(tag)`, `AppendExtensionData(tag, …)` and the throw helpers take
  it as a parameter (v4's shape exactly).
- **Run consumption needs no API at all — the caller-held tag makes it a loop condition.** A
  `TryReadRawTag(expected)` was sketched and then killed on Marc's observation that it re-imports
  state-holding thinking: on a miss it must either decode-and-discard (the main loop then decodes
  the same tag again) or stash the decoded tag somewhere (the store the convention exists to
  avoid). Instead the repeated case makes the tag read its own loop condition and hands a miss
  straight back to dispatch:

  ```csharp
  case (2 << 3) | 2:
      do { value.Names.Add(state.ReadRawString()); }
      while ((tag = state.ReadRawTag()) == ((2 << 3) | 2));
      continue;   // tag already populated — back to dispatch, skipping the bottom read
  ```

  (The `continue` is the structured spelling of "goto the line after the original header read,
  value already populated".) Every tag decodes exactly once, `ReadRawTag` stays single-shape, and
  scope termination is free: a 0 or a group sentinel fails the run compare and falls to dispatch,
  which already handles both. The same argument disposes of the fields-in-order speculation API —
  with the tag in a local, speculation is a compare against the next field's constant plus a
  `goto case`, no `Try` member needed. Decided: no `TryReadRawTag`; the legacy
  `TryReadFieldHeader(field)` is a veneer (see the forward-only rule below for its shape).
- **The reader is forward-only: rewind is illegal, everywhere, permanently.** Nothing can
  un-consume source bytes — a `Stream` cannot be rewound by definition, and a sequence walk may
  have discarded (or returned the lease on) the very segment a saved offset pointed into, with a
  `ReadOnlySequence<byte>` of single-byte segments as the pathological case. So "save position,
  decode, restore on miss" is legal **only when the decode provably cannot leave the current
  segment**, and every speculative read must switch on that up front: with 5+ local bytes (a
  tag's maximum width) no refill can occur and restore is fine; nearer the segment tail the
  decode must run *forward* — crossing refills exactly as ordinary reads do — and a miss hands
  the already-decoded result onward instead of pushing bytes back. The raw path obeys this
  natively (the missed tag rides a caller local into dispatch); the `TryReadFieldHeader` veneer
  mirrors it one level down with a single `_pendingTag` slot beside `_fieldNumber`/`_wireType`,
  drained by the next header read — veneer-owned state for a veneer-only need, never touched by
  the raw path. **And once a hand-forward slot exists, restore is not merely unnecessary but
  strictly worse** (Marc's follow-up): the drain check is already paid unconditionally, a miss
  stores one field either way (`_pendingTag` vs `_offset`), and only the restore variant parses
  the same bytes twice — so the veneer is forward-only *unconditionally*, and the provably-local
  guard survives only as the rule for any future speculative read that lacks somewhere to hand
  its result. This rule also constrains the refill design itself: `GetNextBuffer` owes its
  callers nothing about bytes before the current offset, which is what keeps a Stream refill a
  simple shift-and-top-up.
- **One exception, forced by an immovable signature: termination scope is a state slot.** The
  end-group tag cannot be a parameter, because `ISerializer<T>.Read(ref State, T value)` cannot
  change — and since the slot must exist for that path, it is the *only* mechanism (direct calls
  use it too: one approach, smaller frames). The slot is **one sign-discriminated long**, exactly
  as legacy `SubItemToken` always was: positive/zero = absolute end position (length mode),
  negative = `-(long)fieldNumber` for a group — the sentinel's wire type is always 4, so the
  29-bit field number is its whole identity. It stays off the hot path: the length check is a
  derived segment-clamped int compare inside `ReadRawTag`, and the group check is the switch's
  `default:` case (`(tag & 7) == 4 && (long)(tag >> 3) == -scope`, the wiretype test doubling as
  the mismatched-end-group throw gate). **Every dive pushes scope, either kind** — a group dive
  sets the sentinel, a length dive clears it (a stale outer sentinel inside a length-bounded
  sub-message could false-match) — with `PushLimit`/`PushGroup` returning the prior `ReadScope`
  (one long) into a generated-code local for `PopScope`; state holds only the innermost. Known
  trade, matching legacy exactly: a group scope replaces the positional bound, so malformed input
  missing an end-group overruns the parent limit until mismatch/EOF — unobservable on valid input,
  and the match makes the `StartSubItem`/`EndSubItem` veneer mechanical.
- The legacy API becomes a veneer: `ReadFieldHeader()` is `ReadRawTag()` plus the shift/mask and
  state writes — one implementation core, two surfaces, the stateful one paying for its own state.
- **The legacy header state stays as two separate ints** (`_fieldNumber`, `_wireType`), written
  only by the veneers and left stale by the raw path (the two APIs do not interleave within one
  consumer). They cannot be re-packed into a raw-tag field: `Hint` stretches the wire type past
  3 bits — `SignedVarint = Varint | (1 << 3)` = 8, a literal fourth bit, upgraded in place when
  the low 3 bits match (verified in `ProtoReader.Hint`) — and `WireType.None = -1` needs sign on
  top. Cold fields for raw consumers; the cost lands where the statefulness lives.
- **Wire-type tolerance is preserved by case labels, not by state.** The legacy reader accepts an
  int32 arriving as Fixed32/Fixed64 (and protobuf-net itself writes those under
  `DataFormat.FixedSize`), so a naive single-label raw-tag switch would silently demote known
  fields to unknown-field handling when a writer's format evolves. The generator instead emits
  multiple case labels per field — `case (5 << 3) | Varint:` *and* `case (5 << 3) | Fixed32:` —
  each dispatching to the correctly-named raw read: the jump table absorbs the labels for free, and
  strictness stays expressible per contract. Note the differential suite is currently blind to this
  divergence (fixtures read and write the same format), so a deliberate cross-format test lands
  with the change. **Message fields get the same treatment for framing** — length prefix or group,
  accepted without prejudice as legacy always has, as a case-label pair over one `PushScope(tag)`
  body (the framing is in the tag; an end-group sentinel is the start tag plus one, so the group
  arm costs nothing to derive). In a repeated run the loop compares against a `last` local rather
  than a constant; a payload alternating framings mid-run just exits the run compare and re-enters
  the sibling label. Two categories must not be confused with tolerance, because they are **spec,
  not lenience**: packed↔unpacked for repeated primitives (both Google.Protobuf and protobuf-net
  deliberately write whichever encoding is optimal, regardless of the declared option), and the
  message framing pair above (`DataFormat.Group` is a legitimate writer choice). Any future
  strict-mode knob (a model attribute accepting only the natural wire type per field — build it
  only if measurement shows tolerance labels cost on realistic sparse switches) governs scalar
  wire interchange ONLY; the spec pairs stay unconditional.

**The read signature is value-in, value-out** — `static T NanoRead_X(ref ReaderState, T value)`,
with `??=` construction inside and merge by mutation. The alternatives were weighed: v4's
`void Merge(ref T? value, …)` saves the return shuffle, but a `ref` argument binds only to
fields, locals and array/span elements — never to properties or fresh collection slots — and the
generated targets are overwhelmingly properties, so the general path would spill through a local
and assign back regardless. The return form also matches `ISerializer<T>.Read`, so one emitted
body serves both the direct-call path and the interface/veneer bridge. A `ref` overload remains a
*targeted* future specialization — struct contracts (return = copy), the `[UnsafeAccessor]` field
path (which already yields a `ref`), and in-place merge over `CollectionsMarshal.AsSpan` — gated
by the tiebreaker rule: benchmark it only if a decision actually hinges on it.

**Merge semantics for `string`/`bytes`, confirmed empirically** (Google.Protobuf 3.34.1 vs
protobuf-net 3.3.8, duplicated-field-in-payload and merge-across-parses both): `string` is
last-one-wins **replace** in both implementations; `bytes` **replaces in Google but APPENDS in
protobuf-net** — `AppendBytes` is literal, and it is a real cross-implementation divergence, not a
doc artifact. **Decided: the nano/generated path defaults to replace** — the simpler logic, and
Google-aligned — with `[ProtoModel(LegacyAppendBytes = true)]` as the opt-in for the historical
behaviour (the `AllowParseableTypes` pattern). Consequences: the differential runs bytes parity
*with the flag set* (a duplicated bytes field is a known divergence class under the default, since
the differential manufactures repeated fields by concatenation), and the replace default gets
directed tests of its own; `OverwriteList` on a bytes member becomes a no-op under the default and
stays the per-member replace escape under the flag; the *veneer* surface is unaffected either way
— `AppendBytes` keeps its legacy name and semantics for old API callers, the flag governs what the
generator emits.

**Extension data: capture, not skip, with two byte-fidelity rules.** An extensible contract's
unknown fields are captured into the instance's `IExtension` bag in wire format, so the write
side can blit them back out. The TAG is re-encoded canonically — its original bytes are behind
the offset, unreachable under forward-only, and the caller's parameter supplies the value: the
raw convention solving its own constraint (legacy re-encodes headers through ProtoWriter too, so
this is parity, not divergence). The PAYLOAD is teed byte-preserving — original varint encodings
kept, overlong or not — resident block-writes where possible, byte-wise across refills
otherwise; group-framed unknowns capture recursively with re-encoded markers, depth-guarded
unconditionally. The emit side mirrors legacy's resolved `ProtoExtensibleKind` (untyped/typed
per the `UseTypedExtensible` table), with the capture in the switch `default:` after the
sentinel test. The gate uses LEGACY as the referee, twice over: an empty-extensible narrowed
descriptor contract (everything unknown, so each file's whole body lands in the bag in original
order), read back via legacy's `Extensible.GetValue` (cross-stack bag compatibility) and
re-serialized via legacy's writer, which must reproduce the original payload BYTE-IDENTICAL —
including through a 1-byte-chunk stream, so the straddle tee faces the same referee.

**Safeguard parity, and the elision lever.** The reader carries legacy's safeguards: field-0
rejection (folded into the single-byte tag range check — `8..127` is one compare, the same cost as
the bare MSB test), max depth (the `TypeModel.MaxDepth` cap, default 512 — nano's direct child
calls recurse per wire nesting level, so the cap is also the stack-overflow defence),
exact-consumption validation on length-scope pop, and truncated-group detection on the end path.
Reference-tracking recursion detection is deliberately *not* reproduced: the depth cap is the fair
trade, decided. The measured cost lands on the dive-heavy paths (per-record push/pop), which opens
a lever unique to the closed world: **the generator can prove whether a model can recurse at
all** — if a dive's child contract reaches no type cycle, nesting is bounded by a compile-time
constant and the depth check is provably dead, so the generator can select an unchecked push for
that site, retaining checks only for genuinely self-repeating trees. Two caveats pin the boundary:
the *skip* path keeps its guard unconditionally (unknown fields can nest groups arbitrarily
regardless of the model - the wire decides, not the schema), and the open-world paths (veneers,
`ISerializer<T>` dispatch) always check, because they cannot see the caller's model. This is not
unfairness to legacy, which checks because it cannot know: eliding provably-dead checks is the
compile-time-knowledge thesis itself. Trigger: measured need after full-job runs, per the
tiebreaker rule.

**Generated code calls generated code directly — `this`-as-`ISerializer<T>` becomes the exception.**
Today the generated model passes itself as the serializer into
`state.WriteMessage<T>(field, value, this)`: interface dispatch on a generic method for a callee the
generator knew *at compile time*, which defeats inlining, drags the `GetSerializer` fallback
machinery into the call graph, and (under AOT) keeps interface implementations alive that a direct
call would not. The v4 hand-written shape is the target: sub-messages as direct static calls —
`writer.WriteVarint(Item.Measure(in item)); Item.WriteSingle(in item, ref writer);` — with the
framing done inline by the caller, `in`/`ref` parameter discipline throughout, and function pointers
where a callback shape is genuinely needed (repeated runs). The interface implementations remain,
but they retreat to the boundaries: model-level resolution, proxies, surrogate hand-offs, and the
niche fence — the places where the callee genuinely is not known until runtime.

## The buffer model: designed before built, as step 2 always demanded

Everything is normalized to "a `byte[]` window, maybe leased"; the three sources differ only in
how the next window arrives. The constraints were all written before this design — forward-only
(nothing un-consumes), residency (bulk arms need local bytes), and the refill contract
(`GetNextBuffer` owes callers nothing before the current offset) — and they compose into the
load-bearing simplification: **a refill never preserves or carries anything.** Every straddle is
handled by byte-wise consumption into locals or scratch (the per-byte slow paths already cross
refills naturally; the bulk arms are gated on residency and never straddle), so there is no
partial-primitive state to hand across a boundary, ever.

- **Array / `ReadOnlyMemory`**: the degenerate single segment; today's whole implementation.
- **`Stream`**: one leased buffer for the reader's lifetime. Refill = shift the unconsumed tail
  `[_offset, _count)` to the front, top up from the stream (looping reads to fill — maximizing
  residency is the refill's "legal courtesy", since resident is the common case worth designing
  for), bump `_positionBase` by the bytes shifted out. `lengthHint` seeds `_remaining` when the
  caller knows (a length-prefixed network frame). **The MemoryStream unwrap is product parity,
  not an optimization we invented**: legacy (`ProtoReader.Stream.cs`) special-cases exact-type
  `MemoryStream` + `CanSeek`, reaching the private buffer by reflection when `TryGetBuffer`
  declines, and collapses to the span path - nano's Stream constructor must do the same unwrap
  into the array case, and **benchmarks must deliberately defeat it** (a non-MemoryStream
  chunk-feeding wrapper) or the "stream" rows measure the span path on both stacks.
- **`ReadOnlySequence`**: walk the segment list; `TryGetArray` uses the segment in place (the
  fast-path window is per-segment, so the guarded-tail rule already protects against overread),
  else lease-and-copy (returning any prior lease first). `_remaining` tracks the known tail.
- **`_effectiveEnd` recomputation** is part of every refill: `min(_count, scopeEnd -
  _positionBase)` in length mode, `_count` otherwise — `PopScope` already computes exactly this,
  so the multi-segment form is the code that exists.
- **The plausible-length guard splits into three**, because "length exceeds available" stops
  proving corruption once more data can arrive: resident → bulk arm (common case); non-resident
  but within a known `_remaining` → verifiable, assemble byte-wise; unknown remaining (bare
  stream) → **allocation is bounded by bytes actually read, never by the claimed length** - the
  scratch for a straddling string/bytes grows as real data arrives (pooled, doubling), so a
  hostile length prefix costs at most the real payload, the eager-allocation problem dissolved
  rather than capped.
- **Snapshot/resume** (`ReaderSnapshot`): plain fields, the ref recovered as an index; the
  unconsumed tail `[_offset, _count)` is copied into the snapshot (typically small at await
  points), because a leased buffer cannot outlive the reader and streams cannot re-seek. Design
  note; built when the async story lands.
- **Group skip rides along** (`SkipTag` wire-type 3): read-and-skip until the matching sentinel,
  nesting via its own counter, depth-guarded unconditionally — unknown fields nest arbitrarily
  regardless of the model; the wire decides, not the schema.

Correctness gates for this brick, cheap and brutal: the descriptor payload parsed as a
two-segment sequence **split at every byte offset** (7,670 splits × ~8µs — every straddle case
for every wire construct, gated on the census); the all-single-byte-segments sequence (the
pathological case); and a 1-byte-chunk feeding stream. Benchmarks: nano-Stream vs legacy's real
Stream backend through the unwrap-defeating wrapper, chunk size as an axis, with the Memory rows
as the control for what refill machinery costs when it never fires.

## The north star: descriptor.proto, "the good way"

The working goal that sequences everything below: enough reader + emission to handle the
**FileDescriptorSet object model**, measured on real data (descriptor.proto's own descriptor —
the payload describes itself), three-way against legacy *and* Google.Protobuf, which parses
descriptors on home turf. It exercises, in landing order: strings (everywhere), repeated messages
and repeated strings (the tree is lists all the way down), enums, bytes (UninterpretedOption),
deep nesting with a **genuinely recursive** model (DescriptorProto contains itself - the depth
checks earn their keep and the elision analysis meets a case it must NOT elide), and getter-only
collection properties. Extension retention stays out of scope for the milestone: the payload
contains no unknown fields for its own schema, so SkipTag-as-default is honest. The milestone
gate: benchmarks reviewed, and the emitted code read top-to-bottom by a human.

Decisions from the first human read of the document-scale emitted shape (2026-08-12):

- **`??=` construction at method entry is the sealed-type shape only.** A `[ProtoInclude]`
  hierarchy must DEFER construction — the sub-type marker decides the concrete type, so the emit
  shape for a hierarchy root is: no entry-point construction, marker cases construct the derived
  type in their dive, member cases materialize the current layer on first touch. The corner is
  root-members-before-marker: legacy handles it via `SubTypeState` convert-and-merge (`Cast`,
  with its recorded incompatible-siblings hazard); refusing instead would reject payloads legacy
  accepts. Well-formed protobuf-net output always writes markers first, so the corner only opens
  on reordered/concatenated input. Design note for the inheritance brick, recorded here so it is
  not rediscovered.
- **Every emitted case label carries a comment** — `// options, field 7, group` — unconditionally:
  comments have no runtime existence in any configuration, and gating them on Debug would split
  the incremental cache and the golden files per configuration for zero benefit.
- **Benchmark DTOs use auto-properties, never public fields.** The generator's real targets are
  properties; a field-assigning benchmark measures a capability the emitted code will not have.
- Parked, protogen-scoped (the `.proto`→C# generator, NOT this work): its generated DTOs could
  bit-pack presence — and `bool` values — hasBits-style, as Google's generated C# does; a run of
  twenty `bool?` properties is ~40 bytes of nullable machinery for 40 bits of information, and
  the property surface (and so any serializer emitting against it) is unchanged.

## Step plan

1. **(done)** Shape clone in `src/NanoState/` — the surface, as compiling stubs.
2. **Buffer model decision** — the one question v4 dodged. Contiguous fast path + refill boundary;
   sequence/stream input feeds the refill. Write it down before writing code.
3. **Scalar hot paths** — varint read/write/measure with the intrinsic variants from the v4 tables,
   re-measured on net8/net10; fixed32/64; string materialization (see `StringMaterialization.cs`).
4. **The new-surface API set** — the raw-tag loop (`ReadRawTag` plus the tag-local run-consumption
   shape; no `Try` member, see the Raw convention), static measure primitives; the generator emits
   against these, and each existing member that they
   subsume becomes a veneer. This list is additive API, so it also lands in `PublicAPI.Unshipped`
   when it reaches Core — the API tracking makes the new surface reviewable as such.
5. **The niche fence** — enumerate which `State` members are hot-path and which sit on the boring
   implementation; this list is the real design review. **The fallback mechanism is decided
   (Marc, at the extension-data scoping): niche scenarios fall back to the utility methods
   against the generated model's `Instance`**, at CONTRACT granularity — which the nano
   eligibility fixpoint already implements on the read side: an ineligible contract keeps its
   legacy-emitted body, an eligible one gets the nano body, both on the same services type, and
   `Instance` is the join point (the same accessor the migration fixer leans on, doing double
   duty). This composes safely because nano readers and legacy veneers share one `ReaderState` -
   same scope slot, same position - so crossing between them AT MESSAGE BOUNDARIES (via
   `ISerializer<T>` dispatch) keeps the wire coherent; what is not safe is mixing the two APIs
   within one field loop (pending-tag and header state go stale across raw reads). Per-member
   interleaving is therefore a later refinement for contracts where one niche member drags an
   otherwise-hot contract onto the slow path - and even then at message boundaries, never
   intra-loop.
6. **Swap-in** — the new implementation becomes the internals of the real `State` types. The
   differential suite (`src/AotDifferential`) is the correctness gate: byte-for-byte agreement over
   ~3,000 contracts, both directions. Resurrect the Nano benchmarks as the performance gate.

## The move into Core, and how the emit gate flips

Nano never ships as a separate assembly: the spike (`src/NanoState`) is scaffolding, and the
destination is inside protobuf-net.Core — which is also what dissolves the internal-access edges
the spike keeps hitting (`SubItemToken`, `ProtoReader.SolidState`). The move is deliberately *not*
yet: the spike's iteration speed (no Core rebuild, no PublicAPI churn, no `[Experimental]`
ceremony per member) is worth keeping while the reader's shape is moving. It lands in one hop when
the shape stabilises: types into Core under `[Experimental]`, the new surface into
`PublicAPI.Unshipped` (reviewable as API), the veneers gaining the real
`StartSubItem`/`EndSubItem`/`SubItemToken` integration — and the emit gate flips, because
"symbol visible" stops meaning opt-in the moment everyone can see the symbol:

- **opt-in becomes explicit**: a `[ProtoModel]` flag (the `AllowParseableTypes` pattern —
  per-model, plan-equatable, already under the `PBN9001` experimental umbrella);
- **the symbol probe stays as the safety check**, not the trigger: new BuildTools against an older
  Core has no nano types, and emitting calls to absent types is a build break in code the consumer
  never wrote. Emit iff opted-in AND symbol present; opted-in without the symbol gets a clean
  "needs protobuf-net.Core ≥ X" diagnostic — the same probe-the-reference discipline as
  `UnsafeAccessorAttribute` and `ReadDateOnly`.
- **classic emission stays available as an opt-out, disabled by default** (Marc's call, at the
  swap; confirmed as a committed post-green work item during gate 3): a model flag forcing the
  stateful-API-shaped codegen even for nano-eligible contracts. The intellisense wording is part
  of the design (paraphrase to polish, keep the intent): *"Only for use if you experience
  problems with the default optimized emit; if enabling this fixes a symptom, that symptom is a
  bug in the optimized path - please report it as an issue."* Self-triaging by construction:
  every use of the flag becomes a field report rather than a silent divergence.
  Near-free, because the `Instance` fallback keeps the classic pass maintained regardless — and
  post-swap it is NOT "the old reader back": both shapes run over the nano internals (classic =
  the veneer path, the measured `NanoViaLegacyApi` configuration). Its value is bisection — flip
  the flag on a field report and emission bugs separate from core bugs — plus one documented
  consequence: classic reads bytes through `AppendBytes`, so the flag pins legacy append
  semantics regardless of `LegacyAppendBytes`, which is likely what anyone reaching for it
  wants. Deletion horizon, so it stays an escape hatch rather than a second dialect: when nano
  eligibility reaches parity and a release cycle passes without a report needing it.

## The nano-swap sub-branch: replacing State's internals (in flight)

Step 1 landed: `ReaderState` lives in Core (internal + IVT for spike speed; public +
PublicAPI.Unshipped + `[Experimental]` at the real merge), the generator's symbol gate learned
accessibility, and the whole gate battery runs against the Core-hosted reader. Step 2 is the
port of `ProtoReader.State.ReadMethods.cs` (~80 members) onto the nano core, with these shapes
settled by survey rather than discovery:

- **`GetReader()` has two call sites in all of Core** — serializers reach the model through
  State properties, so the port perimeter is ReadMethods.cs itself.
- **State's new storage**: the nano `ReaderState` fields plus `TypeModel`/user-state and a
  lazily-allocated reference-tracking cache (class-typed, so struct copies share it; State
  travels by `ref`, so mutations flow).
- **The obsolete class API bridges through `ReaderSnapshot`** — the solid form nano always
  planned. The class holds a snapshot (plain fields, including the leased buffer, which the
  bridge may hold directly since ownership stays in-process); each instance-API call liquifies,
  operates, re-solidifies (~40 mechanical rewrites, since `DefaultState().X()` temporaries stop
  carrying state). Museum API, museum prices — and the old class backends
  (`StreamProtoReader`, `ReadOnlySequenceProtoReader`) become fully deletable, which is the
  point of the exercise.
- **Bytes members keep legacy append semantics on this surface** (`AppendBytes` is the veneer;
  the replace default is the generated path's, selected by the emitted code, not by State).
- Gates in order, per Marc: does it compile; the byte corpus (`AotDifferential`); the entire
  compat suite.

## Landing strategy: side-by-side, with the v4 lesson as the guardrail

Incremental (build the nano reader alongside the incumbent, migrate logic, swap at the end) beats
big-bang (gut the existing storage and brute-force every member at once) — decided, not just
preferred, because side-by-side is the only option where the gates work: equivalence needs both
implementations alive (the differential can run the generator against the nano reader behind an
emit flag while ref-emit stays on the incumbent — wire-level A/B over the whole corpus, per
commit), and performance needs an incumbent to beat. A big-bang's real cost is diagnosis: every
intermediate state is broken, so a wrong byte and a slow path look identical mid-flight.

Side-by-side has a known failure mode, and it is sitting on the `v4` branch: the parallel
implementation that never merges. The guardrails:

- **the swap gate is defined by capability, now**: (i) the differential runs the whole corpus
  through the nano path, byte-identical in both directions; (ii) the hot-path benchmark tables
  exist. Not "when it feels done";
- **the incumbent is frozen** for the duration — no feature work lands on the old reader, so the
  target does not move;
- the swap itself is mechanical, because the veneer design *is* the swap plan: raw surface plus
  veneers reproduce the old API, so the final step is replacing `State`'s storage and re-pointing
  the legacy members — with the shape clone as the checklist.

Rules of the road, inherited from the AOT work: derive rather than guess (the shape files are
generated; the perf tables are measured), and nothing merges on "should be faster" — the
differential decides correctness, and correctness is only the entry ticket: BenchmarkDotNet tables,
committed beside the benchmarks, decide the rest.
