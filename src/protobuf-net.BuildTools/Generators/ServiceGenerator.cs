#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProtoBuf.BuildTools.Internal;
using System;
using System.Collections.Immutable;
using System.Threading;

namespace ProtoBuf.BuildTools.Generators
{
    /// <summary>
    /// Generates protobuf-net types from .proto schemas
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class ServiceGenerator : IIncrementalGenerator, ILoggingAnalyzer
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
                    || context.SemanticModel.GetDeclaredSymbol(context.Node, token) is not ITypeSymbol { TypeKind: TypeKind.Interface } type)
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
        }

        private sealed class ServiceDeclaration
        {
            public ServiceDeclaration(ITypeSymbol type)
            {
                Type = type;
            }
            public ITypeSymbol Type { get; }
        }
    }
}
