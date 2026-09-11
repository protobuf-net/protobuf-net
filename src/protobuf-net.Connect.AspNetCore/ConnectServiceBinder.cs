using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProtoBuf.Connect.AspNetCore;

/// <summary>
/// Handles one unary call.
/// </summary>
/// <remarks>
/// A delegate per shape, with the runtime owning the reading and writing, is the shape that lets the
/// other four method shapes be added by adding delegate types rather than by growing a second pipeline.
/// The alternative - a generated handler that took a request and returned a response - is unreachable
/// from any streaming shape.
/// </remarks>
public delegate Task<TResponse> ConnectUnaryHandler<in TService, in TRequest, TResponse>(
    TService service, TRequest request, ConnectServerCallContext context);

/// <summary>
/// Implemented by generated code to describe a service's methods to the runtime.
/// </summary>
/// <typeparam name="TService">The service implementation type, resolved per call from DI.</typeparam>
public interface IConnectServiceBinder<TService> where TService : class
{
    /// <summary>Called once at startup to enumerate the service's methods.</summary>
    void Bind(ConnectServiceBinderContext<TService> context);
}

/// <summary>
/// Collects the methods a service declares. One instance per <c>MapConnectService</c> call.
/// </summary>
/// <typeparam name="TService">The service implementation type.</typeparam>
public sealed class ConnectServiceBinderContext<TService> where TService : class
{
    private readonly List<ConnectMethodRegistration<TService>> _methods = new();

    internal ConnectServiceBinderContext() { }

    internal IReadOnlyList<ConnectMethodRegistration<TService>> Methods => _methods;

    /// <summary>Declares a unary method.</summary>
    /// <param name="method">The method's name and shape.</param>
    /// <param name="handler">Invokes the service.</param>
    /// <param name="metadata">
    /// Endpoint metadata - <c>[Authorize]</c>, a CORS policy, a rate-limiter policy, and so on. This is
    /// why each method becomes its own endpoint: ASP.NET Core resolves all of those from the matched
    /// endpoint, in middleware that runs before any handler, so a service-wide endpoint could not carry
    /// per-method values at all.
    /// </param>
    public void AddUnaryMethod<TRequest, TResponse>(
        ConnectMethod<TRequest, TResponse> method,
        ConnectUnaryHandler<TService, TRequest, TResponse> handler,
        IReadOnlyList<object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(handler);

        if (method.Type != ConnectMethodType.Unary)
        {
            throw new ArgumentException($"'{method}' is {method.Type}, not unary.", nameof(method));
        }

        _methods.Add(new ConnectMethodRegistration<TService>(
            method.Path,
            method.ToString(),
            metadata ?? Array.Empty<object>(),
            Internal.ConnectUnaryInvoker.Create(method, handler)));
    }
}

internal sealed record ConnectMethodRegistration<TService>(
    string Path,
    string DisplayName,
    IReadOnlyList<object> Metadata,
    Internal.ConnectInvoker<TService> Invoker) where TService : class;
