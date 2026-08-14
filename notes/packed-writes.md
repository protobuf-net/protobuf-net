# Packed repeated writes: the scenario matrix

**The question this answers** (Marc, 2026-08-14): *what packed scenarios exist, and have we
considered how to optimise each — where it isn't a simple block copy?*

The short version is that the matrix has a clear shape, and the **cheapest cell is the one that is
missing**. See the finding below before reading the SIMD entries in `notes/gaps.md` (B19–B21):
they are all downstream of it.

---

## The finding: there is no block copy at all

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

That reorders everything: block copy is trivially correct, portable, needs no intrinsics, and
covers the fixed-width cells outright. It should land before any of the vectorised work.

---

## The matrix

Three dimensions: the **wire encoding**, whether the **CLR width matches** it, and the
**collection shape**. Sizing and writing behave differently, so both are shown.

### Fixed-width, CLR width matches the wire

| CLR | wire | sizing | writing today | optimal | gap |
| --- | --- | --- | --- | --- | --- |
| `float[]` | `float` (I32) | `count * 4`, O(1) ✅ | per element | **block copy** | **missing** |
| `double[]` | `double` (I64) | `count * 8`, O(1) ✅ | per element | **block copy** | **missing** |
| `int[]` | `sfixed32` | `count * 4` ✅ | per element | **block copy** | **missing** |
| `uint[]` | `fixed32` | `count * 4` ✅ | per element | **block copy** | **missing** |
| `long[]` | `sfixed64` | `count * 8` ✅ | per element | **block copy** | **missing** |
| `ulong[]` | `fixed64` | `count * 8` ✅ | per element | **block copy** | **missing** |

**Sizing is already optimal** here — `WritePacked` special-cases `Fixed32`/`Fixed64` to
`count * 4` / `count * 8` without touching the elements. It is only the *write* that is missing a
bulk path. Needs a `BitConverter.IsLittleEndian` guard, which the JIT folds away on LE; see the
endianness note in gaps.md B20.

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

## Ranked, by value over effort

1. **Block copy for the matching fixed-width cells.** Portable, no intrinsics, trivially correct
   behind an `IsLittleEndian` guard, and it is currently *absent* — a per-element interface
   dispatch where a `memcpy` belongs.
2. **`bool[]`**: O(1) sizing and a near-blit write, for a type that currently walks the ladder.
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
