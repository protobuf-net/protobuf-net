#nullable enable
// A hand-maintained snapshot of the protobuf-net.Grpc / Grpc.Core / Grpc.AspNetCore.Server surface
// that generated code binds to, compiled into the golden tests' input compilation.
//
// The Grpc.Core and ProtoBuf.Grpc portions are adapted from
// https://github.com/protobuf-net/protobuf-net/pull/1255 by Victor Irzak (@virzak); the ClientFactory,
// MarshallerFactory and Grpc.AspNetCore.Server portions are new, because this approach binds to a
// different (and smaller) set of the runtime.
//
// Why a snapshot rather than a package reference: BuildTools *compiles in* protobuf-net.Core's
// sources, so referencing protobuf-net.Grpc - which references protobuf-net - would make every type
// in Core ambiguous here. ServiceContractAnalyzerTests already takes this approach for the same
// reason. Drift is caught by src/AotGrpcSmoke, which uses the real packages.
//
// Every signature here was checked against the real assemblies; notably the three-arity server-method
// delegates are Grpc.AspNetCore.Server.Model's, not Grpc.Core's (whose own are two-arity).

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Grpc.Core
{
    public abstract class SerializationContext
    {
        public virtual void Complete(byte[] payload) { }
        public virtual System.Buffers.IBufferWriter<byte> GetBufferWriter() => null!;
        public virtual void SetPayloadLength(int payloadLength) { }
        public virtual void Complete() { }
    }

    public abstract class DeserializationContext
    {
        public virtual int PayloadLength => 0;
        public virtual System.Buffers.ReadOnlySequence<byte> PayloadAsReadOnlySequence() => default;
    }

    public class Marshaller<T>
    {
        public Marshaller(Func<T, byte[]> serializer, Func<byte[], T> deserializer) { }
        public Marshaller(Action<T, SerializationContext> serializer, Func<DeserializationContext, T> deserializer) { }
    }

    public enum MethodType { Unary, ClientStreaming, ServerStreaming, DuplexStreaming }

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

    public interface IAsyncStreamReader<T> { }

    public interface IServerStreamWriter<T> { }
}

namespace Grpc.AspNetCore.Server
{
    public interface IGrpcServerBuilder { }
}

namespace Grpc.AspNetCore.Server.Model
{
    using global::Grpc.Core;

    public delegate Task<TResponse> UnaryServerMethod<TService, TRequest, TResponse>(
        TService service, TRequest request, ServerCallContext context)
        where TService : class where TRequest : class where TResponse : class;

    public delegate Task ServerStreamingServerMethod<TService, TRequest, TResponse>(
        TService service, TRequest request, IServerStreamWriter<TResponse> writer, ServerCallContext context)
        where TService : class where TRequest : class where TResponse : class;

    public delegate Task<TResponse> ClientStreamingServerMethod<TService, TRequest, TResponse>(
        TService service, IAsyncStreamReader<TRequest> reader, ServerCallContext context)
        where TService : class where TRequest : class where TResponse : class;

    public delegate Task DuplexStreamingServerMethod<TService, TRequest, TResponse>(
        TService service, IAsyncStreamReader<TRequest> reader, IServerStreamWriter<TResponse> writer, ServerCallContext context)
        where TService : class where TRequest : class where TResponse : class;

    public class ServiceMethodProviderContext<TService> where TService : class
    {
        public void AddUnaryMethod<TRequest, TResponse>(Method<TRequest, TResponse> method,
            IList<object> metadata, UnaryServerMethod<TService, TRequest, TResponse> invoker)
            where TRequest : class where TResponse : class { }

        public void AddServerStreamingMethod<TRequest, TResponse>(Method<TRequest, TResponse> method,
            IList<object> metadata, ServerStreamingServerMethod<TService, TRequest, TResponse> invoker)
            where TRequest : class where TResponse : class { }

