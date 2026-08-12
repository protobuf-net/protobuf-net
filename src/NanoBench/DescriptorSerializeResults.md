# Descriptor serialize composite (docs/nano-writer.md)

The self-describing descriptor payload (7,670 bytes), serialized five ways into one reused
MemoryStream. Setup gates: byte identity with the legacy payload for BOTH Reflection-DTO
rows *and* (observed, reported rather than required) the bench-DTO row; census equality
through a legacy reparse for the Google row; and `Measure_` returning exactly the byte
count the write then produces.

## net10.0, 2026-08-12, writer cuts 1-4 (surface-first veneers, recompute-always measure)

| row | mean | vs LegacyReal | notes |
| --- | ---: | ---: | --- |
| LegacyReal | 20.64 us | baseline | RuntimeTypeModel over protobuf-net.Reflection's DTOs |
| GeneratedProtogen | 13.56 us | **-34%** | the SAME object graph through the shipped generated model - the pure engine-swap row |
| NanoGenerated | 14.48 us | -30% | generated model over the bench DTOs (nullable-guard shapes) |
| NanoGeneratedMeasure | 2.23 us | - | `Measure_` alone, one full-tree traversal |
| GoogleProtobuf | 13.08 us | -37% | home turf (generated parser/writer, its own DTOs) |

Readings, for the next cuts:

- **The engine swap alone is worth 34%** before the fast buffer core exists: every raw op
  still routes through the virtual `Impl*` backends, and every sub-message length is
  recomputed. Both planned cuts attack the remaining ~13.5 us directly.
- **Measure is ~2.2 us for one traversal, but recompute-always pays it repeatedly**: the
  write re-measures each subtree per nesting level, so the aggregate measure share of the
  write rows is a multiple of 2.2 us on this tree. That bounds what the `??=` lengthCache
  can recover - worth several microseconds here, i.e. potentially ~20% of the write row -
  and gives the race its stakes.
- Google.Protobuf is only ~4% ahead of the shipped generated model already; the read arc
  ended ~26% ahead of Google on parse, which is the shape to aim for once the buffer core
  and memoization land.
