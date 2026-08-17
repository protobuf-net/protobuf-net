#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal.Grpc;
using System.Collections.Immutable;
using System.Threading;

namespace ProtoBuf.BuildTools.Generators
{
    public sealed partial class GrpcProxyGenerator
    {
        private const string ClientFactoryTypeName = "ProtoBuf.Grpc.Configuration.ClientFactory";

        /// <summary>
        /// Reduce a consumer-declared <c>[ProtoGrpc]</c> class to a plan, or to the diagnostics
        /// explaining why nothing was generated for it.
        /// </summary>
        /// <remarks>
        /// The seeds arrive as <c>typeof(...)</c> arguments, so they are resolved from <em>metadata</em>
        /// rather than from syntax in this compilation. That is deliberate and is what lets a consumer
        /// name contracts declared in a referenced assembly - the common shape, since service contracts
        /// usually live in a shared package. It is the same thing <c>[ProtoSerializable(typeof(Foo))]</c>
        /// already does for the serializer model.
        /// </remarks>
        private static GrpcModelCandidate? ParseModel(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ctx.TargetSymbol is not INamedTypeSymbol type || type.TypeKind != TypeKind.Class) return null;

            var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

            // The consumer's half must be partial, or there is nowhere to put our half.
            if (!IsPartial(type))
            {
                diagnostics.Add(new DiagnosticInfo(ModelMustBePartial, Where(type), type.Name));
                return new GrpcModelCandidate(null, diagnostics.ToImmutable());
            }

            // Deriving from ClientFactory is what makes `channel.CreateGrpcService<IFoo>(MyServices.Instance)`
            // work with *today's* protobuf-net.Grpc, unmodified: the seam already exists, and the two
            // members we override (BinderConfiguration, CreateClient<T>) are already abstract.
            if (!DerivesFromClientFactory(type))
            {
                diagnostics.Add(new DiagnosticInfo(ModelMustDeriveClientFactory, Where(type), type.Name, ClientFactoryTypeName));
                return new GrpcModelCandidate(null, diagnostics.ToImmutable());
            }

            string? modelTypeFullName = null;
            string? registrationMethodName = null;
            foreach (var attribute in type.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != ProtoGrpcAttributeName) continue;
                foreach (var named in attribute.NamedArguments)
                {
                    switch (named.Key)
                    {
                        case "Model" when named.Value.Value is INamedTypeSymbol model:
                            modelTypeFullName = model.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            break;
                        case "RegistrationMethodName" when named.Value.Value is string name && name.Length != 0:
                            registrationMethodName = name;
                            break;
                    }
                }
            }

            // Not naming a model is legal but almost never what anyone wants: the proxies are static
            // and the payloads are not, so the build succeeds and the AOT publish is the thing that
            // fails. Say so rather than let it through silently.
            if (modelTypeFullName is null)
            {
                diagnostics.Add(new DiagnosticInfo(NoModelNamed, Where(type), type.Name));
            }

            // Below the language floor nothing we would normally emit will parse, so the contracts are
            // not even inspected - their diagnostics would be noise stacked on the one that matters.
            // The check lives here rather than in the emit step so that it can be *anchored*: reported
            // from there it had no location at all, since the plan carries none.
            var languageVersion = ctx.TargetNode.SyntaxTree.Options is Microsoft.CodeAnalysis.CSharp.CSharpParseOptions options
                ? options.LanguageVersion
                : Microsoft.CodeAnalysis.CSharp.LanguageVersion.Default;
            if (languageVersion < MinimumLanguageVersion)
            {
                diagnostics.Add(new DiagnosticInfo(
                    LanguageVersionTooLow, Where(type), type.Name, MinimumLanguageVersionDisplay));
                return new GrpcModelCandidate(
                    DownLevelPlan(type, modelTypeFullName, registrationMethodName), diagnostics.ToImmutable());
            }

