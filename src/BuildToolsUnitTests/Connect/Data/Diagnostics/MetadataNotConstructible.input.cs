#nullable enable
// PBN5008: an attribute the generated file cannot construct.
//
// The generated code sits at namespace scope in the consumer's own assembly, so "can this be
// constructed" is not the same question as "does this compile where it is written": a private nested
// attribute is perfectly legal on the method that declares it and unnameable from anywhere else.
// That is the realistic shape too - the common real case is an attribute that is internal to another
// assembly, which a single-file fixture cannot express.
//
// The metadata is dropped WHOLE for that operation, taking [Authorize] with it, which is why this is
// a warning worth escalating rather than a note: the shared parse gives up on the first attribute it
// cannot render, because emitting a partial list would be a more permissive endpoint that nothing
// would notice. The sibling operation still gets its metadata, so the give-up is per-operation.
using ProtoBuf;
using ProtoBuf.Connect;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeAttribute : Attribute
    {
        public AuthorizeAttribute() { }
        public string? Policy { get; set; }
    }
}

namespace ConnectFixtures.MetadataNotConstructible
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

    [Service("connectfixtures.v1.Hidden")]
    public interface IHidden
    {
        Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default);

        Task<HelloReply> PingAsync(HelloRequest request, CallContext context = default);
    }

    public class HiddenService : IHidden
    {
        [Authorize(Policy = "admin")]
        [Secret]
        public Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default) => null!;

        // the sibling is unaffected: giving up is per operation, not per contract
        [Authorize(Policy = "admin")]
        public Task<HelloReply> PingAsync(HelloRequest request, CallContext context = default) => null!;

        [AttributeUsage(AttributeTargets.Method)]
        private sealed class SecretAttribute : Attribute { }
    }

    public partial class HiddenModel : TypeModel
    {
        public static HiddenModel Instance { get; } = new HiddenModel();
    }

    [ProtoConnect(Model = typeof(HiddenModel))]
    [ProtoService(typeof(IHidden), typeof(HiddenService))]
    internal partial class HiddenServices
    {
    }
}
