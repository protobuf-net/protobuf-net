# Connect (connectrpc.com) for protobuf-net — findings

Working notes for a possible Connect implementation, in the same spirit as `notes/aot/findings.md`.

> **Handover** (2026-09-11). Branch `marc/connect`, **current with `main` as of `9acf850f`** — the
> dependency sweep and the xunit.v3/Microsoft.Testing.Platform move to the .NET 11 SDK. Merged rather
> than rebased, because the branch is pushed. Everything was re-measured afterwards and **nothing
> moved**: 33 IL warnings, a byte-identical 15,443,448 native binary, 9/9 and 7/7. The net8.0 ILC packs
> are TFM-pinned, so the SDK bump does not reach the output. **Nothing has been built or run** — this is a
> desk investigation only, and no code exists. Protocol facts are from the published spec and from
> connect-go's source, cited inline; everything about *our* side is either read out of this repo or
> flagged as an assumption. **§12 lists what is unverified; read it before acting on any of this.**
>
> **The four findings that would change a decision:**
>
> | | |
> | --- | --- |
> | gRPC's HTTP/2 requirement is *only* trailers | Connect uses none — `trailer-` headers on unary, a final enveloped JSON message on streams. Unary and single-direction streaming run on HTTP/1.1; only bidi needs HTTP/2 |
> | **there is no .NET implementation** | official: Go, ES/Node, Swift, Kotlin, Python, Dart; community: Scala. Searched specifically; nothing for .NET |
> | **JSON is optional, and binary is the default** | connect-go *clients* default to binary; the conformance suite takes `features.codecs: [CODEC_PROTO]` and skips the JSON matrix. So the long pole is off the critical path — §4 |
> | the existing gRPC proxies are protocol-blind | every generated proxy calls `Reshape.*(…, this.CallInvoker, …)`. A `ConnectCallInvoker : CallInvoker` gives Connect to every existing protobuf-net.Grpc client with **zero** generator or consumer changes — §8(1) |
>
> **Considered and dropped**, so they are not rediscovered as oversights — both in §3b:
> build-time descriptor/schema emission (nothing in Connect consumes a schema at run time), and
> server reflection (optional, ecosystem convention rather than protocol, and already an open item
> against `protobuf-net.Grpc.Reflection`).
>
> **§13 is measured, not read**: a live binary-codec unary call to `demo.connectrpc.com` over
> **HTTP/1.1**, with the bare-body request/response and the JSON error shape confirmed by hexdump.
> The protocol reading in §2 is correct.
>
> **Stage 0 is done — §15.** `src/protobuf-net.Connect` + `src/ConnectProbe` read **7/7** against
> `demo.connectrpc.com`, as a JIT run *and* as a native AOT binary (7.3 MB, 33 IL warnings, **none of
> them ours**). Code-first protobuf-net bytes interoperate with connect-go with no `.proto` anywhere.
>
> **All four method shapes work — §22, §23, §24 — plus routing prefixes (§25) and several services per
> container (§26).** `AotConnectSmoke` reads **19/19**, JIT and native,
> and duplex is proven to *interleave* rather than merely complete. Native AOT is **33 IL warnings
> across every shape added** - streaming contributed no annotation debt at all.
>
> **Server-streaming is done — §22**, verified in both directions against connect-go. Four of the five
> §14.1 constraints held unchanged; the fifth ("one error path") held in substance but was phrased
> wrongly — see §22. `AotConnectSmoke` 13/13, `ConnectProbe` 9/9, native AOT still 33 IL warnings.
>
> **Stage 1 is done — §16.** `src/protobuf-net.Connect.AspNetCore` + `src/AotConnectSmoke` read
> **8/8**, JIT and native AOT, and the server is verified from outside .NET with `curl`. It is served
> over **HTTP/1.1**, from a plaintext Kestrel endpoint that could not serve gRPC at all. The entire
> ASP.NET Core server added **zero** IL warnings.
>
> **So the MVP asked for is met**: a defined service, a working ASP.NET Core server, a working client.
> `src/AotConnectSmoke/HandWritten.cs` is the generator's target output, written by hand and ready to
> review — that review is the next decision point, ahead of §14 stage 2 (the generator).
>
> **The vocabulary is decided — §17.** protobuf-net.Grpc's own `[Service]` and `CallContext`, so
> **an existing protobuf-net.Grpc contract is served over Connect with no edit**. Cost: zero IL
> warnings and 25 KB. It needed a `Grpc.Core.ServerCallContext` over `HttpContext`, and the runtime
> libraries reference only `Grpc.Core.Api` - protobuf-net.Grpc appears in *generated* code, which is
> what keeps the v2/v3 `TypeModel` collision in the consumer's project where it resolves normally.

## 1. What Connect is, and why it is interesting here

Connect is a CNCF sandbox project from Buf: a small RPC protocol, plus implementations, designed so
that one server can speak **Connect, gRPC and gRPC-Web** on the same port, and so that the Connect
protocol itself is plain HTTP — POST a body, get a body back, errors are HTTP status codes plus a
JSON object. Official implementations: Go, TypeScript/JS (web and Node), Swift, Kotlin, Python, Dart.

The reason it is worth our attention is narrower than "another RPC framework", and it is this:

> **gRPC's hard dependency on HTTP/2 comes from exactly one thing — HTTP trailers — and Connect
> removes it.** Connect carries trailing metadata as `trailer-`-prefixed *response headers* on unary
> calls, and inside a final enveloped JSON message on streams. It uses no HTTP trailers anywhere.

Every operational complaint about gRPC in .NET descends from that one requirement: the proxy that
won't forward it, the load balancer that downgrades, the browser that can't reach it, and — concretely
in Kestrel — the fact that a **plaintext** endpoint cannot serve HTTP/1.1 and HTTP/2 at once, because
without TLS there is no ALPN to negotiate with, so you must pick one for the port. Connect makes all of
that go away for unary, client-streaming and server-streaming. Only bidirectional streaming still needs
HTTP/2.

The second reason: **there is no .NET implementation, official or community.** Searched for one
specifically; found Go, ES, Swift, Kotlin, Python, Dart officially and a Scala community port
(`connect-rpc-scala`), and nothing for .NET. Buf's own roadmap has historically solicited collaborators
for other languages. So this is a real hole, and protobuf-net is a plausible occupant of it: we already
have the code-first contract model, the `[Service]` shape analysis, and — since the AOT work — a Roslyn
generator that emits both client proxies and server bindings.

## 2. The protocol, in enough detail to size the work

From <https://connectrpc.com/docs/protocol/>.

### Unary

- `POST /[prefix/][package.]ServiceName/MethodName` — **the same path shape gRPC uses.**
- `content-type: application/proto` or `application/json`.
- Request body is the **bare message**. No envelope, no length prefix. (gRPC always has the 5-byte
  prefix; gRPC-Web unary does too. Connect unary is simpler than both.)
- `connect-protocol-version: 1` — recommended on clients; servers/proxies *may* reject requests without
  it with 400.
- `connect-timeout-ms` — positive integer, ≤10 digits. Absent means infinite.
- `content-encoding` / `accept-encoding`: `identity`, `gzip`, `br`, `zstd`, or custom. An unsupported
  request encoding is answered `unimplemented` with the supported list.
- Custom metadata is ordinary headers; binary metadata is a `-bin`-suffixed name with unpadded base64.
  `connect-` prefixed names are reserved.
- Success: 200, same content-type, bare response message. Trailing metadata arrives as `trailer-`
  prefixed response headers.
- Failure: non-200, `content-type: application/json`, body is
  `{"code": "...", "message": "...", "details": [{"type","value","debug"}]}`.

### Errors

Sixteen codes, the gRPC set, spelled snake_case (`invalid_argument`, `deadline_exceeded`,
`failed_precondition`, …), each with a fixed HTTP status (`not_found`→404, `permission_denied`→403,
`resource_exhausted`→429, `unimplemented`→501, `canceled`→**499**, …). There is also an inference table
for the other direction, so a bare 502 from an intermediary becomes `unavailable` rather than a parse
failure. `details[].value` is base64 of a serialized protobuf message identified by `type`.

### Streaming

- `content-type: application/connect+proto` or `application/connect+json`. Anything else beginning
  `application/connect+` that we don't know is 415.
