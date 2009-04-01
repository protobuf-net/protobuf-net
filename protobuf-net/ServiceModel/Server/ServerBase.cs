#if !SILVERLIGHT && !CF
using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Collections.Specialized;
namespace ProtoBuf.ServiceModel.Server
{
    /// <summary>
    /// Provides common functionality required by RPC servers.
    /// </summary>
    public class ServerBase
    {

        private Dictionary<string, ServiceBase> services = new Dictionary<string, ServiceBase>(StringComparer.InvariantCulture);
        private static Type GetType(object serviceSingleton)
        {
            if (serviceSingleton == null) throw new ArgumentNullException("serviceSingleton");
            return serviceSingleton.GetType();
        }
        /// <summary>
        /// Represents a service endpoint provided by the server.
        /// </summary>
        protected abstract class ServiceBase : IDisposable
        {
            private Dictionary<string, MethodInfo> actions = new Dictionary<string, MethodInfo>(StringComparer.InvariantCulture);
            /// <summary>
            /// Obtains the instance representing the service endpoint for a call.
            /// </summary>
            public abstract object GetInstance();
            /// <summary>
            /// Releases the instance representing the service endpoint for a call.
            /// </summary>
            public abstract void ReleaseInstance(object instance);
            private readonly Type serviceType;
            /// <summary>
            /// Releases any resources associated with the endpoint.
            /// </summary>
            public virtual void Dispose() { }
            Type ServerType { get { return serviceType; } }
            /// <summary>
            /// Initialises a new service endpoint for the given service type.
            /// </summary>
            protected ServiceBase(Type serviceType)
            {
                if (serviceType == null) throw new ArgumentNullException("serviceType");
                if (!serviceType.IsInterface) throw new ArgumentException(serviceType.FullName + " is not an interface", "serviceType");
                this.serviceType = serviceType;

                foreach (MethodInfo method in serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (method.IsGenericMethod || method.IsGenericMethodDefinition
                        || method.DeclaringType != serviceType) continue;
                    string key = RpcUtils.GetActionName(method);
                    if (actions.ContainsKey(key))
                    {
                        throw new ArgumentException("Duplicate action \"" + key + "\" found on service-contract " + serviceType.FullName, "serviceContractType");
                    }
                    actions.Add(key, method);
                }
            }
            /// <summary>
            /// Obtains the method that represents a given action.
            /// </summary>
            /// <param name="name">The name of the action.</param>
            /// <returns>The method that should be invoked.</returns>
            public MethodInfo GetAction(string name)
            {
                if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
                MethodInfo method;
                if (!actions.TryGetValue(name, out method))
                {
                    throw new InvalidOperationException("Action not found on the service: " + ServiceName + "." + name);
                }
                return method;
            }

            /// <summary>
            /// The name of the service endpoint.
            /// </summary>
            public string ServiceName { get { return RpcUtils.GetServiceName(serviceType); } }
        }
        class ServiceSingleton<T> : ServiceBase where T : class
        {
            private T instance;
            public ServiceSingleton(T instance)
                : base(typeof(T))
            {
                if (instance == null) throw new ArgumentNullException("instance");
                this.instance = instance;
            }
            public override void ReleaseInstance(object instance) { }
            public override object GetInstance() { return instance; }
            public override void Dispose()
            {
                ServerBase.Dispose(ref instance);
            }
        }
        class ServicePerCall<TContract, TService> : ServiceBase
            where TContract : class
            where TService : class, TContract, new()
        {
            public ServicePerCall() : base(typeof(TContract))
            {
            }
            public override void ReleaseInstance(object instance)
            {
                TService service = instance as TService;
                ServerBase.Dispose(ref service);
            }
            public override object GetInstance()
            {
                return new TService();
            }
        }

