# Support for `null` values and empty collections

For many common scenarios, protobuf-net *just works* with null values; for example:

``` c#
[ProtoMember(5)]
public int? AccountId {get;set;} // optional primitive

[ProtoMember(7)]
public Metadata Metadata {get;set} // optional sub-message
```

The values will only be serialized if they are non-null, and will be deserialized correctly. But for more nuanced scenarios, or where
cross-platform / schema compatibility is required, we need to understand a bit more clearly what is happening. This is complicated
because although the protobuf *data* specification has not changed, at the protobuf *schema, library and tooling* level, protobuf-net
needs to consider multiple things:

- that protobuf intentionally has no concept of null as a first-class value
- how scalars, sub-messages, and `repeated` data is encoded
- the meaning of default values in protobuf
- the differences between `proto2` and `proto3`
- the later addition of "field presence" to `proto3`
- the existence of `wrappers.proto`

I'm not going to attempt to cover all of these things in detail! However, we can give a flavour. If we just consider `proto3`, and a
simple integer:

``` proto
syntax = "proto3";
message SomeMessage
{
    // implicit zero default; always conceptually has a value; only
    // written to the payload if the value is non-zero
    int32 x = 3;

    // optional === "field presence"; tracks whether the value is
    // implicitly zero vs explicitly specified; only written to
    // the payload if explicitly specified, even as zero
    optional y = 4;
}
```

