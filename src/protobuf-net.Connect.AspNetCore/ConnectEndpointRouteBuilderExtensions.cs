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

namespace ProtoBuf.Connect.AspNetCore
{
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
        /// <typeparam name="TImplementation">
        /// The service implementation, resolved from the request's services on each call.
        /// </typeparam>
        /// <param name="endpoints">The route builder.</param>
        /// <param name="binder">Describes the service's methods; normally generated.</param>
        /// <param name="routingPrefix">
        /// An optional prefix in front of every method path. The protocol allows one
        /// (<c>/[prefix/]package.Service/Method</c>), and it is what lets Connect sit beside gRPC on one
        /// host - the two use identical paths otherwise, so mapping both at the root puts two endpoints
        /// on one route, and the duplicated path then answers 500 at request time.
        /// </param>
        public static IEndpointConventionBuilder MapConnectService<TImplementation>(
            this IEndpointRouteBuilder endpoints,
            IConnectServiceBinder<TImplementation> binder,
            string? routingPrefix = null)
            where TImplementation : class
        {
            ArgumentNullException.ThrowIfNull(endpoints);
            ArgumentNullException.ThrowIfNull(binder);

            var prefix = string.IsNullOrWhiteSpace(routingPrefix)
                ? string.Empty
                : "/" + routingPrefix.Trim('/');

            var options = endpoints.ServiceProvider.GetRequiredService<IOptions<ConnectServerOptions>>().Value;
            if (options.Codecs.Count == 0)
            {
                throw new InvalidOperationException(
                    "No Connect codec is registered. Call services.AddConnect(o => o.Codecs.Add(new ProtoConnectCodec(MyModel.Instance))).");
            }

            var context = new ConnectServiceBinderContext<TImplementation>();
            binder.Bind(context);

            var builders = new List<IEndpointConventionBuilder>(context.Methods.Count);
            foreach (var method in context.Methods)
            {
                // POST only for now; GET arrives with idempotency, which needs the query-parameter form
                var builder = endpoints
                    .MapPost(prefix + method.Path, CreateHandler(method, options))
                    .WithDisplayName(method.DisplayName);

                foreach (var metadata in method.Metadata) builder.WithMetadata(metadata);
                builders.Add(builder);
            }

            return new CompositeEndpointConventionBuilder(builders);
        }

        private static RequestDelegate CreateHandler<TImplementation>(ConnectMethodRegistration<TImplementation> method, ConnectServerOptions options)
            where TImplementation : class
            => async http =>
            {
                // One error path for the whole call, per the constraint recorded in notes/connect/findings.md:
                // three separate throw sites would not converge once streaming exists.
                CancellationTokenSource? timeout = null;
                ConnectCodec? codec = null;
                ConnectServerCallContext? callContext = null;
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

                    codec = SelectCodec(http, options, method.Type);
                    RejectUnsupportedCompression(http, method.Type);

                    var cancellationToken = http.RequestAborted;
                    TimeSpan? span = TryGetTimeout(http, out var parsed) ? parsed : null;
                    if (span is { } deadlineIn)
                    {
                        timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeout.CancelAfter(deadlineIn);
                        cancellationToken = timeout.Token;
                    }

                    callContext = new ConnectServerCallContext(
                        http, method.Path, ConnectServerCallContext.DeadlineFrom(span), cancellationToken,
                        isUnary: method.Type == ConnectMethodType.Unary);
                    var service = http.RequestServices.GetRequiredService<TImplementation>();

                    await method.Invoker.InvokeAsync(http, service, codec, callContext).ConfigureAwait(false);
                }
                catch (ConnectException ex)
                {
                    await TryWriteError(http, ex, method.Type, codec, callContext).ConfigureAwait(false);
                }
                catch (Grpc.Core.RpcException ex)
                {
                    // how a contract-first service states a failure; it is a deliberate status, not a fault,
                    // so it must not fall through to the Internal catch-all below
                    await TryWriteError(http, ConnectException.FromRpcException(ex), method.Type, codec, callContext).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (http.RequestAborted.IsCancellationRequested)
                {
                    // the client went away; there is nobody to tell
                }
                catch (OperationCanceledException ex)
                {
                    // ...whereas this one is our own connect-timeout-ms firing, which the caller asked for
                    await TryWriteError(http, new ConnectException(
                        ConnectCode.DeadlineExceeded, "The call exceeded its deadline.", innerException: ex),
                        method.Type, codec, callContext).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var message = options.IncludeExceptionDetailInErrors
                        ? ex.GetType().Name + ": " + ex.Message
                        : null; // an unhandled exception's message is not for the caller to read
                    await TryWriteError(http, new ConnectException(
                        ConnectCode.Internal, message, innerException: ex), method.Type, codec, callContext).ConfigureAwait(false);
                }
                finally
                {
                    timeout?.Dispose();
                }
            };

