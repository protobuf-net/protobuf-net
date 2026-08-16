using ProtoBuf;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;

namespace AotGrpcSmoke;

/// <summary>
/// The serializer half: compile-time serializers for the payload types, from the AOT generator that
/// already ships.
/// </summary>
/// <remarks>
/// Seeded by hand here. Teaching <c>[ProtoSerializable]</c> to accept a service contract - and pull
/// in every request/response type from its operations - is the obvious next ergonomic step, and is
/// not done yet; this project is what would prove it.
/// </remarks>
[ProtoModel]
[ProtoSerializable(typeof(HelloRequest))]
[ProtoSerializable(typeof(HelloReply))]
public partial class SmokeModel : TypeModel { }

/// <summary>
/// The gRPC half: proxies and server bindings, pointed at <see cref="SmokeModel"/>.
/// </summary>
/// <remarks>
/// This is the whole consumer-facing surface of the approach - one declaration, one typeof link to
/// the serializer model, and one per contract. Nothing is registered, discovered or resolved at run
/// time.
/// </remarks>
[ProtoGrpc(Model = typeof(SmokeModel))]
[ProtoService(typeof(IGreeter), typeof(GreeterService))]
public sealed partial class SmokeServices : ClientFactory { }
