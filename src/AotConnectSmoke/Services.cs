using ProtoBuf.Connect;
using ProtoBuf.Grpc.Configuration;

namespace ProtoBuf.AotConnectSmoke;

// ---------------------------------------------------------------------------------------------
// THE USER-WRITTEN HALF. This is the whole route in.
//
// A consumer writes their contract (Contracts.cs) and this declaration; everything in
// Services.HandWritten.cs is what the generator will produce from it. Keeping the two in separate
// files is the point - it is otherwise impossible to see how much a consumer is signing up for.
//
// Note [ProtoService] is protobuf-net.Grpc's own, unchanged: it already says exactly what is needed,
// and the same declaration serves a [ProtoGrpc] container. Only the container attribute selects the
// transport.
//
// The accessibility declared here governs: the generated half restates no modifier, so a consumer who
// writes `public` gets a public surface and one who writes `internal` gets an internal one. Note also
// that `static` is NOT required - the generator emits static members and a private constructor onto an
// ordinary partial class, so the consumer is not made to think about it.
// ---------------------------------------------------------------------------------------------

[ProtoConnect(Model = typeof(SmokeModel))]
[ProtoService(typeof(IGreeter), typeof(GreeterService))]
internal partial class SmokeServices
{
}
