#nullable enable
// Endpoint metadata: the attributes ASP.NET Core resolves from the matched endpoint reach the
// generated binding as constructed instances, gathered in ServiceBinder.GetMetadata's order.
//
// The shapes here are chosen so the ORDER is observable rather than merely the set: the contract
// type's attributes come first and the implementation method's last, so a per-method [AllowAnonymous]
// lands after the class-level [Authorize] it is meant to beat. Getting that backwards would be
// invisible in a test that only asserted the set.
//
// Block namespaces rather than this repo's usual file-scoped one, because the ASP.NET Core stubs need
// a namespace of their own and the two forms cannot be mixed in one file.
using ProtoBuf;
using ProtoBuf.Connect;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// The golden tests compile against the two surface snapshots, which carry the protobuf-net and
// Connect vocabulary but nothing of ASP.NET Core's authorization. Only what the generated code has to
// name and construct is declared.
namespace Microsoft.AspNetCore.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeAttribute : Attribute
    {
        public AuthorizeAttribute() { }
        public AuthorizeAttribute(string policy) => Policy = policy;
        public string? Policy { get; set; }
        public string? Roles { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AllowAnonymousAttribute : Attribute { }
}

namespace ConnectFixtures.Authorization
{
    using Microsoft.AspNetCore.Authorization;

    [ProtoContract]
    public class HelloRequest
    {
        [ProtoMember(1)]
        public string? Name { get; set; }
    }

    [ProtoContract]
    public class HelloReply
    {
        [ProtoMember(1)]
        public string? Message { get; set; }
    }

    /// <summary>
    /// A consumer's own attribute - the reason the emitted list is everything gathered rather than an
    /// authorization allowlist, which would drop precisely this.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AuditAttribute : Attribute
    {
        public AuditAttribute(string category, int level = 0)
        {
            Category = category;
            Level = level;
        }

        public string Category { get; }
        public int Level { get; }
        public bool Redact { get; set; }
    }

    [Service("connectfixtures.v1.Guarded")]
    public interface IGuarded
    {
        Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default);

        // the contract method carries one too, so both method sources are exercised
        [Audit("greeting")]
        Task<HelloReply> PingAsync(HelloRequest request, CallContext context = default);

        IAsyncEnumerable<HelloReply> Subscribe(HelloRequest request, CallContext context = default);
    }

    [Authorize]
    public class GuardedService : IGuarded
    {
        [Authorize(Policy = "admin")]
        public Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default) => null!;

        [Audit("greeting", 2, Redact = true)]
        public Task<HelloReply> PingAsync(HelloRequest request, CallContext context = default) => null!;

        // beats the class-level [Authorize] only because it is emitted after it
        [AllowAnonymous]
        public IAsyncEnumerable<HelloReply> Subscribe(HelloRequest request, CallContext context = default) => null!;
    }

    // Stands in for the [ProtoModel]-generated model; see Basic.input.cs.
    public partial class AuthorizationModel : TypeModel
    {
        public static AuthorizationModel Instance { get; } = new AuthorizationModel();
    }

    [ProtoConnect(Model = typeof(AuthorizationModel))]
    [ProtoService(typeof(IGuarded), typeof(GuardedService))]
    internal partial class AuthorizationServices
    {
    }
}
