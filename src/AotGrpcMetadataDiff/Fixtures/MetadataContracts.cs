// The contract shape the endpoint-metadata oracle runs against.
//
// This file is compiled TWICE, deliberately: once by the SDK, giving live types for the reflective
// side; and once by Roslyn in-process at run time, giving symbols for the compile-time side. It is
// therefore both <Compile> and <Content> in the csproj, and must stay self-contained - anything it
// references has to be resolvable from the harness's own reference set.
//
// Every attribute here is placed to pin a rule rather than to look realistic:
//
//   * IAudited carries a type-level attribute that must NOT appear, because
//     Type.GetCustomAttributes(inherit: true) does not walk base interfaces;
//   * ThingServiceBase carries one that MUST appear (base classes are walked) beside one marked
//     Inherited = false that must not;
//   * Tag is AllowMultiple, so it survives the most-derived-wins deduplication that Authorize does not;
//   * WhoAmIAsync's implementation attribute is the one protobuf-net.Grpc#369 changed - absent in
//     1.3.6, present on main - so it is what the divergence report is about.
using Microsoft.AspNetCore.Authorization;
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using System;
using System.Threading.Tasks;

namespace MetadataFixtures;

/// <summary>Repeatable, so several survive on one endpoint and order is observable.</summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class TagAttribute : Attribute
{
    public TagAttribute(string name) => Name = name;

    public string Name { get; }

    public int Order { get; set; }
}

/// <summary>Opts out of the base-class walk; present to prove we honour that.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class NotInheritedAttribute : Attribute
{
}

/// <summary>Non-repeatable, to exercise most-derived-wins deduplication.</summary>
[AttributeUsage(AttributeTargets.All)]
public sealed class SingletonAttribute : Attribute
{
    public SingletonAttribute(string origin) => Origin = origin;

    public string Origin { get; }
}

public enum Level
{
    None = 0,
    Low = 1,
    High = 2,
}

/// <summary>Every argument kind the renderer has to reproduce: enum, typeof, array, named.</summary>
[AttributeUsage(AttributeTargets.All)]
public sealed class ShapesAttribute : Attribute
{
    public ShapesAttribute(Level level, Type target, string[] names)
    {
        Level = level;
        Target = target;
        Names = names;
    }

    public Level Level { get; }

    public Type Target { get; }

    public string[] Names { get; }

    public long Ticks { get; set; }

    public double Ratio { get; set; }
}

[ProtoContract]
public class Request
{
    [ProtoMember(1)]
    public string? Name { get; set; }
}

[ProtoContract]
public class Reply
{
    [ProtoMember(1)]
    public string? Message { get; set; }
}

// bound as part of whatever inherits it; its own type-level attribute is NOT collected
[SubService]
[Tag("subservice-type")]
public interface IAudited
{
    [Tag("subservice-method")]
    Task<Reply> WhoAmIAsync(Request request, CallContext context = default);
}

[Service]
[Tag("contract-type")]
[Singleton("contract")]
public interface IThing : IAudited
{
    [Tag("contract-method")]
    [Authorize(Roles = "admin")]
    [Shapes(Level.High, typeof(Reply), new[] { "alpha", "beta" }, Ticks = 42L, Ratio = 1.5)]
    Task<Reply> GetAsync(Request request, CallContext context = default);
}

[Tag("base-class")]
[NotInherited]
[Singleton("base")]
public abstract class ThingServiceBase
{
    // on the *overridden* method: inherit: true walks the override chain, so this must appear
    [Tag("base-method")]
    public abstract Task<Reply> GetAsync(Request request, CallContext context = default);
}

[Tag("service-type", Order = 3)]
[Authorize(Policy = "service-wide")]
[Singleton("service")]
public class ThingService : ThingServiceBase, IThing
{
    [Tag("service-method")]
    public override Task<Reply> GetAsync(Request request, CallContext context = default) => null!;

    // the attribute protobuf-net.Grpc#369 is about
    [Tag("service-method-sub")]
    public Task<Reply> WhoAmIAsync(Request request, CallContext context = default) => null!;
}
