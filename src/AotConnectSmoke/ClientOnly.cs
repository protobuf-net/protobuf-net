using ProtoBuf.Connect;
using ProtoBuf.Grpc.Configuration;

namespace ProtoBuf.AotConnectSmoke;

// ---------------------------------------------------------------------------------------------
// A CLIENT-ONLY container, and the consumer-written half is again the whole route in.
//
// [ProtoService] has a one-argument form for exactly this: "Generate a client proxy for contract. No
// server bindings are generated: use the two-argument form in the project that hosts the service."
// It is the COMMON shape rather than an edge case - contracts ship in a shared package, and a client
// project references that package and has no implementation to name.
//
// Note there is no `Model` difference and no second model here; this container exists only to prove
// the client-only shape. In real code it would live in a different project entirely, which is why the
// contract being resolvable from metadata matters.
// ---------------------------------------------------------------------------------------------

[ProtoConnect(Model = typeof(SmokeModel))]
[ProtoService(typeof(IFarewell))]
internal partial class SmokeClientOnly
{
}
