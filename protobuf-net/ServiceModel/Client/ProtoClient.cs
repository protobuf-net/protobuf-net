using System;
using System.Reflection;
using System.Net;
using System.Threading;
#if NET_3_0
using System.ServiceModel;
#endif

namespace ProtoBuf.ServiceModel.Client
{
    /// <summary>
    /// Provides transport-independent wrapper logic for
    /// managing RPC calls to the server.
    /// </summary>
    public class ProtoClient : IDisposable
    {
        private int timeout = 30000;

        /// <summary>
        /// Gets or sets the timeout (in milliseconds) for synchronous RPC operations.
        /// </summary>
        public int Timeout
        {
            get { return timeout; }
            set { timeout = value; }
        }

        private ITransport transport;
        private bool IsDisposed { get { return transport == null; } }
        private void CheckDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(ToString());
        }
        /// <summary>
        /// Releases any resources associated with the client.
        /// </summary>
        public void Dispose()
        {
            if(transport != null)
            {
                transport.Dispose();
                transport = null;
            }
            if (waitEvent != null)
            {
                waitEvent.Set(); // release any pending thread
                waitEvent.Close();
                waitEvent = null;
            }
        }
        private readonly Type serviceType;
        /// <summary>
        /// Create a new client object.
        /// </summary>
        /// <param name="transport">The transport implementation to use.</param>
        /// <param name="serviceType">The service available for RPC invokation.</param>
        public ProtoClient(ITransport transport, Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException("serviceType");
            if (transport == null) throw new ArgumentNullException("transport");
            if (!serviceType.IsInterface) throw new ArgumentException("Services must be expressed as interfaces", "serviceType");
            if (serviceType.IsGenericTypeDefinition) throw new ArgumentException("Services must be closed types", "serviceType");
            this.transport = transport;
            this.serviceType = serviceType;
        }

        /// <summary>
        /// Begins an RPC invokation asynchrononously.
        /// </summary>
        /// <typeparam name="TRequest">The type representing the request payload.</typeparam>
        /// <typeparam name="TResponse">The type representing the response payload.</typeparam>
        /// <param name="methodName">The name of the method (on the service interface) to invoke.</param>
        /// <param name="request">The request payload.</param>
        /// <param name="callback">The operation to perform when a response is received.</param>
        public void SendRequestAsync<TRequest, TResponse>(
            string methodName,
            TRequest request,
            Action<TResponse> callback)
            where TRequest : class
            where TResponse : class
        {
            SendRequestAsync(ResolveMethod(methodName), request, callback);
        }