- `connect-content-encoding` / `connect-accept-encoding` — note the *different header names* from unary.
- **Response status is always 200**, even on failure. The error is in the terminating message.
- Body is a sequence of envelopes:

  ```
  [1 byte flags][4 bytes length, big-endian][length bytes of message]
  ```

  **That is byte-identical in layout to gRPC's framing.** The flag byte differs in meaning: bit 0 is
  "compressed" (as gRPC's whole byte is), bit 1 is "end of stream", bits 2–7 reserved zero. So a framing
  reader/writer is shared between the two protocols, which matters if we ever do both.
- The final envelope (flags = 2) carries `EndStreamResponse`: `{"error": {...}, "metadata": {"name":
  ["v1","v2"]}}`, or `{}` on clean success. This is where trailers live for streams.
- Compression contexts are **not** carried across message boundaries.

### HTTP versions

Unary, client-streaming and server-streaming: HTTP/1.1 or HTTP/2. Bidirectional: HTTP/2 required.

### GET, and why it needs schema metadata

An RPC annotated `idempotency_level = NO_SIDE_EFFECTS` may be invoked with GET:
`?connect=v1&encoding=proto&base64=1&message=<base64url>` (plus `compression=`). `connect-timeout-ms`
stays a header. Servers must tolerate unknown query parameters. Clients opt in.

Two things follow that matter to us:

- This is the **only** part of the Connect protocol that depends on proto-level schema metadata. It is a
  method option, i.e. a compile-time property of the contract. Code-first would need an attribute of our
  own (there is no protobuf-net.Grpc equivalent today).
- The spec asks for **deterministic** codec output so the URL is a stable cache key. protobuf-net is
  deterministic in field order, but **map members are not obviously deterministic** — `Dictionary<K,V>`
  enumeration order is not a documented guarantee. Flag this before claiming GET+caching works; it may
  mean refusing GET for contracts containing maps, or sorting keys on that path.
- Caching is *not* automatic: `Cache-Control` is the handler's job.

## 3. How services are defined — proto-first tooling, schema-free protocol

Asked directly, and the answer is good for us.

**The tooling is proto-first**: `protoc-gen-connect-go`, `@connectrpc/protoc-gen-connect-es`, etc., and
the docs consistently present Protobuf as "the specification and documentation".

**The protocol is not.** On the wire, a Connect call is a path, a content-type and a body. The service
and method names are strings in a URL; the message shape is whatever the named codec says it is. There
is no descriptor exchange, no schema negotiation, and no reflection requirement. That is exactly the
position we are already in with gRPC, where protobuf-net.Grpc has shipped code-first for years against a
proto-first ecosystem.

So **code-first is very likely to work, and for the same reasons it works today** — the interop contract
is "both ends agree on field numbers and method names", not "both ends parsed the same .proto". Three
caveats, in order of seriousness:

1. **`application/json` moves the agreement from field *numbers* to field *names*.** Canonical Protobuf
   JSON is defined against a schema: lowerCamelCase field names (accepting the original), enums by name,
   64-bit integers as strings, `bytes` as base64, special forms for the well-known types. Code-first
   would derive those names from C# members instead of from a .proto. That is fine and self-consistent,
   but it means cross-language JSON interop needs the .proto to be *shared*, precisely as binary interop
   needs the field numbers to be shared. Not a new problem, but a more visible one — JSON is the format
   people read, so a name mismatch is louder than a number mismatch.
2. **`idempotency_level` has no code-first spelling.** Needs an attribute. Small.
3. **We cannot currently emit a .proto from an AOT model.** `TypeModel.GetSchema(SchemaGenerationOptions)`
   is `virtual` and the base **throws `NotSupportedException`** (`protobuf-net.Core/Meta/TypeModel.cs`
   around line 1940); only `RuntimeTypeModel` implements it. A `[ProtoModel]`-generated model therefore
   has no schema. **The fix is small and attractive: have the generator emit the schema as a `const
   string` / static property at build time.** It is pure build-time work, needs no reflection, and it is
   independently useful — it is what lets a code-first .NET service hand a `.proto` to the TypeScript
   team, and what would back a Connect/gRPC server-reflection endpoint. I would treat this as a
   standalone task worth doing regardless of whether Connect happens.

## 3b. Is there an inbuilt metadata API? No — and the convention is gRPC's

Asked specifically, because a WCF-style `mex` endpoint would change the schema-emit calculus.

**There is nothing schema-related in the Connect protocol.** No descriptor exchange, no negotiation, no
capability document. A call is a path, a content-type and a body; that is the whole contract. Nothing in
the spec obliges a server to be able to describe itself.

**The ecosystem convention is gRPC server reflection, mounted as an ordinary service.** In Go it is a
separate module, `connectrpc.com/grpcreflect`, which handles `grpc.reflection.v1.ServerReflection` over
any of the three protocols. Two details worth carrying:

- there are **two versions** and the guidance is to mount **both** (`NewHandlerV1` *and*
  `NewHandlerV1Alpha`), because grpcurl and much other tooling still ask for the older one;
- it is what `buf curl`, `grpcurl`, `grpcui` and Buf Studio use to call a service without a local copy
  of the schema. Optional in every sense — services without it are simply called with a schema in hand.

### So the schema workstream evaporates, and that is the useful finding

The first draft of this note used the absence of a `mex`-style API to argue *for* build-time descriptor
emission. That was backwards, and it is worth recording why, because the reasoning is what generalises:
**a schema is only needed at run time by something that serves it, and Connect has nothing that does.**

Walking every candidate consumer:

| wants a schema | when | needs it from the running process? |
| --- | --- | --- |
| the Connect protocol | — | **no** — nothing schema-related is on the wire, ever |
| generating a client in another language | build time | no — an out-of-band `.proto`, consumed by codegen |
| documentation, `buf breaking`, review | build time | no |
| `buf curl` / `grpcurl` with no local schema | run time | yes — **but only via server reflection** |

Only the last row survives, and it is an optional add-on service that is **already owned elsewhere**:
`protobuf-net.Grpc.Reflection` exists (1.2.2, pinned in `src/Directory.Packages.props`) and
`notes/aot/grpc.md` already carries it as an open AOT item. It is not Connect work and should not be
funded as Connect work.

**And the AOT model's missing `GetSchema` does not block the build-time artifact**, which was the other
half of the bad argument. The `.proto` is derived from the *contracts* — attributed POCOs — not from the
model; `RuntimeTypeModel.Create()` + `Add(typeof(Foo))` + `GetSchema()` on a dev machine or in a test
produces it today, on a JIT runtime, whatever the shipping model looks like. A consumer who has gone
all-in on `[ProtoModel]` has not lost the ability to emit a schema; they have only lost the ability to
ask the *model* for one, which nothing needs to do.

Having `dotnet build` drop a `.proto` beside the assembly would be pleasant, and emitting descriptor
bytes rather than text would be the right shape for it if it ever happens (server reflection serves
`FileDescriptorProto` bytes, not `.proto` text; protobuf-net.Reflection can already go text →
`FileDescriptorSet` in-process via `Parsers.cs` + `Descriptor.cs`, which is how protogen works). But it
is a protobuf-net convenience with no Connect dependency, and it comes out of this plan entirely.

### The one schema-adjacent thing Connect *does* need, and it is small

Error details are `{"type": "fully.qualified.Protobuf.MessageName", "value": "<base64 proto>"}`. So rich
errors need a **name ↔ type map**, in both directions: the producing side needs each detail type's
fully-qualified protobuf name, and the consuming side needs to turn a name back into something it can
deserialize.

It is only needed for *rich* errors; `code` and `message` alone need nothing. But it is worth knowing
exactly what that name is, because it is **not ours to define**.

#### It is `Any`'s namespace, with the URL prefix stripped

Read out of connect-go's `error.go` rather than inferred:

```go
type ErrorDetail struct {
	pbAny    *anypb.Any
	pbInner  proto.Message
	wireJSON string
}

func (d *ErrorDetail) Type() string {
	return typeNameForURL(d.pbAny.GetTypeUrl())
}

func typeNameForURL(url string) string {
	return url[strings.LastIndexByte(url, '/')+1:]
}
```

with `defaultAnyResolverPrefix = "type.googleapis.com/"`, and the comment *"proto.Any tries to make
messages self-describing by using type URLs rather than plain type names… To hide this from users, we
should trim the URL prefix"*.

So, precisely:

- a Connect error detail **is a `google.protobuf.Any`**. The JSON `type`/`value` pair is a *rendering*
  of one, not a parallel concept;
- the `type` string is the **global protobuf fully-qualified name** — `package.Message`, from the
  `.proto` namespace. It is **not** relative to a local model, registry or schema, and there is no
  scoping or aliasing;
- the trim is `LastIndexByte('/')`, so it is **lossy with respect to `Any`**: a custom (non-
  `type.googleapis.com`) type-URL prefix collapses and cannot be reconstructed. The reverse direction
  necessarily re-prepends the default prefix;
- and it has to be an `Any`, because the same details must survive a **gRPC** hop — gRPC's rich error
  model is `google.rpc.Status { repeated google.protobuf.Any details }`, and Connect servers speak both.

**Therefore doing Connect error details properly is doing `Any` properly, minus the prefix** — it is the
same gap, not a Connect-shaped extra.

#### Which lands on a real weakness: our fully-qualified names are not stable

Probed, and it is worse than "we allow a name to be set":

- **protobuf-net has no `Any` support whatsoever.** `grep -rn "type.googleapis.com\|TypeUrl\|type_url"
  src --include=*.cs` returns **nothing**.
- `[ProtoContract]` exposes `Name` and `Origin` — **there is no `Package`**.
- A name counts as qualified **only if it starts with a dot**. `MetaType.GuessPackage()` is four lines,
  commented *"very speculative; turns .Foo.Bar.Blap into Foo.Bar"*, and returns `null` outright when
  `s[0] != '.'`.
- Otherwise the package comes from `SchemaGenerationOptions.Package`, or is inferred **across the whole
  requested type set** at `GetSchema` time — and `RuntimeTypeModel.GetSchema` sets `package = null` when
  the candidates disagree.

So the fully-qualified name protobuf-net produces today is **a function of which types you asked about,
computed per `GetSchema` call**, and may legitimately come out with no package at all. That is fine for
"suggest me a `.proto`". It is not fine for an identity that goes on the wire, where two ends must agree
and the value must not depend on the caller's type set.

Fixing that is a protobuf-net change with no Connect dependency — most likely a `Package` on
`[ProtoContract]` (and/or an assembly-level default, following the `[CompatibilityLevel]` precedent of
type → module → assembly resolution), plus a per-type qualified name that does not consult the rest of
the model. It is the prerequisite for `Any`, for gRPC rich errors, and for Connect error details alike,
which is three reasons to do it once.


## 4. JSON: optional, and binary is the *expected* default

`grep -ril json src/protobuf-net.Core src/protobuf-net --include=*.cs` returns **nothing**. protobuf-net
has no JSON support of any kind. This turns out to matter much less than it first looks.

**Binary is not a degraded mode.** Checked deliberately, because the first pass of this note overstated
it:

- **The protocol negotiates.** `content-type` names the codec; a codec the server does not have is
  answered **415**. A proto-only server is a legal Connect server, not a fudge.
- **connect-go clients default to binary Protobuf.** JSON is opt-in on the client (`WithProtoJSON()`);
  handlers accept both automatically *when the runtime has both*. So for service-to-service — .NET to
  .NET, .NET to Go — binary is the path callers take by default, and JSON never enters it.
- **The conformance suite has an explicit declaration for this.** Its config has a `features` block with
  a `codecs` axis (`CODEC_PROTO`, `CODEC_JSON`); *"If not configured, support is assumed for both"*, so
  declaring `codecs: [CODEC_PROTO]` **excludes the JSON cases from the generated matrix**. A binary-only
  implementation can therefore claim a real, externally-measured conformance result rather than an
  asterisked one.

So JSON buys exactly one thing, and it is a big thing but a *separable* one: **browsers, `curl`, and the
network inspector.** That is the "open devtools and read it" pitch, and it is the reason Connect-for-Web
exists. Without JSON we have a Connect implementation that is fully useful for services and unusable
from a browser — which is a coherent v1, and reorders the plan below considerably.

When it is built, it means canonical Protobuf JSON, emitted by the same Roslyn generator, as a **third
emit shape in `ProtoModelGenerator`** alongside the binary read/write. The rules are well-defined and finite, and
most of the hard cases in general proto-JSON (`Any`, `Struct`/`Value`, `FieldMask`) do not arise in
code-first contracts. The ones that do: name mapping, enum-by-name, 64-bit-as-string, bytes-as-base64,
`Nullable<T>`/presence, the compatibility-level BCL types (`DateTime`→`Timestamp` RFC3339 string,
`TimeSpan`→`Duration` `"1.5s"`), maps as JSON objects with stringified keys, and repeated as arrays.

**It belongs in protobuf-net, not in a Connect package.** JSON is a serializer feature; people would
want `MyModel.Instance` to round-trip JSON with no RPC anywhere near it. That also means it is separable:
Connect can ship binary-only first and gain JSON when the codec lands.

Sizing honestly: smaller than the binary AOT generator, larger than any single feature in it. Assume it
is the majority of the total effort of "Connect in .NET, done properly" — which is precisely why it being
*optional* is the most useful fact in this document.

## 5. Client: `HttpClient`, not raw

Unambiguous. Connect is HTTP-native, so a raw-socket client would mean reimplementing HTTP for no gain,
and would throw away HTTP/1.1+2+3, proxies, `DelegatingHandler` pipelines, `SocketsHttpHandler` pooling,
`IHttpClientFactory`, auth handlers and every diagnostic anyone has. The entire argument for Connect is
that it is ordinary HTTP; a client that does not use the ordinary HTTP stack is not taking the win.

Details worth planning for:

- **Unary maps onto our existing serialization shape almost exactly.** The generated gRPC marshaller in
  `Basic.output.cs` already does `IMeasuredProtoOutput<IBufferWriter<byte>>.Measure(value)` →
  `SetPayloadLength(length)` → `GetBufferWriter()` → `Serialize`. A custom `HttpContent` overriding
  `TryComputeLength` and `SerializeToStream(Async)` wants precisely that: known length, then write into
  the stream/writer. So `Content-Length` is free and there is no buffering.
- **Streaming needs `HttpCompletionOption.ResponseHeadersRead`** and, for client-streaming and duplex, a
  request `HttpContent` that writes incrementally. `SocketsHttpHandler` supports duplex over HTTP/2.
  Over HTTP/1.1 you get client-streaming and server-streaming but not full duplex — which matches what
  the protocol says anyway.
- **Browser (Blazor WASM) is a separate risk.** The fetch-based handler's streaming support is narrower
  than `SocketsHttpHandler`'s; assume server-streaming works and treat client-streaming as unverified.
- Cancellation → `connect-timeout-ms` plus `CancellationToken`; deadline maps cleanly.

## 6. Server: Kestrel, but target ASP.NET Core rather than Kestrel

**HttpListener is not an option.** It is explicitly compat-only in .NET — critical fixes, no new work,
no modern protocol support, and the standing guidance is to use Kestrel. No HTTP/2 means no bidi, and no
future means no reason to start.

**HTTP.sys is a real second**, but you get it for free and should not think about it: if we sit on
ASP.NET Core's abstractions (`HttpContext`, endpoint routing) rather than on Kestrel's internals, then
Kestrel, HTTP.sys, IIS *and* `TestServer` all work with one implementation. That is the actual design
guidance here — **"Kestrel is a must" resolves to "ASP.NET Core is a must", and Kestrel comes with it.**

**A "raw" option is still worth offering, cheaply**: expose the handler as a bare `RequestDelegate`
(or a `Func<HttpContext, Task>`) so it can be hung off `WebApplication.CreateEmptyBuilder`, a
hand-configured `KestrelServer`, or someone's existing middleware graph with no `MapXxx` involved.
That costs nothing if the core is written as a handler and the routing is a thin layer over it.

The AOT position is good: ASP.NET Core Native AOT support covers Kestrel, routing, CORS, auth, rate
limiting, output caching and WebSockets; the unsupported list is MVC, Blazor Server, SignalR, Session.
Minimal APIs are "partial" because `RequestDelegateFactory` is reflective — **writing raw
`RequestDelegate` handlers bypasses that entirely**, which is what we want anyway. `CreateSlimBuilder`
is the template, noting it excludes HTTPS and HTTP/3 by default.

And the thing Connect gets that gRPC does not: **a Connect endpoint is an ordinary HTTP endpoint**, so
CORS, `[Authorize]`, rate limiting, output caching (interesting for GET RPCs), response compression,
YARP and everything else compose with it without special cases, and it does not need the HTTP/2
enforcement that makes `Grpc.AspNetCore.Server` reject an HTTP/1.1 request.

## 7. Bindings and registration

Reuse the shape people already know. The gRPC generator's surface is
`[ProtoGrpc(Model = typeof(MyModel))] partial class MyServices : ClientFactory` seeded by
`[ProtoService(typeof(IContract), typeof(Impl))]`, producing `MyServices.Instance.CreateClient<T>(...)`
and an `AddMyServices()` extension. A Connect equivalent should be the same shape —
`[ProtoConnect(Model = typeof(MyModel))]` — differing only where the protocol forces it.

Two decisions I would make up front:

- **One endpoint per method, not one per service.** The *cost* of this is nil — we generate the wiring,
  so emitting N `Map` calls instead of one is the same amount of consumer-visible work, and the choice
  is invisible to anyone using it. But the choice still matters, and not for the reason it looks:

  **ASP.NET Core's per-request features attach to the matched `Endpoint` object, and the middleware that
  reads them runs before our handler does.** Authorization, CORS policy selection, rate limiting and
  output caching all resolve their configuration from `HttpContext.GetEndpoint()?.Metadata` in
  middleware. Route a whole service through one `/{service}/{method}` endpoint and every method shares
  one metadata set — so a per-method `[Authorize]` cannot be expressed through the framework at all, and
  we would have to re-implement authorization inside the handler against `IAuthorizationService`. That
  is not a thing we control by wiring it ourselves; it is upstream of us.

  Output caching on idempotent GET RPCs is the other one, and it is one of Connect's actual selling
  points. `Grpc.AspNetCore` maps per method for exactly these reasons.

  So: per method, it is free, and it means the existing endpoint-metadata machinery (`MetadataGather`,
  `AttributeRenderer`, `PBN4019`, and the `src/AotGrpcMetadataDiff` oracle) is directly reusable — a
  large amount of already-solved, already-CI-gated work.
- **Registration emits routing entries directly**, i.e. a generated `MapMyServices(this
  IEndpointRouteBuilder)` that calls `endpoints.Map(pattern, handler)` per method, with the handler a
  static generated `RequestDelegate`. No `IServiceMethodProvider`, no `Grpc.AspNetCore.Server`, no
  reflection, nothing to discover at startup.

Contract parsing — the five method shapes, `CallContext`, `[SubService]`, void/`Empty`, overloads,
closed generics, the WCF markers — is the single biggest reusable asset and should be **shared with
`GrpcProxyGenerator`, not forked**. `Internal/Grpc/` is already under the no-Roslyn-references rule, so
the model types are already in the right shape to be shared; the thing to avoid is a copy that drifts.

Diagnostics: `PBN5xxx` is free. Per the id table in `AGENTS.md`, taken blocks are `PBN0001`–`PBN0026`,
`PBN1000+`, `PBN2001`–`PBN2010`, `PBN3000`–`PBN3013`, `PBN4000`–`PBN4018`. New ids must go in
`AnalyzerReleases.Unshipped.md` or the build fails (`RS2000`).

## 8. Crossover with protobuf-net.Grpc — three distinct levels

This is the part where the existing investment pays, and the three levels are genuinely separable.

### (1) A `CallInvoker`. Highest value, lowest cost — do this first.

Reading `src/BuildToolsUnitTests/Grpc/Data/Basic.output.cs`: every generated client proxy derives from
`Grpc.Core.ClientBase`, holds `Grpc.Core.Method<TReq,TResp>` built from `Grpc.Core.Marshaller<T>`, and
calls `Reshape.UnaryTaskAsync(in context, this.CallInvoker, __op0, request, null)` and friends. The
protocol never appears. **Everything goes through `Grpc.Core.CallInvoker`.**

So a `ConnectCallInvoker : CallInvoker` over `HttpClient` gives the Connect protocol to *every existing
protobuf-net.Grpc client* — reflective and AOT-generated alike — with **zero generator changes and zero
consumer code changes** beyond constructing a different channel. Interceptors, `CallContext`, deadlines,
DI registration and the `[ProtoGrpc]` migration story all come along unaltered.

Cost: implementing `AsyncUnaryCall<T>` and the three streaming call types, plus `SerializationContext` /
`DeserializationContext` subclasses (both are public abstract in `Grpc.Core.Api` and Grpc.Net.Client does
exactly this internally). Small and well-bounded. It is also the ideal first spike, because it can be
pointed at connect-go's reference server on day one.

Limit: `Marshaller<T>` is codec-fixed, so this is **binary only** — no JSON, no content negotiation,
no GET. Service-to-service, not browsers. And it keeps a dependency on `Grpc.Core.Api` (small, AOT-clean).

### (2) A server middleware over existing gRPC endpoints. Cheap, but not the destination.

The precedent is `Grpc.AspNetCore.Web`, which translates gRPC-Web ↔ gRPC in front of unmodified gRPC
endpoints, including relaxing the HTTP/2 check that otherwise answers *"Request protocol of 'HTTP/1.1'
is not supported"*. A `ConnectMiddleware` doing the same translation — add/strip the 5-byte prefix,
convert `Status` to the JSON error body, move trailers into `trailer-` headers — would give Connect to
*any* grpc-dotnet service, including Google.Protobuf ones, for very little code.

But the same `Marshaller<T>` limitation bites harder here: by the time the request reaches the endpoint,
the codec is fixed, so JSON and content negotiation are not reachable, and neither is GET. It is an
adoption ramp, not an architecture. I would build it as a sample or not at all.

### (3) First-class Connect: own generator, own endpoints, no `Grpc.*` dependency.

This is where JSON, GET, browser support and true AOT cleanliness live. Nothing on the path needs
`Grpc.Core.Api`, `Grpc.AspNetCore.Server`, `CallInvoker`, `DynamicStub` or `MakeGenericType`. It is the
version that matches "done with ref-emit and reflection" completely, and it is the version worth
shipping.

**Recommendation: do (1) and (3); skip (2).** Share the contract model between the Connect and gRPC
generators.

## 9. Where it should live

The generator half should live **here**, in `protobuf-net.BuildTools`, for the reason a third generator
in that assembly is nearly free: the golden-test harness, the fixture conventions, the incremental-cache
discipline, the `Internal/*` no-Roslyn-references rule, the release-tracking gate and the contract
parsing all already exist. Forking them into another repo would duplicate a great deal of hard-won
machinery.

The runtime half (`protobuf-net.Connect`, `protobuf-net.Connect.AspNetCore`) can live either place.
There is precedent for splitting it — `[ProtoGrpc]`/`[ProtoService]` are real API in **protobuf-net.Grpc**
and the generator here matches them **by full name**, exactly so that the generator need not reference
the runtime. The same trick works for `[ProtoConnect]`.

Package shape: `protobuf-net.Connect` (client, `HttpClient`-based, no ASP.NET Core dependency) and
`protobuf-net.Connect.AspNetCore` (server). Keeping the client free of the ASP.NET Core shared framework
matters — a console/mobile/WASM client must not drag it in. The JSON codec, if and when it happens, is
a protobuf-net feature and belongs in **this** repo regardless: `protobuf-net.Core` plus a third emit
shape in `ProtoModelGenerator`.

### Single repo now, split later — and the repo's own history says so

**Work in this repo, on a branch, and split only when there is a reason to.** The end state may well be
a sibling repo; starting there would be paying the cost of the split before earning any of its benefit.

The decisive argument is the cross-repo release loop, and it is documented in this repo rather than
hypothesised. `src/AotGrpcSmoke` takes `protobuf-net.Grpc` as a **`PackageReference`**, deliberately —
its comment says *"protobuf-net.Grpc comes from the package, which is the point - this proves the
generated code binds to the shipped runtime surface"*. That is exactly right for a **settled** API. For
an **unsettled** one it is a hard serialisation of the work: `notes/aot/grpc.md`'s handover has a whole
table headed *"Landed elsewhere, and this branch depends on it"*, gating the gRPC generator on
protobuf-net.Grpc 1.3.6 plus three merged PRs. Every API adjustment becomes release-a-package-then-
consume-it, and `Directory.Packages.props` still carries the floor comment explaining which version
introduced what.

The same conclusion is already recorded as a *decision* in `AGENTS.md`: `[ProtoModel]`/`[ProtoSerializable]`
were **generator-owned** — emitted via `RegisterPostInitializationOutput`, one internal copy per
assembly — *"while the shape was still moving"*, and became real Core API only once a cross-assembly
requirement forced it. A Connect trigger attribute is in precisely that position now.

The split stays cheap because of three properties that already hold:

- **the generator never moves.** It has to be in `protobuf-net.BuildTools` either way, so the split is
  only ever about the runtime half;
- **the generator matches trigger attributes by full name, not by symbol** — a rule `AGENTS.md` insists
  on keeping. So moving `[ProtoConnect]` between assemblies is invisible to it, provided the namespace
  and name are stable;
- **CI globs `src\*\*.csproj`**, so new projects here are picked up automatically, and removing them
  later is equally automatic.

And the incremental cost of hosting it here is close to zero: this repo already builds ASP.NET Core
projects (`protobuf-net.AspNetCore`, `protobuf-net.TestWeb`, `AotGrpcSmoke` on `Microsoft.NET.Sdk.Web`)
and already runs native-AOT smoke tests on two RIDs in CI. A `src/AotConnectSmoke` slots into an
existing pattern rather than inventing one.

Concretely: `src/protobuf-net.Connect`, `src/protobuf-net.Connect.AspNetCore`, `src/AotConnectSmoke`,
`src/BuildToolsUnitTests/Connect/Data/*` — with the trigger attributes **generator-owned (post-init,
internal) at first**, exactly as `[ProtoModel]` began, so that no packaging decision is forced until the
shape stops moving. Revisit the split when there is an actual trigger: a divergent release cadence, or a
dependency this repo should not carry.

## 10. Suggested phasing

Reordered from the first draft, now that JSON is known to be declarable-optional: the long pole moves
out of the critical path, and there is a shippable product before it.

0. **Spike: `ConnectCallInvoker`, unary, binary.** Point it at connect-go's reference server. Proves the
   protocol reading is right, and immediately gives every existing protobuf-net.Grpc client Connect.
   Days, not weeks.
1. **Server: generated endpoints, unary + server-streaming, `application/proto`.** Run the official
   conformance suite with `features.codecs: [CODEC_PROTO]`.
2. **Streaming complete + compression + GET/idempotency.** Conformance green *for the declared feature
   set*, across HTTP/1.1 and HTTP/2. **This is a shippable v1** — a fully conformant, fully AOT,
   service-to-service Connect implementation for .NET, which is a thing that does not exist today.
3. **JSON codec in protobuf-net.** The long pole, now taken by choice rather than by necessity. Unlocks
   browsers, `curl` and the network inspector — i.e. Connect-for-Web. Flip `CODEC_JSON` on in the
   conformance config and watch the matrix grow.

**Not on this plan**, having been considered and dropped — see §3b: build-time descriptor/schema
emission (nothing in Connect consumes a schema at run time, and the build-time `.proto` is already
reachable through `RuntimeTypeModel` on a dev machine), and server reflection (optional, ecosystem
convention rather than protocol, and already an open item against `protobuf-net.Grpc.Reflection`).

## 11. The external oracle, which suits how this repo works

`connectrpc/conformance` is an open-source suite that drives *your* client against a reference
connect-go server, and *your* server from a reference connect-go client, across Connect, gRPC and
gRPC-Web. It is a process plus a set of protos, so it is usable from .NET.

That is worth calling out because it is the same shape as the things this repo already trusts —
`AotDifferential`'s corpus, `AotGrpcMetadataDiff`'s oracle: an **external** source of truth that can be
CI-gated and that is capable of failing. A Connect implementation with a conformance percentage is a
very different claim from one with a passing unit-test suite.

It is run as `connectconformance --mode client -- <client>`, `--mode server -- <server>`, or
`--mode both -- <client> ---- <server>`, driven by a YAML config with three top-level keys: `features`
(what we support), `include_cases` and `exclude_cases`. The runner *"first processes your configuration
and uses that to select which test cases are relevant"* — so the declared feature set is the honest
denominator, in the same way `AotDifferential` reports "of the N actually compared".

The axes, which double as a roadmap since each one is a dial we can turn up:

| axis | values |
| --- | --- |
| protocols | `PROTOCOL_CONNECT`, `PROTOCOL_GRPC`, `PROTOCOL_GRPC_WEB` |
| HTTP versions | `HTTP_VERSION_1`, `HTTP_VERSION_2`, `HTTP_VERSION_3` |
| codecs | `CODEC_PROTO`, `CODEC_JSON` — *"if not configured, support is assumed for both"* |
| compressions | `COMPRESSION_IDENTITY`, `_GZIP`, `_BR`, `_ZSTD`, `_DEFLATE`, `_SNAPPY` |
| stream types | `STREAM_TYPE_UNARY`, `_CLIENT_STREAM`, `_SERVER_STREAM`, `_HALF_DUPLEX_BIDI_STREAM`, `_FULL_DUPLEX_BIDI_STREAM` |
| other | TLS, h2c, client certificates, trailers, message receive limits |

Note the protocols axis: the suite would also measure a gRPC or gRPC-Web implementation. If the
`ConnectCallInvoker` spike works, running it in `PROTOCOL_GRPC` mode is a free external check on the
*existing* protobuf-net.Grpc client surface, which nothing currently tests from outside.

## 13. Probed live, 2026-09-11 — the protocol reading is correct

The first things in this note that are **measured rather than read**. `demo.connectrpc.com` runs the
Eliza demo service on connect-go; it is public, always on, and needs no toolchain, which makes it a
day-one oracle for a .NET client.

**Binary unary, forced HTTP/1.1** — the shape the MVP targets:

```
$ printf '\x0a\x02hi' > say.bin          # SayRequest { sentence = "hi" }
$ curl -i --http1.1 -H "Content-Type: application/proto" -H "Connect-Protocol-Version: 1" \
       --data-binary @say.bin https://demo.connectrpc.com/connectrpc.eliza.v1.ElizaService/Say

HTTP/1.1 200 OK
content-type: application/proto
Content-Length: 31

00000000: 0a1d 4869 2074 6865 7265 2e2e 2e68 6f77  ..Hi there...how
00000010: 2061 7265 2079 6f75 2074 6f64 6179 3f     are you today?
```

Confirmed by that, rather than inferred:

- **the request body is the bare message.** `0a 02 68 69` is exactly what protobuf-net emits for
  `[ProtoContract] class SayRequest { [ProtoMember(1)] public string Sentence }`. No envelope, no
  length prefix, no framing of any kind;
- **the response is the bare message**, same deal;
- **HTTP/1.1 works, with `Content-Length` and no trailers anywhere.** This is the claim the whole case
  rests on, and it is now evidence;
- `application/json` works on the same endpoint with the same path, returning
  `{"sentence":"Hello, how are you feeling today?"}` — i.e. codec is purely content-type.

**Errors**, also measured:

| request | response |
| --- | --- |
| malformed body | `400`, `content-type: application/json`, `{"code":"invalid_argument","message":"unmarshal message: …"}` |
| unknown method (`/Nope`) | `404`, `content-type: **text/plain**`, no JSON body at all |

The second row is why the spec carries an HTTP-status → code *inference* table: an unrouted path is
answered by the HTTP layer, not by Connect, so a client must map `404` → `unimplemented` itself rather
than expecting an error object. **A client that assumes every non-200 carries a JSON error will throw a
parse exception on the most ordinary failure there is.** Worth a test.

**Tooling**: `connectconformance` ships as a **single statically-linked binary** per platform on the
GitHub releases page — no Go toolchain — so the external oracle is CI-gatable, in the same way
`AotDifferential` is.

## 14. Path to an MVP

Goal: a service defined the way protobuf-net.Grpc defines one, a working ASP.NET Core server, and a
working client. Staged so that **each stage is falsifiable against something external** before the next
one depends on it, and so that no Roslyn work happens until the shape it must emit is known to work.

> **Status, 2026-09-11.** Stages 0 and 1 are done, and stage 1 grew well past its original scope:
> all four method shapes work, plus routing prefixes. Stage 3's smoke half is done and CI-gated;
> its conformance half is not started. Stage 2 - the generator - has not been started, deliberately:
> the target output it must emit is now complete, and reviewing that is cheaper than reworking a
> generator written to a shape that then moves.

### Stage 0 — client against Eliza. No generator, no abstractions. **Done (§15).**

~50 lines: `HttpClient`, a protobuf-net `[ProtoContract]` pair, POST the bare body, read the bare body.
Point it at `demo.connectrpc.com` (§13). Add the error path — including the `text/plain` 404.

What it proves: our serializer's bytes are interoperable with a reference Connect implementation, and
the request/response/error shapes are understood. What it costs: a day. What it de-risks: everything
downstream. **If this does not work, nothing else is worth starting.**

### Stage 1 — the hand-written target output. **Done (§16, §22–§25).**

Write by hand, for one `[Service]` interface, exactly what the generator will eventually emit: the
client proxy and the server endpoint registration. Get it working .NET → .NET over Kestrel, and *also*
verify the new server against an external Connect **client** (`buf curl`, or `connectconformance` in
`--mode server`).

This is the repo's established method rather than a shortcut — `AotRefGen` exists precisely so that
expected generator output is *derived and reviewable* rather than invented. Here there is no ref-emit to
derive from, so the substitute is: write it, make it work, review it, *then* freeze it as the target.

Reviewing this file is the real decision point on API shape, and it is much cheaper to change here than
after a generator emits it.

### Stage 2 — the generator. **Started; emitting, and the hand-written halves are deleted (§36).**

`ProtoConnectGenerator` in `protobuf-net.BuildTools`, emitting stage 1's file. Golden fixtures under
`src/BuildToolsUnitTests/Connect/Data/` on the existing harness (`*.input.cs` → `*.output.cs` +
`*.txt`, rewritten in-tree on every run). Diagnostics in the free **`PBN5xxx`** block, registered in
`AnalyzerReleases.Unshipped.md` or the build fails (`RS2000`).

Contract parsing is **shared with `GrpcProxyGenerator`, not forked** — the five method shapes,
`CallContext`, `[SubService]`, void/`Empty`, overloads, closed generics. That sharing is the single
largest reason to do this in-repo.

### Stage 3 — conformance, and a smoke test. **Smoke done and CI-gated; conformance not started.**

`src/AotConnectSmoke` on the `AotSmoke`/`AotGrpcSmoke` pattern: a `PublishAot` app that round-trips and
exits non-zero on mismatch, published on both RIDs in the existing CI job. Then `connectconformance`
with a narrow `features` declaration (`codecs: [CODEC_PROTO]`, unary only), widened as stages land.

### 14.1 Unary first, but all five shapes are expected — what that forbids

Decided: the first MVP is unary only, but every method shape is expected in the end, so nothing may be
designed in a way that shuts them out. That is a constraint on **stage 1**, not on stage 0, and it is
close to free if taken at the start and invasive afterwards. The specific traps, each one a thing that
works perfectly for unary and cannot be extended:

- **Do not make the transport buffer-shaped.** The obvious unary implementation is "serialize to a
  `byte[]`, POST it, read a `byte[]` back", which has no seam a framed stream can be threaded through.
  The protocol itself supplies the right abstraction, so take it: **codec** (`proto` / `json`) and
  **framing** (none / enveloped) are independent, and the content-type already spells them separately
  — `application/proto` versus `application/connect+proto`. Unary is the framing that happens to be
  "none", not a different path.
- **Do not let the generated server handler be `Task<TResponse> Handle(TRequest)`.** That signature is
  unreachable from a streaming shape. Follow grpc-dotnet: the generated registration hands the runtime
  a **typed delegate per shape** and the runtime owns reading and writing, so adding a shape adds a
  delegate type rather than a second pipeline.
- **Do not assume trailing metadata is response headers.** It is for unary (`trailer-` prefixed), and
  it very much is not for streaming, where it arrives inside the terminating `EndStreamResponse`. The
  call object's trailer accessor must be an abstraction that both satisfy from day one; hard-code the
  unary answer and every streaming shape breaks it.
- **Do not assume `Content-Length` is knowable.** For unary it is — protobuf-net's
  `IMeasuredProtoOutput<>` gives the length before writing, which is a genuine win worth keeping. For a
  client-streaming or duplex request it is not. So the request body wants a small family of
  `HttpContent`, with the measured one as an optimisation rather than the only case.
- **Do not surface errors twice.** A unary failure is a non-200 with a JSON body; a streaming failure is
  a **200** with the error inside the final envelope; and per §13 an unrouted path is a `text/plain`
  404 with no error object at all. All three must land on one exception type carrying
  code/message/details, decided in one place. Three separate throw sites will not converge later.

None of these costs anything at stage 1 if known in advance, which is the entire reason for writing
them down before the code exists rather than after.

### What is deliberately *not* in the MVP

> Written when the MVP was unary-only. **Streaming is no longer on this list** - all four shapes
> landed in stage 1, because §14.1's constraints turned out to hold and each shape cost little once
> the framing existed. What remains excluded is as follows.

- **JSON** (§4) — optional, declarable-away in conformance, and the long pole.
- **Streaming** — unary is the bare-body form and needs no framing at all; streaming needs the
  envelope reader/writer and `EndStreamResponse`. Additive, and a clean second increment. **But see
  §14.1: all five shapes are expected eventually, so the MVP must not preclude them.**
- **GET/idempotency, compression negotiation, reflection, rich error details.** The last of these is
  blocked on stable qualified names anyway (§3b).
- **The `ConnectCallInvoker`** (§8.1). Still high-value and still cheap, but it is a *parallel*
  deliverable for existing protobuf-net.Grpc consumers, not a step on this path — an MVP with two
  client implementations is an MVP with one too many.

## 15. Stage 0 — built and measured, 2026-09-11

`src/protobuf-net.Connect` (the client) and `src/ConnectProbe` (the harness) exist, and the probe reads
**7 passed, 0 failed** against `demo.connectrpc.com` — both as a JIT run and as a **native AOT binary**.

```
dotnet run --project src/ConnectProbe          # hits the network; manually run, not a CI test
```

| check | result |
| --- | --- |
| unary round-trip, binary codec | `"Hello there...how are you today?"` |
| leading metadata surfaced | 8 headers, 0 trailers |
| custom leading metadata accepted | yes |
| `connect-timeout-ms` accepted | yes |
| wire-type mismatch | **tolerated** — see below |
| server error object parsed | `invalid_argument (HTTP 400)`, message preserved |
| unrouted path inferred, not parsed | `unimplemented (HTTP 404)`, `CodeWasInferred` true |

So the §2 protocol reading is correct, and protobuf-net's **code-first** bytes interoperate with
connect-go with no `.proto` anywhere. The contracts were written by hand from the Eliza schema on
purpose: generating them from the `.proto` would have tested the wrong thing.

### What it is built on, and what that proves

- `protobuf-net.Core` **only**. `protobuf-net` is not referenced, so `RuntimeTypeModel` is not on the
  graph at all and there is no reflective path to fall into by accident. The model is an ordinary
  `[ProtoModel]`-generated one.
- **Native AOT: `linux-x64`, 7,345,320 bytes, 33 IL warnings — none of them ours.** Every one is
  attributed to `protobuf-net.Core` or to bare ILC; `protobuf-net.Connect` and `ConnectProbe`
  contribute zero. The native binary passes all seven checks, so this is measured rather than compiled.

### The warning residue is the IO interfaces, and it cannot be fixed from outside Core

Roughly twelve of the 33 (6 × `IL2095`, ~6 × `IL2091`) are one family: the explicit interface
implementations in `TypeModel.InputOutput.cs`, which forward an **unannotated** interface `T` to
`TypeModel.Serialize<T>` / `Deserialize<T>` / `Measure<T>`, each of which still carries
`DynamicAccess.ContractType` on its own `T`.

This is the residue of the cleanup `AGENTS.md` records — the annotations came *off*
`IProtoInput<TInput>` / `IProtoOutput<TOutput>` / `IMeasuredProtoOutput<TOutput>`, worth 14 warnings and
808 KB, but stayed on the concrete methods. **Calling the concrete methods directly does not help**: a
generic codec would then need the annotation itself, which relocates the warnings into the transport
rather than removing them — precisely what `AGENTS.md` records about `Requires*`.

So a transport built on protobuf-net's public IO surface inherits these, and nothing outside
`protobuf-net.Core` can do anything about it. The recorded technique that *does* terminate cleanly is a
**`RuntimeFeature.IsDynamicCodeSupported` feature switch**, which is what took
`TypeModel.ResolveSerializer<T>` and the writer/reader cluster from 49 → 34. Applying the same to the
root `Serialize<T>`/`Deserialize<T>`/`Measure<T>` is the obvious follow-up.

**This is the v4 spike's territory, not Connect's** — recorded here only so the numbers above are
attributable and so nobody re-derives the cause. Do not chase it from this branch.

### Two things the probe turned up

- **A wire-type mismatch is not an error — it is an unknown field.** Sending a varint on field 1 where
  the service's schema says `string` succeeds: protobuf skips the field and the service sees an empty
  sentence. Found by asserting the opposite. Worth pinning, because "the two ends disagree about the
  schema" failing *silently* is the whole reason field numbers must be managed rather than assumed.
- **A non-200 does not imply a Connect error object.** An unknown method is answered by the HTTP layer
  with `404` and `text/plain` — no JSON at all — which is why the spec carries a status → code
  inference table. A client that assumes every failure carries a parseable error throws on the most
  ordinary failure there is. `ConnectException.CodeWasInferred` distinguishes the two, so a caller can
  tell "the service said `unimplemented`" from "something between us returned 404".

### Shape, and where the seams are

Deliberately small, and arranged per §14.1 so the streaming shapes can be added without disturbing it:

| type | why it is shaped that way |
| --- | --- |
| `ConnectCodec` / `ProtoConnectCodec` | payload only; framing is *not* its business. `ContentTypeFor(type)` is the one place the two meet — `application/proto` versus `application/connect+proto` |
| `ConnectMethod<TReq,TResp>` | carries `ConnectMethodType` and `IsIdempotent` from the start, though only `Unary` is implemented — the type selects the framing, and idempotency is what GET will need |
| `MeasuredCodecContent<T>` | one member of an intended family. Unary can state `Content-Length` because protobuf-net measures before writing; a duplex body cannot, and gets a sibling |
| `ConnectCallResult.Trailers` | an accessor, not "the `trailer-` headers" — which is where they live for unary and emphatically not for streaming |
| `ConnectException` | one type for all three failure shapes, decided in one place |
| `ConnectErrorReader` | hand-written `Utf8JsonReader`. Keeps the honest position that this assembly has no JSON *codec*: it parses the protocol's error envelope, never a payload |

The `RawCodec` in the probe — used to put deliberately malformed bytes on the wire — needed **no**
change to `ConnectChannel`, which is a small confirmation that the codec seam is in the right place.

## 16. Stage 1 — a server, and an end-to-end MVP, 2026-09-11

`src/protobuf-net.Connect.AspNetCore` (server) and `src/AotConnectSmoke` (harness) exist. The harness
reads **8 passed, 0 failed**, as a JIT run *and* as a native AOT binary, and the server has been
verified from **outside .NET** with `curl`.

```
dotnet run --project src/AotConnectSmoke              # self-check, exits non-zero on mismatch
dotnet run --project src/AotConnectSmoke -- --serve   # just the server, on :8080, for an external client
```

| check | result |
| --- | --- |
| unary round-trip over HTTP/1.1 | `hello marc; hello marc`, second field intact |
| trailing metadata | 1 trailer, `trailer-` prefix stripped, not also reported as a header |
| a deliberate failure | `permission_denied` / 403, message preserved, `CodeWasInferred` false |
| an unhandled exception | `internal` / 500, **message not leaked** |
| `connect-timeout-ms` | `deadline_exceeded` after 300 ms of a 30 s call |
| an unknown codec | 415 + JSON, naming the codecs the server does have |
| a streaming content-type | 415, saying *why*, rather than mishandling it |
| the wire form | bare message, `Content-Length` stated, HTTP/1.1, no envelope |

### It gates CI

`AotConnectSmoke` is published natively and **run** by the existing `native-aot-linux` job, alongside
`AotSmoke` and `AotGrpcSmoke`. It is self-contained - Kestrel on an ephemeral loopback port, calling
itself - so it needs nothing from the network.

`src/ConnectProbe` is deliberately **not** in CI. It is the interoperability check against
`demo.connectrpc.com`, and a third party's uptime has no business gating our builds; it stays a
manually-run tool, like `src/AotCoverage`.

**The gate was shown able to fail**, not merely observed to pass: changing the service's greeting from
`hello {name}` to `hi {name}` was caught by three checks and exited 1. Worth having done, because a
harness that cannot fail is worse than no harness - it reports green forever.

### Verified from outside .NET

The self-check proves the two halves agree with each other, which is not the same as being a Connect
server. `curl` against `--serve`:

```
HTTP/1.1 200 OK
Content-Length: 14
Content-Type: application/proto
trailer-greeter-version: 1

00000000: 0a0a 6865 6c6c 6f20 6375 726c 100a       ..hello curl..
```

and the failure path:

```
HTTP/1.1 403 Forbidden
Content-Type: application/json
{"code":"permission_denied","message":"\u0027curl\u0027 may not greet."}
```

That is the protocol's unary shape exactly: bare message both ways, trailing metadata as a `trailer-`
prefixed header, the error as a JSON object under its mapped HTTP status.

### It is served over HTTP/1.1, and that is the whole point

The harness leaves Kestrel's plaintext endpoint at its default, which is HTTP/1.1. **gRPC could not be
served from that port at all** — `Grpc.AspNetCore.Server` validates `HttpRequest.Protocol` and answers
*"Request protocol of 'HTTP/1.1' is not supported"*, and Kestrel cannot negotiate between the two on a
plaintext port because without TLS there is no ALPN. Nothing here asks for HTTP/2 and everything works.

### Native AOT: 15,405,832 bytes, 33 IL warnings — **the same 33** as the client alone

Adding the entire ASP.NET Core server moved the count **not at all**. Every warning is attributed to
`protobuf-net.Core` (`TypeModel.cs` 9, `TypeModel.InputOutput.cs` 7, `TypeHelperT.cs` 4, `DynamicStub.cs`
4, …) or to bare ILC; none to `protobuf-net.Connect`, `protobuf-net.Connect.AspNetCore` or the harness.
So Kestrel, endpoint routing and the server runtime are AOT-clean, and the residue is the v4 spike's
(§15) rather than anything this work introduced.

Note `WebApplication.CreateSlimBuilder` and the **`RequestDelegate`** overload of `MapPost` are what make
that true: minimal APIs are only "partially" AOT-supported because `RequestDelegateFactory` is
reflective, and writing raw request delegates bypasses it entirely — which is what we want anyway.

### The route in — what a consumer actually writes

Review asked "what's the route in to all of this?", and the honest answer was that there wasn't one
visible: the container was written as though entirely generated, so the consumer's side was invisible.
The two halves are now separate files, which is the only way to see how much someone is signing up for.

**`src/AotConnectSmoke/Services.cs` — the whole of it:**

```csharp
[ProtoConnect(Model = typeof(SmokeModel))]
[ProtoService(typeof(IGreeter), typeof(GreeterService))]
internal partial class SmokeServices { }
```

Three lines, beside the contract itself. Two things about it are decisions rather than incidentals:

- **`[ProtoService]` is protobuf-net.Grpc's own, unchanged.** It is real API there
  (`ProtoBuf.Grpc.Configuration.ProtoServiceAttribute`, taking contract and implementation) and already
  says exactly what is needed, so a Connect-specific copy would be a second spelling to keep in step.
  **Only the container attribute selects the transport** — and the same `[ProtoService]` declarations
  would serve a `[ProtoGrpc]` container, which is the "your existing contracts just work" claim applied
  one level up.
- **`[ProtoConnect]` is real API in `protobuf-net.Connect`**, not a fixture stub, mirroring how
  `[ProtoGrpc]` is real API in protobuf-net.Grpc. Matched by **full name** like every other trigger
  attribute here.

**Accessibility mirrors the consumer's declaration.** The generated part is a bare `partial class
SmokeServices` restating no modifier, so `public` in Services.cs yields a public surface and `internal`
an internal one. Verified rather than asserted: flipping the declaration to `public` compiles and the
surface becomes reachable from code that could not see an internal one.

**`static` is deliberately not required of the consumer.** The generated half emits static members plus
a `private SmokeServices()`, which gets the same effect without making anyone think about it — the same
thing `GrpcProxyGenerator` emits, for its own reasons. A generated part that restated `static` or an
accessibility would either fight the consumer or force them.

### The shape the generator has to emit

`src/AotConnectSmoke/HandWritten.cs` **is** the target output, written by hand and marked as such. This
is the repo's own method — `AotRefGen` exists so that expected generator output is derived and
reviewable rather than invented — and since there is no ref-emit to derive Connect output from, writing
it, making it work and reviewing it is the substitute.

Everything is nested inside **one consumer-declared partial class**, which is what a
`[ProtoConnect(Model = typeof(SmokeModel))]` would mark; `SmokeServices` stands in for it. That mirrors
`GrpcProxyGenerator`, whose proxy and bindings likewise nest inside the `[ProtoGrpc]` type, and the
container is not decoration — it is where the model is named, where `CreateClient<T>` lives, and what
the registration extension hangs off. The first cut had three peer types at namespace scope and **no
container at all**, which review caught: it left the proxy and bindings as visible API, and it was
missing the type the generator will need anyway.

**The whole visible surface is two static verbs**, `CreateClient<TService>` and `BindServer`.
Everything else is **private**:

| | |
| --- | --- |
| `Greeter` (private static) | a `ConnectMethod<,>` per operation, shared by both sides, so the service and method names exist once — and where the serializers are resolved, in the static initialiser |
| `GreeterClientProxy` (private) | one call to `channel.UnaryAsync` per method; a consumer reaches it through `CreateClient<TService>` and only ever sees `IGreeter` |
| `GreeterServerBindings` (private) | `AddUnaryMethod(method, handler)` per operation, the handler a `static` lambda so it allocates nothing |

**The container is `static`**, which took a third round of review to get right. The first cut mirrored
`GrpcProxyGenerator`'s output — an instance with an `Instance` accessor — but there the instance is
load-bearing and here it is not: a `[ProtoGrpc]` container derives from the abstract `ClientFactory`,
holds a `BinderConfiguration` with a marshaller cache, and is `TryAddSingleton`'d into DI. Ours derives
from nothing, has no fields, and caches nothing — the codec lives on the `ConnectChannel`, the
serializers in the method holder's static initialiser. So `Instance` was ceremony inherited from a shape
whose justification does not carry over. **Worth noticing as a pattern: three of the four review
findings on this file were things copied from the gRPC generator whose reasons did not survive the
move.**

What would change the answer is a **DI client-factory story** — `services.AddConnectClient<T>()`
resolving "the thing that makes clients" — which needs an instance implementing some interface, as
protobuf-net.Grpc's `ClientFactory` does. Open question, and one to answer deliberately rather than by
pre-emptively inventing an instance: note it would be a consumer-visible break to add later.

Note the generator need not force consumers to write `static partial class` — static members can be
emitted onto an ordinary partial class, with a private constructor to stop it being instantiated, which
is what `GrpcProxyGenerator` already emits for its own reasons.

Getting the accessibility right was review pushing twice more, and the second push found a test smell. `Greeter` had to be
`internal` **only because the harness reached into it** for the service name and a method descriptor —
a test affordance leaking into the design. Removing it made the checks *better*: the raw-HTTP checks now
state the expected service name independently, where before they derived it from the implementation's
own constant and so **could not have caught a wrong service name** — they would have agreed with
whatever the bindings did.

`endpoints.MapSmokeServices()` survives as a one-line alias for `BindServer`, because `app.MapXxx()` is
what an ASP.NET Core consumer reaches for — the counterpart of `GrpcProxyGenerator`'s `AddXxx`. It adds
no capability. Underneath, `MapConnectService` maps one endpoint per method (§14.1) and returns a
composite `IEndpointConventionBuilder`, so `.RequireAuthorization()` on the service applies to all of
them while generated per-method metadata still attaches individually.

### The vocabulary decision is still open, and the fixture does not pre-empt it

`IGreeter` carries **no `[Service]` attribute and no protobuf-net.Grpc `CallContext`**. That is
deliberate: §9's question — whether the contract-facing vocabulary is protobuf-net.Grpc's, a
Connect-local mirror, or something lifted into Core — is unsettled, and *nothing needs it settled yet*,
because no attribute is read until the generator exists and the bindings here are hand-written.
Settling a decision like that by accident, in a fixture, is how it gets made badly.

What the fixture does commit to is the *shape*: an interface of async methods, each taking a request
message and a context, which is protobuf-net.Grpc's shape whichever types end up filling it.

### Things the build caught, worth keeping

- **`ConnectCodec` is buffer-oriented, not stream-oriented**, and that was forced rather than chosen:
  ASP.NET Core forbids synchronous stream reads, so a `Read<T>(Stream)` codec would make the server
  buffer the body for no reason. `IBufferWriter<byte>` and `ReadOnlySequence<byte>` are what both ends
  actually hold — a `PipeWriter` *is* the former, a `PipeReader` yields the latter — and protobuf-net
  implements `IProtoOutput<IBufferWriter<byte>>` and `IProtoInput<ReadOnlySequence<byte>>` natively. The
  server now writes straight to `Response.BodyWriter` with no intermediate buffer.
- **The traversal build is what caught the refactor**, not a test: `ConnectProbe`'s `RawCodec` still
  overrode the stream-shaped members. `Build.csproj` globs `src\*\*.csproj`, so both new projects and
  both harnesses are in CI automatically with no registration.
- **`ConnectException.RawMessage`** exists because `Message` synthesizes a `code (HTTP nnn): ` prefix for
  readability, and relaying an error must not accumulate prefixes. The server writes `RawMessage`.

## 17. The contract vocabulary — decided, and what it cost

**Decided: option (a) of §9 — the contract vocabulary is protobuf-net.Grpc's own.** `[Service]`,
`[Operation]` and `CallContext`, the same types, not lookalikes. `src/AotConnectSmoke` now declares:

```csharp
[Service]
public interface IGreeter
{
    Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default);
}
```

and serves it over Connect unmodified. Nine checks pass, JIT and native.

**The sell is the reason, and it is better than "it is a dependency":** *your existing protobuf-net.Grpc
contracts work over Connect, with no edit*. One interface, both transports, no adapter. The client-side
half of the same sell is §8.1's `ConnectCallInvoker`, which would give an existing protobuf-net.Grpc
*client* the Connect protocol with no generator changes at all.

Option (c) — lifting the vocabulary into protobuf-net.Core — is ruled out for a concrete reason:
**protobuf-net.Grpc hard-depends on protobuf-net `2.4.8`**, so it would not see anything added to Core
v4 without a version bump, which is the cross-repo release loop again. That may change with v4.

### It is not just a package reference: `CallContext` has two constructors

Read off the shipped package rather than assumed. `CallContext` can only be built two ways:

| | |
| --- | --- |
| `CallContext(in Grpc.Core.CallOptions, CallContextFlags, object)` | client side |
| `CallContext(object server, Grpc.Core.ServerCallContext)` | **server side** |

plus implicit conversions from `CallOptions` and from `CancellationToken`. So sharing the vocabulary
server-side means **implementing `Grpc.Core.ServerCallContext` over `HttpContext`** — twelve abstract
members. grpc-dotnet does exactly this (`HttpContextServerCallContext`), so the shape is proven, and it
pays for itself: `Metadata` for headers and trailers in a form consumers already know, and a `Status`
the handler can set instead of throwing.

`ConnectCode`'s ordinals were aligned with `Grpc.Core.StatusCode` back in §15 on general principle; that
now pays off, because `StatusCore` → Connect error is a **cast**, not a table.

Two members cannot be answered honestly and say so rather than inventing an answer:
`CreatePropagationTokenCore` is Grpc.Core-native with no Connect equivalent, and `AuthContextCore`
describes transport-level peer identity we do not collect — ASP.NET Core authentication lands on
`HttpContext.User`, which is reachable through the exposed `HttpContext`.

### The v2/v3 `TypeModel` collision, and where it belongs

The first attempt put `protobuf-net.Grpc` on the runtime libraries and **failed to compile**:

```
error CS0433: The type 'TypeModel' exists in both 'protobuf-net.Core, Version=3.0.0.0' and
              'protobuf-net, Version=2.4.0.0'
