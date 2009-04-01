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
using System.Collections.Specialized;

namespace ProtoBuf.ServiceModel.Server
{
    /// <summary>
    /// Standalone http server compatible with <seealso cref="ProtoBuf.ServiceModel.Client.HttpBasicTransport"/>.
    /// </summary>
    public class HttpServer : ServerBase, IDisposable
    {
        private Uri uriPrefix;
        private HttpListener listener;

        

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

            Execute(serviceName, actionName, context.Request.Headers, context.Request.InputStream, context.Response.OutputStream, context);            
        }

        /// <summary>
        /// Performs any pre-response operations required.
        /// </summary>
        protected override void OnBeforeWriteResponse(object state)
        {
            base.OnBeforeWriteResponse(state);
            HttpListenerContext context = (HttpListenerContext)state;
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = RpcUtils.HTTP_RPC_MIME_TYPE;
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