        private MethodInfo ResolveMethod(string methodName)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(methodName)) throw new ArgumentNullException("methodName");
            MethodInfo method = serviceType.GetMethod(methodName);
            if (method == null) throw new ArgumentException("Method " + methodName
                 + " not found on service " + serviceType.Name);
            return method;
        }

        /// <summary>
        /// Performs an RPC invokation synchrononously.
        /// </summary>
        /// <typeparam name="TRequest">The type representing the request payload.</typeparam>
        /// <typeparam name="TResponse">The type representing the response payload.</typeparam>
        /// <param name="methodName">The name of the method (on the service interface) to invoke.</param>
        /// <param name="request">The request payload.</param>
        /// <returns>The response payload.</returns>
        public TResponse SendRequestSync<TRequest, TResponse>(
            string methodName,
            TRequest request)
            where TRequest : class
            where TResponse : class
        {
            return SendRequestSync<TRequest, TResponse>(ResolveMethod(methodName), request);
        }

        private ManualResetEvent waitEvent;

        /// <summary>
        /// Performs an RPC invokation synchrononously.
        /// </summary>
        /// <typeparam name="TRequest">The type representing the request payload.</typeparam>
        /// <typeparam name="TResponse">The type representing the response payload.</typeparam>
        /// <param name="method">The method (on the service interface) to invoke.</param>
        /// <param name="request">The request payload.</param>
        /// <returns>The response payload.</returns>
        public TResponse SendRequestSync<TRequest, TResponse>(
            MethodInfo method,
            TRequest request)
            where TRequest : class
            where TResponse : class
        {
            CheckDisposed();
            if (request == null) throw new ArgumentNullException("request", "At a temporary restriction, only non-null requests can be made");
            if (waitEvent == null)
            {
                waitEvent = new ManualResetEvent(false);
            }
            else
            {
                waitEvent.Reset();
            }
            TResponse response = null;
            Exception exception = null;
            SendRequestAsync<TRequest, TResponse>(method, request, delegate(TResponse r, Exception ex)
            {
                response = r;
                exception = ex;
                waitEvent.Set();
            });
            if (!waitEvent.WaitOne(Timeout
#if !SILVERLIGHT
                , false
#endif
                ))
            {
                throw new TimeoutException("Timeout from " + method.Name);
            }
#if !CF2
            Thread.MemoryBarrier();
#endif
            CheckDisposed();
            if (exception != null) throw exception;
            return response;
        }


        /// <summary>
        /// Begins an RPC invokation asynchrononously.
        /// </summary>
        /// <typeparam name="TRequest">The type representing the request payload.</typeparam>
        /// <typeparam name="TResponse">The type representing the response payload.</typeparam>
        /// <param name="method">The method (on the service interface) to invoke.</param>
        /// <param name="request">The request payload.</param>
        /// <param name="callback">The operation to perform when a response is received.</param>
        public void SendRequestAsync<TRequest, TResponse>(
            MethodInfo method,
            TRequest request,
            Action<TResponse> callback)
            where TRequest : class
            where TResponse : class
        {
            SendRequestAsync<TRequest, TResponse>(method, request, delegate(
                TResponse r, Exception ex)
            {
                if (ex != null) OnException(ex);
                else if (callback != null) callback(r);
            });
        }
        internal static string ResolveActionStandard(MethodInfo method)
        {
#if NET_3_0
            OperationContractAttribute oca = (OperationContractAttribute)Attribute.GetCustomAttribute(
                method, typeof(OperationContractAttribute));
            if (oca != null && !string.IsNullOrEmpty(oca.Action)) return oca.Action;
#endif
            return method.Name;
        }

        /// <summary>
        /// Identify the action to use for a given method.
        /// </summary>
        /// <param name="method">The method requested.</param>
        /// <returns>The action to use.</returns>
        protected virtual string ResolveAction(MethodInfo method)
        {
            return ResolveActionStandard(method);
        }

        private void SendRequestAsync<TRequest, TResponse>(
            MethodInfo method,
            TRequest request,
            ServiceRequestCallback<TResponse> callback)
            where TRequest : class
            where TResponse : class
        {
            CheckDisposed();
            if(method == null) throw new ArgumentNullException("method");
            if (method.IsGenericMethod || method.IsGenericMethodDefinition) throw new InvalidOperationException("Cannot process generic method: " + method.Name);
            if (method.DeclaringType != serviceType) throw new ArgumentException(method.Name + " is not defined on service " + serviceType.Name, "method");
            ParameterInfo[] parameters = method.GetParameters();
            if(parameters.Length != 1 || parameters[0].ParameterType != typeof(TRequest)
                || method.ReturnType != typeof(TResponse))
            {
                throw new InvalidOperationException("The service signature for " + serviceType.Name + "." + method.Name + " does not match");
            }


            Action<ServiceRequest> responseCallback = GotResponse<TResponse>;

            ServiceRequest reqWrapper = new ServiceRequest(
                ResolveAction(method), method, request, callback, responseCallback);

            transport.SendRequestAsync(reqWrapper);
        }

        private void GotResponse<T>(ServiceRequest response) where T : class
        {
            ServiceRequestCallback<T> callback;
            if (response != null && !IsDisposed
                && (callback = (ServiceRequestCallback<T>)response.UserState) != null)
            {
                callback((T)response.ResponseObject, response.Exception);
            }
        }

        /// <summary>
        /// Raised when an error occurs processing RPC calls.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ServiceException;

        /// <summary>
        /// Signals that an error occured processing RPC calls.
        /// </summary>
        /// <param name="exception">The error details.</param>
        protected virtual void OnException(Exception exception)
        {
            EventHandler<ExceptionEventArgs> handler = ServiceException;
            if(handler != null) handler(this, new ExceptionEventArgs(exception));
        }

        private delegate void ServiceRequestCallback<T>(T success, Exception failure) where T : class;
    }
    

    
}
