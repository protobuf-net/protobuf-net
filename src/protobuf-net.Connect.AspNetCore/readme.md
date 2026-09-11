# protobuf-net.Connect.AspNetCore

**Experimental.** Serves the [Connect protocol](https://connectrpc.com/docs/protocol/) from ASP.NET Core.

> **Usage and examples live in [`../protobuf-net.Connect/readme.md`](../protobuf-net.Connect/readme.md)**,
> which covers both halves - client and server, code-first and contract-first - in one place. This file
> is the design note for the server half only.

All four method shapes are supported: unary, client-streaming, server-streaming and bidirectional.
Binary protobuf only; see `notes/connect/findings.md` in the repository for what is declared
unsupported and why.

Binds to `HttpContext` and endpoint routing rather than to Kestrel, so Kestrel, HTTP.sys, IIS and
`TestServer` all work from one implementation. Each method becomes its own endpoint, so `[Authorize]`,
CORS policies, rate limiting and output caching attach per RPC - ASP.NET Core resolves all of those from
the matched endpoint in middleware that runs before any handler.

**Only full-duplex bidirectional streaming needs HTTP/2.** Everything else - including *half*-duplex
bidi, where every request is sent before any response is read - works over a plaintext HTTP/1.1
endpoint, because Connect carries trailing metadata in the body rather than in HTTP trailers. That is
the operational argument for Connect over gRPC. The server does not refuse HTTP/1.1 for bidi: it cannot
tell the two apart, and the demand for HTTP/2 belongs on the caller that knows it is about to
interleave.
