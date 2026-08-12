# nano-swap step 2: porting notes (transient - delete when the swap completes)

The working scaffold for rewriting `ProtoReader.State` over the nano core. Design decisions live
in docs/nano-core.md; this file is the mechanical state of the port.

## The cut is all-or-nothing across five files

The class backends consume State's internal window helpers (`Consume`/`Span`/`ReadVarintUInt32`
- e.g. ProtoReader.ReadOnlySequence.cs:228), so State's storage cannot change without the
backends changing in the same commit:

- `ProtoReader.State.cs` - the shell: window fields go, nano fields + model/context/netcache come
- `ProtoReader.State.ReadMethods.cs` - the ~80 members (inventory below)
- `ProtoReader.cs` - class core: holds `ReaderSnapshot`, instance API becomes
  liquify/operate/re-solidify (~40 mechanical rewrites; `DefaultState().X()` temporaries stop
  carrying state)
- `ProtoReader.Stream.cs` + `ProtoReader.ReadOnlySequence.cs` - backends DELETED; construction
  routes to the nano constructors (including the MemoryStream unwrap, already ported)

## Member buckets

**A - veneers already exist in Nano/ReaderState.Legacy.cs** (port = move/adjust):
ReadFieldHeader, TryReadFieldHeader, ReadInt32, ReadString(sans map), StartSubItem, EndSubItem,
SkipField, Dispose, WireType, FieldNumber, ReadRawFixed32/64 (+straddles).

