protobuf-net
-

# Memory use and message size limits

The Protocol Buffers wire format is compact, and deliberately so - but that means the in-memory
representation of a message can be substantially *larger* than the bytes it arrived as. If you
enforce a maximum request or message size (and you should), it is worth knowing the ratio between
the two, because the limit you set in wire bytes is not the limit you get in memory.

This is a property of the wire format rather than anything specific to protobuf-net; the same
arithmetic applies to any implementation.

## Where the ratio comes from

Variable-length integer encoding is the main source. A `long` with a small value encodes as a
single byte on the wire, but still occupies eight bytes in a `long[]` or `List<long>`. A packed
repeated field of small values is therefore about eight times larger in memory than on the wire,
before any buffering overhead.

Messages and strings have a similar effect via per-object overhead: a zero-length sub-message is
two bytes on the wire, but a distinct object on the heap.

## Indicative ratios

Approximate bytes allocated per byte of payload, for 1MiB payloads of small values. Treat these as
order-of-magnitude guidance rather than exact figures - they vary with runtime, buffer pool state,
and the values involved:

| field shape | approx. ratio |
| ----------- | ------------- |
| packed `long[]` / `List<long>`, varint (default) | 8x, up to ~24x with buffer growth |
| packed `int[]`, varint (default) | 4x |
| packed `long[]`, `DataFormat.FixedSize` | 1x |
| repeated sub-message (small/empty messages) | ~16x |
| repeated short `string` | ~11x |

The high end of the first row is buffer growth: collections are accumulated into a pooled buffer
that grows geometrically, so a request that misses the array pool transiently costs more than the
resulting collection.

## How to apply a limit

There is no single "maximum message size" setting on `TypeModel` - the limit is applied at the point
you hand data to the serializer, and how you do that depends on where the data comes from.

### The `length` argument is not a limit

It is worth being explicit about this, because the name invites the wrong reading. The `length`
argument on `Deserialize` declares the **exact size of the message being read**. It is a framing
mechanism, not a ceiling, and passing a maximum rather than the true size will not do what you want:

| `length` versus the actual message | result |
| ---------------------------------- | ------ |
| equal to the message size | reads correctly |
| larger, and the stream ends | `ProtoException` - "Incorrect number of bytes consumed" |
| larger, and more data follows | reads on into whatever came next; usually a field-corruption error |
| smaller | `EndOfStreamException` |

A `MemoryStream` is a misleading special case: because the whole buffer is handed to the reader at
once, an over-large `length` is quietly clipped to the buffer and appears to work. Do not infer from
that test passing that the same call is safe against a network stream.

### When you know the exact size

Framed protocols, a `Content-Length`, a database column, a file on disk - pass it:

```csharp
var obj = Serializer.Deserialize<MyMessage>(stream, length: knownSize);
```

This is worth doing wherever you can. Besides the framing, it tells the reader how much data
legitimately exists, so a length-prefix *inside* the message that claims more than that is rejected
immediately rather than being acted upon.

### When the size is declared in the data

Read the prefix, decide, then pass the accepted length as the exact size:

```csharp
int len = ProtoReader.ReadLengthPrefix(stream, expectHeader: false, PrefixStyle.Base128,
    out _, out _);
if (len > MaxBytes) throw new InvalidDataException($"message too large: {len} bytes");
var obj = Serializer.Deserialize<MyMessage>(stream, length: len);
```

### When you don't know the size and want a cap

This is the case the `length` argument cannot serve. Bound the *input* instead - read up to your
ceiling and refuse anything larger, then deserialize what you accepted:

```csharp
using var buffered = new MemoryStream();
var chunk = new byte[8192];
int read;
while ((read = source.Read(chunk, 0, chunk.Length)) > 0)
{
    if (buffered.Length + read > MaxBytes)
        throw new InvalidDataException("message too large");
    buffered.Write(chunk, 0, read);
}
buffered.Position = 0;
var obj = Serializer.Deserialize<MyMessage>(buffered);
```

Buffering is not the cost it looks like here: you have capped it, and the reader then knows the exact
size, which is the best position for it to be in. If you would rather not buffer, the equivalent is a
small `Stream` wrapper that counts bytes and throws once it passes your ceiling.

### From a buffer

When deserializing from `byte[]`, `ReadOnlyMemory<byte>` or `ReadOnlySequence<byte>` you already own
the data, so check its length before you call - and the reader knows the exact size regardless, so it
can reject implausible prefixes without help.

### At the host

If you are behind ASP.NET Core or gRPC, the cheapest limit is usually the one you never write
yourself: Kestrel's `MaxRequestBodySize`, or `MaxReceiveMessageSize` for gRPC. Set those from your
memory budget using the ratios above.

## Practical guidance

- **Size your limits from the memory budget, not the wire budget.** If you can afford *M* bytes of
  memory for a deserialized message, a wire limit somewhere around *M*/24 is a safe starting point
  for messages containing large packed varint collections, and you can raise it considerably if you
  know your shapes.
- **Use `DataFormat.FixedSize` for large numeric collections** if you want a predictable 1:1 ratio.
  It costs more bytes on the wire for small values, but the memory cost stops being a multiple of
  the payload size. This is worth considering for anything reading numeric arrays from an untrusted
  source.
- **Constrain nesting with `TypeModel.MaxDepth`** (default 512) if you accept arbitrary messages;
  deeply nested data costs stack and bookkeeping independently of payload size.
- **Prefer bounded readers where you can.** A `ReadOnlySequence<byte>`, a `MemoryStream`, or a stream
  read with its true length lets the reader see how much data actually exists, so an implausible
  length-prefix is rejected immediately rather than being taken at face value. An unbounded `Stream`
  cannot offer that; reads there proceed incrementally instead of failing up front.