        public void AddClientStreamingMethod<TRequest, TResponse>(Method<TRequest, TResponse> method,
            IList<object> metadata, ClientStreamingServerMethod<TService, TRequest, TResponse> invoker)
            where TRequest : class where TResponse : class { }

        public void AddDuplexStreamingMethod<TRequest, TResponse>(Method<TRequest, TResponse> method,
            IList<object> metadata, DuplexStreamingServerMethod<TService, TRequest, TResponse> invoker)
            where TRequest : class where TResponse : class { }
    }

    public interface IServiceMethodProvider<TService> where TService : class
    {
        void OnServiceMethodDiscovery(ServiceMethodProviderContext<TService> context);
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    using global::Grpc.AspNetCore.Server;

    public interface IServiceCollection { }

    public class ServiceDescriptor
    {
        public static ServiceDescriptor Singleton<TService>(TService implementationInstance) where TService : class => null!;
    }

    public static class GrpcServicesExtensions
    {
        public static IGrpcServerBuilder AddGrpc(this IServiceCollection services) => null!;
    }
}

namespace Microsoft.Extensions.DependencyInjection.Extensions
{
    using global::Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionDescriptorExtensions
    {
        public static void TryAddEnumerable(this IServiceCollection services, ServiceDescriptor descriptor) { }
    }
}

namespace ProtoBuf.Grpc
{
    using global::Grpc.Core;

    public readonly struct CallContext
    {
        public static readonly CallContext Default = default;
        public CallContext(object server, ServerCallContext context) { }
        public static explicit operator CallContext(CancellationToken cancellationToken) => default;
    }
}

namespace ProtoBuf.Grpc.Configuration
{
    using global::Grpc.Core;
    using global::ProtoBuf.Meta;

    // Name is get-only on both of these, and the constructor is the only way to set it - so a
    // fixture writing [Service(Name = "x")] is CS0617 in a real consumer's build. The snapshot had
    // them settable, which made that spelling compile here and nowhere else.
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

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class SubServiceAttribute : Attribute { }

    // The trigger attributes, real API in protobuf-net.Grpc 1.3.0+. Stubbed here rather than
    // referenced for the reason at the top of this file, and because the real ones are
    // [Experimental("PBN9001")] - which is an *error* by default, so a stub also keeps the fixtures
    // free of suppression noise. The generator matches these by full name, so a stub is equivalent.
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ProtoGrpcAttribute : Attribute
    {
        public Type? Model { get; set; }
        public string? RegistrationMethodName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ProtoServiceAttribute : Attribute
    {
        public ProtoServiceAttribute(Type contract) => Contract = contract;

        public ProtoServiceAttribute(Type contract, Type implementation)
        {
            Contract = contract;
            Implementation = implementation;
        }

        public Type Contract { get; }
        public Type? Implementation { get; }
    }

    public abstract class MarshallerFactory
    {
        protected MarshallerFactory() { }
        protected internal abstract bool CanSerialize(Type type);
    }

    public class ProtoBufMarshallerFactory : MarshallerFactory
    {
        protected internal override bool CanSerialize(Type type) => true;
        public static MarshallerFactory Create(TypeModel? model = null) => null!;
    }

    public class ServiceBinder
    {
        public static ServiceBinder Default { get; } = new ServiceBinder();
        protected ServiceBinder() { }
        public virtual IList<object> GetMetadata(MethodInfo method, Type contractType, Type serviceType) => null!;
    }

    public sealed class BinderConfiguration
    {
        public static BinderConfiguration Default { get; } = null!;
        public static BinderConfiguration Create(IList<MarshallerFactory>? marshallerFactories = null, ServiceBinder? binder = null) => null!;
        public Marshaller<T> GetMarshaller<T>() => null!;
        // public, and the reason the generated config can sidestep CanSerialize(Type) entirely
        public void SetMarshaller<T>(Marshaller<T>? marshaller) { }
        // public from 1.3.6; before that the generated bindings had to use ServiceBinder.Default,
        // which silently ignores a consumer's custom binder
        public ServiceBinder Binder { get; } = ServiceBinder.Default;
    }

    // the seam this whole approach hangs off: already abstract, already public, already accepted by
    // CreateGrpcService<T>(this CallInvoker, ClientFactory?) - so no runtime change is needed
    public abstract class ClientFactory
    {
        public static ClientFactory Default { get; } = null!;
        protected abstract BinderConfiguration BinderConfiguration { get; }
        public static implicit operator BinderConfiguration(ClientFactory? value) => null!;
        public abstract TService CreateClient<TService>(CallInvoker channel) where TService : class;
    }
}

namespace ProtoBuf.Grpc.Internal
{
    using global::Grpc.Core;
    using global::ProtoBuf.Grpc;

