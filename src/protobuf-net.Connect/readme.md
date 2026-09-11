# protobuf-net.Connect

**Experimental.** A client for the [Connect protocol](https://connectrpc.com/docs/protocol/), built on
`HttpClient` and a build-time generated protobuf-net model.

Connect is a plain-HTTP RPC protocol: a path, a content-type and a body. Unlike gRPC it uses no HTTP
trailers, so unary and single-direction streaming work over HTTP/1.1 as well as HTTP/2, and it is
reachable from a browser without a translating proxy.

Status: **unary only, binary protobuf only**. See `notes/connect/findings.md` in the repository.

Nothing here reflects. `RuntimeTypeModel` is deliberately not on the reference graph - the only
dependency is `protobuf-net.Core` - so the model must be one generated at build time by
`[ProtoModel]`, and there is no reflective path to fall into by accident.