```

because protobuf-net v2 has `TypeModel` in the **`protobuf-net`** assembly and v3 moved it to
**`protobuf-net.Core`**. This is the same class of trap `AGENTS.md` records for `AotDifferential`, from
a different direction.

The resolution is a better structure, and it is the one `GrpcProxyGenerator` already uses — its server
bindings construct the `CallContext` themselves rather than the runtime doing it:

| assembly | references | why |
| --- | --- | --- |
| `protobuf-net.Connect` | `protobuf-net.Core` only | unchanged; still no `RuntimeTypeModel` on the graph |
| `protobuf-net.Connect.AspNetCore` | **`Grpc.Core.Api`** only | `ServerCallContext` lives there, and that package depends on nothing of ours |
| *generated code* | `protobuf-net.Grpc` | `new CallContext(service, ctx)` server-side; `CallOptions` → `ConnectCallOptions` client-side |

So the collision lives in the **consumer's** project, where a reference to protobuf-net v3 resolves it
the ordinary way — exactly as `src/AotGrpcSmoke` already does. The runtime libraries stay clean.

**Correction, from review:** the collision is per-**usage**, not per-assembly. `CS0433` fired only on the
two lines of `ConnectCodec.cs` that *name* `TypeModel`; an assembly that references both and never names
it compiles fine. So the collision is a weaker constraint than stated above, and the real reason to keep
protobuf-net.Grpc out of the runtime libraries is **design**: the server runtime takes a plain
`ServerCallContext`, which leaves it vocabulary-agnostic, and generated code adapts. Were the runtime to
construct the `CallContext` itself, the vocabulary decision would be baked into the transport.

That distinction matters, because it is what lets the client-side conversion be **library code**:
`ConnectCallOptions.From(in CallOptions)` takes `Grpc.Core.CallOptions` rather than protobuf-net.Grpc's
`CallContext`, so it needs only `Grpc.Core.Api` — which depends on nothing of ours. gRPC states an
**absolute deadline**, Connect a **relative** `connect-timeout-ms`, and `Metadata`'s binary entries
become base64 under their `-bin` name. Generated code passes `context.CallOptions` and is one expression
per call site. Two of the nine checks exercise it in both directions.

This was originally emitted per assembly as a `ConnectCallContextBridge`, which review correctly
questioned: **it is not service-related in any way** — a function of two types and nothing else — so
generating it was a symptom of a package boundary drawn on the weaker constraint.

### What it cost: nothing measurable

| | before | after |
| --- | --- | --- |
| IL warnings (`linux-x64`) | 33 | **33** |
| native size | 15,405,832 | 15,430,856 (**+25 KB**) |
| checks | 8/8 | 9/9 |

Note the fixture now also references the **full `protobuf-net`** (v3, with `RuntimeTypeModel`) to resolve
the collision, and that moved nothing either: ILC trims it entirely, because the generated path never
calls it. Which is the point of the whole exercise stated from the other end.

### Still not demonstrated

The claim is currently *structural* — the contract uses the real types and is served over Connect. The
convincing version is **hosting the same `GreeterService` over gRPC and Connect simultaneously** and
showing both clients work. That needs `Grpc.AspNetCore.Server` + `protobuf-net.Grpc.AspNetCore` and a
second Kestrel endpoint on HTTP/2, and it should be a **separate project**: protobuf-net.Grpc's
reflective code-first path is what `GrpcProxyGenerator` exists to replace, so hosting it inside a
`PublishAot` project would pollute the warning baseline that makes §16's numbers meaningful.

## 18. Where the marshallers went — there are none, deliberately

`HandWritten.cs` names no marshaller, and `ConnectMethod<TRequest, TResponse>` carries no serializer,
unlike `Grpc.Core.Method<,>`. That is a structural difference from the gRPC path rather than an
omission, and it is worth recording because the obvious "fix" is to add them back.

### The thing marshallers work around does not exist here

`AGENTS.md` records why `GrpcProxyGenerator` pre-registers marshallers, and is explicit that it is
**load-bearing, not an optimisation**: `MarshallerCache.CreateMarshaller<T>` gates on
`CanSerialize(typeof(T))`, which reaches `DynamicStub` → `MakeGenericType` and returns **false** under
AOT, so the generator calls `BinderConfiguration.SetMarshaller<T>` per payload to sidestep the gate.

The Connect path never enters that machinery. Traced rather than assumed:

```
codec.Read<T>(seq) → TypeModel.Deserialize<T>(ReadOnlySequence<byte>)
                   → state.DeserializeRootImpl<T>(value)              // the *generic* root path
                   → GetSerializer<T>()
                   → SerializerCache.Get<ProtoBufGeneratedServices, T>()