            var contracts = ImmutableArray.CreateBuilder<GrpcInterfaceModel>();
            var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
            foreach (var attribute in type.GetAttributes())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (attribute.AttributeClass?.ToDisplayString() != ProtoServiceAttributeName) continue;
                if (attribute.ConstructorArguments.Length == 0) continue;
                if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol contract) continue;

                var implementation = attribute.ConstructorArguments.Length > 1
                    ? attribute.ConstructorArguments[1].Value as INamedTypeSymbol
                    : null;

                // an unresolved typeof(...) arrives as an error symbol; the usual cause is naming a
                // type generated by another source generator in this same compilation, which no
                // generator can see - the same trap [ProtoModel] documents for .proto DTOs
                if (contract.TypeKind == TypeKind.Error)
                {
                    diagnostics.Add(new DiagnosticInfo(UnresolvedContract, Where(type), contract.Name));
                    continue;
                }

                var candidate = ParseContract(contract, implementation, cancellationToken);
                diagnostics.AddRange(candidate.Diagnostics);
                if (candidate.Model is { } model && seen.Add(model.InterfaceFullName))
                {
                    contracts.Add(model);
                }
            }

            var plan = new GrpcModelPlan(
                namespaceName: type.ContainingNamespace.IsGlobalNamespace
                    ? null : type.ContainingNamespace.ToDisplayString(),
                typeName: type.Name,
                modelTypeFullName: modelTypeFullName,
                isSealed: type.IsSealed,
                // suppressed where the consumer already declares `Instance` (which would be CS0102 in
                // their build) - their code, so the answer is to emit nothing rather than to complain
                emitInstance: !HasMember(type, "Instance"),
                emitConstructor: !HasExplicitConstructor(type),
                registrationMethodName: registrationMethodName ?? "Add" + type.Name,
                contracts: contracts.ToImmutable(),
                downLevel: false);

            return new GrpcModelCandidate(plan, diagnostics.ToImmutable());
        }

        /// <summary>
        /// What is emitted for a <c>[ProtoGrpc]</c> type below the language floor.
        /// </summary>
        /// <remarks>
        /// Emitting nothing was the obvious answer and the wrong one: <c>ClientFactory</c> has two
        /// abstract members, so a consumer who was merely too old for us got CS0534 twice and a build
        /// that failed - where <c>PBN4000</c> promised them the runtime proxy. The down-level shape
        /// keeps that promise instead, and is the same trade <c>src/DownLevelSmoke</c> pins for the
        /// serializer generator: a smaller model, not a broken build.
        /// </remarks>
        private static GrpcModelPlan DownLevelPlan(INamedTypeSymbol type, string? modelTypeFullName, string? registrationMethodName)
            => new GrpcModelPlan(
                namespaceName: type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
                typeName: type.Name,
                modelTypeFullName: modelTypeFullName,
                isSealed: type.IsSealed,
                emitInstance: !HasMember(type, "Instance"),
                emitConstructor: !HasExplicitConstructor(type),
                registrationMethodName: registrationMethodName ?? "Add" + type.Name,
                contracts: ImmutableArray<GrpcInterfaceModel>.Empty,
                downLevel: true);

        private static bool IsPartial(INamedTypeSymbol type)
        {
            foreach (var reference in type.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax declaration
                    && declaration.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool DerivesFromClientFactory(INamedTypeSymbol type)
        {
            for (var current = type.BaseType; current is not null; current = current.BaseType)
            {
                if (current.ToDisplayString() == ClientFactoryTypeName) return true;
            }
            return false;
        }

        private static bool HasMember(INamedTypeSymbol type, string name)
            => !type.GetMembers(name).IsDefaultOrEmpty;

        private static bool HasExplicitConstructor(INamedTypeSymbol type)
        {
            foreach (var constructor in type.InstanceConstructors)
            {
                if (!constructor.IsImplicitlyDeclared) return true;
            }
            return false;
        }

        private static ImmutableArray<DiagnosticInfo> One(DiagnosticDescriptor descriptor, Location? location, params string[] args)
            => ImmutableArray.Create(new DiagnosticInfo(descriptor, location, args));
    }
}
