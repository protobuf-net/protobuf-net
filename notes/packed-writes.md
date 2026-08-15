# Packed repeated writes: the scenario matrix

**The question this answers** (Marc, 2026-08-14): *what packed scenarios exist, and have we
considered how to optimise each — where it isn't a simple block copy?*

The short version is that the matrix has a clear shape, and the **cheapest cell is the one that is
missing**. See the finding below before reading the SIMD entries in `notes/gaps.md` (B19–B21):
they are all downstream of it.

---

## The finding: there was no block copy at all — **now fixed for the matching fixed-width cells**

`RepeatedSerializer.WritePacked` writes **every** packed element through an enumerator and a
virtual serializer call, whatever the type:

``` c#
while (values.MoveNext())
{
    var value = values.Current;
    state.WireType = wireType;          // tell the serializer what we want to do
    serializer.Write(ref state, value);
}
```

There is no `MemoryMarshal`, no `AsBytes`, no bulk path anywhere in the file. So a packed
`float[]` → `repeated float` — which on a little-endian machine is a **pure `memcpy`** — is
currently emitted one float at a time through an interface dispatch.

That reordered everything: block copy is trivially correct, portable, needs no intrinsics, and
covers the fixed-width cells outright — so it landed first, ahead of any vectorised work.

**Landed 2026-08-14** (`VectorSerializer<T>.TryWritePackedBlock` + `State.WriteRawBytesBody`): the
matching fixed-width cells now emit the array's bytes in one copy. Guarded on
`BitConverter.IsLittleEndian`, which the JIT folds away on LE; big-endian falls back to the
per-element loop, which goes through `BinaryPrimitives` and is already correct.

**It needed its own oracle**, and that is the part worth remembering. The differential compares the
generated model against `RuntimeTypeModel` — but **both go through `RepeatedSerializer`**, so a
wrong block copy would be wrong identically on both sides and pass. A round trip would agree with
itself for the same reason. `PackedBlockCopyTests` therefore pins **hand-computed wire bytes**
(`1f` is `00-00-80-3F` little-endian, `3F-80-00-00` big-endian — different in every position that
matters), plus an 8192-element payload that crosses buffer boundaries so the out-of-line arm runs
too.

Two behaviours were pinned incidentally, both found by a *wrong expectation* rather than by
design:

- **a single element is written UNPACKED** — `(count == 0 || count > 1)` in
  `RepeatedSerializer.Write`, since tag+value beats tag+length+value, and packing is the writer's
  choice so both are legal. The test's first draft expected `0A-…` and got `09-…`, i.e. wire type 1;
- **an empty packed collection writes a zero-length header** (`0A-00`), not nothing. That is the
  exact shape gap B1 was filed against as a disagreement — and since both paths are this one piece
  of code, there was never a disagreement available to find.

---

## The matrix

Three dimensions: the **wire encoding**, whether the **CLR width matches** it, and the
**collection shape**. Sizing and writing behave differently, so both are shown.

### Fixed-width, CLR width matches the wire

| CLR | wire | sizing | writing | status |
| --- | --- | --- | --- | --- |
| `float[]` | `float` (I32) | `count * 4`, O(1) | **block copy** | ✅ done |
| `double[]` | `double` (I64) | `count * 8`, O(1) | **block copy** | ✅ done |
| `int[]` | `sfixed32` | `count * 4` | **block copy** | ✅ done |
| `uint[]` | `fixed32` | `count * 4` | **block copy** | ✅ done |
| `long[]` | `sfixed64` | `count * 8` | **block copy** | ✅ done |
| `ulong[]` | `fixed64` | `count * 8` | **block copy** | ✅ done |

**Both halves are now optimal.** Sizing always was — `WritePacked` special-cases
`Fixed32`/`Fixed64` to `count * 4` / `count * 8` without touching the elements — and the write is
now one copy, behind the `IsLittleEndian` guard described above. **`List<T>` of these types is not
covered**: it needs `CollectionsMarshal.AsSpan` (net5+), and protogen emits arrays for packable
scalars anyway, so the schema-first surface is complete.

### Fixed-width, CLR width does NOT match