**B - mechanical over raw primitives**:
ReadUInt16/Int16/Byte/SByte/UInt32/Int64/UInt64/Boolean/Double/Single/IntPtr/UIntPtr,
GetPosition (nano Position), Assert, Hint (upgrades _wireType in place when low 3 bits match),
CheckFullyConsumed, throw helpers (AddErrorData: reads position/field for the message),
ReadType, SkipAllFields (loop header+skip), AssertPlausibleLength/CanAllocate (nano guards
subsume; keep entry points), ReadStringOversized (nano string slow path subsumes),
AppendExtensionData x2 (LEGACY signatures: no tag parameter - reconstruct from
_fieldNumber/_wireType exactly as the SkipField veneer does, then the nano capture),
AppendBytes family (LEGACY append semantics on this surface - the veneer contract; replace is
the generated path's default, selected by emitted code), ReadBytes(Span) pair.

**C - composites that port nearly verbatim** (discovered: they are written against State's own
public surface + `_reader.WireType = x` pokes, which become `_wireType = x`):
FillBuffer/PrepareToReadRepeated/ReadRepeatedCore/ReadPackedScalar (the repeated engine;
ReadPackedScalar's List<T> capacity boost + MAX_GROW=8192 carries over), ReadMessage family,
ReadAny, ReadWrapped, ReadBaseType, CreateInstance, GetSerializer, Model/Context (fields now),
HasSubValue.

**D - COLLAPSED**: the keyed-object/reference-tracking surface (GetKeyedObject/SetKeyedObject/
TrapNextObject/NoteObject, SetRootObject in ReadAsRoot) is entirely inside `#if
FEAT_DYNAMIC_REF`, which the shipped build never defines - it is not in the product assembly.
What remains: DeserializeType (a model call) and InternStrings + StringMap (ReadString(map)).

**E - root machinery: READ, ports verbatim** - DeserializeRoot/ReadAsRoot/DeserializeRootImpl
are written against State's own surface (ReadFieldHeader/ReadAny/CreateInstance/
CheckFullyConsumed); the CategoryMessageWrappedAtRoot/Scalar arms use the field-one loop.
Still to read: DeserializeRootFallback(WithModel), ReadTypedObject/ReadObject (aux path),
SolidState/Solidify/Liquify callers (bridge via ReaderSnapshot), AppendBytes internals,
ReadString(map) internals.

## Revised scope after the survey

The true rewrite surface is the ~25 PRIMITIVE members (scalar reads over _reader.Impl*, the
window helpers, header machinery) - everything above them is self-hosted on State's public
surface and ports verbatim with `_reader.WireType = x` pokes becoming `_wireType = x`. Plus the
snapshot bridge for the class API, and the backend deletions.

## FINAL STATUS: ALL GATES GREEN (2026-08-12)

Gate 1 (compiles): the whole solution. Gate 2 (bytes): 3,021 corpus contracts, 100% match,
zero asymmetric throws. Gate 3 (compat): protobuf-net.Test 1046/1046 on both TFMs (one
non-reproducing net472 flake, cleared twice), AotConformanceTests 655/655, BuildToolsUnitTests
green with the nano pass now live across ALL fixtures (the real State is always present, so
every golden gained its nano emission - the emit-gate flip to opt-in remains a merge item),
Reflection.Test 556/556 both TFMs, Examples green both TFMs. NanoBench battery over the
swapped core: census, generated-vs-hand equivalence, extension byte-identity oracle,
7,669-split sweep, stream and scalar gates - all green.

The one systematic failure of the entire port was the one member fabricated from memory
instead of read text (DeserializeRootFallback); everything ported from READ text survived
untouched. Exception fidelity restored post-sweep: EoF = EndOfStreamException, varint
exhaustion = OverflowException, protoSource decoration on every raw throw.

Post-green backlog (the merge-phase ceremony, in no particular order): PublicAPI.Unshipped
entries for the raw surface + ReadScope; [Experimental] ceremony; the classic-emit
escape-hatch flag (task, wording recorded in nano-core.md); field-0 tags throw IOE where
legacy said ProtoException (no test distinguishes); benchmark re-verification on the swapped
tree (numbers should be the spike numbers - same code, new home); delete this file when the
swap merges.

Done since: ISerializer.Read now PROXIES to the RawRead_ static for eligible contracts (the
question the emit-gate item was really asking - the optimized emit is the live read path,
and the conformance suite exercises it for real); the NanoState spike is deleted; "nano"
naming is purged from code (files: ProtoReader.State.Raw.cs / .WellKnown.cs; generated
surface: RawRead_ + "raw read pass" breadcrumbs; the term survives only in planning docs
and the NanoBench rig's project name).

Routing immediately caught four latent divergences that compiled and passed everything
while the statics sat unrouted - the strongest argument the proxy was worth it:
- plain-bytes merge is APPEND in legacy; the raw form replaced. AppendRawBytes (raw-convention,
  no _wireType consultation) now carries both: the emit uses it, and AppendBytes' String arm
  delegates to it. OverwriteList bytes stay on replace.
- a classic caller reading a GROUP-framed value (StartSubItem mints a token, pushes no raw
  scope) left the proxied raw read blind to the end-group tag. IsScopeEnd now has a legacy
  fallback: inside a legacy frame it stashes _wireType/_fieldNumber exactly as ReadFieldHeader's
  spoof would, and EndSubItem's existing verification takes it from there.
- repeated arms assumed the collection existed (bench DTOs initialize inline); a settable null
  List member NRE'd. Each arm now opens with construct-on-first-presence (??= new), guarded to
  members with an ordinary setter (IsReadOnly / UsesAccessor emit nothing - CS0200 otherwise).
- the packed List<int> fast path is typed to the non-nullable list; a List<int?> member now
  falls to the inline drain (every other form already converts implicitly).

## Cut status (historical)

DONE: NanoCore retargeted (cut 1); snapshot machinery real (cut 2); shell + full ReadMethods
rewrite over the nano core (cut 3, commit 0a870e7c) - including SetTag's end-group spoof, the
legacy sub-item state machine, the repeated engine, roots, plausibility, AppendBytes over
ReadRawBytesInto, extension veneers over the nano capture, error helpers, and the StateContext
shim class.

FIRST COMPILE: exactly 6 errors, all CS0111 duplicate `State.Create` - the old backend statics
in ProtoReader.Stream.cs / ProtoReader.ReadOnlySequence.cs vs the relocated shell ones. The
next wave is fully characterized:

1. **ProtoReader.cs bridge rewrite** (the last big artifact): delete the Impl* abstracts and
   the class fields State now owns (SetTag/Hint/Intern already copied); the class holds a
   `ReaderSnapshot` + becomes concrete-or-bridge (`SnapshotProtoReader`); the ~40 museum
   `DefaultState().X()` instance methods become liquify/operate/re-solidify (temporaries stop
   carrying state); keep TO_EOF, EagerAllocationLimit, UTF8, Read32VarintMode, PreferStateAPI
   messages, ISerializationContext implementation over the snapshot's model/userState.
2. **Slim ProtoReader.Stream.cs** to: museum `ProtoReader.Create(Stream,...)` statics (bridge-
   based) + `TryConsumeSegmentRespectingPosition` (kept verbatim - the WRITER's extension-blit
   uses it, sole external caller). **Delete ProtoReader.ReadOnlySequence.cs entirely** (its
   ToString/TryParseUInt32Varint helpers have no external callers).
3. Compile loop ripples expected: SubTypeState.Create(Context,...) signature (speculatively
   changed from reader to ISerializationContext - adjust SubTypeState), BclHelpers.GetReader
   caller, ISerializationContext member names on the StateContext shim, TypeModel Solidify
   call sites, `Unsafe.AsRef(in _segment)` in Snapshot() (readonly-context ref-field access),
   PublicAPI.Unshipped entries for the new public raw surface + ReadScope, emit/probe rename
   (ProtoBuf.Nano.ReaderState -> ProtoBuf.ProtoReader+State) + NanoPass fixture de-stubbing +
   NanoBench signature updates + NanoState project retirement from the build.

## Gates, in order (Marc)

1. does it compile; 2. `AotDifferential` corpus on bytes; 3. the entire compat suite.

## ReadString(StringMap): retained, and newly viable (Marc, during the port)

The map parameter never went anywhere under the runtime model; codegen changes that. The intent
- "read strings, pre-armed with a corpus of expected common values" - becomes real because the
call site is compile-time (a PER-MEMBER map is expressible: `ReadRawString(s_statusNames)`),
the generator can sometimes author the corpus from schema knowledge (enum-name spellings,
[DefaultValue] strings, reserved names), and the nano resident path can probe the raw UTF-8
bytes in place against pre-encoded entries before materializing - hit = zero allocation, miss =
one bytes-hash on top of the GetString it was doing anyway. Port the parameter as-is
(accepted-and-ignored today); the pre-armed design is a future brick, composing with the
interning lever parked in StringParseResults.md.

## The complete member map (ReadMethods.cs fully read)

Substitution glossary for the verbatim ports: `_reader.WireType` get -> `_wireType`; set ->
same field (internal); `_reader._fieldNumber` -> `_fieldNumber`; `_reader._longPosition`/
`GetPosition()` -> nano `Position`; `_reader._model`/`Model` -> `_model` field;
`_reader._depth`/`IncrDepth/DecrDepth` -> nano `_depth` vs model MaxDepth (configured, not the
spike's constant 512); `_reader.blockEnd64` -> nano `_scope` (length mode);
`_reader.ImplReadUInt32Fixed/UInt64Fixed(ref this)` -> `ReadRawFixed32/64()`;
`ImplReadBytes(ref this, span)` -> new primitive `ReadRawBytesInto(Span<byte>)` (FillFrom with
span dest); `ImplSkipBytes` -> `Advance`; `ImplReadString(len)` -> nano string tail (resident
fast/straddle slow); `ImplTryReadUInt32VarintWithoutMoving(FieldHeader)` -> the forward-only
veneer patterns already built (pending-tag slot); `ReadUInt32Varint(mode)`/`ReadUInt64Varint`
-> `ReadRawVarint32/64` (Signed mode = 64-tolerant truncation: nano's tolerant read IS this).

Wire-switch primitives (ReadUInt32/Int32/Int64/UInt64/Double/Single + narrowing
UInt16/Int16/Byte/SByte checked casts, IntPtr/UIntPtr): bodies as-is with glossary; float via
safe reinterpret (nano style) not unsafe pointers. Zag statics: keep.

State-machine semantics that MUST be reproduced (divergences found between veneer and legacy):
- several ops set `WireType = None` after consuming: bytes reads, ReadBytes(span),
  StartSubItem(group arm), EndSubItem(group arm), SkipGroup exit. Reproduce exactly - Assert/
  Hint/HasSubValue flows depend on it.
- legacy SubItemToken: String arm = PRIOR blockEnd (matches nano prior-scope); Group arm =
  -fieldNumber IDENTITY (not prior scope!) with EndSubItem validating WireType==EndGroup &&
  matching field, then WireType=None. Port EndSubItem faithfully; nano scope slot carries the
  group sentinel already - the token becomes identity for groups, prior _scope for strings, and
  PopScope-style restore happens from the token (strings) or is a no-op unbounding (groups,
  which never bounded position).
- ReadFieldHeader end conditions: blockEnd both arms via nano EndOfScope; the EndGroup arm:
  encountering the group sentinel sets _wireType=EndGroup + _fieldNumber=group field WITHOUT
  releasing (returns 0); EndSubItem releases via None. CURRENT VENEER HOLE to fix in the port:
  a NON-matching end-group tag must throw (legacy SetTag errors), not decompose as a field.
- SkipField(None/EndGroup) throws; SkipGroup = loop-headers implementation preserving the
  above state machine (exit checks EndGroup + same field, sets None).
- TryReadFieldHeader: forward-only pending-tag design stands (it subsumes legacy's
  peek-without-moving), respecting the same end-conditions gate.
- AssertPlausibleLength/CanAllocate/ReadBytesOversized/ReadStringOversized: legacy's
  EagerAllocationLimit policy; nano guards subsume but keep the entry points delegating to the
  nano remaining-calculation (GetMaxRemaining = min(source remaining, scope remaining), -1
  unknowable).
- AppendBytesImpl: len; WireType=None; converter.Expand(Context, ref value, len) then fill the
  chunk span (CanAllocate ? direct : oversized-buffered). Uses Context. APPEND semantics live
  in the converter - the veneer keeps them by construction.
- AppendExtensionData(instance) legacy signatures: replace the ProtoWriter-based re-encode
  with the nano capture (byte-preserving, allocation-free) using the tag reconstructed from
  _fieldNumber/_wireType, then WireType=None. The byte-identity oracle already proved bag
  compatibility; nano preserves overlong varints where legacy canonicalized - strictly more
  faithful.
- Error helpers: AddErrorData decorates with "protoSource" = tag/wire-type/offset/depth; keep
  the static's shape (ProtoReader source param may become nullable/ignored).
- ThrowTooDeep quotes TypeModel.MaxDepth and current depth.
- InternStrings: State property has a SETTER; becomes a State field + lazy interner map.
- ReadType -> TypeModel.DeserializeType(_model, ReadString()).
- ReadBaseType: `SubTypeState<TBaseType>.Create<T>(_reader, value)` captures the READER
  INSTANCE - re-point at the serialization-context holder (check SubTypeState.Create's actual
  use of it during the write).
- Composites confirmed verbatim: ReadMessage family (StartSubItem/serializer.Read/EndSubItem),
  ReadAny (category switch + HintIfNeeded), ReadWrapped (field-one loop + NonTrivialDefault),
  DeserializeRoot/ReadAsRoot/DeserializeRootImpl, FillBuffer engine, GetSerializer.

Still unread before writing: ProtoReader.cs class internals (SetTag, Hint impl, Intern,
IncrDepth/MaxDepth wiring, MaxRemaining, DeserializeType, Model set, Create/recycle),
Stream/ROS State.Create entries, TypeModel call sites of State.Create + SolidState/Solidify
callers, SubTypeState.Create, DeserializeRootFallback(WithModel) + TryDeserializeAuxiliaryType
touchpoints (aux path likely ports verbatim - it consumes State surface).

## Class-side finds (partial - the last pre-write reads)

- **SolidState is load-bearing for the iterator paths**: `ExtensibleUtil.GetExtendedValues`'s
  reflective arm does `State.Create(stream...).Solidify()` and iterates via
  `TryDeserializeAuxiliaryType` (an iterator cannot hold a ref struct), and `TypeModel.cs:1196`
  re-solidifies after a liquid pass. The ReaderSnapshot bridge covers both: SolidState becomes
  snapshot + model extras, Liquify reconstructs. Note ExtensibleUtil's other arm already uses
  the real State directly.
- `EagerAllocationLimit` = 32 * 1024 - align nano's scratch initial rent (spike used 64K).
- `MaxRemaining` is abstract per backend; nano computes it from `_remaining` (+ unknowable=-1).
- `InternStrings` also lives as a settable property on the ProtoReader class (bridge).
- Still to grep at write time: SetTag (field-0 + end-group handling), Hint impl body, Intern
  impl (the custom interner), IncrDepth/MaxDepth wiring, the Stream/ROS `State.Create` bodies
  (recycling/pooling to delete), `SubTypeState.Create(ProtoReader, value)` usage of the reader.

## Discovered so far

- `GetReader()` has exactly two call sites in Core (BclHelpers.cs + State.cs itself).
- `Hint` semantics verified earlier: upgrades stored wire type in place when low 3 bits match
  (SignedVarint = Varint | 8).
- `ReadPackedScalar` validates fixed lengths (%4/%8), uses position-bounded varint loop with
  over-read throw - all shapes nano's packed helpers already implement; the generic
  serializer-driven form here stays for the veneer path.
- TooDeep message quotes TypeModel.MaxDepth: nano's fixed 512 must become the model's
  configured MaxDepth at construction (State knows the model now).
