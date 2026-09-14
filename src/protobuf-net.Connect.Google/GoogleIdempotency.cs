using System;
using Google.Protobuf.Reflection;
using Grpc.Core;

namespace ProtoBuf.Connect.Google
{
    /// <summary>
    /// Reads <c>idempotency_level</c> off a <c>protoc</c>-generated service descriptor.
    /// </summary>
    /// <remarks>
    /// Connect serves a side-effect-free unary method over <c>GET</c> as well as <c>POST</c>, which makes
    /// it an ordinary cacheable HTTP request - the one thing Connect does that gRPC structurally cannot.
    /// Whether a method qualifies is declared in the <c>.proto</c>:
    /// <code>
    /// rpc GetUser(GetUserRequest) returns (User) {
    ///   option idempotency_level = NO_SIDE_EFFECTS;
    /// }
    /// </code>
    /// That option reaches the generated <c>ServiceDescriptor</c> but <b>not</b> the
    /// <see cref="Method{TRequest, TResponse}"/> the binder sees, so it has to be read from the
    /// descriptor separately - which is why this lives here rather than in <c>protobuf-net.Connect</c>,
    /// where Google.Protobuf is deliberately absent.
    /// </remarks>
    public static class GoogleIdempotency
    {
        /// <summary>
        /// Builds the predicate <c>MapConnectService</c> takes, over one service's descriptor.
        /// </summary>
        /// <example>
        /// <code>
        /// app.MapConnectService&lt;GreeterImpl&gt;(
        ///     Greeter.BindService,
        ///     isIdempotent: GoogleIdempotency.For(Greeter.Descriptor));
        /// </code>
        /// </example>
        public static Func<IMethod, bool> For(ServiceDescriptor service)
        {
            if (service is null) throw new ArgumentNullException(nameof(service));

            return method =>
            {
                // the binder may be serving several services; only ours is answerable
                if (method is null || !string.Equals(method.ServiceName, service.FullName, StringComparison.Ordinal))
                {
                    return false;
                }

                return IsIdempotent(service.FindMethodByName(method.Name));
            };
        }

        /// <summary>
        /// Whether a method declares <c>NO_SIDE_EFFECTS</c>.
        /// </summary>
        /// <remarks>
        /// Only <c>NO_SIDE_EFFECTS</c> qualifies, deliberately. <c>IDEMPOTENT</c> means "safe to retry",
        /// which is a weaker claim than "safe to cache and prefetch" - a method that deletes something is
        /// idempotent and emphatically must not be a <c>GET</c>.
        /// </remarks>
        public static bool IsIdempotent(MethodDescriptor? method)
            => method?.GetOptions()?.IdempotencyLevel == MethodOptions.Types.IdempotencyLevel.NoSideEffects;
    }
}
