# protobuf-net.Connect.AspNetCore

**Experimental.** Serves the [Connect protocol](https://connectrpc.com/docs/protocol/) from ASP.NET Core.

```csharp
builder.Services.AddConnect(o => o.Codecs.Add(new ProtoConnectCodec(MyModel.Instance)));
builder.Services.AddScoped<GreeterService>();
...
app.MapConnectService(new GreeterBindings());
```

Binds to `HttpContext` and endpoint routing rather than to Kestrel, so Kestrel, HTTP.sys, IIS and
`TestServer` all work from one implementation. Each method becomes its own endpoint, so `[Authorize]`,
CORS policies, rate limiting and output caching attach per RPC - ASP.NET Core resolves all of those from
the matched endpoint in middleware that runs before any handler.

A Connect endpoint is an ordinary HTTP endpoint, and needs no HTTP/2: a plaintext Kestrel endpoint
serving HTTP/1.1 works, which is the operational argument for Connect over gRPC.

Status: **unary only, binary protobuf only**. See `notes/connect/findings.md` in the repository.
