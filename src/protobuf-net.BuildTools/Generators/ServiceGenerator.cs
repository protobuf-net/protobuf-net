#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using ProtoBuf.BuildTools.Internal;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using static ProtoBuf.Generators.ServiceGenerator;

namespace ProtoBuf.BuildTools.Generators
{
    /// <summary>
    /// Generates protobuf-net types from .proto schemas
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed partial class ServiceGenerator : IIncrementalGenerator, ILoggingAnalyzer
    {
        private event Action<string>? Log;
        event Action<string>? ILoggingAnalyzer.Log
        {
            add => this.Log += value;
            remove => this.Log -= value;
        }

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            var nodes = context.SyntaxProvider.CreateSyntaxProvider(PreFilter, Parse)
                .Where(x => x is not null)
                .Select((x, _) => x!);
            var combined = context.CompilationProvider.Combine(nodes.Collect());
            context.RegisterImplementationSourceOutput(combined, Generate);
        }

        private bool PreFilter(SyntaxNode node, CancellationToken token)
            => node is InterfaceDeclarationSyntax;

        private ServiceDeclaration? Parse(GeneratorSyntaxContext context, CancellationToken token)
        {
            try
            {
                if (context.Node is not InterfaceDeclarationSyntax interfaceDeclaration
                    || context.SemanticModel.GetDeclaredSymbol(context.Node, token) is not INamedTypeSymbol { TypeKind: TypeKind.Interface } type)
                {
                    return null;
                }

                AttributeData? sa = null, sca = null;
                foreach (var attrib in type.GetAttributes())
                {
                    var ac = attrib.AttributeClass;

                    if (ac is null || ac.ContainingType is not null) continue; // we don't expect any known attributes to be inner types
                    if (ac.Name == "ServiceAttribute" &&
                        IsProtobufGrpcConfigurationNamespace(ac.ContainingNamespace))
                    {
                        sa = attrib;
                    }
                    else if (ac.Name == "ServiceContractAttribute" &&
                        ac.ContainingNamespace is
                        {
                            Name: "ServiceModel",
                            ContainingNamespace:
                            {
                                Name: "System",
                                ContainingNamespace.IsGlobalNamespace: true
                            }
                        })
                    {
                        sca = attrib;
                    }
                }

                if (sa is null && sca is null && !ImplementsIGrpcService(type))
                {
                    return null;
                }

                Log?.Invoke($"Discovered service: '{type}' via {(sa ?? sca)?.AttributeClass?.Name ?? "IGrpcService"}");
                return new ServiceDeclaration(type);
            }
            catch (Exception ex)
            {
                Log?.Invoke($"Unexpected error: '{ex.Message}' parsing {context.Node?.GetType()?.Name}");
                return null;
            }
        }

        private static bool ImplementsIGrpcService(ITypeSymbol type)
        {
            foreach (var i in type.Interfaces)
            {
                if (i.Name == "IGrpcService" && IsProtobufGrpcConfigurationNamespace(i.ContainingNamespace)
                    || ImplementsIGrpcService(i))
                {
                    return true;
                }
            }
            return false;
        }

        static bool IsProtobufGrpcConfigurationNamespace(INamespaceSymbol ns) => ns is
        {
            Name: "Configuration",
            ContainingNamespace:
            {
                Name: "Grpc",
                ContainingNamespace:
                {
                    Name: "ProtoBuf",
                    ContainingNamespace.IsGlobalNamespace: true
                }
            }
        };

        private void Generate(SourceProductionContext context, (Compilation Compilation, ImmutableArray<ServiceDeclaration> Right) tuple)
        {
            var sb = CodeWriter.Create();
            foreach (var grp  in tuple.Right.GroupBy(x => x.Type.ContainingSymbol, SymbolEqualityComparer.Default))
            {
                if (HasNonPartialType(grp.Key, context, out var ns)) continue;

                if (ns is not null)
                {
                    sb.Append("namespace ").Append(ns).Indent();
                }

                sb.IndentToContainingType(grp.Key, out int nestLevels);
                if (ns is not null) nestLevels++;

                foreach (var svc in grp)
                {
                    var type = svc.Type;
                    if (!IsPartial(type))
                    {
                        RequirePartialType(context, type);
                        continue;
                    }

                    sb.Append("partial ").Append(CodeWriter.TypeLabel(type)).Append(" ").Append(type.Name).Indent().Outdent();
                }
                sb.Outdent(nestLevels);
            }

            var s = sb.ToStringRecycle();
            if (!string.IsNullOrWhiteSpace(s))
            {
                context.AddSource("protobuf-net.services.generated.cs", SourceText.From(s, Encoding.UTF8));
            }

            static void RequirePartialType(in SourceProductionContext context, INamedTypeSymbol type)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.PartialTypeRequired, type.Locations.FirstOrDefault(), type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }
            static bool IsPartial(INamedTypeSymbol type)
            {
                foreach (var syntax in type.DeclaringSyntaxReferences)
                {
                    if (syntax.GetSyntax() is TypeDeclarationSyntax decl && decl.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)))
                    {
                        return true;
                    }
                }
                return false;
            }

            static bool HasNonPartialType(ISymbol container, in SourceProductionContext context, out INamespaceSymbol? ns)
            {
                if (container is INamespaceSymbol containerNs)
                {
                    ns = containerNs;
                    return false;
                }
                var type = container as INamedTypeSymbol;
                bool hasAnyNonPartial = false;
                ns = null;
                while (type is not null)
                {
                    if (!IsPartial(type))
                    {
                        RequirePartialType(context, type);
                        hasAnyNonPartial = true;
                    }
                    ns = type.ContainingNamespace;
                    type = type.ContainingType;
                }
                if (ns is { IsGlobalNamespace: true})
                {
                    // export as null
                    ns = null;
                }
                return hasAnyNonPartial;
            }
        }

        private sealed class ServiceDeclaration
        {
            public ServiceDeclaration(INamedTypeSymbol type)
            {
                Type = type;
            }
            public INamedTypeSymbol Type { get; }
        }
    }
}