| CLR | wire | writing today | optimal | gap |
| --- | --- | --- | --- | --- |
| `double[]` | `float` (I32) | per element, `(float)value` | `Vector.Narrow` | **gaps.md B20** |
| `float[]` | `double` (I64) | per element | `Vector.Widen` | B20 |
| `long[]` | `sfixed32` | per element | `Vector.Narrow` | B20 |

Real, and confirmed: `ProtoWriter.State.WriteMethods.cs` documents `WriteDouble` as *"supported
wire-types: Fixed32, Fixed64"*. Sizing is still O(1); only the conversion is per element.

### Varint and zigzag — never a block copy

| CLR | wire | sizing | writing | gap |
| --- | --- | --- | --- | --- |
| `int[]` | `int32` | ladder, **negatives → 10 bytes** | LEB128 per element | B19 sizing, B21 write |
| `uint[]` | `uint32` | ladder, 1–5 | LEB128 per element | B19, B21 |
| `long[]`/`ulong[]` | `int64`/`uint64` | ladder, 1–10 (`long` **puns to `ulong`**) | LEB128 | B19, B21 |
| `int[]`/`long[]` | `sint32`/`sint64` | zigzag ladder (**no shifts needed**) | zigzag + LEB128 | B19, B21 |
| enum`[]` | varint | *never packed at all* — `EnumSerializer` is not `IMeasuringSerializer` | — | see gaps.md B1 |

B19 measured the sizing at **1.8×–6.6×** vectorised. B21 tiers the write: homogeneous-block
narrow-and-store (portable, covers the single-byte majority), measure-then-write-unchecked (free
with B19), and full shuffle-compaction LEB128 (modern TFMs only).

### `bool[]` — the overlooked cell

| CLR | wire | sizing | writing | optimal |
| --- | --- | --- | --- | --- |
| `bool[]` | `bool` (varint) | **always `count`** — every value is one byte | per element | **near block copy** |

Worth calling out separately because it looks like a varint and behaves like a fixed width. Sizing
is O(1) and could skip the ladder entirely; the write is `MemoryMarshal.AsBytes` **plus a
normalisation**, since .NET only guarantees `false == 0` — a non-zero `bool` byte is not guaranteed
to be `1`, and protobuf requires exactly `0x01`. So it is a vectorised
`Vector.GreaterThan(v, Zero) & 1`, not a raw blit.

### Collection shape (orthogonal to all of the above)

| shape | span available? | notes |
| --- | --- | --- |
| `T[]` | **yes, natively, every TFM** | and this is what protogen emits for every packable scalar |
| `List<T>` | `CollectionsMarshal.AsSpan`, **net5+** | down-level keeps the per-element loop |
| anything else | no | per-element loop, unavoidable |

The alignment worth knowing: **protogen emits `T[]` for packable scalars** and getter-only
`List<T>` only for `string`/`bytes`/message — which are never packed. So the array path covers the
entire schema-first packed surface, on every TFM, with no `CollectionsMarshal` dependency.

---

## Measured: the matrix payload, and what has landed against it

`src/NanoBench/PackedMatrix.cs` + `PackedMatrixBenchmarks.cs` — Marc's challenge, and the payload
every SIMD item was blocked on. One contract per encoding category, each carrying the same shape as
both `T[]` and `List<T>`, **999 elements** (not a multiple of 8, 4 or even 2, so every vectorised
tail is ragged on every run). Two models over the same domain — one raw, one `ClassicEmit` — and the
setup **asserts they agree byte-for-byte** before any timing.

| category | baseline classic / raw | after | change |
| --- | ---: | ---: | ---: |
| **bool** | 4.56 / 4.70 µs | **2.65 / 2.64** | **−42% / −44%** |
| **varint unsigned** | 18.92 / 21.67 | **14.77 / 14.67** | **−22% / −32%** |
| **zigzag** | 18.48 / 19.96 | **15.95 / 15.75** | **−14% / −21%** |
| **varint signed** | 19.68 / 19.10 | **17.09 / 16.60** | −13% / −13% |
| fixed int | 3.64 / 3.79 | 4.01 / 3.86 | within noise |
| floating | 4.30 / 4.48 | 4.26 / 5.19 | within noise |
| enum | 5.14 / 5.07 | 5.15 / 5.08 | untouched — never packed |

