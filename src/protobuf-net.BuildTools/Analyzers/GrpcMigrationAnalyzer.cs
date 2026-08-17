#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.BuildTools.Internal.Grpc;
using System.Collections.Immutable;

namespace ProtoBuf.BuildTools.Analyzers
{
    /// <summary>
    /// The gRPC counterpart of <see cref="AotMigrationAnalyzer"/>'s <c>PBN3012</c>: a project that asks
    /// for AOT or trimming, uses protobuf-net.Grpc, and has not squared the circle with
    /// <c>[ProtoGrpc]</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Worth having separately from the serializer one because the failure is *further* from the
    /// developer: turning on <c>PublishAot</c> changes nothing at build time, the JIT run keeps working,
    /// and the first sign of trouble is a native publish or a startup that cannot bind. Both halves of
    /// protobuf-net.Grpc reach reflection without a model - the proxies through <c>ProxyEmitter</c>, the
    /// payloads through <c>MarshallerCache</c> - so there is nothing to fall back to.
    /// </para>
    /// <para>
    /// <b>The trigger is consumer-side usage, not the presence of service contracts</b>, and that
    /// distinction is load-bearing. Shipping <c>[Service]</c> interfaces in a shared package is the
    /// recommended layout, and such a package needs no <c>[ProtoGrpc]</c> of its own - its consumers do.
    /// Triggering on contract declarations would therefore nag hardest at exactly the project that is
    /// laid out correctly. So the two things that mean "this project is a client or a server" are what
    /// count: a plain <c>CreateGrpcService&lt;T&gt;</c>, and the server's <c>AddCodeFirstGrpc</c>.
    /// </para>
    /// <para>
    /// A <c>CreateGrpcService</c> call that already passes a factory is left alone - that consumer has
    /// done the thing we would ask for. It is the *plain* form that is going to reflect.
    /// </para>
    /// <para>
    /// A warning rather than an error, for the same reason <c>PBN3012</c> is: switching on
    /// <c>PublishAot</c> should not break someone's build on the spot. Anyone wanting it to is one
    /// <c>WarningsAsErrors</c> away.
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class GrpcMigrationAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor NoProtoGrpcUnderAot = new(
            id: "PBN4015",
            title: "This project publishes AOT or trimmed, but has no build-time gRPC proxies",
            messageFormat: "This project uses protobuf-net.Grpc and asks for {0}, but declares no "
                + "[ProtoGrpc]; the proxies and their payload marshallers will be built by reflection, "
                + "which is exactly what will not survive. See https://grpc.protobuf-net.dev/aot",
            category: "ProtoBuf.Grpc",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// A call that could be using a generated factory and is not.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The direct counterpart of <c>PBN3010</c> for the serializer half, and it fires for the same
        /// reason: declaring a <c>[ProtoGrpc]</c> does not move existing call sites onto it. A plain
        /// <c>CreateGrpcService&lt;T&gt;</c> keeps working - through <c>ProxyEmitter</c> - so nothing
        /// complains until a publish.
        /// </para>
        /// <para>
        /// No AOT request is required here, unlike <c>PBN4015</c>: a project that has built a proxy for
        /// this contract and is not using it is paying ref-emit for nothing, whatever it publishes as.
        /// </para>
        /// <para>
        /// Not reported when interceptors are enabled for our namespace - the generator has taken the
        /// call site over, so there is nothing left to ask of the consumer.
        /// </para>
        /// </remarks>
        internal static readonly DiagnosticDescriptor CallDoesNotUseGeneratedFactory = new(
            id: "PBN4016",
            title: "Call does not use the build-time gRPC proxies",
            messageFormat: "This call builds its proxy by reflection, but '{0}' has one for '{1}'. Pass "
                + "'{0}.Instance', or enable interceptors for ProtoBuf.AOT to have this done for you",
            category: "ProtoBuf.Grpc",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// DI-registered clients resolve their factory from the container, and this one has not been put
        /// there.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>AddCodeFirstGrpcClient&lt;T&gt;</c> - all six overloads - funnels through
        /// <c>ConfigureCodeFirstGrpcClient&lt;T&gt;</c>, whose body is
        /// <c>CreateGrpcService&lt;T&gt;(callInvoker, services.GetService&lt;ClientFactory&gt;())</c>. So
        /// unlike a direct call there is no argument to fix and no call site to intercept: the seam is the
        /// container, and one registration covers every client registered in it.
        /// </para>
        /// <para>
        /// That is the only place protobuf-net.Grpc resolves a <c>ClientFactory</c> from DI, so this
        /// suggestion has exactly one lever to point at.
        /// </para>
        /// <para>
        /// Whether the registration exists is a *dynamic* question being answered statically, so the check
        /// is a heuristic and deliberately biased toward silence: anything that looks like a
        /// <c>ClientFactory</c> registration anywhere in the compilation suppresses it. A registration in
        /// another assembly, or built by a helper, is invisible - so this can miss, and that is preferable
        /// to nagging someone who has already done it.
        /// </para>
        /// </remarks>
        internal static readonly DiagnosticDescriptor DiFactoryNotRegistered = new(
            id: "PBN4017",
            title: "DI-registered gRPC clients are not using the build-time proxies",
            messageFormat: "'{0}' has a build-time proxy for '{1}', but clients registered with "
                + "AddCodeFirstGrpcClient resolve their factory from the container. Add "
                + "'services.AddSingleton<ClientFactory>({0}.Instance);' so these clients use it",
            category: "ProtoBuf.Grpc",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>The factory to pass, for <c>UseGeneratedClientFactoryCodeFixProvider</c>.</summary>
        internal const string FactoryProperty = "factory";

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(NoProtoGrpcUnderAot, CallDoesNotUseGeneratedFactory,
                DiFactoryNotRegistered);

        private const string ProtoGrpcAttributeName = "ProtoBuf.Grpc.Configuration.ProtoGrpcAttribute";
        private const string ClientFactoryHolder = "ProtoBuf.Grpc.Client.GrpcClientFactory";
        private const string ServerExtensions = "ProtoBuf.Grpc.Server.ServicesExtensions";
        private const string CreateGrpcService = "CreateGrpcService";
        private const string AddCodeFirstGrpc = "AddCodeFirstGrpc";
        private const string ClientFactoryExtensions = "ProtoBuf.Grpc.ClientFactory.ServicesExtensions";
        private const string AddCodeFirstGrpcClient = "AddCodeFirstGrpcClient";
        private const string ClientFactoryTypeName = "ProtoBuf.Grpc.Configuration.ClientFactory";

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext ctx)
        {
            ctx.EnableConcurrentExecution();
            ctx.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            ctx.RegisterCompilationStartAction(static start => RegisterUnusedFactory(start));

            ctx.RegisterCompilationStartAction(static compilationStart =>
            {
                // the opening line everywhere in this assembly: declining the tooling costs one lookup
                if (compilationStart.Options.AnalyzerConfigOptionsProvider.BuildToolsDisabled()) return;

                // nothing to say to a project that has not asked for AOT or trimming; the runtime model
                // is a perfectly good way to use protobuf-net.Grpc
                if (compilationStart.Options.AnalyzerConfigOptionsProvider.AsksForAot() is not string asked) return;

                var compilation = compilationStart.Compilation;

                // ...nor to one that has already done what we would ask for
                if (compilation.GetTypeByMetadataName(ProtoGrpcAttributeName) is not { } protoGrpc) return;
                if (HasAnyProtoGrpc(compilation, protoGrpc)) return;

                compilationStart.RegisterOperationAction(context =>
                {
                    if (context.Operation is not IInvocationOperation invocation) return;
                    if (!IsUnmigratedUsage(invocation)) return;

                    context.ReportDiagnostic(Diagnostic.Create(
                        NoProtoGrpcUnderAot, invocation.Syntax.GetLocation(), asked));
                }, OperationKind.Invocation);
            });
        }

        /// <summary>
        /// <c>PBN4016</c>: a plain call that a declared model could have served.
        /// </summary>
        /// <remarks>
        /// Silent once interceptors are enabled, because the generator then rewrites the call and there is
        /// nothing to ask for. That check reads the syntax tree's own parse options, which is where the
        /// feature lands.
        /// </remarks>
        private static void RegisterUnusedFactory(CompilationStartAnalysisContext compilationStart)
        {
            if (compilationStart.Options.AnalyzerConfigOptionsProvider.BuildToolsDisabled()) return;

            var compilation = compilationStart.Compilation;
            if (compilation.GetTypeByMetadataName(ProtoGrpcAttributeName) is not { } protoGrpc) return;

            var covered = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal);
            foreach (var type in EnumerateTypes(compilation.Assembly.GlobalNamespace))
            {
                if (!CarriesAttribute(type, protoGrpc)) continue;
                foreach (var contract in NamedContracts(type))
                {
                    var key = contract.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (!covered.ContainsKey(key))
                    {
                        covered[key] = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    }
                }
            }
            if (covered.Count == 0) return;

            // Resolved at most once, and only if a DI-registered client is actually found - so a project
            // that does not use that path never pays for the scan.
            var registered = new System.Lazy<bool>(
                () => HasClientFactoryRegistration(compilation),
                System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

            compilationStart.RegisterOperationAction(context =>
            {
                if (context.Operation is not IInvocationOperation invocation) return;

                if (IsDiClientRegistration(invocation)
                    && invocation.TargetMethod.TypeArguments.Length == 1
                    && covered.TryGetValue(
                        invocation.TargetMethod.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        out var diFactory)
                    && !registered.Value)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiFactoryNotRegistered, invocation.Syntax.GetLocation(),
                        Readable(diFactory), invocation.TargetMethod.TypeArguments[0].ToDisplayString()));
                    return;
                }

                if (!IsPlainCreateGrpcService(invocation)) return;

                if (InterceptorSupport.IsEnabled(
                    invocation.Syntax.SyntaxTree.Options as Microsoft.CodeAnalysis.CSharp.CSharpParseOptions))
                {
                    return;
                }

                if (invocation.TargetMethod.TypeArguments.Length != 1) return;
                var contractSymbol = invocation.TargetMethod.TypeArguments[0];
                var contract = contractSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (!covered.TryGetValue(contract, out var factory)) return;

                // Two spellings of the same two types, deliberately. The *property* is fully qualified,
                // because the fixer parses it into an expression and has no idea what is in scope at the
                // call site; the *message* is the readable form, because "global::MyServices" in prose is
                // noise a consumer has to mentally strip.
                var properties = ImmutableDictionary<string, string?>.Empty.Add(FactoryProperty, factory);
                context.ReportDiagnostic(Diagnostic.Create(
                    CallDoesNotUseGeneratedFactory, invocation.Syntax.GetLocation(), properties,
                    Readable(factory), contractSymbol.ToDisplayString()));
            }, OperationKind.Invocation);
        }

        private static bool IsDiClientRegistration(IInvocationOperation invocation)
        {
            var declared = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod.OriginalDefinition;
            return declared.ContainingType?.ToDisplayString() == ClientFactoryExtensions
                && declared.Name == AddCodeFirstGrpcClient;
        }

        /// <summary>
        /// Whether anything in this compilation looks like a <c>ClientFactory</c> service registration.
        /// </summary>
        /// <remarks>
        /// Syntax-only on purpose: it is a suppression test for a suggestion, so being cheap and
        /// generous matters more than being exact. Any <c>Add*</c>/<c>TryAdd*</c> invocation naming
        /// <c>ClientFactory</c> - as a type argument or in an argument - counts.
        /// </remarks>
        private static bool HasClientFactoryRegistration(Compilation compilation)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                // Generated trees are skipped, and that matters rather than being tidiness: the generated
                // AddXxx() *contains* a ClientFactory registration, so counting it would suppress this
                // suggestion for every project - including one that never calls AddXxx.
                if (tree.FilePath.EndsWith(".g.cs", System.StringComparison.OrdinalIgnoreCase)) continue;

                foreach (var node in tree.GetRoot().DescendantNodes())
                {
                    if (node is not Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax invocation)
                    {
                        continue;
                    }
                    var name = invocation.Expression switch
                    {
                        Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax member => member.Name,
                        _ => null,
                    };
                    var identifier = name switch
                    {
                        Microsoft.CodeAnalysis.CSharp.Syntax.GenericNameSyntax generic => generic.Identifier.ValueText,
                        Microsoft.CodeAnalysis.CSharp.Syntax.SimpleNameSyntax simple => simple.Identifier.ValueText,
                        _ => null,
                    };
                    if (identifier is null || !IsRegistrationMethod(identifier)) continue;
                    if (invocation.ToString().Contains("ClientFactory")) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The DI registration methods, named explicitly rather than matched by prefix.
        /// </summary>
        /// <remarks>
        /// A prefix test was the first cut and it suppressed the very diagnostic it was guarding:
        /// <c>AddCodeFirstGrpcClient</c> starts with "Add", and fully qualified its text contains
        /// "ClientFactory" - because the namespace is literally <c>ProtoBuf.Grpc.ClientFactory</c>. So the
        /// call being flagged looked like the registration that would excuse it.
        /// </remarks>
        private static bool IsRegistrationMethod(string identifier) => identifier switch
        {
            "AddSingleton" or "AddScoped" or "AddTransient" => true,
            "TryAddSingleton" or "TryAddScoped" or "TryAddTransient" => true,
            "Add" or "TryAdd" or "TryAddEnumerable" => true,
            _ => false,
        };

        /// <summary>Drops the <c>global::</c> prefix, which belongs in code rather than in prose.</summary>
        private static string Readable(string qualified)
            => qualified.StartsWith("global::", System.StringComparison.Ordinal)
                ? qualified.Substring("global::".Length)
                : qualified;

        private static bool CarriesAttribute(INamedTypeSymbol type, INamedTypeSymbol attribute)
        {
            foreach (var candidate in type.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(candidate.AttributeClass, attribute)) return true;
            }
            return false;
        }

        /// <summary>The contracts a <c>[ProtoGrpc]</c> type names via <c>[ProtoService]</c>.</summary>
        private static System.Collections.Generic.IEnumerable<INamedTypeSymbol> NamedContracts(INamedTypeSymbol type)
        {
            const string ProtoService = "ProtoBuf.Grpc.Configuration.ProtoServiceAttribute";
            foreach (var attribute in type.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != ProtoService) continue;
                if (attribute.ConstructorArguments.Length == 0) continue;
                if (attribute.ConstructorArguments[0].Value is INamedTypeSymbol contract
                    && contract.TypeKind != TypeKind.Error)
                {
                    yield return contract;
                }
            }
        }

        private static bool IsPlainCreateGrpcService(IInvocationOperation invocation)
        {
            var declared = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod.OriginalDefinition;
            return declared.ContainingType?.ToDisplayString() == ClientFactoryHolder
                && declared.Name == CreateGrpcService
                && NoFactoryPassed(invocation);
        }

        /// <summary>
        /// Whether any type in this compilation carries <c>[ProtoGrpc]</c>.
        /// </summary>
        /// <remarks>
        /// Only this assembly: a <c>[ProtoGrpc]</c> in a referenced one names its own model and does
        /// nothing for this project, exactly as for seeding.
        /// </remarks>
        private static bool HasAnyProtoGrpc(Compilation compilation, INamedTypeSymbol protoGrpc)
        {
            foreach (var type in EnumerateTypes(compilation.Assembly.GlobalNamespace))
            {
                foreach (var attribute in type.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, protoGrpc)) return true;
                }
            }
            return false;
        }

        private static System.Collections.Generic.IEnumerable<INamedTypeSymbol> EnumerateTypes(
            INamespaceOrTypeSymbol root)
        {
            foreach (var member in root.GetMembers())
            {
                switch (member)
                {
                    case INamespaceSymbol ns:
                        foreach (var nested in EnumerateTypes(ns)) yield return nested;
                        break;
                    case INamedTypeSymbol type:
                        yield return type;
                        foreach (var nested in EnumerateTypes(type)) yield return nested;
                        break;
                }
            }
        }

        private static bool IsUnmigratedUsage(IInvocationOperation invocation)
        {
            var method = invocation.TargetMethod;
            var holder = method.ContainingType?.ToDisplayString();

            if (holder == ServerExtensions && method.Name == AddCodeFirstGrpc) return true;

            if (holder != ClientFactoryHolder || method.Name != CreateGrpcService) return false;

            return NoFactoryPassed(invocation);
        }

        /// <summary>
        /// Whether no factory reached the call. One that already passes a factory is fine - that is the
        /// shape we would ask for. An omitted optional argument arrives as
        /// <c>ArgumentKind.DefaultValue</c>, and an explicit null is the same thing said out loud.
        /// </summary>
        private static bool NoFactoryPassed(IInvocationOperation invocation)
        {
            foreach (var argument in invocation.Arguments)
            {
                if (argument.Parameter?.Type is not INamedTypeSymbol type) continue;
                if (type.ToDisplayString() != "ProtoBuf.Grpc.Configuration.ClientFactory") continue;

                return argument.ArgumentKind == ArgumentKind.DefaultValue
                    || argument.Value.ConstantValue is { HasValue: true, Value: null };
            }
            return true;
        }
    }
}
