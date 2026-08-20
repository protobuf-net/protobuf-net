using ProtoBuf;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;

namespace AotGrpcSmoke;

/// <summary>
/// The serializer half: compile-time serializers for the payload types, from the AOT generator that
/// already ships.
/// </summary>
/// <remarks>
/// <para>
/// **There is deliberately no <c>[ProtoSerializable]</c> here**, and that absence is the test. The
/// payload types are seeded from <see cref="SmokeServices"/> below: it names this model, so every
/// request and response type of every contract it binds is pulled into the model automatically.
/// </para>
/// <para>
/// Which makes this project the proof of that feature, not just a beneficiary of it - if seeding
/// regressed, the marshallers would fall back to the reflective model and the native publish would
/// fail exactly as it did before any of this existed. A JIT run would still pass, so the native leg
/// is the one that matters here.
/// </para>
/// </remarks>
[ProtoModel]
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
