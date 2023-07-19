#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using ProtoBuf.BuildTools.Internal;
using System;
using System.Buffers;
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
            int typeIndex = 0;
            foreach (var grp in tuple.Right.GroupBy(x => x.PrimaryType.ContainingSymbol, SymbolEqualityComparer.Default))
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
                    var primaryType = svc.PrimaryType;
                    if (!IsPartial(primaryType))
                    {
                        RequirePartialType(context, primaryType);
                        continue;
                    }

                    sb.Append("// [global::ClientProxyAttribute(typeof(ServiceProxy").Append(typeIndex)
                        .Append("))]").NewLine();
                    sb.Append("partial ").Append(CodeWriter.TypeLabel(primaryType)).Append(" ").Append(primaryType.Name).Indent().Outdent();
                    sb.NewLine();
                    sb.Append("sealed file class ServiceProxy").Append(typeIndex).Append(" : global::Grpc.Core.ClientBase<ServiceProxy").Append(typeIndex).Append(">");

                    sb.Indent();

                    sb.Append("// public ServiceProxy").Append(typeIndex).Append("() : base() {}").NewLine()
                        .Append("// public ServiceProxy").Append(typeIndex).Append("(global::Grpc.Core.ChannelBase channel) : base(channel) {}").NewLine()
                        .Append("public ServiceProxy").Append(typeIndex).Append("(global::Grpc.Core.CallInvoker callInvoker) : base(callInvoker) {}").NewLine()
                        .Append("private ServiceProxy").Append(typeIndex).Append("(global::Grpc.Core.ClientBase.ClientBaseConfiguration configuration) : base(configuration) {}").NewLine()
                        .Append("protected override ServiceProxy").Append(typeIndex).Append(" NewInstance(global::Grpc.Core.ClientBase.ClientBaseConfiguration configuration) => new ServiceProxy").Append(typeIndex).Append("(configuration);").NewLine()
                        .NewLine()
                        .Append("private const string _pbn_ServiceName = ").AppendVerbatimLiteral(svc.ServiceName).Append(";").NewLine();

                    int methodIndex = 0;
                    foreach (var method in svc.Methods)
                    {
                        sb.NewLine().Append("private static readonly global::Grpc.Core.Method<").Append(method.RequestType).Append(", ").Append(method.ResponseType).Append("> _pbn_Method").Append(methodIndex).Append(" = new global::Grpc.Core.Method<").Append(method.RequestType).Append(", ").Append(method.ResponseType).Append(">(")
                            .Append("global::Grpc.Core.MethodType.").Append(method.MethodKind).Append(", _pbn_ServiceName, ").AppendVerbatimLiteral(method.MethodName)
                            .Append(", null!, null!);").NewLine();
                    }
                    sb.Outdent();
                    typeIndex++;
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
                if (ns is { IsGlobalNamespace: true })
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
                PrimaryType = type;

                // TODO: namespace qualification, service lookup, etc
                ServiceName = type.Name;

                var members = type.GetMembers();
                if (members.Length > 0)
                {
                    int count = 0;
                    var lease = ArrayPool<MethodDeclaration>.Shared.Rent(members.Length);
                    foreach (var member in members)
                    {
                        if (member is IMethodSymbol method)
                        {
                            var svcMethod = MethodDeclaration.TryCreate(method);
                            if (svcMethod is not null)
                            {
                                lease[count++] = svcMethod;
                            }
                        }
                    }
                    Methods = ImmutableArray.Create(lease, 0, count);
                    ArrayPool<MethodDeclaration>.Shared.Return(lease);
                }
            }
            public INamedTypeSymbol PrimaryType { get; }
            public ImmutableArray<MethodDeclaration> Methods { get; }
            public string ServiceName { get; }
        }

        private sealed class MethodDeclaration
        {
            public static MethodDeclaration? TryCreate(IMethodSymbol method)
            {
                if (method.ReturnsVoid || method.Parameters.IsDefaultOrEmpty) return null;

                var req = method.Parameters[0].Type;
                var resp = method.ReturnType;
                var flags = MethodFlags.None;
                return req is INamedTypeSymbol namedReq && resp is INamedTypeSymbol namedResp ? new(method, namedReq, namedResp, flags) : null;
            }

            [Flags]
            public enum MethodFlags
            {
                None = 0,
                RequestStreaming = 1 << 0,
                ResponseStreaming = 2 << 0,
            }

            private MethodDeclaration(IMethodSymbol method, INamedTypeSymbol requestType, INamedTypeSymbol responseType, MethodFlags flags)
            {
                Method = method;

                // TODO: lots more
                MethodName = method.Name;
                RequestType = requestType;
                ResponseType = responseType;
                Flags = flags;
            }
            public IMethodSymbol Method { get; }

            public string MethodName { get; }
            public INamedTypeSymbol RequestType { get; }
            public INamedTypeSymbol ResponseType { get; }
            public MethodFlags Flags { get; }

            public string MethodKind => (Flags & (MethodFlags.RequestStreaming | MethodFlags.ResponseStreaming)) switch
            {
                MethodFlags.None => "Unary",
                MethodFlags.RequestStreaming => "ClientStreaming",
                MethodFlags.ResponseStreaming => "ServerStreaming",
                _ => "DuplexStreaming",
            };
        }
    }
}
