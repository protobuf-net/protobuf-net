#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProtoBuf.BuildTools.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.Internal.RoslynUtils;

namespace ProtoBuf.Generators
{
    /// <summary>
    /// Generates ProtoUnion-classes implementation
    /// </summary>
    [Generator]
    public sealed class ProtoUnionGenerator : ISourceGenerator, ILoggingAnalyzer
    {
        private event Action<string>? Log;
        event Action<string>? ILoggingAnalyzer.Log
        {
            add => this.Log += value;
            remove => this.Log -= value;
        }

        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            LogDebugInfo();
            var unionClasses = GetUnionClassesToGenerate(context.Compilation, context.CancellationToken);

            void LogDebugInfo()
            {
                Log?.Invoke("Execute with debug log enabled");

                Version?
                    pbnetVersion = context.Compilation.GetProtobufNetVersion(),
                    pbnetGrpcVersion = context.Compilation.GetReferenceVersion("protobuf-net.Grpc"),
                    wcfVersion = context.Compilation.GetReferenceVersion("System.ServiceModel.Primitives");

                Log?.Invoke($"Referencing protobuf-net {ShowVersion(pbnetVersion)}, protobuf-net.Grpc {ShowVersion(pbnetGrpcVersion)}, WCF {ShowVersion(wcfVersion)}");

                string ShowVersion(Version? version)
                    => version is null ? "(n/a)" : $"v{version}";

                if (Log is not null)
                {
                    foreach (var ran in context.Compilation.ReferencedAssemblyNames.OrderBy(x => x.Name))
                    {
                        Log($"reference: {ran.Name} v{ran.Version}");
                    }
                }
            }
        }

        private ClassDeclarationSyntax[] GetUnionClassesToGenerate(Compilation compilation, CancellationToken cancellationToken)
        {
            return compilation.SyntaxTrees
                .SelectMany(t => t.GetRoot(cancellationToken).DescendantNodes())
                .OfType<ClassDeclarationSyntax>()
                    // taking only classes with `partial` modifier
                .Where(syntax => syntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)))
                    // only classes which have `ProtoUnion` attribute
                .Where(classDeclaration => classDeclaration.ContainsAttribute<ProtoUnionAttribute>(compilation))
                .ToArray();
        }
    }
}
