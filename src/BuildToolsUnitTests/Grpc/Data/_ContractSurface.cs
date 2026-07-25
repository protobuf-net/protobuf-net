// A snapshot of the protobuf-net.Grpc / Grpc.Core surface that generated proxies bind to, so the
// generator tests can compile their own output without this repository taking a dependency on
// protobuf-net.Grpc (the same approach as ServiceContractAnalyzerTests, for the same reason).
//
// Only what generated code touches is declared, but where it is declared the signature - including
// generic constraints, `in` modifiers and optional arguments - matches the real API exactly, since
// the point of compiling the output is to catch mismatches. Keep in step with:
//   https://github.com/protobuf-net/protobuf-net.Grpc/blob/main/src/protobuf-net.Grpc/Internal/Reshape.cs
//   https://github.com/protobuf-net/protobuf-net.Grpc/blob/main/src/protobuf-net.Grpc/Internal/GeneratedProxyRegistry.cs
//   https://github.com/protobuf-net/protobuf-net.Grpc/blob/main/src/protobuf-net.Grpc/Configuration/IServerMethodBinder.cs
#nullable enable
using Grpc.Core;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Grpc.Core
{
    public enum MethodType { Unary, ClientStreaming, ServerStreaming, DuplexStreaming }

    public class Marshaller<T> { }

    public class Method<TRequest, TResponse>
    {
        public Method(MethodType type, string serviceName, string name,
            Marshaller<TRequest> requestMarshaller, Marshaller<TResponse> responseMarshaller) { }
    }

    public abstract class CallInvoker { }

    public abstract class ClientBase
    {
        protected ClientBase(CallInvoker callInvoker) => CallInvoker = callInvoker;

        protected CallInvoker CallInvoker { get; }
    }

    public abstract class ServerCallContext
    {
        public CancellationToken CancellationToken => default;
    }

    // invariant, exactly as in Grpc.Core: variance here would let the generated code compile against
    // conversions the real API does not offer
    public interface IAsyncStreamReader<T> { }

    public interface IServerStreamWriter<T> { }
}

namespace ProtoBuf.Grpc
{
    public readonly struct CallContext
    {
        public static readonly CallContext Default;

        public CallContext(object server, ServerCallContext context) { }

        public static implicit operator CallContext(CancellationToken cancellationToken) => default;
    }
}

namespace ProtoBuf.Grpc.Configuration
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class ServiceAttribute : Attribute
    {
        public ServiceAttribute(string? name = null) => Name = name;

        public string? Name { get; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class OperationAttribute : Attribute
    {
        public OperationAttribute(string? name = null) => Name = name;

        public string? Name { get; }
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class SubServiceAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class ProxyAttribute : Attribute
    {
        public ProxyAttribute(Type type) => Type = type;

        public Type Type { get; }
    }

    public class BinderConfiguration
    {
        public static BinderConfiguration Default { get; } = new BinderConfiguration();

        public Marshaller<T> GetMarshaller<T>() => new Marshaller<T>();
    }

    public delegate Task<TResponse> UnaryServerHandler<TService, TRequest, TResponse>(
        TService service, TRequest request, ServerCallContext context)
        where TService : class where TRequest : class where TResponse : class;

    public delegate Task ServerStreamingServerHandler<TService, TRequest, TResponse>(
        TService service, TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context)
        where TService : class where TRequest : class where TResponse : class;

    public delegate Task<TResponse> ClientStreamingServerHandler<TService, TRequest, TResponse>(
        TService service, IAsyncStreamReader<TRequest> requestStream, ServerCallContext context)
        where TService : class where TRequest : class where TResponse : class;

    public delegate Task DuplexStreamingServerHandler<TService, TRequest, TResponse>(
        TService service, IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context)
        where TService : class where TRequest : class where TResponse : class;

    public interface IServerMethodBinder<TService> where TService : class
    {
        void AddUnaryMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, IList<object> metadata,
            UnaryServerHandler<TService, TRequest, TResponse> handler)
            where TRequest : class where TResponse : class;

        void AddServerStreamingMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, IList<object> metadata,
            ServerStreamingServerHandler<TService, TRequest, TResponse> handler)
            where TRequest : class where TResponse : class;

        void AddClientStreamingMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, IList<object> metadata,
            ClientStreamingServerHandler<TService, TRequest, TResponse> handler)
            where TRequest : class where TResponse : class;

        void AddDuplexStreamingMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, IList<object> metadata,
            DuplexStreamingServerHandler<TService, TRequest, TResponse> handler)
            where TRequest : class where TResponse : class;

        BinderConfiguration Configuration { get; }

        IList<object> GetMetadata(Type contractType, string methodName);
    }
}

