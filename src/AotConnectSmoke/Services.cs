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
// transport. Its own documentation says "Repeat for each contract", so one container holding several
// services is the designed shape rather than something being stretched.
//
// The accessibility declared here governs: the generated half restates no modifier, so a consumer who
// writes `public` gets a public surface and one who writes `internal` gets an internal one.
//
// `static` is optional, and it CHANGES WHAT IS GENERATED. Declared static, the registration and binding
// methods are emitted as extension methods - `builder.Services.AddSmokeServices()`,
// `app.BindSmokeServices()` - and no constructor is emitted, since a static class cannot have one.
// Declared non-static (see ClientOnly.cs) they are emitted as plain statics plus a private constructor.
// Both work; static reads the way .NET usually does, which is why it is what this container picks.
// ---------------------------------------------------------------------------------------------

[ProtoConnect(Model = typeof(SmokeModel))]
[ProtoService(typeof(IGreeter), typeof(GreeterService))]
[ProtoService(typeof(IFarewell), typeof(FarewellService))]
internal static partial class SmokeServices
{
}