Three passes got there: the **vectorised measure**; then **`bool`** (O(1) sizing plus a blit of the
span, guarded by a vectorised scan for a non-canonical byte); then a **direct varint write** that
bypasses the per-element `serializer.Write` — which was a virtual dispatch *and* a wire-type switch
before any byte was produced. Arrays take all three on every TFM; `List<T>` takes them on net5+
through `CollectionsMarshal`.

**This harness is adversarial for what remains** (B21 tier 1, the single-byte homogeneous block):
its values are spread evenly across the width classes, so a block of eight is rarely all-small.
Real data is the opposite — the census puts almost everything in the single-byte class — so this
would **understate** tier 1, and a "small" distribution should be added before anyone judges it.

**Both were done (2026-08-15), and the harness still could not answer the question** — which is the
more useful finding of the two. A `[Params("spread", "small")]` distribution went in, and tier 1
landed for all four varint element types; the end-to-end arms moved by an amount indistinguishable
from noise, because the run-to-run spread on a single arm (7.26–7.84 µs for *identical* code) is
larger than the effect. Two successive runs put the same arm on opposite sides of its baseline.

Two things dilute it, and naming them is what made the result readable:

- **the ~1 µs/member overhead below** — at four members that is over half the total before a single
  element is encoded;
- **the control shares the code under test.** Classic-emit tracked raw within 1% throughout, which
  reads as "the change did nothing" and actually means *both* models go through the same library
  `RepeatedSerializer` and both got the optimisation. A control that shares the code under test is
  not a control.

The sink was eliminated as a cause rather than assumed: `IBufferWriter` arms were added alongside
the `MemoryStream` ones and are only ~6% faster, so the stream is not where the time goes.

**So tier 1 is measured by `PackedWriteBenchmarks` instead**, which times the primitive directly
against the scalar loop it replaces, at ±0.5% noise: **8.6× (`uint32`), 8.5× (`int32`), 6.6×
(`uint64`)** where a block is uniform, against **+3% to +6%** where it never is. The end-to-end
harness remains the right *final* check and is the wrong instrument for attributing a delta to one
loop — keep both, and reach for the isolated one when the answer is smaller than the variance.

### ~~The largest unexplained cost is NOT packed-specific: ~1 µs per member~~ — **RETRACTED**

Now that the copies are bulk, the per-byte figures separate cleanly:

| | ns/member | ns/byte |
| --- | ---: | ---: |
| fixed int | 965 | **0.161** |
| floating | 1065 | **0.178** |
| bool | 1320 | 1.321 |

Fixed and floating are at **memcpy speed** — those cells are finished, and no further copy work is
warranted. But **every member costs roughly a microsecond regardless of payload size**: bool carries
six times *less* data than fixed and costs *more* per member. For bool that overhead is essentially
the entire remaining cost, which is why optimising the bool copy further did nothing (measured, not
assumed — sharing the vectorised canonical scan moved the number by zero).

So the biggest single number left in this matrix is **not about packing at all** — it is whatever
the per-member machinery does before and after the payload. That wants a profile rather than a
guess, and it may dwarf everything else here: at ~1 µs × members, a contract with twenty repeated
members spends 20 µs before any bytes are counted.

**Do not chase further packed micro-optimisation until that number is understood**, or the effort
will keep landing on the 0.16 ns/byte side of a 1000 ns/member problem.

> **RETRACTED, 2026-08-15. There is no ~1 µs per-member cost; it is ~10–17 ns.** The blocker above
> is lifted.
>
> The claim was arithmetic, not measurement: it divided a *total* by the member count, which
> attributes real per-byte work to a fixed cost. The tell was there in the same table and was read
> as corroboration instead — for the fixed-int cells, `965 ns/member` and `0.161 ns/byte` are the
> **same number** (each member carries ~6 KB, and 6000 × 0.161 ≈ 965). Those two figures are
> consistent with a fixed per-member cost *and* with none at all, so they cannot distinguish them.
> The bool row was offered as the discriminator — "six times less data, and costs *more* per
> member" — but that shows bool is slower per **byte** (1.32 vs 0.16 ns), which is a different
> claim.
>
> Separating them needs the payload swept while the member count is held still, so the intercept is
> visible. `PackedOverheadBenchmarks` does that, and the intercept is flat:
>
> | | 0 elements | 999 elements |
> | --- | ---: | ---: |
> | no members at all | **30.6 ns** | **30.6 ns** |
> | one packed member | 47.5 ns | 348.6 ns |
> | four packed members | 71.3 ns | 1294 ns |
>
> A contract with nothing to write costs ~30 ns and does not move with payload — that is the
> per-call floor. Each additional member costs **~10 ns** fixed (`(71.3 − 32.2) / 4`), and each
> element about **0.31 ns**. So the twenty-repeated-member contract projected at 20 µs above is
> really about 200 ns of fixed cost; the rest was always the bytes.
>
> **The methodology lesson is the durable part**: a per-unit figure derived by division cannot
> establish that the cost *is* per-unit. Only holding one variable still and sweeping the other can,
> and that experiment costs one benchmark class.

