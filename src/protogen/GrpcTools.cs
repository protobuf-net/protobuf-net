#if GRPC_TOOLS
using Google.Protobuf.Reflection;
using grpc.reflection.v1alpha;
using Grpc.Core;
using Grpc.Net.Client;
using ProtoBuf;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Reflection;
using protogen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ProtoBuf
{
    internal static class GrpcTools
    {
        enum GrpcMode
        {
            List,
            Get,
        }

        internal static async Task<int> ExecuteAsync(string modeString, string uri, string serviceName,
            CodeGenerator codegen, string outPath, Dictionary<string,string> options)
        {
            
            if (!Enum.TryParse<GrpcMode>(modeString, true, out var mode))
            {
                Console.Error.WriteLine($"Unknown gRPC mode: '{modeString}'");
                return 1;
            }
            if (string.IsNullOrWhiteSpace(outPath) &&
                mode == GrpcMode.Get)
            {
                Console.Error.WriteLine($"Missing output directive; please specify --csharp_out etc");
                return 1;
            }

            switch (mode)
            {
                case GrpcMode.List:
                    Console.WriteLine($"Requesting gRPC service directory from '{uri}'...");
                    break;
                case GrpcMode.Get:
                    Console.WriteLine($"Requesting gRPC '{serviceName}' service from '{uri}'...");
                    break;
                default:
                    Console.Error.WriteLine($"Unexpected mode: {mode}");
                    return 1;
            }
            int errorCount = 0;
            GrpcClientFactory.AllowUnencryptedHttp2 = true;
            using var channel = GrpcChannel.ForAddress(uri);
            var service = channel.CreateGrpcService<IServerReflection>();

            FileDescriptorSet set = null;
            int services = 0;
            try
            {
                await foreach (var reply in service.ServerReflectionInfoAsync(GetRequest()))
                {
                    switch (reply.MessageResponseCase)
                    {
                        case ServerReflectionResponse.MessageResponseOneofCase.ListServicesResponse:
                            foreach (var availableService in reply.ListServicesResponse.Services)
                            {
                                services++;
                                Console.WriteLine($"- {availableService.Name}");
                            }
                            break;
                        case ServerReflectionResponse.MessageResponseOneofCase.FileDescriptorResponse:
                            {
                                var file = reply.FileDescriptorResponse;
                                if (file is null) continue;
                                foreach (byte[] payload in file.FileDescriptorProtoes)
                                {
                                    var proto = Serializer.Deserialize<FileDescriptorProto>(new Span<byte>(payload));
                                    proto.IncludeInOutput = true; // have to assume all

                                    foreach (var dependency in proto.Dependencies)
                                    {
                                        proto.AddImport(dependency, true, default);
                                    }

                                    (set ??= new FileDescriptorSet()).Files.Add(proto);
                                }
                            }
                            break;
                        case ServerReflectionResponse.MessageResponseOneofCase.ErrorResponse:
                            errorCount++;
                            var code = (StatusCode)reply.ErrorResponse.ErrorCode;
                            Console.Error.WriteLine($"{code}: {reply.ErrorResponse.ErrorMessage}");
                            break;
                    }
                }

                switch (mode)
                {
                    case GrpcMode.List:
                        Console.WriteLine($"gRPC services discovered: {services}");
                        break;
                    case GrpcMode.Get:
                        Console.WriteLine($"gRPC descriptors fetched: {set?.Files?.Count ?? 0}");
                        if (set is object)
                        {
                            set.Process();
                            foreach (var error in set.GetErrors())
                            {
                                errorCount++;
                                Console.WriteLine($"{(error.IsError ? "error" : "warning")} {error.ErrorNumber}: {error.Message}");
                            }
                            Program.WriteFiles(codegen.Generate(set, options: options), outPath);
                        }
                        break;
                }
            }
            catch (RpcException fault)
            {
                errorCount++;
                Console.Error.WriteLine($"{fault.StatusCode}: {fault.Status.Detail}");
            }
            return errorCount;

            IAsyncEnumerable<ServerReflectionRequest> GetRequest() => mode switch {
                GrpcMode.List => ListServices(),
                GrpcMode.Get => GetFileContainingSymbol(serviceName),
                _ => Nothing(),
            };
            static async IAsyncEnumerable<ServerReflectionRequest> ListServices()
            {
                if (Never()) await Task.Yield();
                yield return new ServerReflectionRequest { ListServices = "*" };
            }
            static async IAsyncEnumerable<ServerReflectionRequest> GetFileContainingSymbol(string symbol)
            {
                if (Never()) await Task.Yield();
                yield return new ServerReflectionRequest { FileContainingSymbol = symbol };
            }
            static async IAsyncEnumerable<ServerReflectionRequest> Nothing()
            {
                if (Never()) await Task.Yield();
                yield break;
            }
            static bool Never() => false;
        }

    }
}
#endif