    public sealed class Empty
    {
        public static readonly Empty Instance = new Empty();
        public static readonly Task<Empty> InstanceTask = Task.FromResult(Instance);
    }

    [Obsolete("Not intended for direct consumption", false)]
    public static class Reshape
    {
        public static Task<Empty> EmptyTask(Task task) => null!;
        public static Task<Empty> EmptyValueTask(ValueTask task) => null!;

        public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(IAsyncStreamReader<T> reader, CancellationToken cancellationToken) => null!;
        public static Task WriteTo<T>(IAsyncEnumerable<T> reader, IServerStreamWriter<T> writer, CancellationToken cancellationToken) => null!;

        public static Task<TResponse> UnaryTaskAsync<TRequest, TResponse>(
            in CallContext context, CallInvoker invoker, Method<TRequest, TResponse> method, TRequest request, string? host)
            where TRequest : class where TResponse : class => null!;

        public static ValueTask<TResponse> UnaryValueTaskAsync<TRequest, TResponse>(
            in CallContext context, CallInvoker invoker, Method<TRequest, TResponse> method, TRequest request, string? host)
            where TRequest : class where TResponse : class => default;

        public static ValueTask UnaryValueTaskAsyncVoid<TRequest, TResponse>(
            in CallContext context, CallInvoker invoker, Method<TRequest, TResponse> method, TRequest request, string? host)
            where TRequest : class where TResponse : class => default;

        public static TResponse UnarySync<TRequest, TResponse>(
            in CallContext context, CallInvoker invoker, Method<TRequest, TResponse> method, TRequest request, string? host)
            where TRequest : class where TResponse : class => null!;

        public static void UnarySyncVoid<TRequest, TResponse>(
            in CallContext context, CallInvoker invoker, Method<TRequest, TResponse> method, TRequest request, string? host)
            where TRequest : class where TResponse : class { }

        public static IAsyncEnumerable<TResponse> ServerStreamingAsync<TRequest, TResponse>(
            in CallContext context, CallInvoker invoker, Method<TRequest, TResponse> method, TRequest request, string? host)
            where TRequest : class where TResponse : class => null!;

        public static Task<TResponse> ClientStreamingTaskAsync<TRequest, TResponse>(
            in CallContext context, CallInvoker invoker, Method<TRequest, TResponse> method,
            IAsyncEnumerable<TRequest> request, string? host)
            where TRequest : class where TResponse : class => null!;

        public static ValueTask<TResponse> ClientStreamingValueTaskAsync<TRequest, TResponse>(
            in CallContext context, CallInvoker invoker, Method<TRequest, TResponse> method,
            IAsyncEnumerable<TRequest> request, string? host)
            where TRequest : class where TResponse : class => default;

        public static ValueTask ClientStreamingValueTaskAsyncVoid<TRequest, TResponse>(
            in CallContext context, CallInvoker invoker, Method<TRequest, TResponse> method,
            IAsyncEnumerable<TRequest> request, string? host)
            where TRequest : class where TResponse : class => default;

        public static IAsyncEnumerable<TResponse> DuplexAsync<TRequest, TResponse>(
            in CallContext context, CallInvoker invoker, Method<TRequest, TResponse> method,
            IAsyncEnumerable<TRequest> request, string? host)
            where TRequest : class where TResponse : class => null!;
    }
}
