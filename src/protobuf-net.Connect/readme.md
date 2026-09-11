# protobuf-net.Connect

**Experimental.** The [Connect protocol](https://connectrpc.com) for .NET, built for AOT from the
start — no ref-emit, no runtime marshaller lookup, no reflection on any serving or calling path.

> This is the root doc for **both** halves — `protobuf-net.Connect` (client) and
> `protobuf-net.Connect.AspNetCore` (server). It will move if these become their own repository.

## Why Connect

It is gRPC's wire semantics over ordinary HTTP. The difference that matters operationally: Connect
puts trailing metadata **in the body** rather than in HTTP trailers, and HTTP trailers are the only
reason gRPC insists on HTTP/2.

So unary, client-streaming and server-streaming all work over **HTTP/1.1** — through proxies, CDNs,
load balancers and corporate middleboxes that will not carry gRPC. Only full-duplex bidirectional
streaming still needs HTTP/2, for the same reason it always did: interleaving two bodies is not
something HTTP/1.1 can express.

A unary request is also just an HTTP POST with a protobuf body, so `curl` works:

```bash
curl --http1.1 -H 'Content-Type: application/proto' \
     --data-binary @request.bin http://localhost:5000/mypackage.v1.Greeter/SayHello
```

## Two ways in

### 1. Code-first — your existing protobuf-net.Grpc contracts

The contract is the one you already have. Nothing about it is Connect-specific, and the same
interface still serves gRPC.

```csharp
// the wire name is pinned rather than derived: [Service] with no name gives
// "{namespace}.{name-without-I}", which is fine within .NET but is not a name another
// language's schema would have chosen
[Service("mypackage.v1.Greeter")]
public interface IGreeter
{
    Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default);
    IAsyncEnumerable<HelloReply> Subscribe(HelloRequest request, CallContext context = default);
    Task<HelloReply> CollectAsync(IAsyncEnumerable<HelloRequest> requests, CallContext context = default);
    IAsyncEnumerable<HelloReply> Chat(IAsyncEnumerable<HelloRequest> requests, CallContext context = default);
}
```

Declare a container, and the generator writes the bindings and the client factory:

```csharp
[ProtoConnect(Model = typeof(MyModel))]
[ProtoService(typeof(IGreeter), typeof(GreeterService))]
public static partial class MyServices { }
```

`static` is optional and changes what is emitted: declared `static`, the registration and binding
methods arrive as **extension methods** (as used below); declared non-static they are plain statics.
The accessibility you write governs the generated surface too.

**Server:**

```csharp
builder.Services.AddMyServices();      // generated: registers the codec and the implementations
var app = builder.Build();
app.BindMyServices();                  // generated: one endpoint per method
```

**Client:**

```csharp
var channel = new ConnectChannel(httpClient, new ProtoConnectCodec(MyModel.Instance), new Uri(address));
IGreeter client = MyServices.CreateClient<IGreeter>(channel);

var reply = await client.SayHelloAsync(new HelloRequest { Name = "Marc" });
```

Both halves are generated at build time from `[ProtoConnect]`, and the serializers come from a
`[ProtoModel]` — so there is nothing to emit at runtime and nothing to reflect over.

### 2. Contract-first — your existing `protoc`-generated gRPC service, unchanged

If you already have a `.proto` and `Grpc.Tools`, there is **no generator, no attribute and no change
to your contracts**. `protoc` already emits everything needed.

**Server** — the whole opt-in is one line:

```csharp
builder.Services.AddConnect(o => o.Codecs.Add(MarshallerConnectCodec.Instance));
builder.Services.AddScoped<GreeterImpl>();          // your existing Greeter.GreeterBase subclass

var app = builder.Build();
app.MapConnectService<GreeterImpl>(Greeter.BindService);   // protoc's own method group
```

**Client** — `protoc`'s generated client, with a different `CallInvoker` under it:

```csharp
var client = new Greeter.GreeterClient(new ConnectCallInvoker(httpClient, new Uri(address)));

var reply = await client.SayHelloAsync(new HelloRequest { Name = "Marc" });
```

`RpcException`, `ServerCallContext`, `IServerStreamWriter<T>`, `IAsyncStreamReader<T>`, leading and
trailing metadata and deadlines all behave as they do under gRPC. Nothing is re-encoded: for
`application/proto` a Connect body and a gRPC body are the same bytes, and the marshallers `protoc`
generated are used unchanged.

> ⚠️ **`[Authorize]` is not inferred on this path.** `Grpc.AspNetCore.Server` collects endpoint
> metadata by reflecting over your implementation; this deliberately does not reflect, so an
> authorization attribute would be silently dropped. Chain `.RequireAuthorization(...)`, or pass the
> `metadata` argument. The analyzer **PBN5007** warns when your implementation carries an
> authorization attribute and the call supplies neither — it is the one rule here worth escalating
> with `<WarningsAsErrors>PBN5007</WarningsAsErrors>`.

## Native AOT

Both paths publish clean. The contract-first sample publishes with **zero** trim/AOT warnings and the
native binary passes its full test suite:

```bash
dotnet publish -c Release -r linux-x64     # PublishAot=true
```

## Conformance

Measured against [`connectrpc/conformance`](https://github.com/connectrpc/conformance) v1.0.5,
declaring binary (`CODEC_PROTO`) support over HTTP/1.1 and HTTP/2 — **both directions pass in full**:

| mode | |
| --- | --- |
| **server** | **207 / 207** |
| **client** | **253 / 253** |

Server mode drives a real Connect client against our server; client mode drives our client against the
suite's reference server. Between them there is no step where both ends are ours.

Declared unsupported, and therefore not counted: JSON, compression, TLS, and Connect GET. Those are
recorded as gaps rather than hidden — see `notes/connect/findings.md`.

## What is not here yet

- **JSON codec.** Binary only today. Connect clients in other languages default to binary, so this is
  an interoperability *limit*, not a blocker.
- **Compression**, **Connect GET** for side-effect-free methods, and **TLS client certificates**.
- Endpoint metadata inference for contract-first (see the warning above).

## Layout

| project | what it is |
| --- | --- |
| `protobuf-net.Connect` | client, codecs, framing, the call invoker |
| `protobuf-net.Connect.AspNetCore` | server, on endpoint routing — Kestrel, HTTP.sys, IIS or TestServer |
| `ConnectContractFirst` | the contract-first sample, and its 27 checks |
| `AotConnectSmoke` | the code-first smoke test, native-AOT published |
| `ConnectConformance` | the conformance suite's server harness |
| `ConnectProbe` | live probes against connect-go's reference server |
