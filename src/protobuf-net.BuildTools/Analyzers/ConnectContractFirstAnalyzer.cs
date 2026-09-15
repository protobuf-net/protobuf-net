#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using ProtoBuf.BuildTools.Internal;
using System.Collections.Immutable;
using System.Linq;

namespace ProtoBuf.BuildTools.Analyzers
{
    /// <summary>
    /// Catches the one way the contract-first Connect path can be silently <em>wrong</em> rather than
    /// merely incomplete: authorization attributes that will not be honoured.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Grpc.AspNetCore.Server</c> collects endpoint metadata by reflecting over the implementation's
    /// methods, so <c>[Authorize]</c> on a service method becomes endpoint metadata and ASP.NET Core's
    /// authorization middleware enforces it. <c>MapConnectService</c> deliberately does not reflect, so
    /// the same attribute becomes <b>nothing at all</b>, and the endpoint is served unauthenticated.
    /// </para>
    /// <para>
    /// That failure has no symptom. The build succeeds, the service answers, the tests pass, and the
    /// only difference is that anybody may call it - which is precisely the shape AGENTS.md records
    /// <c>AotGrpcMetadataDiff</c> as existing to prevent on the code-first side. A consumer migrating an
    /// existing gRPC service is exactly the person who has already written the attribute and will not
    /// think to re-state it.
    /// </para>
    /// <para>
    /// A warning rather than an error, per this assembly's convention - but this is the one here most
    /// worth escalating with <c>WarningsAsErrors</c>, and the message says so.
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ConnectContractFirstAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor AuthorizationNotCarried = new(
            id: "PBN5007",
            title: "Authorization attributes are not carried onto Connect endpoints",
            messageFormat: "'{0}' declares {1}, but this MapConnectService call supplies no endpoint "
                + "metadata, so the attribute is not honoured and the endpoint is served without it. "
                + "Chain .RequireAuthorization(...), or pass the 'metadata' argument",
            category: "ProtoBuf.Connect",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(AuthorizationNotCarried);

        private const string MapConnectService = "MapConnectService";
        private const string MetadataParameter = "metadata";

        // matched by name, following this assembly's convention throughout: the tests declare their own
        // stubs, and an analyzer that demanded the real ASP.NET Core reference would be silent in them
        private const string AuthorizeAttribute = "Microsoft.AspNetCore.Authorization.AuthorizeAttribute";
        private const string AllowAnonymousAttribute = "Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute";

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext ctx)
        {
            ctx.EnableConcurrentExecution();
            ctx.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            ctx.RegisterCompilationStartAction(static compilationStart =>
            {
                // the opening line everywhere in this assembly
                if (compilationStart.Options.AnalyzerConfigOptionsProvider.BuildToolsDisabled()) return;

                compilationStart.RegisterOperationAction(static context =>
                {
                    if (context.Operation is not IInvocationOperation invocation) return;
                    var method = invocation.TargetMethod;
                    if (method.Name != MapConnectService || method.TypeArguments.Length != 1) return;

                    // the contract-first overload is the one taking the generated BindService as a
                    // delegate; the code-first one takes an IConnectServiceBinder and infers nothing,
                    // so it has nothing to be wrong about
                    if (!TakesABindServiceDelegate(method)) return;

                    // metadata supplied, explicitly: the consumer has answered the question
                    if (Supplied(invocation, MetadataParameter)) return;

                    // ...or answered it with the ASP.NET Core idiom, on the builder we hand back
                    if (IsFollowedByAConvention(invocation)) return;

                    if (method.TypeArguments[0] is not INamedTypeSymbol implementation) return;
                    if (DescribeAuthorization(implementation) is not string described) return;

                    context.ReportDiagnostic(Diagnostic.Create(
                        AuthorizationNotCarried, invocation.Syntax.GetLocation(),
                        implementation.Name, described));
                }, OperationKind.Invocation);
            });
        }