### A real cliff found while checking it: `Serialize<object>` on a `RuntimeTypeModel`

Not what the section above claimed, and worth knowing on its own terms — the two variables had to
be separated before either could be read:

| model | dispatch | per call | allocated |
| --- | --- | ---: | ---: |
| generated | generic | 58 ns | 0 |
| generated | `object` | 76 ns | 0 |
| `RuntimeTypeModel` | generic | 72 ns | 0 |
| `RuntimeTypeModel` | **`object`** | **2951 ns** | **2272 B** |

A `RuntimeTypeModel` asked to serialize at `T = object` costs **41× the typed form and allocates
2.2 KB per call**, while the same object-typed dispatch against a *generated* model costs 18 ns
extra and allocates nothing. So it is not "the object API is slow" and not "the runtime model is
slow" — it is the pair, and either alone is fine.

This is the shape `PBN2011` warns about for AOT reasons; it turns out to have a large throughput
cost as well, on a call shape that plenty of pre-generic protobuf-net code still uses.

**It did not contaminate the matrix below**, which was the first hypothesis and was wrong: those
harnesses use generated models, where the penalty is 18 ns. Moving them to generic dispatch anyway
(done) removes the confound rather than a constant.

**Three findings from the baseline itself**, before any optimisation:

1. **varint costs ~5× fixed** — about 5 ns/element against 0.9 — because a packed varint makes
   **two per-element passes**, measure then write, where fixed measures in O(1);
2. **the raw writer was NO BETTER than classic for packed, and sometimes worse** (unsigned was 15%
   *slower*). A packed member is measure-*blocked* — `RawRepeatedWritable` declines `IsPacked` — so
   the containing contract loses measure-first entirely and both models end in the same
   `RepeatedSerializer` code, with the raw model paying setup for nothing. That gap is now closed
   by accident rather than design: both land on the same vectorised measure;
3. **zigzag barely moved.** The measure is vectorised for it too, so what remains is the *write* —
   which for zigzag is a transform plus an encode per element. It is the clearest evidence that the
   remaining cost is B21's territory, not B19's.

**Still to do, in order:** ~~`bool[]`~~ (done); ~~B21 tier 1~~ (**done 2026-08-15**, all four
varint element types, both collection paths); B21 tier 2 (write without per-element room checks,
free once the measure is vectorised); enums, which are never packed at all. And ahead of all of
them, the ~1 µs/member overhead above — it is now the largest number in this file by a wide margin.


### Enums: already packed — the recorded blocker was false

This file and gaps.md B1 both said a packed enum column "is never actually packed, because
`EnumSerializer` is not an `IMeasuringSerializer`". It is packed, and has been throughout;
`PackedBlockCopyTests.PackedEnumsAreActuallyPacked` pins the bytes (`0A-03-01-02-03`).

Neither half of the reason survives reading the code. `TypeHelper.CanBePacked` returns true for
`type.IsEnum` **outright**, before the type-code switch. And the concrete
`EnumSerializer<TEnum, TRaw>` **does** implement `IMeasuringSerializer<TEnum>` — it is only the
public abstract `EnumSerializer<TEnum>` that does not, and `RepeatedSerializer`'s gate tests the
*instance* (`serializer is IMeasuringSerializer<TItem>`), which succeeds.

**The real gap is a different and much smaller one**: a packed enum is packed but takes the
**per-element** path, because the fast varint arms match on `typeof(T) == typeof(uint)` and friends,
and an enum is none of them. So it pays an enumerator step and a virtual `serializer.Write` per
element — measured at 2.54 ns/element against 1.74 for `uint32` in the same harness, i.e. ~46%
worse. The fix is the pun this file already relies on elsewhere (an enum reinterprets as its
underlying primitive), routed through the same `PackedVarintMeasure` entry points; it is worth
doing and is not what the old note described.

