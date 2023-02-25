# Deltas from v2 in null list support

- v2 has a glitch where the model will - in some cases, depending on the compilation mode - **without** null support enabled,
  accept and serializes `null` vales in a list as though they were default non-null values, serializing an empty token; v3
  deliberately throws in this scenario in all cases
- v2, in packed mode, would *always* use packed encoding even if longer; v3 uses non-packed mode when shorter (1 item)
- v3 was disallowing packed encoding for `Nullable<T>`, which v2 allowed; this has been reinstated
- the schema output for a model with null support is incorrect (it does not reflect the nested layer); this has not yet been fixed, but will be resolved in v3 (not v2)
