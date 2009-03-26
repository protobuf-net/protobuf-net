#if !SILVERLIGHT && !CF

using System;
using System.Net;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using ProtoBuf.ServiceModel.Client;
#if NET_3_0
using System.ServiceModel;
#endif
using System.Text;

namespace ProtoBuf.ServiceModel.Server
{
    /// <summary>
    /// Standalone http server compatible with <seealso cref="ProtoBuf.ServiceModel.Client.HttpBasicTransport"/>.
    /// </summary>
    public class HttpServer : IDisposable
    {
        abstract class ServerBase : IDisposable {
            private Dictionary<string, MethodInfo> actions = new Dictionary<string, MethodInfo>(StringComparer.InvariantCulture);
            public abstract object GetInstance();
            public abstract void ReleaseInstance(object instance);
            private readonly Type serviceType;
            public virtual void Dispose() { }
            Type ServerType { get { return serviceType; } }
            protected ServerBase(Type serviceType)
            {
                if(serviceType == null) throw new ArgumentNullException("serviceType");
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

            public string ServiceName { get { return RpcUtils.GetServiceName(serviceType); } }
        }
        class ServerSingleton<T> : ServerBase where T : class
        {
            private T instance;
            public ServerSingleton(T instance) : base(typeof(T)) {
                if(instance == null) throw new ArgumentNullException("instance");
                this.instance = instance;
            }
            public override void ReleaseInstance(object instance) {}
            public override object GetInstance() {return instance;}
            public override void Dispose()
            {
                HttpServer.Dispose(ref instance);
            }
        }
        class ServerPerCall<T> : ServerBase where T : class, new()
        {
            public ServerPerCall() : base(typeof(T)) { }
            public override void ReleaseInstance(object instance)
            {
                T service = instance as T;
                HttpServer.Dispose(ref service);
            }
            public override object GetInstance()
            {
                return new T();
            }
        }

        private Dictionary<string, ServerBase> services = new Dictionary<string, ServerBase>(StringComparer.InvariantCulture);
        private Uri uriPrefix;
        private HttpListener listener;

        /// <summary>
        /// Adds a per-call service to the server. An instance of the type will
        /// be created (and disposed if appropriate) per request. 
        /// </summary>
        /// <typeparam name="T">The type of service to provide.</typeparam>
        public void Add<T>() where T : class, new()
        {
            AddCore(new ServerPerCall<T>());
        }

        /// <summary>
        /// Adds a singleton service to the server. All requests will be
        /// serviced by the supplied instance. This instance will be
        /// disposed (if appropriate) with the server.
        /// </summary>
        /// <typeparam name="T">The type of service to provide.</typeparam>
        public void Add<T>(T singleton) where T : class
        {
            AddCore(new ServerSingleton<T>(singleton));
        }

        private void AddCore(ServerBase server)
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
        /// Create a new HttpServer instance for the given service-type.
        /// </summary>
        /// <param name="uriPrefix">The base uri on which to listen for messages.</param>
        /// <param name="serviceContractType">The interface that represents the service contract.</param>
        /// <param name="serviceImplementationType">The concrete type that implements the service contract.</param>
        public HttpServer(string uriPrefix, Type serviceContractType, Type serviceImplementationType)
        {
            if (string.IsNullOrEmpty(uriPrefix)) throw new ArgumentNullException("uriPrefix");
            this.uriPrefix = new Uri(uriPrefix);
            listener = new HttpListener();
            listener.Prefixes.Add(uriPrefix);
            gotContext = GotContext;
        }