        /// <summary>
        /// Adds a per-call service to the server. An instance of the type will
        /// be created (and disposed if appropriate) per request. 
        /// </summary>
        /// <typeparam name="TContract">The type of service-contract to provide.</typeparam>
        /// <typeparam name="TService">The concrete type that will implement the service.</typeparam>
        public void Add<TContract, TService>()
            where TContract : class
            where TService : class, TContract, new()
        {
            AddCore(new ServicePerCall<TContract, TService>());
        }

        /// <summary>
        /// Adds a singleton service to the server. All requests will be
        /// serviced by the supplied instance. This instance will be
        /// disposed (if appropriate) with the server.
        /// </summary>
        /// <typeparam name="T">The type of service to provide.</typeparam>
        public void Add<T>(T singleton) where T : class
        {
            AddCore(new ServiceSingleton<T>(singleton));
        }

        private void AddCore(ServiceBase server)
        {
            if (server == null) throw new ArgumentNullException("server");
            string key = server.ServiceName;
            if (services.ContainsKey(key))
            {
                throw new InvalidOperationException("Cannot add duplicate service: " + key);
            }
            services.Add(key, server);
        }

        /// <summary>
        /// Releases and nulls a given field/variable.
        /// </summary>
        protected static void Dispose<T>(ref T obj) where T : class

        { // note no IDisposable constraint; we're not always sure if
            // the item is disposable...
            if (obj != null && obj is IDisposable)
            {
                try { ((IDisposable)obj).Dispose(); }
#if SILVERLIGHT || CF
                catch {}
#else
                catch (Exception ex)
                { // log only
                    Trace.Write(ex, "HttpServer.Dispose");
                }
#endif
            }
            obj = null;
        }

        /// <summary>
        /// Performs any pre-response operations required.
        /// </summary>
        protected virtual void OnBeforeWriteResponse(object state) { }

        /// <summary>
        /// Performs server-side processing of an action, including deserialization
        /// of arguments, method-invokation, and serialization of the return value and
        /// any `out`/`ref` arguments.
        /// </summary>
        protected void Execute(string service, string action, NameValueCollection headers, Stream request, Stream response, object state)
        {
            if (string.IsNullOrEmpty(service))
            {                
                if (services.Count == 1) { // assume that one, then
                    foreach (string key in services.Keys)
                    {
                        service = key;
                    }
                }
                throw new ArgumentNullException("service", "The service must be specified when multiple services are supported");
            }
            if (string.IsNullOrEmpty(action)) throw new ArgumentNullException("action");
            if (headers == null) throw new ArgumentNullException("headers");
            if (request == null) throw new ArgumentNullException("request");
            if (response == null) throw new ArgumentNullException("response");

            string rpcVer = headers[RpcUtils.HTTP_RPC_VERSION_HEADER];
            if (!string.IsNullOrEmpty(rpcVer) && rpcVer != "0.1")
            {
                throw new InvalidOperationException("Incorrect RPC version");
            }
            
            ServerBase.ServiceBase serviceImpl;
            if (!services.TryGetValue(service, out serviceImpl))
            {
                throw new InvalidOperationException("Service not available: " + service);
            }
            MethodInfo method = serviceImpl.GetAction(action);
            if (method == null) throw new InvalidOperationException("Action not found on service: " + service + "/" + action);
            ParameterInfo[] parameters = method.GetParameters();
            object[] args = new object[parameters.Length];

            RpcUtils.UnpackArgs(request, method, args, RpcUtils.IsRequestArgument);

            object serviceInstance = serviceImpl.GetInstance();
            try
            {
                object responseObj = method.Invoke(serviceInstance, args);
                OnBeforeWriteResponse(state);
                RpcUtils.PackArgs(response, method, responseObj, args, RpcUtils.IsResponseArgument);
            }
#if !(SILVERLIGHT || CF)
            catch (Exception ex)
            {
                Trace.WriteLine(ex, GetType() + ":" + serviceImpl.ServiceName);
                throw;
            }
#endif
            finally // release the singleton if we own it...
            {
                serviceImpl.ReleaseInstance(serviceInstance);
            }
        }
    }
}
#endif