```

`DynamicStub` appears only in `TypeModel`'s **`Type`-based** overloads (`DeserializeRootFallback`,
`TrySerializeRoot`, …), none of which is on this path. There is no `MarshallerCache`, no
`CanSerialize`, no `MakeGenericType`. So there is nothing to pre-register *around*.

### And a per-type marshaller could not work anyway

The server picks the codec **per request, from the content-type**: `application/proto` and
`application/json` must serialize the same method differently. A serializer baked into a method
descriptor is codec-fixed by construction, so it cannot express content negotiation at all.

This is the same fact, seen from the other side, as §8.1's note that a `ConnectCallInvoker` would be
binary-only: `Grpc.Core.Marshaller<T>` is where the gRPC design puts the codec, and the Connect design
cannot put it there.

Hence `ConnectCodec` holds the `TypeModel`, and lives on the channel (client) or in
`ConnectServerOptions.Codecs` (server) — not on the method.

### Nothing is lost, and both halves were checked

- **No caching gap.** `SerializerCache.Get<TProvider, T>()` already resolves to a *static generic
  field* for a generated model, so caching a serializer per `ConnectMethod<,>` would save a static
  field read. The gRPC proxy caches marshallers in fields because `Method<,>` demands them, not because
  resolution is expensive.
- **ILC still generates the instantiations without pre-registration**, because they are statically
  reachable from generated code: `GreeterMethods.SayHello` is a `ConnectMethod<HelloRequest, HelloReply>`,
  and `UnaryAsync<HelloRequest, HelloReply>` → `Read<HelloReply>` → `Deserialize<HelloReply>`. That is
  *why* the native smoke test round-trips; had it not been true, §16 would have failed rather than
  passed quietly.

### The one real consequence

**A `ConnectChannel` is bound to a single `TypeModel`,** since the codec holds it — the role
`BinderConfiguration` plays in protobuf-net.Grpc. Two models means two channels. That looks right
rather than limiting, but it is the design's one visible constraint and should be a deliberate choice
if it ever changes.

## 19. Hoisting serializer resolution out of the per-message path

§18 established that there are no marshallers and that nothing on this path enters `MarshallerCache`
or `DynamicStub`. What remained was a genuine, if small, per-message resolution. It is now hoisted to
static-initialiser time, and the *how* is the interesting part.

### What the lookup actually was

Measured against the source rather than guessed. `TypeModel.Deserialize<T>` →
`state.DeserializeRootImpl<T>` → `TypeModel.TryGetSerializer<T>(model)`, which is:

```csharp
=> SerializerCache<PrimaryTypeProvider, T>.InstanceField   // static generic field; null for a contract
 ?? model?.GetSerializer<T>();                             // virtual → SerializerCache<Generated, T>.InstanceField