        private static ConnectCodec SelectCodec(HttpContext http, ConnectServerOptions options, ConnectMethodType type)
        {
            var contentType = http.Request.ContentType;
            if (!ConnectContentType.TryParse(contentType, out var parsed))
            {
                throw ConnectException.UnsupportedMediaType($"'{contentType}' is not a Connect content-type.");
            }

            // the content-type states the framing, and it has to agree with the method's shape: unary is
            // the bare message, everything else is enveloped
            var wantsEnvelopes = type != ConnectMethodType.Unary;
            if (parsed.IsEnveloped != wantsEnvelopes)
            {
                throw ConnectException.UnsupportedMediaType(
                    wantsEnvelopes
                        ? $"'{contentType}' is the unary framing, but this method is {type}; use 'application/connect+{parsed.CodecName}'."
                        : $"'{contentType}' asks for enveloped framing, but this method is unary; use 'application/{parsed.CodecName}'.");
            }

            foreach (var codec in options.Codecs)
            {
                if (string.Equals(codec.Name, parsed.CodecName, StringComparison.OrdinalIgnoreCase)) return codec;
            }

            throw ConnectException.UnsupportedMediaType(
                $"The codec '{parsed.CodecName}' is not supported; this server accepts: {string.Join(", ", Names(options))}.");

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

        /// <summary>
        /// Reports a failure in whichever form the caller is able to read.
        /// </summary>
        /// <remarks>
        /// <b>The shape decides the form, and getting this wrong is invisible from our side.</b> A unary
        /// caller reads a non-200 with a JSON error body. A <em>streaming</em> caller is reading
        /// envelopes: it looks for the terminating one and parses the error out of that, so a bare JSON
        /// body is not something it can find at all - it reports "unexpected end of JSON input" and falls
        /// back to guessing from the HTTP status, which is how a deliberate <c>unimplemented</c> reached
        /// the conformance suite as <c>internal</c>.
        /// <para>
        /// So a streaming call answers <c>200</c> and puts the error in a terminating envelope <em>even
        /// when it failed before the stream began</em> - a request that could not be read, a message that
        /// arrived compressed, a cardinality the method does not allow. There is no "too early for the
        /// stream" case; there is only the framing the caller is reading.
        /// </para>
        /// </remarks>
        private static async Task TryWriteError(
            HttpContext http, ConnectException error, ConnectMethodType type, ConnectCodec? codec,
            ConnectServerCallContext? context)
        {
            if (http.Response.HasStarted)
            {
                // the response is committed, so the status and body are already gone; aborting is the only
                // way left to tell the client that what it has is incomplete
                http.Abort();
                return;
            }

            // no codec means content negotiation itself failed, and that is answered by status alone -
            // there is no agreed framing to answer in
            if (type == ConnectMethodType.Unary || codec is null)
            {
                // a failed call still carries whatever trailing metadata the handler set, and for a unary
                // call that is a `trailer-` prefixed header - so it has to be written before the body
                // commits, exactly as on the success path
                context?.FlushTrailers();
                await ConnectErrorWriter.WriteAsync(http.Response, error).ConfigureAwait(false);
                return;
            }

            http.Response.StatusCode = StatusCodes.Status200OK;
            http.Response.ContentType = codec.ContentTypeFor(type);

            // ...and for a streaming call the same values travel in the terminating envelope
            await EndStreamWriter
                .WriteAsync(http.Response.BodyWriter, error, context?.ResponseTrailers, http.RequestAborted)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Refuses a request whose body is compressed, since no compression is implemented yet.
        /// </summary>
        /// <remarks>
        /// The header differs by shape - <c>content-encoding</c> for a unary body, and Connect's own
        /// <c>connect-content-encoding</c> for the enveloped shapes, which exists precisely so that the
        /// envelope payloads can be compressed independently of the HTTP body. Ignoring either means
        /// handing compressed bytes to a codec and reporting the resulting garbage as a parse error.
        /// </remarks>
        private static void RejectUnsupportedCompression(HttpContext http, ConnectMethodType type)
        {
            var name = type == ConnectMethodType.Unary ? "content-encoding" : "connect-content-encoding";
            var header = http.Request.Headers[name];
            if (header.Count == 0) return;

            var encoding = header.ToString();
            if (string.IsNullOrEmpty(encoding)
                || string.Equals(encoding, "identity", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            throw new ConnectException(
                ConnectCode.Unimplemented,
                $"The '{encoding}' compression is not supported; this server accepts 'identity' only.");
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
}
