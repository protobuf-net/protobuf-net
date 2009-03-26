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
    /// <typeparam name="TService">The service contract that the client represents.</typeparam>
    public class ProtoClient<TService> : IDisposable where TService : class
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
        /// <summary>
        /// Gets the transport mechanism associated with the client.
        /// </summary>
        public ITransport Transport { get { return transport; } }
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
        
        /// <summary>
        /// Create a new client object.
        /// </summary>
        /// <param name="transport">The transport implementation to use.</param>
        public ProtoClient(ITransport transport)
        {
            if (transport == null) throw new ArgumentNullException("transport");
            if (!typeof(TService).IsInterface) throw new ArgumentException("Services must be expressed as interfaces");
            this.transport = transport;
        }

        /// <summary>
        /// Begins an RPC invokation asynchrononously.
        /// </summary>
        /// <param name="methodName">The name of the method (on the service interface) to invoke.</param>
        /// <param name="args">The request payload.</param>
        /// <param name="callback">The operation to perform when a response is received.</param>
        public void InvokeAsync(
            string methodName,
            Action<AsyncResult> callback,
            params object[] args)
        {
            InvokeAsync(ResolveMethod(methodName), callback, args);
        }

        private MethodInfo ResolveMethod(string methodName)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(methodName)) throw new ArgumentNullException("methodName");
            MethodInfo method = typeof(TService).GetMethod(methodName);
            if (method == null) throw new ArgumentException("Method " + methodName
                 + " not found on service " + typeof(TService).Name);
            return method;
        }

        /// <summary>
        /// Performs an RPC invokation synchrononously.
        /// </summary>
        /// <param name="methodName">The name of the method (on the service interface) to invoke.</param>
        /// <param name="args">The request payload.</param>
        /// <returns>The response payload.</returns>
        public object Invoke (
            string methodName,
            params object[] args)
        {
            return Invoke(ResolveMethod(methodName), args);
        }

        private ManualResetEvent waitEvent = new ManualResetEvent(false);

        /// <summary>
        /// Performs an RPC invokation synchrononously.
        /// </summary>
        /// <param name="method">The method (on the service interface) to invoke.</param>
        /// <param name="args">The request payload.</param>
        /// <returns>The response payload.</returns>
        public object Invoke(
            MethodInfo method,
            params object[] args)
        {
            CheckDisposed();
            if (args == null) throw new ArgumentNullException("args");
            
            waitEvent.Reset();
            AsyncResult result = null;
            SendRequestAsync(method, delegate(AsyncResult ar)
            {
                result = ar;
                waitEvent.Set();
            }, args);
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
            return result();
        }


        /// <summary>
        /// Begins an RPC invokation asynchrononously.
        /// </summary>
        /// <param name="method">The method (on the service interface) to invoke.</param>
        /// <param name="args">The request payload.</param>
        /// <param name="callback">The operation to perform when a response is received.</param>
        public void InvokeAsync (
            MethodInfo method,
            Action<AsyncResult> callback,
            params object[] args)
        {
            SendRequestAsync(method, callback, args);
        }

        /// <summary>
        /// Identify the action to use for a given method.
        /// </summary>
        /// <param name="method">The method requested.</param>
        /// <returns>The action to use.</returns>
        protected virtual string ResolveAction(MethodInfo method)
        {
            return RpcUtils.GetActionName(method);
        }

        /// <summary>
        /// Identify the service to use for a given method.
        /// </summary>
        /// <param name="serviceType">The service requested.</param>
        /// <returns>The service to use.</returns>
        protected virtual string ResolveService(Type serviceType)
        {
            return RpcUtils.GetServiceName(serviceType);
        }

        private void SendRequestAsync(
            MethodInfo method,
            Action<AsyncResult> callback,
            object[] args)
        {
            CheckDisposed();
            if(args == null) throw new ArgumentNullException("args");
            if(method == null) throw new ArgumentNullException("method");
            if (method.IsGenericMethod || method.IsGenericMethodDefinition) throw new InvalidOperationException("Cannot process generic method: " + method.Name);
            if (method.DeclaringType != typeof(TService)) throw new ArgumentException(method.Name + " is not defined on service " + typeof(TService).Name, "method");
            ParameterInfo[] parameters = method.GetParameters();
            if(parameters.Length != args.Length)
            {
                throw new InvalidOperationException("Parameter mismatch calling " + method.Name);
            }

            ServiceRequest reqWrapper = new ServiceRequest(
                ResolveService(typeof(TService)), ResolveAction(method), method, args, callback, callback);

            transport.SendRequestAsync(reqWrapper);
        }

        /*
        private void GotResponse(ServiceRequest response)
        {
            ServiceRequestCallback callback;
            if (response != null && !IsDisposed
                && (callback = (ServiceRequestCallback)response.UserState) != null)
            {
                callback(response.ResponseObject, response.Exception);
            }
        }
         * */

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
    }
    

    
}