```

So: a static field read, a **virtual call**, a second static field read. No dictionary, no reflection.
Small — but it is per message, and "the model is closed at compile time" ought to mean the binding is
made at compile time too.

### The mechanism needs no change to protobuf-net.Core

Four things are already public, which together are enough:

| | |
| --- | --- |
| `SerializerCache.Get<TProvider, T>()` | public |
| `ProtoReader.State.Create(ReadOnlySequence<byte>, model)` / `ProtoWriter.State.Create(IBufferWriter<byte>, model)` | public |
| `state.DeserializeRoot<T>(value, serializer)` / `state.SerializeRoot<T>(value, serializer)` | public, and both take an `ISerializer<T>` |

The missing link looked like `TProvider`: `ProtoModelGenerator` emits `ProtoBufGeneratedServices` as a
**private nested class of the model**, and `TypeModel.GetSerializer<T>()` is `protected`, so no
consumer can name either. **But the model is `partial`** — so another part of the same class *can*
name the nested type:

```csharp
public partial class SmokeModel
{
    public static ISerializer<T> Serializer<T>() => SerializerCache.Get<ProtoBufGeneratedServices, T>();
}
```

That is `src/AotConnectSmoke/ModelAccessor.cs`, hand-written for now. **It should become a
`ProtoModelGenerator` emit** — a one-line addition to the model, useful to anything that wants to bind
a serializer once rather than per call, not only to Connect.

`ConnectMethod<,>` then carries the pair, resolved in the static initialiser, and the codec's
`Write`/`Read` take an optional `ISerializer<T>` with the `serializer ??= …` idiom protobuf-net uses
throughout — so a codec for which a binary serializer is meaningless (a JSON one) ignores it.

### The annotation trap, which is the part worth remembering

Annotating the codec's `Read<T>`/`Write<T>` with `DynamicAccess.ContractType` — the obvious fix for the
`IL2091` that `DeserializeRoot`/`SerializeRoot` raise — **propagates the demand across the entire public
generic surface**: it immediately reached `ConnectChannel.UnaryWithMetadataAsync<TRequest, TResponse>`
and `MeasuredCodecContent<T>`, and would have carried on to `ConnectMethod<,>` and thence to every
consumer's payload types.

That is precisely the mistake `AGENTS.md` records against `ISerializer<T>`, whose removal took
`AotSmoke` from **200 warnings to 33**. Repeating it to save a virtual call would be a poor trade.

The right answer is a **local `UnconditionalSuppressMessage`**, which is what Core itself does in
`GetSerializerAllowingReflection`, and the justification is exact rather than hand-waving: both methods
are `serializer ?? TypeModel.GetSerializer<T>(Model)`, and `??` does not evaluate its right-hand side,
so a supplied serializer means the reflective arm is unreachable. Verified by reading both bodies, not
inferred from the names.

The one annotation that *is* correct is on `SmokeModel.Serializer<T>()`, because it terminates
immediately — every caller is a static initialiser naming a concrete contract type.

### Measured

| | before | after |
| --- | --- | --- |
| IL warnings (`linux-x64`) | 33 | **33** |
| native size | 15,430,856 | 15,443,448 (**+12 KB**) |
| checks | 9/9 | 9/9, JIT and native |

**One resolution remains, on the write path only:** `TypeModel.Measure<T>` takes no serializer, so
there is nothing to hand it. It buys `Content-Length`, which is worth more than it costs. A
`Measure<T>(T, ISerializer<T>)` overload would close it, and that *would* be a Core change.

## 20. Can one type serve both Connect and gRPC?

Asked in review. Three separable layers, and the sharp one is not where it looks.

### The attributes coexist; the generated members collide

`[ProtoGrpc]` and `[ProtoConnect]` are different types, both `AttributeTargets.Class`, so one class can
carry both — and `[ProtoService]` is already shared (§"The route in"), so the seeding needs no
duplication at all.

What collides is the *emitted* code. Both generators would emit `private MyServices() { }` → **CS0111**.
Both want `CreateClient<TService>`; those could coexist as overloads, since the parameter types differ
(`CallInvoker` versus `ConnectChannel`), but gRPC's is an instance `override` inherited from
`ClientFactory` while ours is static. Nested type names differ only because the two naming schemes
happen not to clash, which is luck rather than design.

So it is *possible*, but only with the two generators aware of each other — coupling that buys little.
**Two containers sharing the same `[ProtoService]` declarations** is trivially safe and costs two
duplicated attribute lines. Prefer that unless someone asks otherwise.

### The server side is where it bites, and two registrations is the wrong shape

**gRPC and Connect use the identical URL path** — `/[package.]Service/Method` — so mapping both puts
two endpoints on one route. Probed rather than assumed, by registering a second `MapPost` at
`SayHello`'s path:

| | |
| --- | --- |
| startup | **succeeds** |
| `SayHello` (the duplicated path) | **HTTP 500**, `AmbiguousMatchException`, at *request* time |
| the three methods at distinct paths | unaffected |

Which is an unpleasant shape of failure: a green startup, most of the service working, and one method
500ing. Worth knowing before anyone tries it. (Our client reports it `unknown (HTTP 500)` — the
status-inference table working correctly on a 500 that carries no Connect error object.)

**The right answer is one handler dispatching on content-type**, which is exactly how connect-go
supports "Connect, gRPC and gRPC-Web on the same port" — `application/grpc*` versus
`application/proto|json` versus `application/connect+*`. We are closer to that than it looks:
`ConnectContentType.TryParse` already separates codec from framing and already declines what it does
not know, and per §2 gRPC's framing is **the same 5-byte envelope layout** Connect streaming uses,
differing in the flag byte's meaning and in where trailers go. Bounded work, on machinery the streaming
shapes need anyway.

**The cheap escape is in the protocol already**: Connect's path grammar allows a routing prefix
(`/[prefix/]package.Service/Method`), so mounting Connect under `/connect/` sidesteps the collision for
the cost of a non-default URL. **We do not support it today** — `ConnectMethod` hard-codes
`"/" + service + "/" + method` — and it is worth adding regardless of this question.

## 21. Streaming framing, probed against connect-go

Before building it. `demo.connectrpc.com`'s Eliza has a server-streaming `Introduce`, so the wire form
is observable rather than inferred. Request — an **enveloped** `IntroduceRequest { name = "hi" }` — over
forced HTTP/1.1:

```
$ printf '\x00\x00\x00\x00\x04\x0a\x02hi' > intro.bin
$ curl --http1.1 -H "Content-Type: application/connect+proto" --data-binary @intro.bin \
       https://demo.connectrpc.com/connectrpc.eliza.v1.ElizaService/Introduce | xxd

00 00000013  0a 11 "Hi hi. I'm Eliza."           <- flags 0, 19 bytes
00 0000003e  ...                                  <- flags 0, 62 bytes
00 00000061  ...                                  <- flags 0, 97 bytes
00 0000001c  0a 1a "How are you feeling today?"   <- flags 0, 28 bytes
02 00000002  7b 7d                                <- flags 2 = END OF STREAM, payload {}
```

Confirmed byte for byte, not read off the spec:

- **the request is enveloped too.** So *all three* streaming shapes use identical framing in **both**
  directions and differ only in cardinality — 1×N for server-streaming, N×1 for client-streaming, N×N
  for duplex. This is the fact the build order turns on;
- `EndStreamResponse` is `{}` on clean success, in an envelope whose flag byte is `0x02`;
- HTTP/1.1 throughout, `200`, and the response carries `connect-accept-encoding` — note the
  streaming-specific header name, not `accept-encoding`.

### Which settles the build order, and not for the obvious reason

Since the framing is identical across the three shapes, **duplex-first buys no framing coverage that
server-streaming does not already give**. What it adds is two risks *orthogonal to the protocol* — HTTP/2
hosting (and the smoke test's HTTP/1.1 plaintext endpoint is load-bearing: it is the demonstration that
Connect escapes gRPC's constraint) and a duplex `HttpContent`, which is §12's open item. A failure would
have three suspects.

Ordered by risk introduced per step, each adding exactly one new thing:

| step | what is new | HTTP |
| --- | --- | --- |
| **server-streaming** | envelope read+write, `EndStreamResponse`, trailers in the terminator, unknown `Content-Length` on the response, the 200-with-error path | 1.1 |
| **client-streaming** | an incremental request body — chunked, no `Content-Length` — and `IAsyncEnumerable` in | 1.1 |
| **duplex** | HTTP/2 and true interleaving; **nothing new in the protocol** | 2 |

Server-streaming alone exercises **every** §14.1 constraint, which is what this stage is for: falsifying
the design, not adding features. Worth spiking the duplex-`HttpClient` question separately and early
though — it is orthogonal to Connect, answerable in ~30 lines against an echo endpoint, and better known
now than after client-streaming lands.

## 22. Server-streaming — built, and the §14.1 verdict

Built end to end, and **verified in both directions against connect-go**: our client drives Eliza's
server-streaming `Introduce` (§21's probe, now a `ConnectProbe` check), and `curl` drives our server.

Our server's output, read off the wire:

```
00 00000011  0a 0d "hello curl #1" 10 0d           <- flags 0
00 00000011  0a 0d "hello curl #2" 10 0d           <- flags 0
02 00000024  {"metadata":{"greeter-count":["2"]}}  <- flags 2 = end of stream
```

Byte-for-byte the shape connect-go produced. `AotConnectSmoke` reads **13/13**, JIT and native;
`ConnectProbe` reads **9/9** against `demo.connectrpc.com`. Native AOT is **33 IL warnings, unchanged**,
and +173 KB.

### The verdict: four of five constraints held unchanged, one needed refining

This stage existed to falsify §14.1, so the result matters more than the feature.

| constraint | verdict |
| --- | --- |
| transport not buffer-shaped | **held.** The codec was untouched; streaming is the same codec with different framing, and `EnvelopedCodecContent` slotted in beside `MeasuredCodecContent` exactly as that file's comment predicted |
| no `Task<TResponse> Handle(TRequest)` server handler | **held, and this is the big one.** Adding server-streaming was one new delegate type plus one new invoker, with **zero** changes to `MapConnectService`, to endpoint creation, or to the registration record beyond carrying the shape |
| trailers behind an accessor | **held, and it would have hurt.** Streaming populates the same accessor from the terminating message instead of from `trailer-` headers. Had unary hard-coded headers, every streaming shape would have broken |
| `Content-Length` not assumed | **held**, but only half-exercised: the streaming *response* states none, while the streaming *request* still can, being one message. Client-streaming is what really tests this |
| one error path | **held in substance, wrong in my phrasing** — see below |

### "One error path" meant one error *type*, not one catch block

The refinement is worth recording because the original wording would have misled whoever implemented
this next. A streaming invoker **has to catch its own failures**: once the first message is out the
status is committed to `200`, so the endpoint's error path — which writes a non-200 with a JSON body —
has nowhere to put them. It cannot be the single catch block I implied.

What actually holds, and what the constraint should have said: **one exception type, and one decision
about codes**. `ConnectException` reaches two writers, chosen by whether the response has started:

| | |
| --- | --- |
| `ConnectErrorWriter` | non-200 plus a JSON body — the call never started |
| `EndStreamWriter` | the terminating envelope — the call started and then failed |

`ConnectException.HttpStatus` is `null` for the second, and that is load-bearing rather than incidental:
it is how a caller tells "the service reported `resource_exhausted`" from "something between us returned
a 500". One of the checks asserts exactly that.

### Two things this turned up that §14.1 did not anticipate

- **Content-type validation has to be shape-aware.** The content-type states the framing
  (`application/proto` versus `application/connect+proto`), so it must agree with the method's declared
  shape or the body cannot be parsed at all. `SelectCodec` now takes the shape and answers `415` naming
  the framing to use, in both directions. Two checks pin it.
- **`Utf8JsonWriter` escapes `+` as `\u002B`**, so `application/connect+proto` does not appear
  literally in an error body. Valid JSON and every client parses it, but it is worth knowing before
  matching on message text — a check asserted the literal and failed. It is also mild grit against
  Connect's whole debuggability pitch; whether to use a relaxed encoder is an open question with a
  faint security dimension (the default escapes HTML-sensitive characters too), so it is left alone
  rather than decided in passing.

### Next

Client-streaming, which adds exactly one thing: an incremental request body with no `Content-Length`.
Then duplex, which adds only HTTP/2 and interleaving. The duplex-`HttpClient` spike (§21) is still worth
doing out of order, since it is the one genuinely unknown environmental risk.

## 23. Client-streaming — and the last §14.1 constraint properly tested

Built. `AotConnectSmoke` reads **14/14**, JIT and native; native AOT is **33 IL warnings, unchanged**.

### `Content-Length` was the half-tested constraint, and now is not

§22 could only half-exercise it: a streaming *response* states no length, but a server-streaming
*request* still can, being one message. Client-streaming is the shape that cannot.

Rather than assert this from the client — which cannot see its own headers — the **service reports what
it saw**: `CollectAsync` returns `HttpContext.Request.ContentLength ?? -1`, and the check requires `-1`.
So "the request went out chunked" is measured at the far end of a real socket. `TryComputeLength`
returning `false` is what makes that happen, and it is the whole difference between
`EnvelopedStreamContent` and its two siblings.

### What client-streaming cost, which was almost nothing

- **The response side is server-streaming's**, reused rather than rewritten:
  `ClientStreamingAsync` builds a `ConnectServerStream<TResponse>` and takes the single message from
  it, so envelope reading, the terminator, trailers and the error path are shared verbatim. A count
  other than one throws, since that is the peer disagreeing about what the method is.
- **The request side is one new `HttpContent`** and one new invoker — the delegate-per-shape rule
  continuing to pay.
- `EnvelopedRequestReader` now serves both streaming invokers; server-streaming's bespoke reader was
  deleted in favour of it.

### Two asymmetries worth remembering

- **A request stream has no terminating message.** End-of-stream is a *response-only* flag, so a request
  simply ends. Reading one therefore has no "clean end" marker to check, and a partial envelope at the
  end is the only detectable truncation — which `ReadAllAsync` treats as `invalid_argument`.
- **A client-streaming body is not re-sendable.** It is backed by an `IAsyncEnumerable<T>` a caller may
  not be able to replay, so a retrying `DelegatingHandler` fails on the second attempt rather than
  sending a partial body. That is the better of the two outcomes, and is why `MeasuredCodecContent`
  buffers eagerly while this one does not.

### A `ReadOnlySequence` cannot cross a `yield`

`ReadAllAsync` decodes each read's messages into a list before advancing the reader and yielding them.
Both halves are required and for different reasons: a `ReadOnlySequence<byte>` cannot live across a
`yield return` at all, and the payloads reference the reader's buffer, so advancing before the caller
has consumed them would hand out freed memory. Obvious once written down, easy to get wrong.

### Remaining

Duplex only, which adds **HTTP/2 and interleaving and nothing else in the protocol** — the framing,
terminator, trailers and error paths are all now shared and proven. The duplex-`HttpClient` spike (§21)
is the real remaining unknown, and `Grpc.Net.Client` doing exactly this over HTTP/2 is reason to expect
it works.

## 24. Duplex — all four shapes now work

`AotConnectSmoke` reads **16/16**, JIT and native. Native AOT is **33 IL warnings, unchanged** across
every shape added; the binary has grown 15,443,448 → 15,755,160 across all three streaming shapes.

### Interleaving is proven, not assumed

A duplex test that sends everything then reads everything proves nothing — it would pass over HTTP/1.1
against a server that drained the request first. So the check makes the **request producer wait for the
echo of the previous message before yielding the next**:

```csharp
async IAsyncEnumerable<HelloRequest> PingsAwaitingEchoes()
{
    for (var i = 1; i <= 3; i++)
    {
        yield return new HelloRequest { Name = $"ping{i}" };
        await echoes.Reader.ReadAsync();   // ... filled by the response loop
    }
}
```

If `HttpClient` buffered the request body, or the service drained before replying, this **deadlocks**
rather than failing. It completes: three round-trips. So `SocketsHttpHandler` does duplex request bodies
over HTTP/2, and the service echoes as messages arrive — which also closes §12's open question about
duplex `HttpContent`.

### Duplex added no protocol machinery at all

As §21 predicted. The client is composition: the request body is the same `EnvelopedStreamContent` that
client-streaming uses, the response is the same `ConnectServerStream<TResponse>` that server-streaming
returns. The server invoker is the server-streaming one with a sequence in place of a single request.
Framing, terminator, trailers and error handling are shared verbatim across all three streaming shapes.

**Which is the real vindication of §21's build order.** Doing duplex first would have exercised the same
framing while bundling two orthogonal risks; doing it last, the only genuinely new thing was HTTP/2.

### Kestrel will not serve h2c on a mixed plaintext endpoint — measured

The first attempt put `HttpProtocols.Http1AndHttp2` on the single plaintext endpoint, hoping Kestrel
would sniff the HTTP/2 connection preface. It does not:

```
HttpRequestException: The HTTP/2 server closed the connection.
HTTP/2 error code 'HTTP_1_1_REQUIRED' (0xd).
```

Kestrel answers a prior-knowledge h2c attempt by actively telling the client to downgrade. So a plaintext
deployment wanting duplex needs **two listeners** — `Http1` and `Http2` — which is what the harness now
does, and which is what a real deployment would do anyway. With TLS this does not arise, since ALPN
negotiates.

This does **not** weaken the "Connect does not need HTTP/2" claim, and the harness is arranged to keep
that honest: everything except duplex runs on the HTTP/1.1 listener, and the wire-form check still
asserts `HTTP/1.1`. Only the one shape that genuinely requires HTTP/2 uses the second endpoint.

### Failing beats deadlocking

The duplex invoker checks `HttpRequest.Protocol` and answers **505** rather than proceeding. That is not
defensive tidiness: over HTTP/1.1 the client cannot read a response until it has finished sending, while
the service is waiting for messages that will not come — so the call would hang rather than fail. A
check pins it, and the client pins `HttpVersionPolicy.RequestVersionExact` for the same reason: a
plaintext request left to negotiate would silently settle on HTTP/1.1 and deadlock.

## 25. Routing prefix — and a latent bug it exposed

The protocol allows a prefix in front of every method path (`/[prefix/]package.Service/Method`), and
§20 identified it as the thing that lets Connect share a host with gRPC, since the two use identical
paths otherwise. Now supported on both sides: `MapConnectService(binder, routingPrefix)` server-side,
and a base address carrying the prefix client-side. `AotConnectSmoke` maps the same service twice, at
the root and under `/rpc`, and a check drives both.

**It exposed a latent bug worth knowing about.** The channel resolved each call as
`new Uri(baseAddress, method.Path)`, and `Path` has a leading slash — which makes it an *absolute-path*
reference, so `Uri` **discards the base's own path entirely**:

```
new Uri(new Uri("http://host/connect/"), "/pkg.Svc/M")  ->  http://host/pkg.Svc/M
```

So any base address with a path was already being silently ignored, prefix feature or not — a caller
pointing at `http://host/api/` would have had their calls go to `http://host/`. `ConnectMethod` now
carries `RelativePath` alongside `Path`, and the channel combines with that.

The second half is the same trap one step earlier: a base address must **end in `/`**, or `Uri` treats
the last segment as a file name and replaces it, so `http://host/connect` becomes `http://host/`. The
channel normalises rather than requiring callers to know that.

Both are the sort of thing that produces a 404 against a correct server and sends you looking in the
wrong place.

## 26. Several services in one container

Explored by building it rather than deciding on paper: `AotConnectSmoke` now declares a second,
unrelated contract. **19/19**, JIT and native; native AOT still **33 IL warnings**, +30 KB.

### One container, many services — and the vocabulary already said so

`[ProtoService]`'s own documentation reads *"Repeat for each contract"*, so a container holding several
services is the designed shape, not something being stretched. The consumer's half stays three lines
plus one per service:

```csharp
[ProtoConnect(Model = typeof(SmokeModel))]
[ProtoService(typeof(IGreeter), typeof(GreeterService))]
[ProtoService(typeof(IFarewell), typeof(FarewellService))]
internal partial class SmokeServices { }
```

Two containers remain possible and are the answer when the *models* differ, since a container names
exactly one. For services sharing a model there is nothing to gain from splitting.

### Binding: one call for all, plus a per-service overload

Both, and the pairing is the point:

| | |
| --- | --- |
| `BindServer(endpoints, prefix)` | every service the container declares, returning a composite builder — so `.RequireAuthorization()` covers all of them at once |
| `BindServer<TContract>(endpoints, prefix)` | one service, so conventions can differ between them |

The generic overload is keyed on the **contract**, matching `CreateClient<TContract>`, even though the
runtime binds by implementation type. The consumer named the pairing once in Services.cs and should not
have to remember which side each API wants. A check binds `IFarewell` alone under `/solo` and confirms
`IGreeter` is *absent* there — so "bound separately" means separately, not merely ordered differently.

### The thing building it actually settled: registration

The bigger finding was not about binding at all. Adding a second service made the consumer's `Program.cs`
worse in a way one service had hidden — they were hand-wiring a codec they should not need to know
about, and then registering each implementation by hand, where a miss surfaces as a DI failure at first
call rather than at startup.