We can see, then, that C#'s `int?` (or `Nullable<int>` more generally) maps very well to the second case (and indeed, that's
exactly how protobuf-net interprets it), but we need to consider that
[field presence](https://github.com/protocolbuffers/protobuf/blob/master/docs/field_presence.md) (`optional` in `proto3`) is
a *very recent* addition; before that, in raw `proto3` there was no good way to convey "may have a scalar value", as it was
impossible to know whether a zero/empty value meant "was not specified" vs "was specified: it was explicitly zero/empty".

Because of this, historically the protobuf community has made use of
[`wrappers.proto`](https://github.com/protocolbuffers/protobuf/blob/master/src/google/protobuf/wrappers.proto). Then, instead
of having an `int32`, you would use an `Int32Value`, which is a well-known message that *has* an `int32`:

``` c#
syntax = "proto3";
import "google/protobuf/wrappers.proto";
message SomeMessage
{
    // if missing, nothing is written to the payload; if specified, a message-wrapper is created,
    // with, internally, a zero-defaulted value at field 1, i.e. "int32 value = 1;"; if the value
    // is zero, then as before, the field is not explicitly written to the payload
    .google.protobuf.Int32Value z = 5;
}
```

It has always been possible to determine the existence (or not) of a *message*, so cross-platform people using `wrappers.proto`
now had a way to encode a meaning like `int?`, long before the introduction of "field presence".

It must be emphasized, though, that the encoding here is very different! If we want protobuf-net to have good support for
cross-platform scenarios, we should be able to handle both the older `wrappers.proto` usage, and the "field presence" usage,
which is why the following is now possible:

``` c#
[ProtoContract]
class SomeMessage
{
    // think: int32 x = 3;
    [ProtoMember(3)]
    public int X {get;set;}

    // think: optional int32 y = 4;
    [ProtoMember(4)]
    public int? Y {get;set;}

    // think: .google.protobuf.Int32Value z = 5;
    [ProtoMember(5), NullWrappedValue]
    public int? Z {get;set;}
}
```

The `[NullWrappedValue]` here tells protobuf-net to insert a conceptual additional level in the encoding, without actually
having the allocate anything along the way. When used with individual values (we'll discuss collections in a moment), it is
only valid to use this on *scalar values* (which is to say: things that aren't "messages" in the protobuf sense), that are *nullable*. If
protobuf-net encounters this attribute on a value that it isn't writing as a scalar and nullable, **an error will be thrown** - this is
deliberate (vs being silently ignored), so that if we introduce any additional scenarios later we do not need to consider
changes to existing code that executes without error, but would do something different. This feature is not compatible with explicit
default values (`[DefaultValue(...)]`) and an error will be thrown if this is attempted.

## Collections

Another common request is how to encode null values *inside a collection*, for example a `List<int?>`. Since protobuf has
no direct concept of null, a collection (`repeated`) is semantically a sequence of values of a given type; those values
are all explicitly written - for example, if a `repeated int32` collection contains zeros, those zeros are all written -
otherwise, the deserializer would not know to re-insert them. It would be tempting to use, for example:

``` proto
repeated .google.protobuf.Int32Value values = 6;
```

However, this doesn't convey quite the same meaning; if we send the collection `{1, null, 0, 2}`, the serializer would *have*
to write a message wrapper for the null (since it always has to write *something* for any value); it could then choose
to omit the value field, so we'd just have an empty message payload. But because `wrappers.proto` doesn't use "field presence",
then by a strict interpretation: the value for a zero is *also* an empty message payload with no value field
(because of the implicit zero). This means that in the payload there would be no difference between a null and a zero value.
Instead, then, to support this intent, we want to use "field presence" in the internal message; this is not strictly identical
to how `Int32Value` would normally be written, but it expresses our intent more correctly; protobuf-net can apply this to
*scalar and message* types in a collection, including messages, allowing us to convey a range of null values:

``` c#
[ProtoContract]
class SomeMessage
{
    // *similar* to (but with field-presence)
    // repeated .google.protobuf.Int32Value values = 6;
    [ProtoMember(6), NullWrappedValue]
    public List<int?> Ids {get;} = new();

    // likewise, but using field-presence in an artifical
    // message type that has: SomeOtherMessage value = 1;
    [ProtoMember(7), NullWrappedValue]
    public List<SomeOtherMessage> Items {get;} = new();

    // as with Items, but applied to the value portion of the
    // key/value pairs
    [ProtoMember(8), NullWrappedValue]
    public Dictionary<int, SomeOtherMessage> KeyedItems {get;} = new();
}
```

Without `[NullWrappedValue]`, an error will be thrown when encountering a null value; with `[NullWrappedValue]`, any
scalar or message type will be accepted (but not nested collections). When used with a dictionary type (`map` in protobuf),
the wrapping is applied to the value *only*; the key cannot be null-wrapped.

## What about `SupportNull` ?

If you're familiar with the history of protobuf-net, you may be aware of the v2 feature `SupportNull`, which allowed use
of nulls in collections; with the addition of `[NullWrappedValue]`, we can re-introduce this support, with a few notes:

- `SupportNull` *only* has an effect on collections, and this remains the case (for backwards compatibility); it is silently ignored on scalar/message values
- `SupportNull` uses (for reasons lost in the depth of time) "group" rather than "length-prefix" encoding (which should be our default, for cross-platform compatibility reasons)

We can now, then, declare a `SupportNull`-compatible collection using attributes:

``` c#
[ProtoContract]
class SomeMessage
{
    [ProtoMember(7), NullWrappedValue(AsGroup = true)]
    public List<SomeOtherMessage> Items {get;} = new();
}
```

On the topic of `AsGroup`: note that protobuf-net is forgiving and when *deserializing* can interchangeably accept *either* length-prefixed or group encoding, so for usage that *only* uses protobuf-net; however,
the bytes from *serializing* will be different between the two, which may upset other non-protobuf-net consumers. Most serializers will prefer
non-grouped data, so it may be worth considering *removing* the `AsGroup = true` here (although vexingly, [it has performance advantages](https://github.com/protocolbuffers/protobuf/issues/9134)).

## Null collections

In the above, we can see that `[NullWrappedValue]` applies to the *values* in a collection; sometimes - much more rarely - we
wish to apply the same logic to the *collection itself*, to distinguish *null* collections from *empty* collections. Protobuf has
no way of expressing an empty collection, but we can artificially invent an additional message layer that *has* a collection

- for null collections, nothing is written
- for empty collections, an empty message wrapper is written
- for non-empty collections, a message wrapper with items at field 1 is written

This is achieved by:

``` c#
[ProtoContract]
class SomeMessage
{
    [ProtoMember(7), NullWrappedCollection]
    public List<SomeOtherMessage> Items {get;} = new();
}
```

As before, a runtime fault is thrown if `[NullWrappedCollection]` is encountered on an unexpected type; `[NullWrappedCollection]` *also*
supports the same `AsGroup` concept (although it is not expected to be used in many scenarios), and the same comments on `AsGroup` apply from
the previous section. Conveniently, `[NullWrappedCollection]`
can be combined with `[NullWrappedValue]` without difficulty, as they apply at different scopes.

With these features, most common null scenarios can be conveniently and robustly handled.
