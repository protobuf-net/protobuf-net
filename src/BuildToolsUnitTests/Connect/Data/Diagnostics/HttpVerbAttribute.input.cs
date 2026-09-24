#nullable enable
// PBN5010: an ASP.NET Core MVC verb attribute on a Connect contract method does nothing.
//
// It is reported rather than honoured, and "reasonable thing to reach for" is exactly why. A .NET
// developer wanting a GET will try [HttpGet] first; leaving it silently inert means the method stays
// POST-only and nothing says so. Honouring it would be worse - see the diagnostic's remarks for the
// three reasons, of which the practical one is that a transport-neutral contract assembly usually
// cannot even see the attribute.
//
// Matched by BASE TYPE, so [HttpPost] and a consumer's own derivative are caught by the same test as
// [HttpGet] - hence the three shapes below.
using ProtoBuf;
using ProtoBuf.Connect;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System;
using System.Threading.Tasks;

// The MVC surface the fixture needs, declared rather than referenced: the golden tests compile against
// the two snapshots, and pulling in the ASP.NET Core shared framework for two attributes would be the
// very dependency the diagnostic exists to avoid.
namespace Microsoft.AspNetCore.Mvc.Routing
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class HttpMethodAttribute : Attribute { }
}

namespace Microsoft.AspNetCore.Mvc
{
    public sealed class HttpGetAttribute : Routing.HttpMethodAttribute { }
    public sealed class HttpPostAttribute : Routing.HttpMethodAttribute { }
}

namespace ConnectFixtures.HttpVerbAttribute
{
    using Microsoft.AspNetCore.Mvc;

    /// <summary>A consumer's own derivative - caught because the test walks the base chain.</summary>
    /// <remarks>
    /// Fully qualified: a using-directive imports a namespace's <em>types</em>, not its nested
    /// namespaces, so <c>using Microsoft.AspNetCore.Mvc;</c> does not make <c>Routing</c> nameable.
    /// </remarks>
    public sealed class HttpReadAttribute : Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute { }

    [ProtoContract]
    public class Request
    {
        [ProtoMember(1)]
        public string? Key { get; set; }
    }

    [ProtoContract]
    public class Reply
    {
        [ProtoMember(1)]
        public string? Value { get; set; }
    }

    [Service("connectfixtures.v1.Verbs")]
    public interface IVerbs
    {
        // the case that motivated the rule: wanted a GET, gets nothing
        [HttpGet]
        Task<Reply> ReadAsync(Request request, CallContext context = default);

        // accidentally correct, still inert - reported for consistency, since a consumer who later
        // changes it to [HttpPut] would otherwise get no warning at the moment it starts to matter
        [HttpPost]
        Task<Reply> WriteAsync(Request request, CallContext context = default);

        // a derivative, and the reason the test is by base type rather than by name
        [HttpRead]
        Task<Reply> PeekAsync(Request request, CallContext context = default);

        // the attribute that actually works, alongside, so the two do not read as alternatives
        [NoSideEffects]
        Task<Reply> LookupAsync(Request request, CallContext context = default);
    }

    public class VerbsService : IVerbs
    {
        public Task<Reply> ReadAsync(Request request, CallContext context = default) => null!;
        public Task<Reply> WriteAsync(Request request, CallContext context = default) => null!;
        public Task<Reply> PeekAsync(Request request, CallContext context = default) => null!;
        public Task<Reply> LookupAsync(Request request, CallContext context = default) => null!;
    }

    // Stands in for the [ProtoModel]-generated model; see Basic.input.cs.
    public partial class VerbsModel : TypeModel
    {
        public static VerbsModel Instance { get; } = new VerbsModel();
    }

    [ProtoConnect(Model = typeof(VerbsModel))]
    [ProtoService(typeof(IVerbs), typeof(VerbsService))]
    internal partial class VerbsServices
    {
    }
}