So the generator should emit a **registration** method too, the counterpart of `GrpcProxyGenerator`'s
`AddXxx`:

```csharp
builder.Services.AddSmokeServices();   // codec over [ProtoConnect(Model = ...)], plus every implementation
...
app.MapSmokeServices();                // every service's endpoints
```

That is the ASP.NET Core shape (`AddX` then `MapX`), and the consumer never names `ProtoConnectCodec` or
the model. `TryAdd` throughout, so a consumer who registered an implementation themselves — different
lifetime, or a decorator — keeps theirs.

**Two generated verbs on the server, one on the client**, which is the whole surface.

### Nested type naming: qualify only on collision

`GrpcProxyGenerator` names its nested types from the contract's *fully-qualified* name
(`GrpcFixtures_Basic_IGreeter_ClientProxy`) unconditionally. That is safe, and until there were two
services here it looked like over-caution; with two it is obviously guarding against a container holding
two contracts of the same simple name from different namespaces.

But the generator **sees every contract in the container**, so it can use the simple name where it is
unique and qualify only where it is not. That reads far better in the common case and is no less safe.
The fixture uses simple names on that basis.

## 27. Client-only: `[ProtoService(typeof(IFarewell))]`

Yes — and it is real API with an explicit meaning, not a happy accident. `ProtoServiceAttribute` has two
constructors, and the one-argument form's documentation reads:

> *Generate a client proxy for `contract`. **No server bindings are generated**: use the two-argument
> form in the project that hosts the service.*

The docs also call this the **common** shape: *"service contracts usually ship in a shared package"*, so
a client project references that package and has no implementation to name.

### Why the server needs the implementation, and why that transfers to Connect unchanged

`ProtoServiceAttribute.Implementation`'s remark says it outright:

> *Naming it is what lets the generated server bindings close their generics at compile time:
> `IServiceMethodProvider<TService>` is generic in the implementation, so without one there is nothing
> to instantiate it with and the binding would have to go through `MakeGenericMethod`.*

**That reasoning applies to Connect identically**, because `IConnectServiceBinder<TService>` is generic
in the implementation for exactly the same reason. So the constraint is not inherited from gRPC — it is
re-derived, and would exist even if the vocabulary had not already encoded it.

### Client-only is a genuinely smaller shape, not a trimmed one

`AotConnectSmoke` now carries a second container, `SmokeClientOnly`, declaring only
`[ProtoService(typeof(IFarewell))]`; a check drives it against the service the *first* container hosts.
In real code the two would be different projects, which is the whole point. **20/20**, JIT and native,
still 33 IL warnings.

Its generated surface is **one verb**:

| container | generated surface |
| --- | --- |
| hosting (`[ProtoService(contract, impl)]`) | `CreateClient<TContract>`, `BindServer`, `AddXxx` |
| client-only (`[ProtoService(contract)]`) | `CreateClient<TContract>` |

No `BindServer`, for the generic-closing reason above. And no `AddXxx` either, which is the part worth
noticing: a client needs **no DI registration at all**, because the codec lives on the `ConnectChannel`
rather than in the container. Nothing for a client-only container to add.

### The diagnostic this implies

A consumer calling `BindServer<TContract>` for a contract declared client-only reaches the type-test
chain's fallthrough. The message must name the fix — *"no implementation was named; use
`[ProtoService(typeof(X), typeof(XImpl))]` in the project that hosts the service"* — rather than the
generic "no bindings" it would otherwise say. Better still, the generator can catch it at compile time
when the call names the contract as a literal type argument; worth a `PBN5xxx` when the generator is
written.

## 28. `TService` or `TContract`? — and the ambiguity it exposed

Review asked whether `TContract` was justified against the codebase's `TService`. **It was not** — but
checking turned up something worth keeping.

`TService` already means two different things, in **adjacent lines of the same generated file**
(`Basic.output.cs`):

| | means |
| --- | --- |
| `ClientFactory.CreateClient<TService>` (line 63) | the **contract** — tested against `typeof(IGreeter)` |
| `IServiceMethodProvider<TService>` (line 127) | the **implementation** — `GreeterService` |

The first is protobuf-net.Grpc's own API and the direct analogue of ours; the second is grpc-dotnet's,
and no Connect consumer ever sees it. So inventing `TContract` disambiguated in the **wrong place**: it
diverged from the API a consumer actually meets, to avoid a collision inherited from someone else's.

The fix is the other way round — match protobuf-net.Grpc on the consumer-facing verbs, and rename the
type **we** control to say what it means:

| | |
| --- | --- |
| `CreateClient<TService>`, `BindServer<TService>` | the contract, matching `ClientFactory.CreateClient<TService>` |
| `IConnectServiceBinder<TImplementation>`, the handler delegates, `MapConnectService<TImplementation>` | the implementation |

`IConnectServiceBinder` was named to mirror `IServiceMethodProvider<TService>`, which is why it
inherited the ambiguity in the first place. Mirroring a neighbour's *shape* is worth doing; mirroring
its naming mistake is not.

**The general rule this is an instance of**, since it has now come up repeatedly with this fixture:
matching an existing API is right where a consumer meets both, and wrong where the existing API's choice
was itself accidental. The test is whose documentation the consumer would read, not which code looked
similar.

## 29. The fixture was homogeneous, and it hid an unemittable helper

Review noticed that `Greeter`'s `Unary(name)` / `Streaming(name)` / `Method(type, name)` factories did
not look like generated code. They are not — and the reason is worse than style.

**They only compiled because every method in the fixture shared one request/response pair.** A helper
returning `ConnectMethod<HelloRequest, HelloReply>` is expressible only under that accident; a real
contract has different types per method, so a generator must name each method's own types and can emit
no such helper. The fixture's homogeneity had quietly made an unemittable shape look emittable.

Both halves fixed:

- **descriptors are written out in full**, one `new(...)` per method, which is what a generator emits;
- **`IFarewell` is now genuinely heterogeneous** — `Goodbye(HelloRequest) -> HelloReply` beside
  `Wave(WaveRequest) -> WaveReply` — so the accident cannot return unnoticed.

No bug was found by the change: it compiles, and 20/20 still passes, JIT and native, at 33 IL warnings.
That is the honest result. What it removes is **structural blindness** — the fixture could not have
shown a per-method type-variation problem, and a generator written against it would have inherited the
blind spot.

**This is the same class of thing as "nobody writes `public int @case`"**, recorded in `AGENTS.md`
about the hand-written AOT corpus: a fixture written by one person around one example is uniform in
ways real input is not, and the uniformity is invisible until something leans on it. Worth a pass over
the rest of the fixture on the same question before the generator is written — every method currently
takes exactly one request parameter and a `CallContext`, for instance, and `ContractOperation` in
protobuf-net.Grpc recognises far more shapes than that.

## 30. `static` on the consumer's half should change what is generated — *superseded by §31*

Review's suggestion, and it is a good one: if the consumer declares the container `static`, the
generated methods can carry `this` and become **extension methods**, so binding reads the way .NET
normally does.

```csharp
builder.Services.AddSmokeServices();
app.BindSmokeServices();                    // every service
app.BindSmokeServices<IFarewell>("solo");   // one, with its own conventions
```

Implemented, and **both branches are exercised side by side** so the generator cannot be written for
only one: `SmokeServices` is declared `static` and gets extension methods with no constructor;
`SmokeClientOnly` is declared non-static and gets plain statics plus a private constructor. 20/20, JIT
and native, still 33 IL warnings.

The pleasing part is that it **deletes** something: the separate `SmokeServicesExtensions` class existed
only to supply a static home for `this`. A static container is its own.

### But `CreateClient` must *not* be an extension — proven, not guessed

`channel.CreateClient<IGreeter>()` reads better than `SmokeServices.CreateClient<IGreeter>(channel)`, so
it was worth checking whether it could be an extension on `ConnectChannel`. It cannot. With two
containers in scope that both declare a contract — exactly the `SmokeServices` / `SmokeClientOnly`
arrangement here, and a realistic one — the call is ambiguous:

```
error CS0121: The call is ambiguous between the following methods or properties:
'SmokeClientOnly.CreateClient<TService>(ConnectChannel)' and 'SmokeServices.CreateClient<TService>(ConnectChannel)'
```

**The rule that falls out is the useful part: an extension method is only collision-safe if its *name*
carries the container's.** `AddSmokeServices` and `BindSmokeServices` do, so two containers can both
emit them and nothing is ambiguous. `CreateClient` does not, so it cannot be an extension — and the
alternative, `CreateSmokeServicesClient<T>(this ConnectChannel)`, is worse than the plain static.

So: `Add`/`Bind` become extensions when the container is static; `CreateClient` stays a plain static
either way. That asymmetry is not arbitrary, and it independently justifies naming the methods after the
container rather than generically.

### Naming: same name, different arity

`BindSmokeServices()` and `BindSmokeServices<TService>()` share a name and are told apart by generic
arity. Singularising the one-service form (`BindSmokeService<T>`) reads better, but deriving "Service"
from "SmokeServices" means stripping a trailing "s" — string surgery on a consumer's identifier, which
breaks or reads oddly for a container named `Backend`, `Rpc` or `Api`. The arity split needs no
guessing and is unambiguous to the compiler. Open to revisiting; it is a one-line change in the emitter.

## 31. Always emit a companion `{Root}Extensions`, with per-contract client factories

Supersedes §30's conclusion. Review's refinement, and it is better: emit the fluent surface into a
static `{Root}Extensions` type **always**, rather than inlining it when the container happens to be
`static`.

Two things improve. The generator stops branching on **where** members go — `static` on the consumer's
half now decides only whether a private constructor is emitted, which is a much smaller thing to get
right. And the fluent form works for a non-`static` container too, which §30's shape could not offer at
all.

```csharp
builder.Services.AddSmokeServices();
app.BindSmokeServices();                   // every service
app.BindSmokeService<IFarewell>("solo");   // one, with its own conventions
var greeter = channel.GreeterClient();     // per-contract client factory
```

| | |
| --- | --- |
| the container | `CreateClient<TService>`, `AddServices`, `BindServices`, `BindService<TService>` — plain statics, never ambiguous |
| `{Root}Extensions` | the same four as extensions, plus **one client factory per contract** |

### Per-contract client factories are what make this safe — measured

§30 established that a generic `CreateClient<TService>(this ConnectChannel)` is unusable: it is
ambiguous between **any** two containers in scope, whatever they declare. Naming the factory after the
contract narrows that sharply, and the fixture pins both sides by declaring `IGreeter` in one container
and `IFarewell` in two:

| call | result |
| --- | --- |
| `channel.GreeterClient()` — one container declares it | **works** |
| `channel.FarewellClient()` — two containers declare it | `CS0121`, naming both |

Three properties worth having, all confirmed rather than assumed:

- **Declaring both is fine.** Extension ambiguity is a *call-site* error, so two containers can emit
  colliding factories and nothing breaks until someone uses the fluent form for a shared contract.
- **The failure is a compile error naming both candidates**, not a runtime surprise.
- **There is an unambiguous escape hatch**: `SmokeServices.CreateClient<IFarewell>(channel)`, which is
  why the generic form stays on the container rather than moving out.

### The diagnostic this implies

Within one compilation the generator sees every container and every contract, so it can warn when two
declare the same one: *"`IFarewell` is declared by both `X` and `Y`; `channel.FarewellClient()` will be
ambiguous — use `X.CreateClient<IFarewell>(channel)`"*. Worth a `PBN5xxx`.

Across assemblies it cannot know, and does not need to: the consumer gets `CS0121` naming both
candidates, which is self-explanatory. Emitting always and warning where we can see beats suppressing.

### Naming

`AddSmokeServices` / `BindSmokeServices` / `BindSmokeService<T>` — the container's name carries, so
these cannot collide between containers however many there are. The singular/plural split is now
natural rather than surgical, since the extension names are built from the container name plus a
literal suffix rather than by stripping anything.

## 32. No generics in the extension surface, and no surgery on the consumer's name

Review caught that `BindSmokeService<T>` was `Bind` + `SmokeService` — **stripping a trailing `s` from
the consumer's own identifier**, since "Services" is part of the container's name (`SmokeServices`) and
not a suffix we add. Exactly the string surgery §30 had claimed to be avoiding, done two sections later
without noticing.

The obvious repair, a bare `Bind<TService>()`, **reintroduces the ambiguity §31 was built to avoid**.
Measured rather than reasoned, by giving two containers one each:

```
error CS0121: The call is ambiguous between
'SmokeClientOnlyExtensions.Bind<TService>(IEndpointRouteBuilder)' and
'SmokeServicesExtensions.Bind<TService>(IEndpointRouteBuilder)'
```

Same failure as the generic `CreateClient<TService>`, for the same reason: the name carries nothing
container-specific.

### The rule, now uniform

| operation covers | named after | example |
| --- | --- | --- |
| **all** services | the **container** | `AddSmokeServices`, `BindSmokeServices` |
| **one** contract | the **contract** | `BindGreeter`, `BindFarewell`, `GreeterClient`, `FarewellClient` |

**There are no generics in the extension surface at all**, which is what makes it safe: every name
carries either the container or the contract, so the only possible collision is the narrow one — two
containers declaring the same contract — and that is a call-site `CS0121` naming both candidates, with
`SmokeServices.CreateClient<IFarewell>(channel)` as the way through.

And nothing is derived by mutilating an identifier. `AddSmokeServices` is `Add` + the container's name;
`BindGreeter` is `Bind` + the contract's, with the conventional leading `I` dropped as .NET does
everywhere. The singular/plural distinction that caused this stopped existing once the names were
composed from two sources rather than one.

The generic forms live on the container — `BindService<TService>`, `CreateClient<TService>` — where
they cannot be ambiguous, and are the escape hatch when two containers do share a contract.

## 33. `Add` gets the per-service form too

Review spotted the imbalance: `Bind` had both an all-services and a per-service form, `Add` only the
former. **Not deliberate** — it fell out rather than being reasoned.

On inspection the asymmetry was *defensible*: `Bind`'s per-service form earns its place because endpoint
conventions are per-endpoint and there is no other way to make them differ, whereas a consumer wanting
different DI registration can simply register it themselves first — `TryAdd` throughout means ours
yields. But that is an argument about **need**, and an asymmetric surface has its own cost: someone who
learns `Bind` has both forms goes looking for `AddGreeter` and does not find it. It is cheap, so it is
now symmetric.

| | all services | one contract |
| --- | --- | --- |
| register | `AddSmokeServices` | `AddGreeter`, `AddFarewell` |
| map | `BindSmokeServices` | `BindGreeter`, `BindFarewell` |
| client | — | `GreeterClient`, `FarewellClient` |

### Which exposed a real trap: the codec is *not* per-service

Every `Add` entry point needs the codec registered, but the codec is container-level. So calling
`AddGreeter()` and `AddFarewell()` must not register it twice.

**The guard has to live inside the configure delegate, not around `AddConnect`.** Options delegates
accumulate and *all* of them run at resolution, so checking "have I already called `AddConnect`?" at
registration time looks right and still ends up with two codecs. Checking `options.Codecs` from inside
the delegate is what actually works, because by then the earlier delegate has run.

### A duplicate codec would not have failed anything else

Worth stating, because it is why this needed its own check rather than trusting the suite:
`SelectCodec` iterates the list and takes the first match, so a second identical codec is **invisible**
to every other check. The harness now asserts `options.Codecs.Count == 1` directly by resolving
`IOptions<ConnectServerOptions>`, after `AddSmokeServices()`, `AddGreeter()` and `AddFarewell()` have
all been called.

**Verified able to fail**, not merely observed to pass: removing the guard gives *"expected exactly one
codec, found 3"* and a non-zero exit.

## 34. Step one of the generator: making the parse shareable

`GrpcProxyGenerator`'s contract parse is what a Connect generator wants to reuse — it recognises every
method shape (`GrpcMethodKind`, `GrpcContextKind`, `GrpcArgShape`, `GrpcResultShape`, `VoidRequest`/
`VoidResponse`, overloads, `[SubService]`, closed generics, WCF markers) and produces plain data with no
Roslyn references. `AGENTS.md` mandates sharing it rather than forking.

### It was already almost transport-neutral

All ten `"Grpc..."` occurrences in its 753 lines turned out to be, on inspection:

| | |
| --- | --- |
| namespace literals for **type matching** — `IsType(type, "Grpc.Core", "CallOptions")` | correct for both generators; **both** reject those types |
| comments | harmless |
| **one clause**, at three sites | the only real problem |

My first estimate of "three places need parameterising" was wrong: it is *one* clause appearing three
times, two of those being the same expression. The genuinely gRPC-worded diagnostics — *"generates gRPC
proxies"*, *"build-time gRPC proxies"* — all live in `.Diagnostics.cs` and `.Emit.cs`, which a Connect
generator gets its own copies of.

### The clause, and why a neutral noun would not have fixed it

> *a shape only **the runtime proxy handles** — Stream, `IObservable<T>` and `Grpc.Core`'s own call
> types are reshaped at run time*

The type list is fine for both — those really are `Grpc.Core` types, and both generators reject them.
What does not transfer is the assertion that **a fallback exists**: true for gRPC, where the reflective
`CreateGrpcService` handles them; false for Connect, which has no runtime path at all. So substituting
"service" or "RPC" for "gRPC" would not have helped — there is no gRPC-flavoured noun, there is a claim
about a runtime that only one of the two has.

It is now one `private const RuntimeOnlyShapes`, saying what is wrong and nothing about what handles it.

**And gRPC loses nothing**, which is the part worth knowing: `PBN4002`'s own `messageFormat` *already*
ends *"the whole contract is left to the runtime proxy, which is not trim/AOT-friendly"*. The reason
string was duplicating it — the old message said "runtime proxy" twice. Neutralising made the gRPC
diagnostic **better**, not merely compatible.

### The change was unverified until a fixture was added

539 tests passed and **no golden moved** — which is not reassurance, it is the finding. The only PBN4002
fixture exercises a *different* reason ("it is generic"), and `GrpcDroppedUnderAotTests` asserts only
the id, so none of the three rejection paths had its wording pinned by anything.

`MethodShape.input.cs` now carries an `IObservable<Reply>` member, which pins it. Its comment was also
stale: it claimed `Task<Stream>` was refused, which stopped being true when byte streaming landed — the
fixture now keeps `Task<Stream>` deliberately, as the contrast of a shape that once belonged on the list
and no longer does.

## 35. Code-first clients in DI — logged, not built

A consideration to carry into the generator rather than a decision. **Nothing here is verified**; it is
a reading of the two existing designs and what they imply for Connect. Sources:

- <https://learn.microsoft.com/aspnet/core/grpc/clientfactory> — the Microsoft one, `Grpc.Net.ClientFactory`
- <https://github.com/protobuf-net/protobuf-net.Grpc/blob/main/src/protobuf-net.Grpc.ClientFactory/readme.md>

