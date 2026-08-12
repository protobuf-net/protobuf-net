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

## Gates, in order (Marc)

1. does it compile; 2. `AotDifferential` corpus on bytes; 3. the entire compat suite.

## Discovered so far

- `GetReader()` has exactly two call sites in Core (BclHelpers.cs + State.cs itself).
- `Hint` semantics verified earlier: upgrades stored wire type in place when low 3 bits match
  (SignedVarint = Varint | 8).
- `ReadPackedScalar` validates fixed lengths (%4/%8), uses position-bounded varint loop with
  over-read throw - all shapes nano's packed helpers already implement; the generic
  serializer-driven form here stays for the veneer path.
- TooDeep message quotes TypeModel.MaxDepth: nano's fixed 512 must become the model's
  configured MaxDepth at construction (State knows the model now).