**That is the fifth claimed gap in this arc to evaporate on inspection**, after the enum map key,
the enum-proxy plumbing, map-of-map, and B1's packed premise. The rule already recorded — *a
refusal nobody has tried to trigger, or a limitation nobody has re-read since it was written, is
worth checking before it is worked* — keeps paying, and the cost of checking is a single test.


### The "classic vs raw" axis in this file is VACUOUS — and that is the real headline

Verified by emitting the generator output: **`PackedRawModel` contains zero `Measure_` and zero
`RawWrite_` methods.** All seven packed contracts fall off measure-first completely, so every
"raw" number in this file was produced by the classic engine. `NanoDescriptorModel`, for contrast,
emits 27 `Measure_` methods.

This supersedes the weaker reason recorded earlier ("both models end in the same
`RepeatedSerializer` code"). The truth is stronger: for a contract with a packed member there is no
raw code to compare against.

The chain, all confirmed in source:

1. `RawRepeatedWritable` returns false for `member.IsPacked` — *"packed changes the framing
   entirely, and needs measure (plus the zero-length header rules)"*;
2. `RawMemberMeasureBlocked` therefore falls through to `RawRepeatedMessageTarget(...) is null`,
   which for a scalar column is null, so the member is **measure-blocked**;
3. per the fixed-point rule in `AGENTS.md`, a blocked member removes its **whole contract** from the
   measurable set — **and that cascades to every contract referencing it**.

So **one packed scalar member silently drops a subtree off measure-first.** That is a far larger
cost than any element-loop optimisation in this file, and it is the thing to fix next.

**The fix is an architecture, not a patch** (Marc, 2026-08-15): expose the packed primitives as
**raw writer state APIs** and have the generator call them directly at the raw emit level, rather
than routing through `RepeatedSerializer`. That dissolves most of the difficulty rather than
working around it:

- **the runtime `typeof(T)` ladder disappears** — the generator knows the element type at compile
  time, so there is nothing to dispatch on;
- **the enum problem disappears with it** — Roslyn knows the underlying type, including the narrow
  backings that cannot be span-punned at all, so it can widen or decline per case instead of
  needing reflection cached in `TypeHelper<T>`;
- **signedness is picked at compile time**, removing the silent-wrong-bytes risk of routing an
  `int`-backed column to the unsigned arm.

The primitives are already the right shape: `WritePackedUInt32(ref ProtoWriter.State,
ReadOnlySpan<uint>)` **is** a raw state API, just `internal` and in the wrong class, and
`PackedVarintMeasure.Measure(ReadOnlySpan<T>)` is already the vectorised measure the length prefix
needs. Promoting them is close to mechanical.

**What is actually work** is the reason for the original refusal, and it should not be waved past:
the raw path must reproduce protobuf-net's framing rules exactly, including that a **single**
element is written *unpacked* (`count == 0 || count > 1` — see
`ASinglePackedElementIsWrittenUnpacked`) and an empty collection writes a zero-length header. Those
are byte-visible, so `ClassicVsRawTests` becomes a real gate here for the first time — today it
compares classic against classic for these contracts.


### Collection shapes: decide at the call site, frame in the API

Settled with Marc, 2026-08-15, as the design for the raw packed path above.

**The generator resolves the collection shape itself, and emits a span.** It has the context
already — `ProtoRepeatedPlan` carries `Factory`, `TakesCollectionType` and `IsValueType`, and the
element kind is on the member plan — and there is precedent: `RawRepeatedWritable` already gates on
`Factory is "CreateList" or "CreateVector"`, so the call site is where this decision is made today.
The three shapes that matter:

| shape | span | availability |
| --- | --- | --- |
| `T[]` | the array itself | everywhere |
| `List<T>` | `CollectionsMarshal.AsSpan(list)` | net5+ |
| `ImmutableArray<T>` | `.AsSpan()` | needs probing |

**The enum pun happens at the call site too** — `MemoryMarshal.Cast<Level, int>(span)`, which is
legal (both are structs) and far clearer than the `Unsafe.As` + `CreateReadOnlySpan` gymnastics the
library path needs, because there `T` is unconstrained. This is the whole reason the architecture
is better: Roslyn knows the underlying type, so the narrow backings that **cannot** be span-punned
at all (`sbyte`/`byte`/`short`/`ushort`, whose element widths differ) are declined at compile time
rather than needing a runtime guard.

**So: overloads per element ENCODING, never per collection SHAPE.** The former is necessary and
small — `ReadOnlySpan<uint>` / `<int>` / `<ulong>`, times varint and zigzag, plus the fixed-width
blits, so roughly six methods, none of which cares where the span came from. The latter is what
produces shim sprawl and is exactly what the call-site decision avoids.

**The framing rules stay INSIDE the API, and this is the one place not to push to the call site.**
A single element is written *unpacked* (`count == 0 || count > 1`, pinned by
`ASinglePackedElementIsWrittenUnpacked`) and an empty collection writes a zero-length header. Both
are byte-visible and neither is obvious; emitted inline per member they will drift, and every drift
is a wire bug that only a byte oracle catches. One line at the call site, one tested implementation.

**Hence the API takes a FIELD NUMBER, not a pre-encoded tag** — and the reasoning is worth keeping,
because it looks like an inconsistency with the rest of this branch and is not. Elsewhere the tag is
pre-encoded and written through a narrowed ladder, because for a *single scalar* the tag encode can
cost as much as the value it introduces. A packed column is the opposite case: one tag against a
whole payload, so the varint encode is irrelevant (Marc). Taking a pre-encoded tag here would
fragment the surface for nothing, and the API needs the field number anyway to make the
single-element-unpacked decision — that arm writes a *per-element* header.

**Do not carry the derived-list exclusion over by reflex.** `RawRepeatedWritable` excludes derived
lists because `foreach` binds to the **declared** type's `GetEnumerator`, which could be a hiding
redeclaration. `CollectionsMarshal.AsSpan` bypasses the enumerator entirely, so that reasoning does
not apply and `class MyList : List<int>` is safe here. The packed raw path can be *wider* than the
unpacked one.

**Two hazards to settle when building it**, neither yet checked:

- **`ImmutableArray<T>` `default` is not null and throws on most access.** It is a struct, and this
  file's parent notes already record that neither side null-tests it. Whether `AsSpan()` survives a
  `default` instance needs verifying; if it does not, that shape needs an `IsDefaultOrEmpty` guard
  the array case does not.
- **`Measure_` and `RawWrite_` are separate methods**, so the span is acquired twice. That is cheap,
  and it is already the measure-first contract that the object does not change between passes — but
  it does mean a mutation between them surfaces as the length cross-check firing rather than as
  anything more helpful.

Down-level consumers fall back to the stateful path where a symbol is missing, exactly as the
generator already does for `UnsafeAccessorAttribute`, `CreateReadOnlySet` and `BclHelpers.ReadDateOnly`:
a smaller optimisation, not a broken build.

## Ranked, by value over effort

1. ~~**Block copy for the matching fixed-width cells.**~~ **DONE 2026-08-14.** Portable, no
   intrinsics, correct behind an `IsLittleEndian` guard — it replaced a per-element interface
   dispatch where a `memcpy` belonged. `List<T>` of the same types remains open (needs
   `CollectionsMarshal`, net5+).
2. **`bool[]`** — **next, and still unbuilt**: O(1) sizing and a near-blit write, for a type that
   currently walks the ladder element by element.
3. **B19 sizing** (measured 1.8×–6.6×) and **B21 tier 2**, which comes free with it.
4. **B21 tier 1**, the homogeneous single-byte block — portable, and the census says it is the
   common case.
5. **B20 cross-width narrow/widen** — narrower reach, and it settles whether `Vector.Narrow` is
   usable down-level.
6. **B21 tier 3**, full shuffle-compacted LEB128 — research-shaped, modern TFMs only.

**None of 3–6 can be justified in situ yet**: there is still no packed-numeric benchmark payload,
and `DescriptorPayloadCensus.md` shows the descriptor set is 71.5% string/bytes with almost no
packed content. Items 1 and 2 need no benchmark to justify — they replace a per-element virtual
call with a bulk operation — but the payload is what would *quantify* them, and everything below
needs it before landing.