### The two existing shapes

| | `Grpc.Net.ClientFactory` | `protobuf-net.Grpc.ClientFactory` |
| --- | --- | --- |
| registers | `AddGrpcClient<Greeter.GreeterClient>(o => o.Address = ...)` | `AddCodeFirstGrpcClient<IMyAmazingService>(o => o.Address = ...)` |
| `T` is | the **generated concrete client class** | the **contract interface** |
| you inject | `Greeter.GreeterClient` | `IMyAmazingService` |
| returns | an `IHttpClientBuilder`, which is what makes the chaining work | the same, built on top of it |

The code-first difference is the one that matters: **you depend on the contract**, not on a generated
class. That is the shape Connect should keep.

Everything `Grpc.Net.ClientFactory` chains onto that builder is worth cataloguing, because it is the
list of things a Connect equivalent will be compared against:
`ConfigurePrimaryHttpMessageHandler`, `AddInterceptor<T>` (with `InterceptorScope`), `ConfigureChannel`,
`AddCallCredentials`, `EnableCallContextPropagation`, and **named clients** via
`AddGrpcClient<T>("name", …)` + `GrpcClientFactory.CreateClient<T>("name")`.

### Connect should have an easier time of it, for one structural reason

gRPC's factory exists partly to manage a `GrpcChannel` wrapped around an `HttpMessageHandler`, which is
why it needs `ConfigureChannel(GrpcChannelOptions)` at all. **We are already on `HttpClient`**, so a
Connect client factory is closer to plain `IHttpClientFactory` usage: take the `HttpClient` it hands
out, wrap a thin `ConnectChannel` round it, done. There is no channel-options surface to mirror.

**And interception is already solved, by the framework rather than by us.** `AddInterceptor<T>` and
`AddCallCredentials` exist because `CallInvoker` has its own interception model. Connect's interception
model is `DelegatingHandler` — so `.AddHttpMessageHandler<AuthHandler>()` does that job with the
standard .NET mechanism people already know, and Polly arrives through
`Microsoft.Extensions.Http.Resilience` without us doing anything. That is a real simplification rather
than a gap.

### The shape it probably wants, and why it needs no new naming rule

Registration needs to know *which container* knows how to build the proxy — so it cannot be a bare
`AddConnectClient<IGreeter>`. But §32's rule already answers this: **contract-named, so collision-safe**:

```csharp
builder.Services.AddGreeterClient(o => o.Address = new Uri("https://..."))
    .AddHttpMessageHandler<AuthHandler>();
```

returning `IHttpClientBuilder` so the standard chain works, and registering `IGreeter` as transient.
It sits alongside `AddGreeter()` (server-side registration) and `GreeterClient(this ConnectChannel)`
(the manual factory) without inventing anything.

### Two things to decide when it is built

- **Deadline and cancellation propagation.** `EnableCallContextPropagation()` reads the ambient server
  call's deadline and token and applies them to outgoing calls. The Connect equivalent is well-defined -
  `connect-timeout-ms` in, `connect-timeout-ms` out - and is the sort of thing that is much easier to
  build in than to retrofit, because it wants a place to hang ambient state.
- **Named clients**, for two configurations of one contract. `IHttpClientFactory` has named clients
  already, so this may cost nothing; worth checking rather than assuming.

### It does *not* revive the instance-vs-static question

§30 flagged that a DI client-factory story was the thing that might force the container to be an
instance. On this reading it does not: the registration can be a generated static that closes over the
container's static `CreateClient<TService>`, and what DI holds is the resolved `IGreeter`, not the
container. The container stays static.

## 36. The generator exists, and the hand-written halves are gone

`ProtoConnectGenerator` emits what `Services.HandWritten.cs` and `ClientOnly.HandWritten.cs` used to
say, and **both files are deleted**. `AotConnectSmoke` now compiles generated code and reads **22/22**,
JIT and native, at **33 IL warnings** — the same as when it was hand-written, which is the result worth
having: the generator's output is not merely similar, it passes the same external-wire checks, the
duplex interleaving proof, the trailers-in-terminator check and the client-only hand-off.

That is the `AotGrpcSmoke` trick: the smoke test now proves the *generator* rather than my typing.

### It reuses the gRPC parse, and that was the whole bet

`ParseContract` is called directly, so every method shape, context kind, void/`Empty` rule,
`[SubService]` walk, overload and closed generic arrives already classified. The Connect generator is
an **emitter**, and `GrpcMethodKind` → `ConnectMethodType` is a rename.

`GrpcDiagnosticKind` is reused too, mapped to `PBN5000`–`PBN5006`. Its members describe what is wrong
with a *contract* — `LanguageVersionTooLow`, `UnsupportedMethodShape`, `NotAServiceContract` — which is
transport-neutral however the enum is named, so a parallel set would only have been something to keep
in step.

### Three things the build caught, each a documented trap firing exactly as recorded

- **`RS2000`**: `PBN5000`–`PBN5006` were a build break until listed in `AnalyzerReleases.Unshipped.md`.
  `AGENTS.md` says release tracking is enforced; it is.
- **`protobuf-net.BuildTools.Legacy`**: `Internal/**` is a glob there and `Internal/Grpc` is
  `Compile Remove`d, so the new `Internal/Connect` arrived *without* the `DiagnosticInfo` and
  `GrpcInterfaceModel` it reuses. Not dead weight — a build break. Same trap
  `UseAotModelCodeFixProvider` hit, found by the traversal build rather than by remembering.
- **Service names.** The generated descriptors derive the name from `[Service]`, which with no argument
  gives `{namespace}.{name-without-I}` — so the raw-HTTP checks 404'd against their hard-coded
  `aotconnectsmoke.v1.Greeter`. The fixture now **pins** the name, which is what a consumer does for
  cross-language interop anyway, and the checks stating the wire name independently is exactly what made
  the mismatch visible rather than silently agreeing.

### Not done

- **Golden tests.** The generator has no `Connect/Data/*.input.cs` fixtures yet; `AotConnectSmoke` is
  currently the only thing exercising it. That is the next piece, and it needs a `_ConnectSurface.cs`
  snapshot so the emitted code is *compiled* in the test, as the gRPC goldens do.
- **Serializers are not hoisted.** The emitted descriptors omit the optional pre-resolved serializers,
  so the codec resolves per message. Closing that needs `ProtoModelGenerator` to emit the
  `Serializer<T>()` accessor described in §19 — a model-generator change, not a Connect one.
- **`Task<Stream>` byte streaming** (`GrpcResultShape.TaskStream`) is classified by the shared parse but
  not emitted; it currently falls through to the catch-all. Needs either a Connect answer or an explicit
  refusal.

## 37. Golden tests, and refusing byte streaming

### `Task<Stream>` is refused — but not for the reason it looks

Review's instinct was that byte streaming needs full duplex. **It does not**: `notes/aot/grpc.md` records
that protobuf-net.Grpc carries `Task<Stream>` as a **server-streaming call of `BytesValue`**, and we have
server streaming. So this is a *gap*, not an impossibility.

What is actually missing is two things the gRPC runtime supplies: a bytes-carrier message with its own
marshalling (`BytesValue`, which `MarshallerCache` pre-seeds so it never reaches the `TypeModel`), and
the `Stream`↔chunks reshape — `Reshape.WriteStream` on the server, `ServerByteStreaming{Task,ValueTask}Async`
on the client — both of which work against `Grpc.Core`'s writer types rather than ours.

It is now refused through `PBN5001` with a reason saying exactly that. **No separate id**: `PBN5001`'s
format already carries a per-method reason, and a registered-but-unused id is worse than none. Refusing
loudly beats emitting something that compiles and then truncates a download.

### The goldens exist, and the first one earned its keep immediately

`src/BuildToolsUnitTests/Connect/` on the same harness as the other two generators — `*.input.cs` paired
with `*.output.cs` and `*.txt`, both rewritten each run then asserted. **540 tests**, and the first
fixture covers all five method shapes plus both context kinds.

Two surface snapshots are compiled alongside: the **gRPC** one for the contract vocabulary Connect
shares (`[Service]`, `[ProtoService]`, `CallContext`) and a new `_ConnectSurface.cs` for the transport.
The point is that the generated code is **compiled** in the test, and it paid for itself on the first
run by reporting three errors:

- `ServiceCollectionDescriptorExtensions.TryAddScoped` was not in the snapshot;
- `ProtoConnectCodec` did not derive from anything `ConnectServerOptions.Codecs` would accept.

Both were snapshot gaps rather than generator bugs — which is the *good* failure mode, and precisely
what would otherwise have surfaced in a consumer's build.

`CallContext` in the gRPC snapshot also gained `CallOptions` and `CancellationToken`. The real type has
both; the snapshot simply had not needed them, and the Connect proxy reads them. That makes the shared
snapshot a more accurate picture of protobuf-net.Grpc than it was, which is the right direction for a
file whose whole risk is drift.

### Still to do on the generator

- **More fixtures.** One covers the happy path; the refusals and the shapes `ContractOperation`
  recognises but this fixture does not use - void/`Empty`, `ValueTask`, no context, overloads,
  `[SubService]` - are unpinned. §29's warning about a uniform fixture applies here with more force now
  that goldens exist to hold the answers.
- **Serializer hoisting**, which needs `ProtoModelGenerator` to emit the `Serializer<T>()` accessor (§19).
- **Contract-name collisions.** `ContractName` takes the simple name; two contracts of the same simple
  name in one container would collide, and the generator can see that and qualify.

## 38. Backlog: contract-first gRPC customers — easier than code-first, possibly much

An idea, not a plan. But it is worth recording at length because the evidence says it is *easier* than
what we have just built, and in one respect it gets a feature code-first cannot have yet.

"Contract-first" here means the ordinary Microsoft path: `.proto` files, `Grpc.Tools`, `protoc`-generated
`Greeter.GreeterClient`, `Greeter.GreeterBase`, and Google.Protobuf messages. No protobuf-net anywhere.

### The server hook already exists, and says so

`Grpc.Core.ServiceBinderBase`'s own documentation:

> *Allows binding server-side method implementations in **alternative serving stacks**. Instances of
> this class are usually populated by the `BindService` method that is part of the autogenerated code
> for a protocol buffers service definition.*

Connect **is** an alternative serving stack. And its four members are
`AddMethod<TRequest, TResponse>(Method<,>, {Unary|ServerStreaming|ClientStreaming|DuplexStreaming}ServerMethod<,>)`
— which map **one to one** onto `ConnectServiceBinderContext`'s four `Add*Method` calls.

So the server side is: subclass `ServiceBinderBase`, override the four, translate each into a Connect
endpoint. **No generator, and no reflection** if the consumer hands over the generated static directly:

```csharp
app.MapConnectService<GreeterImpl>(Greeter.BindService);
```

(`BindService(ServiceBinderBase, TBase)` is generated public and static. Reflecting to *find* it, the way
`BinderServiceMethodProvider` does, is the automatic-but-reflective variant; naming it is the AOT-clean
one, and it is one token more to type.)

### The client hook is one we already wanted

A generated `Greeter.GreeterClient` derives `ClientBase` and takes a `CallInvoker`. So §8.1's
`ConnectCallInvoker` gives **contract-first clients Connect with no changes at all** — and here it is
cleaner than for code-first, because no protobuf-net is involved.

### The payloads need no conversion

For `application/proto`, a Connect message body and a gRPC message body are **the same bytes** - only
the framing, the status and the trailers differ. The generated `Marshaller<T>` is already correct. So a
binary bridge re-serializes nothing; it re-frames.

### And JSON would come nearly free — which is the striking part

Google.Protobuf ships `JsonFormatter` and `JsonParser`, implementing canonical Protobuf JSON. So a
contract-first Connect implementation could offer `application/json` **without our writing a JSON codec
at all** - the thing §4 calls the long pole for code-first.

**That inverts the usual expectation.** The code-first path is the one we have built and the one that
cannot do JSON until protobuf-net grows a codec; the contract-first path is the one we have not built
and could do JSON on day one.

The catch, and it is the same one as everywhere: `Marshaller<T>` is codec-fixed, so a method descriptor
cannot carry both encodings. Selecting JSON means going around the marshaller, which needs the message's
`Parser` - reachable by testing `TRequest` for `IMessage<T>`, concrete at the call site and so AOT-safe,
but it is a route rather than a free ride.

### Why this might be worth doing early rather than late

- it needs **no generator**, so it does not wait on anything in §36-§37;
- it reuses `ConnectCallInvoker`, which was already the highest-value/lowest-cost item;
- it is the population that most obviously wants Connect - people with `.proto` files usually have
  other languages to talk to, which is Connect's whole pitch;
- and it would let the conformance suite be run with **JSON declared**, which our code-first path cannot
  do yet.

Unverified: that `protoc`'s C# output emits `BindService(ServiceBinderBase, TBase)` in current
`Grpc.Tools` (strongly implied by `ServiceBinderBase`'s documentation and by how
`BinderServiceMethodProvider` works, but not checked here - there is no `Grpc.Tools` in this tree).

## 39. Contract-first, probed — the hook is confirmed; the codec is the work

`src/ConnectContractFirst` exists: a `.proto`, `Grpc.Tools`, and `protoc` generating ordinary
Google.Protobuf messages plus `Greeter.GreeterBase`/`GreeterClient`. Nothing protobuf-net anywhere,
which is the point. `Grpc.Tools` is now centrally versioned; it restores and generates without trouble.

### The binding hook is exactly what §38 predicted

```csharp
public static void BindService(grpc::ServiceBinderBase serviceBinder, GreeterBase serviceImpl)
{
  serviceBinder.AddMethod(__Method_SayHello, serviceImpl == null ? null
      : new grpc::UnaryServerMethod<HelloRequest, HelloReply>(serviceImpl.SayHello));
  ...
}
```

Public, static, one `AddMethod` per operation, all four shapes — and the `serviceImpl == null` branch
confirms the descriptors-only mode that `BinderServiceMethodProvider` uses. A consumer can hand it over
as a delegate, so **no reflection is needed**.

**Caveat, from its own doc comment:** *"part of an experimental API that can change or be removed
without any prior notice."* Worth knowing before building a product on it — though grpc-dotnet's own
server binding depends on it too, which limits how freely it can move.

### But the payload codec is per-method, and that is the actual work

§38 said payloads need no conversion, which is true, and then under-read what follows from it. The
generated descriptors carry their own marshallers:

```csharp
static readonly Marshaller<HelloRequest> __Marshaller_..._HelloRequest =
    Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, HelloRequest.Parser));

static readonly Method<HelloRequest, HelloReply> __Method_SayHello =
    new(MethodType.Unary, __ServiceName, "SayHello", __Marshaller_..._HelloRequest, __Marshaller_..._HelloReply);
```

So the codec is **per method**, and Google.Protobuf's, whereas `ConnectCodec` today is **per channel**
and protobuf-net's. A Google.Protobuf message is not a protobuf-net contract, so `ProtoConnectCodec`
cannot serialize one at all.

### Which points at a generalisation worth having anyway

`ConnectMethod<TRequest, TResponse>` should carry **optional per-message codecs** rather than
protobuf-net `ISerializer<T>`s specifically — something as small as
`{ long? Measure(T); void Write(IBufferWriter<byte>, T); T Read(in ReadOnlySequence<byte>); }`. Then:

- the channel codec stays the default, for code-first;
- a marshaller-backed codec plugs in per method, for contract-first;
- the §19 serializer hoist becomes a *case* of this rather than a separate hook;
- and a **JSON** codec becomes per-method too, which is how contract-first would reach
  `JsonFormatter`/`JsonParser` without a protobuf-net JSON codec existing.

That is a real refactor of `ConnectMethod` and `ConnectCodec`, but it is the same refactor three
different things want, which is usually the sign it is the right one.

### One more shared piece

`Marshallers.Create` takes the *contextual* form — `Action<T, SerializationContext>` and
`Func<DeserializationContext, T>` — so using a marshaller means implementing `SerializationContext` and
`DeserializationContext`. **That is the same machinery §8.1's `ConnectCallInvoker` needs**, and §12 lists
it as unverified. So the two items share a prerequisite and should probably be done together.

### Revised estimate

The *binding* is easy and confirmed. The *codec* is a day or two of real design, and it is shared work
rather than contract-first-specific. §38's "easier than code-first" still looks right for the server
shape, but not for the total: what contract-first buys is JSON, and what it costs is the per-method
codec seam that code-first has so far not needed.

## 40. Contract-first: what the consumer writes, and what the shapes cost

Review asked how the user-provided half gets annotated for contract-first, given there is **no
interface** - just a generated abstract base and a client class. Probed rather than guessed.

### The annotation already exists, and protoc writes it

```csharp
[grpc::BindServiceMethod(typeof(Greeter), "BindService")]
public abstract partial class GreeterBase { ... }
```

That attribute names both the holder type and the method, and it is how `Grpc.AspNetCore.Server` finds
`BindService` from a service type. **So the consumer writes nothing** - no `[ProtoConnect]`, no
`[ProtoService]`, no partial class. Two shapes, both attribute-free:

| | |
| --- | --- |
| `app.MapConnectService<GreeterImpl>()` | walks the base chain for `[BindServiceMethod]` and invokes it — **reflective**, startup-only, annotatable, and exactly what `BinderServiceMethodProvider` does |
| `app.MapConnectService<GreeterImpl>(Greeter.BindService)` | the same, named explicitly — **no reflection at all**, one token more |

Which also settles a question §38 left open: **contract-first needs no generator**, and therefore none
of the container/`Extensions`/naming design that code-first needed. `[ProtoConnect(Model = ...)]` would
be actively wrong here, since there is no protobuf-net model to name.

### But the handler shapes are gRPC's, not ours

This is the part the earlier notes missed. The generated base is writer-and-reader shaped:

```csharp
public virtual Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context);
public virtual Task Subscribe(HelloRequest request, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context);
public virtual Task<HelloReply> Collect(IAsyncStreamReader<HelloRequest> requestStream, ServerCallContext context);
public virtual Task Chat(IAsyncStreamReader<HelloRequest> requestStream, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context);
```

Our invokers are `IAsyncEnumerable`-shaped. So the adapter needs **`IServerStreamWriter<T>` over our
`PipeWriter` and `IAsyncStreamReader<T>` over our `PipeReader`** - mechanical, bounded, and exactly the
shims grpc-dotnet already has, but not nothing. That is a second work item beside the per-method codec.

### Two smaller things the shapes imply

- **The methods are `virtual`, not `abstract`**, and each throws `RpcException(Unimplemented)` by
  default. So an implementation overrides only what it serves, and the binder captures handlers for
  everything - including methods that will throw. `RpcException` therefore has to map onto
  `ConnectException`, and `StatusCode` → `ConnectCode` is a cast, since §15 aligned the ordinals.
