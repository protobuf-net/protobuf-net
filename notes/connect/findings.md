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

### Stage 0 — client against Eliza. No generator, no abstractions.

~50 lines: `HttpClient`, a protobuf-net `[ProtoContract]` pair, POST the bare body, read the bare body.
Point it at `demo.connectrpc.com` (§13). Add the error path — including the `text/plain` 404.

What it proves: our serializer's bytes are interoperable with a reference Connect implementation, and
the request/response/error shapes are understood. What it costs: a day. What it de-risks: everything
downstream. **If this does not work, nothing else is worth starting.**

### Stage 1 — the hand-written target output.

Write by hand, for one `[Service]` interface, exactly what the generator will eventually emit: the
client proxy and the server endpoint registration. Get it working .NET → .NET over Kestrel, and *also*
verify the new server against an external Connect **client** (`buf curl`, or `connectconformance` in
`--mode server`).

This is the repo's established method rather than a shortcut — `AotRefGen` exists precisely so that
expected generator output is *derived and reviewable* rather than invented. Here there is no ref-emit to
derive from, so the substitute is: write it, make it work, review it, *then* freeze it as the target.

Reviewing this file is the real decision point on API shape, and it is much cheaper to change here than
after a generator emits it.

### Stage 2 — the generator.

`ProtoConnectGenerator` in `protobuf-net.BuildTools`, emitting stage 1's file. Golden fixtures under
`src/BuildToolsUnitTests/Connect/Data/` on the existing harness (`*.input.cs` → `*.output.cs` +
`*.txt`, rewritten in-tree on every run). Diagnostics in the free **`PBN5xxx`** block, registered in
`AnalyzerReleases.Unshipped.md` or the build fails (`RS2000`).

Contract parsing is **shared with `GrpcProxyGenerator`, not forked** — the five method shapes,
`CallContext`, `[SubService]`, void/`Empty`, overloads, closed generics. That sharing is the single
largest reason to do this in-repo.

### Stage 3 — conformance, and a smoke test.

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

## 12. Unverified — check before committing to any of this

Everything below is assumption or inference, not measurement:

- That `SerializationContext`/`DeserializationContext` can be subclassed outside `Grpc.Net.Client`
  cleanly enough for a `ConnectCallInvoker`. Believed yes (both are public abstract in `Grpc.Core.Api`);
  not tried.
- Duplex request content over `SocketsHttpHandler` on HTTP/2 — believed fine, not tried. Blazor WASM's
  handler is the real question mark.
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