        private static Type GetType(object serviceSingleton)
        {
            if (serviceSingleton == null) throw new ArgumentNullException("serviceSingleton");
            return serviceSingleton.GetType();
        }
        /// <summary>
        /// Create a new HttpServer instance for the given service-type.
        /// </summary>
        /// <param name="uriPrefix">The base uri on which to listen for messages.</param>
        public HttpServer(string uriPrefix)
        {
            if (string.IsNullOrEmpty(uriPrefix)) throw new ArgumentNullException("uriPrefix");
            this.uriPrefix = new Uri(uriPrefix);
            listener = new HttpListener();
            listener.Prefixes.Add(uriPrefix);
            gotContext = GotContext;
        }
        private void CheckDisposed()
        {
            if (listener == null) throw new ObjectDisposedException(ToString());
        }
        /// <summary>
        /// Begin listening for messages on the server.
        /// </summary>
        public void Start()
        {
            CheckDisposed();
            if (!listener.IsListening)
            {
                Trace.WriteLine("Starting server on " + uriPrefix);
                listener.Start();
                Trace.WriteLine("(started)");
                ListenForContext();
            }
        }
        Action<HttpListenerContext> gotContext;

        private void ProcessContext(HttpListenerContext context)
        {
            string rpcVer = context.Request.Headers[RpcUtils.HTTP_RPC_VERSION_HEADER];
            if (!string.IsNullOrEmpty(rpcVer) && rpcVer != "0.1")
            {
                throw new InvalidOperationException("Incorrect RPC version");
            }
            string[] segments = context.Request.Url.Segments;
            if(segments.Length != uriPrefix.Segments.Length + 2) {
                throw new InvalidOperationException("Invalid path to service");
            }
            string serviceName = segments[segments.Length -2].TrimEnd('/'),
                actionName = segments[segments.Length - 1].TrimEnd('/');

            ServerBase service;            
            if (!services.TryGetValue(serviceName, out service))
            {
                throw new InvalidOperationException("Service not available: " + serviceName);
            }
            MethodInfo method = service.GetAction(actionName);
            if (method == null) throw new InvalidOperationException("Action not found on service: " + serviceName + "/" + actionName);
            ParameterInfo[] parameters = method.GetParameters();
            object[] args = new object[parameters.Length];

            RpcUtils.UnpackArgs(context.Request.InputStream, method, args, RpcUtils.IsRequestArgument);

            object serviceInstance = service.GetInstance();
            try
            {
                object responseObj = method.Invoke(serviceInstance, args);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = RpcUtils.HTTP_RPC_MIME_TYPE;
                RpcUtils.PackArgs(context.Response.OutputStream, method, responseObj, args, RpcUtils.IsResponseArgument);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex, GetType() + ":" + service.ServiceName);
                throw;
            }
            finally // release the singleton if we own it...
            {
                service.ReleaseInstance(serviceInstance);
            }
        }
        static void Dispose<T>(ref T obj) where T : class
        { // note no IDisposable constraint; we're not always sure if
            // the item is disposable...
            if (obj != null && obj is IDisposable)
            {
                try { ((IDisposable)obj).Dispose(); }
                catch (Exception ex)
                { // log only
                    Trace.Write(ex, "HttpServer.Dispose");
                }
            }
            obj = null;
        }
        private void GotContext(HttpListenerContext context)
        {
            try
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest; // assume failure...
                ProcessContext(context);
            }
            catch (Exception ex)
            {
                try
                {
                    context.Response.ContentType = "text/plain";
                    byte[] buffer = Encoding.UTF8.GetBytes(ex.Message);
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                catch { }
            }
            finally
            {
                try
                {
                    context.Response.Close();
                }
                catch { }
                try
                {
                    ListenForContext();
                }
                catch { }
            }
        }
        private void ListenForContext() {
            AsyncUtility.RunAsync(
                    listener.BeginGetContext, listener.EndGetContext,
                    gotContext, null);
        }
        /// <summary>
        /// Stop listening for messages on the server, and release
        /// any associated resources.
        /// </summary>
        public void Close()
        {
            if (listener != null)
            {
                Trace.WriteLine("Stopping server on " + uriPrefix);
                listener.Close();
                Trace.WriteLine("(stopped)");
                Dispose(ref listener);
            }
            gotContext = null;
        }
        void IDisposable.Dispose() { Close(); }
    }
}
#endif