- **`ServerCallContext` is already what we supply.** `ConnectServerCallContext` derives from it, so the
  generated handlers can be handed ours unchanged - which is a nice dividend from §17's decision to
  implement that rather than invent a context.

### Revised shape of the work

| | |
| --- | --- |
| consumer annotation | **nothing** — protoc already did it |
| generator | **none needed** |
| binding | `ServiceBinderBase` subclass; confirmed, easy |
| per-method codec | the `Marshaller<T>` seam — done as of §39's refactor, needs a marshaller-backed implementation |
| stream shims | `IServerStreamWriter<T>` / `IAsyncStreamReader<T>` over pipes — **new, and the bulk of it** |
| errors | `RpcException` → `ConnectException`, ordinals already aligned |

## 41. Contract-first works, end to end, on all four shapes

`src/ConnectContractFirst` is a `protoc`-generated `Greeter` — Google.Protobuf messages, protoc's
`GreeterBase`/`GreeterClient`, a `.proto` with no annotation of ours — served over Connect. **The
consumer's entire opt-in is one line:**

```csharp
builder.Services.AddConnect(o => o.Codecs.Add(MarshallerConnectCodec.Instance));
builder.Services.AddScoped<GreeterImpl>();
app.MapConnectService<GreeterImpl>(Greeter.BindService);   // <- protoc's own method group
```

No generator, no attribute, no change to the contract. 18 checks pass across unary, server-streaming,
client-streaming and duplex, plus `RpcException` mapping. **The checks drive the server with a raw
`HttpClient` and hand-built envelopes**, not with our own `ConnectChannel`: a matched pair of bugs in
our two halves would pass a self-test and prove nothing about interoperability.

### The pieces, and why each is shaped as it is

- **`MarshallerMessageCodec<T>`** wraps a `Grpc.Core` `Marshaller<T>` as an `IConnectMessageCodec<T>`,
  over a `SerializationContext`/`DeserializationContext` pair implemented on `IBufferWriter<byte>` and
  `ReadOnlySequence<byte>`. Nothing is re-encoded: for `application/proto` a Connect body and a gRPC
  body are *the same bytes*, so this is the marshalling the gRPC path would have done, reached through
  different framing.
- **`MarshallerConnectCodec`** is the channel-level codec such an app registers. It marshals nothing —
  a contract-first app has no `TypeModel`, and could not have one, since a Google.Protobuf message is
  not a protobuf-net contract — and exists because the codec *name* is what a content-type selects on
  both sides. Its core methods throw, saying the method arrived without a codec, rather than writing an
  empty message.
- **`GrpcStreamAdapters`** bridges protoc's reader/writer-shaped handlers to our `IAsyncEnumerable`
  invokers. Only the writer side needs a buffer (pulling from a push), and it is a `Channel` **bounded
  at one**, so a handler's `WriteAsync` completes only once the previous message reached the wire —
  the backpressure a gRPC handler already expects.
- **`ContractFirstServiceBinder<T>`** adapts `ServiceBinderBase` to `IConnectServiceBinder<T>`.

### Two phases, and no reflection anywhere

The problem: protoc captures the *instance* in the handler — `new UnaryServerMethod<,>(serviceImpl.SayHello)`
— so a bind yields handlers closed over one object, while ASP.NET wants a per-request instance.
`Grpc.AspNetCore.Server` solves this by binding with `null` and then resolving each method **by name**
through reflection. We do not, and do not need to:

- **startup**: bind with `serviceImpl: null`. protoc emits `serviceImpl == null ? null : new ...`, so
  every `Method<,>` descriptor arrives with no handler. That null-tolerance is not incidental — it is
  there so grpc-dotnet can do exactly this — and it is enough to build every endpoint without
  constructing the consumer's service.
- **per call**: bind again against the DI-resolved instance and take the handlers. A one-entry cache
  keyed on the instance makes a **singleton** service bind once for the life of the process; a
  **scoped** one costs a handful of delegate allocations per call, and no reflection at all.

The shape is read from the descriptor rather than from which `AddMethod` overload we are in, so
`ConnectServiceBinderContext`'s existing "this method says it is unary" check stays a real check
instead of a tautology.

### What contract-first does *not* get, and it is not cosmetic

**`[Authorize]` is not honoured unless it is supplied.** `Grpc.AspNetCore.Server` collects endpoint
metadata by reflecting over the implementation's methods; this path does not reflect, so an
`[Authorize]` on a contract-first service method silently produces a **more permissive endpoint with
no error anywhere** — the exact failure `AotGrpcMetadataDiff` exists to prevent on the code-first side.
`MapConnectService` therefore takes a `Func<IMethod, IReadOnlyList<object>>? metadata` parameter rather
than defaulting to none quietly. Closing this properly is backlog, and the honest options are a
generator (which would need the consumer's implementation type) or documented reflection behind a
switch.

Also absent: `idempotency_level` is in the descriptor set but not on `Method<,>`, so there is nothing to
read and every RPC stays POST.

### Google.Protobuf calls `Advance(0)` before its first `GetSpan`

Worth its own heading, because it cost the first run and nothing about it is guessable. Writing a
message to a logging `IBufferWriter<byte>` gives exactly:

```
Advance(0), GetSpan(0), Advance(14)
```

Kestrel's response writer rejects a leading bare `Advance` — *"Invalid ordering of calling StartAsync or
CompleteAsync and Advance"* — so handing it straight to a marshaller throws, and the endpoint's
catch-all turns that into a bare `{"code":"internal"}`. The fix is one line in the codec: call
`destination.GetSpan(1)` before handing the writer over. An unused `GetSpan` is free and unambiguously
legal; buffering instead would cost a copy per message. This is also *why* grpc-dotnet's
`DefaultSerializationContext` owns its own buffer rather than passing the pipe through — a detail that
reads as an optimisation and is not.

**Measured, not inferred.** The first diagnosis (that this was a `Content-Length` problem) was wrong;
a ten-line probe with a logging writer settled it in one run.

### A latent bug the marshaller found: measuring with the wrong codec

`IConnectMessageCodec<T>` is a fast path for protobuf-net and was *always* the encoder where present —
but five envelope sites measured with the **channel** codec and then wrote with the **per-method** one.
For protobuf-net that is the same answer twice, so nothing showed. A marshaller cannot measure at all,
which turned a silent inconsistency into a visible throw.

A length header that describes different bytes than the ones that follow desynchronises the stream for
every message after it, with no error at the point of the mistake. All five now go through one
`ConnectEnvelope.WriteMessage`, which measures and writes with the same codec, and buffers when the
codec cannot measure. **The general lesson is the one this repo keeps relearning**: a second
implementation of an interface is the only thing that finds the places where the first one's
coincidences were being relied on.

### Status

- `dotnet run --project src/ConnectContractFirst` — 18/18.
- `src/AotConnectSmoke` — 22/22, unchanged. `src/ConnectProbe` — 9/9 against connect-go.
- `BuildToolsUnitTests` — 540/540. Traversal build clean.

## 42. The generated *client* works too, unchanged

`ConnectCallInvoker : CallInvoker` completes the claim from the other end:

```csharp
var client = new Greeter.GreeterClient(new ConnectCallInvoker(http, baseAddress));
```

protoc's own client type, no subclassing, no wrapper. All four shapes pass, plus the blocking unary
overload, plus `RpcException` fidelity in both directions — 27 checks in `src/ConnectContractFirst`.

**`CallInvoker` is the only way in, and that is the design rather than luck.** A generated client keeps
its `Method<,>` descriptors in `private static readonly` fields and routes every call through its
invoker, so there is no other seam — and no need for one.

The headline: **everything but duplex now runs over HTTP/1.1.** The generated client is unchanged; only
the transport under it moved.

### Metadata is where the fidelity is, and a bare sequence loses it

The first cut drove the streaming shapes from `ConnectChannel.ServerStreaming`/`Duplex`, which return a
bare `IAsyncEnumerable`. That compiles, passes a naive test, and is wrong twice:

- `ResponseHeadersAsync` would resolve only when the *stream ended*, where a gRPC caller may await it
  before reading any message;
- trailers would come back **empty**, because Connect delivers trailing metadata in the terminating
  envelope and a bare sequence never exposes it.

Driving from `ServerStreamingAsync`/`DuplexAsync` — which return `ConnectServerStream<T>`, carrying
`Headers` at the start and `Trailers` after the terminator — fixes both. Client-streaming needed a new
`ClientStreamingWithMetadataAsync` for the same reason, symmetric with the unary one that already
existed.

This is pinned rather than asserted: the fixture's `Subscribe` sets a leading header *and* a trailer,
and the generated client reads both back. **That trailer travelled in the terminating envelope over
HTTP/1.1** — which is the entire argument for Connect, demonstrated through an unmodified gRPC client.

### Decisions worth keeping

- **A per-call `host` is refused, not ignored.** gRPC's per-call host overrides the channel authority;
  ignoring it would silently send the call somewhere the caller did not ask for. Generated clients
  always pass `null`.
- **`BlockingUnaryCall` blocks on the async path**, because there is no synchronous transport under
  `HttpClient` and generated clients expose the overload regardless. Refusing would make an ordinary
  client partly unusable for no gain.
- **The status mapping is two explicit tables, not a cast** — `ConnectException.FromRpcException` and
  `ToStatusCode`. The ordinals *do* line up today; they are maintained by different people in different
  repositories, and this codebase has already shipped exactly that bug once (`DataFormat` to
  `ProtoDataFormat`). The two directions are separate maps because they are not quite inverses: every
  gRPC code has a Connect code, but an unrecognised Connect code has no better answer than `Unknown`.
- **`ConnectMethod.FromGrpc` is shared by both halves.** A disagreement between our own client and
  server about a path or a shape is an interoperability bug with ourselves; one definition is the only
  way to be sure of it. The server binder's private copy is gone.
- The gRPC stream shims moved down into `protobuf-net.Connect` with an `InternalsVisibleTo` for the
  AspNetCore half, since the reader shim is *literally the same type* on both sides and a second copy
  of a subtle bridge is worse than a shared internal.

### Status

- `src/ConnectContractFirst` — 27/27 (raw-HTTP checks *and* generated-client checks).
- `src/AotConnectSmoke` — 22/22. `src/ConnectProbe` — 9/9 against connect-go.
- `BuildToolsUnitTests` — 540/540. Traversal build clean, and the six long-standing warnings in
  `protobuf-net.Connect` are gone.

### It publishes natively, with zero warnings

`src/ConnectContractFirst` is `PublishAot`, and the result is the sharpest form of the whole claim:

```
dotnet publish src/ConnectContractFirst -c Release -r linux-x64
-> 16.6 MB native binary, 0 trim/AOT warnings, 27/27 checks pass when RUN
```

A `protoc`-generated gRPC **service and client**, speaking Connect, compiled natively, with no
reflection anywhere on the path. Worth stating why it comes out clean, since Google.Protobuf has a
reputation here and the reputation is about a different part of it:

- **marshalling is generated code**, not reflection — `IBufferMessage.InternalWriteTo`/`InternalMergeFrom`;
- the `FileDescriptor` a generated file builds in its static constructor uses `typeof` and delegates
  (`GeneratedClrTypeInfo`), not name-based lookup. The reflection-heavy parts of Google.Protobuf are
  the JSON formatter and the descriptor/reflection APIs, and this path touches neither;
- our adapter contributes none of its own: the two-phase bind was chosen precisely to avoid the
  name-based `GetMethod` that `Grpc.AspNetCore.Server` needs.

**It was run, not just published.** A clean warning count says nothing about whether the binary works —
this repo has a section on exactly that mistake — so the native binary executes the same 27 checks.

### Next

- `connectconformance` against both halves — see §43 for why it is bigger than it sounds.
- JSON codec — still the largest single piece, and still unsized by anything but judgement.

## 43. `PBN5007`: the authorization gap, caught at build time instead

§41 recorded that contract-first does not infer endpoint metadata, so `[Authorize]` on a service method
is silently *not honoured* and the endpoint is served unauthenticated. That is the one thing on this
path that can be **wrong** rather than merely missing, and a documented parameter is not much of a
guard — the person who hits it is migrating an existing gRPC service, has already written the
attribute, and has no reason to re-read our docs.

So it is an analyzer, which is the answer this repo keeps arriving at and the one asked for: *check
that the user isn't doing the thing we just said they can't do.*

**`ConnectContractFirstAnalyzer` / `PBN5007`** fires on a `MapConnectService` call whose
implementation carries an authorization attribute and which supplies no metadata. Details that were
decided rather than defaulted:

- **It reads the *contract-first* overload only**, told apart by its second parameter being a delegate
  rather than an `IConnectServiceBinder`. The code-first overload infers nothing and gets its metadata
  from the generator, so it has nothing to be wrong about.
- **`[AllowAnonymous]` counts too.** Dropped, it makes an endpoint *less* reachable rather than more —
  a different bug, equally invisible, and worth the same warning.
- **Derived attributes count**, since carrying a policy on a derived `AuthorizeAttribute` is far more
  common in real code than the bare one. Base types are walked, because a shared contract may put the
  attribute on the generated base.
- **Any chained call on the returned builder silences it.** Deliberately *any*, not specifically
  `RequireAuthorization`: the complaint is that authorization was written down and dropped, and a
  consumer using the builder has demonstrably read the return value. Insisting on one method would
  make the rule a style opinion and a noisy one, and a noisy authorization rule is a rule people turn
  off — which protects nothing.
- A **warning**, per this assembly's convention, but the one here most worth `WarningsAsErrors`.

### The tests had a vacuous-pass hole, and it was already open

Most of the twelve cases are *negative* (`DoesNotContain PBN5007`). `AnalyzerTestBase` returns **all**
diagnostics, compiler errors included, and asserts nothing about them — so a negative test whose
fixture does not compile passes trivially, having analysed nothing.

That was not hypothetical: the first run had the stub extension method out of scope, and **six tests
reported it while six passed anyway**. `RunAsync` now asserts the fixture compiled. The positive and
negative cases are also written as controlled pairs differing in one token (`, "rpc"` versus
`, metadata: ...`; chained versus not), so each negative result is attributable.

### Delivery: the analyzer did not reach a contract-first consumer. **Fixed.**

`PBN5007` protects nobody if it is not installed, and it was not going to be. The chain is worth
stating because the mechanism is invisible from the csproj:

- the build-time tooling ships **inside `protobuf-net.Core`**, which packs `protobuf-net.BuildTools.dll`
  into `analyzers/dotnet/cs` and `protobuf-net.BuildTools.props` into `build/protobuf-net.Core.props`
  (renamed, because a package's build props is auto-imported only when named after the package);
- **NuGet's default dependency edge excludes `Build,Analyzers`**, so analyzer assets stop at the first
  hop. `protobuf-net` already opens its own edge with `PrivateAssets="none"` for exactly this reason —
  that comment in `protobuf-net.csproj` is the whole story, and neither Connect project had followed it.

Measured rather than reasoned. Packing `protobuf-net.Connect` before the change:

```xml
<dependency id="protobuf-net.Core" version="..." exclude="Build,Analyzers" />
```

Both edges now carry `PrivateAssets="none"` — `protobuf-net.Connect` → `protobuf-net.Core`, and
`protobuf-net.Connect.AspNetCore` → `protobuf-net.Connect` — and pack as `include="All"`. The
**second** hop is the one that matters most: a contract-first consumer references only
`protobuf-net.Connect.AspNetCore` and has no reason to name protobuf-net at all, and that consumer is
exactly who `PBN5007` is aimed at. The third-party edges (`Grpc.Core.Api`, `System.IO.Pipelines`)
correctly keep the exclusion.

Note this also carries `build/`, which is *required* rather than incidental: the props declares the
`CompilerVisibleProperty` entries, so without it `ProtoBufDisableBuildTools` and the AOT properties
would be invisible to the analyzers that read them.

### Retracted: "a single `OutputItemType="Analyzer"` reference reaches csc twice"

**It does not.** This was recorded here as a repo-wide wart and it was a measurement error, so it is
retracted rather than deleted — the way it looked true is the useful part:

- `dotnet build -v n | grep -c /analyzer:...BuildTools` gives **2**, because MSBuild logs one `Csc`
  invocation twice: once as the command line and once as `BuildResponseFile`. One invocation, one
  analyzer.
- the diagnostic also *appeared* twice at `-v q`, which is the **"Build succeeded" summary block**
  re-printing it — and that same block says **"1 Warning(s)"**, which was on screen the whole time.
- `-p:ErrorLog=...sarif` settles it: **one** result. `ResolveReferences` likewise shows one `Analyzer`
  item.

The lesson is the one this repo keeps writing down: **count diagnostics from SARIF or from the
compiler's own tally, never by grepping console text**, which interleaves at least three renderings of
the same event.

### Sizing `connectconformance`, since it is the obvious next thing and looks smaller than it is

It is not "download a binary and run it". The runner spawns *our* executable and speaks a
protobuf-framed control protocol over stdin/stdout, and the program under test must implement
`connectrpc.conformance.v1.ConformanceService` — a substantial service in its own right — in both a
`--mode client` and a `--mode server` shape. That is a project, not an afternoon, and it should be
started as one rather than half-landed.

### Status

- `BuildToolsUnitTests` — **552/552** (540 + 12).
- `src/ConnectContractFirst` — 27/27, and clean once the temporary `[Authorize]` used to prove the
  analyzer fires on a real build is removed.
- `protobuf-net.BuildTools.Legacy` builds; analyzers are listed there by name, so a new one is
  correctly invisible to it.

## 12. Unverified — check before committing to any of this

Everything below is assumption or inference, not measurement:

- That `SerializationContext`/`DeserializationContext` can be subclassed outside `Grpc.Net.Client`
  cleanly enough for a `ConnectCallInvoker`. Believed yes (both are public abstract in `Grpc.Core.Api`);
  not tried.
- Blazor WASM's handler and streaming. (Duplex over `SocketsHttpHandler` **is** answered — §24 proves
  it interleaves — but the browser handler is a different implementation.)
- protobuf-net map member determinism, which GET-as-cache-key depends on.
- The exact CORS header set browsers need for Connect (`connect-protocol-version`, `connect-timeout-ms`
  and friends must be allowed, and exposed on responses). Only matters once JSON exists.
- Whether protobuf-net's schema output is faithful enough that a `.proto` emitted from a code-first
  model round-trips through another language's codegen to the same field numbers *and names*. Believed
  yes — it is what protobuf-net.Grpc.Reflection already relies on — but it has never been the *interop*
  contract before, and JSON would make it one. Irrelevant unless JSON happens.
- Whether `Grpc.AspNetCore.Web`'s HTTP/2-check relaxation is reachable by a third-party middleware, which
  option (2) would depend on. Only matters if (2) is pursued, and I recommend it is not.
- Sizing of the JSON codec. Called "the majority of the effort" on judgement, not on a spike.
