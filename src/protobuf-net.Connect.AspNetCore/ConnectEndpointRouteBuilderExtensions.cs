using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProtoBuf.Connect.AspNetCore.Internal;

namespace ProtoBuf.Connect.AspNetCore;

/// <summary>
/// Registers Connect services on ASP.NET Core's endpoint routing.
/// </summary>
public static class ConnectEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps every method a service declares, as one endpoint each.
    /// </summary>
    /// <remarks>
    /// One endpoint per method, deliberately. ASP.NET Core resolves authorization, CORS policy, rate
    /// limiting and output caching from the matched <see cref="Endpoint"/>, in middleware that runs
    /// before any handler - so a single endpoint for a whole service could not carry per-method
    /// <c>[Authorize]</c> at all, and the alternative would be re-implementing authorization inside the
    /// handler. Emitting N registrations instead of one costs nothing, since it is generated.
    /// </remarks>
    /// <typeparam name="TService">
    /// The service implementation, resolved from the request's services on each call.
    /// </typeparam>
    /// <param name="endpoints">The route builder.</param>
    /// <param name="binder">Describes the service's methods; normally generated.</param>
    public static IEndpointConventionBuilder MapConnectService<TService>(
        this IEndpointRouteBuilder endpoints,
        IConnectServiceBinder<TService> binder)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(binder);

        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<ConnectServerOptions>>().Value;
        if (options.Codecs.Count == 0)
        {
            throw new InvalidOperationException(
                "No Connect codec is registered. Call services.AddConnect(o => o.Codecs.Add(new ProtoConnectCodec(MyModel.Instance))).");
        }

        var context = new ConnectServiceBinderContext<TService>();
        binder.Bind(context);

        var builders = new List<IEndpointConventionBuilder>(context.Methods.Count);
        foreach (var method in context.Methods)
        {
            // POST only for now; GET arrives with idempotency, which needs the query-parameter form
            var builder = endpoints
                .MapPost(method.Path, CreateHandler<TService>(method.Invoker, options))
                .WithDisplayName(method.DisplayName);

            foreach (var metadata in method.Metadata) builder.WithMetadata(metadata);
            builders.Add(builder);
        }

        return new CompositeEndpointConventionBuilder(builders);
    }

    private static RequestDelegate CreateHandler<TService>(ConnectInvoker<TService> invoker, ConnectServerOptions options)
        where TService : class
        => async http =>
        {
            // One error path for the whole call, per the constraint recorded in notes/connect/findings.md:
            // three separate throw sites would not converge once streaming exists.
            CancellationTokenSource? timeout = null;
            try
            {
                if (options.RequireProtocolVersionHeader
                    && http.Request.Headers[ConnectChannel.ProtocolVersionHeader] != ConnectChannel.ProtocolVersion)
                {
                    throw new ConnectException(
                        ConnectCode.InvalidArgument,
                        $"This server requires the '{ConnectChannel.ProtocolVersionHeader}: {ConnectChannel.ProtocolVersion}' header.",
                        StatusCodes.Status400BadRequest);
                }

                var codec = SelectCodec(http, options);

                var cancellationToken = http.RequestAborted;
                if (TryGetTimeout(http, out var span))
                {
                    timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeout.CancelAfter(span);
                    cancellationToken = timeout.Token;
                }

                var callContext = new ConnectServerCallContext(http, cancellationToken);
                var service = http.RequestServices.GetRequiredService<TService>();

                await invoker.InvokeAsync(http, service, codec, callContext).ConfigureAwait(false);
            }
            catch (ConnectException ex)
            {
                await TryWriteError(http, ex).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (http.RequestAborted.IsCancellationRequested)
            {
                // the client went away; there is nobody to tell
            }
            catch (OperationCanceledException ex)
            {
                // ...whereas this one is our own connect-timeout-ms firing, which the caller asked for
                await TryWriteError(http, new ConnectException(
                    ConnectCode.DeadlineExceeded, "The call exceeded its deadline.", innerException: ex)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var message = options.IncludeExceptionDetailInErrors
                    ? ex.GetType().Name + ": " + ex.Message
                    : null; // an unhandled exception's message is not for the caller to read
                await TryWriteError(http, new ConnectException(
                    ConnectCode.Internal, message, innerException: ex)).ConfigureAwait(false);
            }
            finally
            {
                timeout?.Dispose();
            }
        };

    private static ConnectCodec SelectCodec(HttpContext http, ConnectServerOptions options)
    {
        var contentType = http.Request.ContentType;
        if (!ConnectContentType.TryParse(contentType, out var parsed))
        {
            throw new ConnectException(
                ConnectCode.Unimplemented,
                $"'{contentType}' is not a Connect content-type.",
                StatusCodes.Status415UnsupportedMediaType);
        }

        if (parsed.IsEnveloped)
        {
            throw new ConnectException(
                ConnectCode.Unimplemented,
                $"'{contentType}' asks for a streaming call; only unary is implemented.",
                StatusCodes.Status415UnsupportedMediaType);
        }

        foreach (var codec in options.Codecs)
        {
            if (string.Equals(codec.Name, parsed.CodecName, StringComparison.OrdinalIgnoreCase)) return codec;
        }

        throw new ConnectException(
            ConnectCode.Unimplemented,
            $"The codec '{parsed.CodecName}' is not supported; this server accepts: {string.Join(", ", Names(options))}.",
            StatusCodes.Status415UnsupportedMediaType);

        static IEnumerable<string> Names(ConnectServerOptions options)
        {
            foreach (var codec in options.Codecs) yield return codec.Name;
        }
    }

    private static bool TryGetTimeout(HttpContext http, out TimeSpan timeout)
    {
        timeout = default;
        var header = http.Request.Headers[ConnectChannel.TimeoutHeader];
        if (header.Count == 0) return false;

        var text = header.ToString();
        // "a positive integer of at most ten digits" - anything else is the client's mistake, and
        // ignoring it silently would turn a stated deadline into no deadline at all
        if (text.Length is 0 or > 10 || !long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var ms))
        {
            throw new ConnectException(
                ConnectCode.InvalidArgument,
                $"'{ConnectChannel.TimeoutHeader}: {text}' is not a positive integer of at most ten digits.",
                StatusCodes.Status400BadRequest);
        }

        timeout = TimeSpan.FromMilliseconds(ms);
        return true;
    }

    private static async Task TryWriteError(HttpContext http, ConnectException error)
    {
        if (http.Response.HasStarted)
        {
            // the response is committed, so the status and body are already gone; aborting is the only
            // way left to tell the client that what it has is incomplete
            http.Abort();
            return;
        }

        await ConnectErrorWriter.WriteAsync(http.Response, error).ConfigureAwait(false);
    }

    private sealed class CompositeEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly List<IEndpointConventionBuilder> _builders;

        public CompositeEndpointConventionBuilder(List<IEndpointConventionBuilder> builders) => _builders = builders;

        public void Add(Action<EndpointBuilder> convention)
        {
            foreach (var builder in _builders) builder.Add(convention);
        }

        public void Finally(Action<EndpointBuilder> finallyConvention)
        {
            foreach (var builder in _builders) builder.Finally(finallyConvention);
        }
    }
}

/// <summary>
/// Registers the Connect server's services.
/// </summary>
public static class ConnectServiceCollectionExtensions
{
    /// <summary>Adds Connect server support, and configures the codecs it accepts.</summary>
    public static IServiceCollection AddConnect(this IServiceCollection services, Action<ConnectServerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddRouting();
        services.AddOptions<ConnectServerOptions>();
        if (configure is not null) services.Configure(configure);
        return services;
    }
}