        /// <summary>
        /// Distinguishes the contract-first overload from the code-first one, by its second parameter
        /// being a delegate rather than an interface.
        /// </summary>
        private static bool TakesABindServiceDelegate(IMethodSymbol method)
        {
            var parameters = method.Parameters;
            // the extension's `this` is not in Parameters for a reduced-form call, so check both shapes
            foreach (var parameter in parameters)
            {
                if (parameter.Type.TypeKind == TypeKind.Delegate) return true;
            }

            return false;
        }

        private static bool Supplied(IInvocationOperation invocation, string parameterName)
        {
            foreach (var argument in invocation.Arguments)
            {
                if (argument.Parameter?.Name != parameterName) continue;

                // an omitted optional argument still appears here, synthesised with its default value
                return argument.ArgumentKind == ArgumentKind.Explicit;
            }

            return false;
        }

        /// <summary>
        /// Whether the returned <c>IEndpointConventionBuilder</c> is used for anything.
        /// </summary>
        /// <remarks>
        /// Deliberately broad: <em>any</em> chained call, not only <c>RequireAuthorization</c>. The point
        /// of the diagnostic is that authorization was written down and silently dropped; a consumer who
        /// is doing something with the builder has demonstrably read the return value and is deciding for
        /// themselves. Insisting on a particular method here would make the rule a style opinion, and a
        /// noisy one - `RequireAuthorization` is not the only way to attach a policy, and a variable
        /// assignment is not evidence of anything either way.
        /// </remarks>
        private static bool IsFollowedByAConvention(IInvocationOperation invocation)
            => invocation.Syntax is InvocationExpressionSyntax { Parent: MemberAccessExpressionSyntax access }
                && access.IsKind(SyntaxKind.SimpleMemberAccessExpression);

        /// <summary>
        /// Names the authorization attributes on the implementation, or <c>null</c> when it has none.
        /// </summary>
        /// <remarks>
        /// <c>[AllowAnonymous]</c> counts. It is not merely decorative: on a service whose endpoints are
        /// otherwise protected by a convention it is the thing carving out the exception, so dropping it
        /// silently makes an endpoint <em>less</em> reachable rather than more - a different bug, equally
        /// invisible, and worth the same warning.
        /// </remarks>
        private static string? DescribeAuthorization(INamedTypeSymbol implementation)
        {
            var onType = false;
            var onMembers = 0;

            for (var type = implementation; type is not null; type = type.BaseType)
            {
                if (type.SpecialType == SpecialType.System_Object) break;

                if (HasAuthorizationAttribute(type.GetAttributes())) onType = true;

                foreach (var member in type.GetMembers())
                {
                    if (member is IMethodSymbol { MethodKind: MethodKind.Ordinary } candidate
                        && HasAuthorizationAttribute(candidate.GetAttributes()))
                    {
                        onMembers++;
                    }
                }
            }

            return (onType, onMembers) switch
            {
                (false, 0) => null,
                (true, 0) => "an authorization attribute",
                (false, 1) => "an authorization attribute on one of its methods",
                (false, _) => $"authorization attributes on {onMembers} of its methods",
                (true, 1) => "an authorization attribute, and one on a method",
                (true, _) => $"an authorization attribute, and {onMembers} on its methods",
            };
        }

        private static bool HasAuthorizationAttribute(ImmutableArray<AttributeData> attributes)
        {
            foreach (var attribute in attributes)
            {
                var name = attribute.AttributeClass?.ToDisplayString();
                if (name is AuthorizeAttribute or AllowAnonymousAttribute) return true;

                // a derived attribute is the documented way to carry a policy, and is far more common in
                // real code than the bare one
                for (var type = attribute.AttributeClass?.BaseType; type is not null; type = type.BaseType)
                {
                    if (type.ToDisplayString() is AuthorizeAttribute or AllowAnonymousAttribute) return true;
                }
            }

            return false;
        }
    }
}