namespace ProtoBuf.Grpc.Internal
{
    public sealed class Empty
    {
        public static readonly Empty Instance = new Empty();

        public static readonly Task<Empty> InstanceTask = Task.FromResult(Instance);
    }

    public static class GeneratedProxyRegistry
    {
        public static void RegisterClient<TService>(Func<CallInvoker, BinderConfiguration, TService> factory)
            where TService : class { }

        public static void RegisterServer(Type contractType, Type generatedBindingsType) { }
    }

    public static class Reshape
    {
        public static Task<Empty> EmptyTask(Task task) => Empty.InstanceTask;

        public static Task<Empty> EmptyValueTask(ValueTask task) => Empty.InstanceTask;

        public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IAsyncStreamReader<T> reader, CancellationToken cancellationToken) => null!;

        public static Task WriteTo<T>(this IAsyncEnumerable<T> reader, IServerStreamWriter<T> writer, CancellationToken cancellationToken) => Task.CompletedTask;

        public static TResponse UnarySync<TRequest, TResponse>(this in CallContext context,
            CallInvoker invoker, Method<TRequest, TResponse> method, TRequest request, string? host = null)
            where TRequest : class where TResponse : class => null!;

        public static void UnarySyncVoid<TRequest, TResponse>(this in CallContext context,
            CallInvoker invoker, Method<TRequest, TResponse> method, TRequest request, string? host = null)
            where TRequest : class where TResponse : class { }

        public static Task<TResponse> UnaryTaskAsync<TRequest, TResponse>(this in CallContext context,
            CallInvoker invoker, Method<TRequest, TResponse> method, TRequest request, string? host = null)
            where TRequest : class where TResponse : class => null!;

        public static ValueTask<TResponse> UnaryValueTaskAsync<TRequest, TResponse>(this in CallContext context,
            CallInvoker invoker, Method<TRequest, TResponse> method, TRequest request, string? host = null)
            where TRequest : class where TResponse : class => default;

        public static ValueTask UnaryValueTaskAsyncVoid<TRequest, TResponse>(this in CallContext context,
            CallInvoker invoker, Method<TRequest, TResponse> method, TRequest request, string? host = null)
            where TRequest : class where TResponse : class => default;

        public static IAsyncEnumerable<TResponse> ServerStreamingAsync<TRequest, TResponse>(this in CallContext context,
            CallInvoker invoker, Method<TRequest, TResponse> method, TRequest request, string? host = null)
            where TRequest : class where TResponse : class => null!;

        public static Task<TResponse> ClientStreamingTaskAsync<TRequest, TResponse>(this in CallContext options,
            CallInvoker invoker, Method<TRequest, TResponse> method, IAsyncEnumerable<TRequest> request, string? host = null)
            where TRequest : class where TResponse : class => null!;

        public static ValueTask<TResponse> ClientStreamingValueTaskAsync<TRequest, TResponse>(this in CallContext options,
            CallInvoker invoker, Method<TRequest, TResponse> method, IAsyncEnumerable<TRequest> request, string? host = null)
            where TRequest : class where TResponse : class => default;

        public static ValueTask ClientStreamingValueTaskAsyncVoid<TRequest, TResponse>(this in CallContext options,
            CallInvoker invoker, Method<TRequest, TResponse> method, IAsyncEnumerable<TRequest> request, string? host = null)
            where TRequest : class where TResponse : class => default;

        public static IAsyncEnumerable<TResponse> DuplexAsync<TRequest, TResponse>(this in CallContext options,
            CallInvoker invoker, Method<TRequest, TResponse> method, IAsyncEnumerable<TRequest> request, string? host = null)
            where TRequest : class where TResponse : class => null!;
    }
}
