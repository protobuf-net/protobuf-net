#nullable enable
using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;

namespace ProtoBuf.Generators.Abstractions
{
    [Generator]
    public abstract class GeneratorBase : ISourceGenerator, ILoggingAnalyzer
    {
        private event Action<string>? Logger;
        event Action<string>? ILoggingAnalyzer.Log
        {
            add => Logger += value;
            remove => Logger -= value;
        }

        protected Version? ProtobufVersion;
        protected Version? ProtobufGrpcVersion;
        protected Version? WcfVersion;
        
        public virtual void Initialize(GeneratorInitializationContext context) { }
        public abstract void Execute(GeneratorExecutionContext context);
        
        protected void Log(string message) => Logger?.Invoke(message);

        protected void Startup(GeneratorExecutionContext context)
        {
            Log("Execute with debug log enabled");

            ProtobufVersion = context.Compilation.GetProtobufNetVersion();
            ProtobufGrpcVersion = context.Compilation.GetReferenceVersion("protobuf-net.Grpc");
            WcfVersion = context.Compilation.GetReferenceVersion("System.ServiceModel.Primitives");

            Log($"Referencing protobuf-net {ShowVersion(ProtobufVersion)}, protobuf-net.Grpc {ShowVersion(ProtobufGrpcVersion)}, WCF {ShowVersion(WcfVersion)}");

            string ShowVersion(Version? version)
                => version is null ? "(n/a)" : $"v{version}";

            if (Logger is not null)
            {
                foreach (var ran in context.Compilation.ReferencedAssemblyNames.OrderBy(x => x.Name))
                {
                    Log($"reference: {ran.Name} v{ran.Version}");
                }
            }
        }
    }
}