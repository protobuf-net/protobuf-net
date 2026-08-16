# Dispatch shape: full-tag switch vs field-first switch

The question (Marc, from IL inspection): `switch (tag)` over full-tag constants lowers to a
binary search (the values are sparse — fields spread by 8, wire bits vary), where
`switch (tag >> 3)` with `when` guards gets a dense jump table plus one guard test. The IL
strongly favours the latter; does the hardware agree?

64K-tag streams, dispatch only (bodies are distinct constant adds; `NoInlining` workers);
"ordered" = runs of 4 per field, fields ascending (how writers emit); "shuffled" =
Fisher-Yates over the same tags. dense9 = fields 1..9; wide21 = fields 1..12, 16..21 + 536.
net10.0, full BDN job, 2026-08-13; deltas are the result, not the absolute numbers.

| Method              | FieldSet | Order    | Mean      | Ratio |
| ------------------- | -------- | -------- | --------: | ----: |
| TagSwitch           | dense9   | ordered  |  44.45 us |  1.00 |
| FieldSwitchWhen     | dense9   | ordered  | 183.02 us |  4.12 |
| TagSwitchTolerant   | dense9   | ordered  |  45.06 us |  1.01 |
| FieldSwitchTolerant | dense9   | ordered  | 176.35 us |  3.97 |
| TagSwitch           | dense9   | shuffled | 361.71 us |  1.00 |
| FieldSwitchWhen     | dense9   | shuffled | 326.27 us |  0.90 |
| TagSwitchTolerant   | dense9   | shuffled | 382.64 us |  1.06 |
| FieldSwitchTolerant | dense9   | shuffled | 328.17 us |  0.91 |
| TagSwitch           | wide21   | ordered  |  53.48 us |  1.00 |
| FieldSwitchWhen     | wide21   | ordered  | 162.54 us |  3.04 |
| TagSwitchTolerant   | wide21   | ordered  |  57.50 us |  1.08 |
| FieldSwitchTolerant | wide21   | ordered  | 107.22 us |  2.00 |
| TagSwitch           | wide21   | shuffled | 480.82 us |  1.00 |
| FieldSwitchWhen     | wide21   | shuffled | 329.22 us |  0.68 |
| TagSwitchTolerant   | wide21   | shuffled | 489.71 us |  1.02 |
| FieldSwitchTolerant | wide21   | shuffled | 330.28 us |  0.69 |

## The reading

**The IL intuition inverts under branch prediction.** On ordered data the binary-search
compare chain is near-perfectly predicted — ~0.7 ns/tag — and the jump table's indirect
branch costs ~2.5–2.8 ns/tag: the "vastly preferable" IL is 3–4× slower where it matters.
On shuffled data the chain mispredicts at every level and the table wins (0.68–0.90×). The
two shapes split exactly along the ordered/shuffled line.

**Decision: keep the full-tag switch.** Three reasons, in order of weight:

1. Real payloads are ordered — writers emit fields ascending, in runs. That is also the
   stated optimization target ("contiguous and low field numbers - it is what gets used in
   reality"), and it is the case the current shape wins 3–4×.
2. The run-consumption design already removes dispatch from inside a field run (the
   do-while consumes the run without re-entering the switch), so dispatch happens roughly
   once per field *group*, shrinking the stakes either way.
3. Real arm bodies read data (5–50 ns), diluting even the shuffled-case delta to noise;
   the dispatch-only workers here are the worst case for the difference.

**The tolerance labels are free on the winning shape**: +1% (dense9) / +8% (wide21) on a
stream that never exercises them — the deferred question from the tolerance design, now
answered; no strict-mode knob is warranted.

**The known-field/invalid-wire arm stays where it is.** The or-pattern jump-table trick
(`case 1 or 2 or ... : throw`) belongs to the field-first shape; on the full-tag shape the
same detection lives in the default arm's IsKnownField, which only runs for tags no label
matched — off the hot path entirely. Note for any future revisit of the field-first shape:
end-group tags share the field-number space (`(5 << 3) | 4` lands in a case-5 cluster), so
the scope-end test must precede the throw in both terminal arms, and a legacy-mode member's
bare `case n:` needs a `(tag & 7) != 4` guard.

One unexplained wrinkle, recorded not chased: FieldSwitchTolerant beats FieldSwitchWhen on
wide21/ordered (107 vs 163 us) despite carrying more guards — some lowering-strategy
threshold. Irrelevant to the decision; both lose to the full-tag shape there.
