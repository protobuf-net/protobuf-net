
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Diagnostics;
namespace ProtoBuf.ServiceModel.Client
{
    /// <summary>
    /// Performs RPC using basic http POSTs to a web-server.
    /// </summary>
    public class HttpBasicTransport : ITransport
    {

        const string TOKEN_ACTION = "{action}", TOKEN_SERVICE = "{service}";
        /// <summary>Create a new HttpBasicTransport instance.</summary>
        /// <param name="uri">The endpoint for the service. By default, the servic
        /// is assumed to be RESTful, and the action is appended as a route; the
        /// route can be customized by including the "{action}" token in the uri.</param>
        public HttpBasicTransport(string uri)
        {
            if (string.IsNullOrEmpty(uri)) throw new ArgumentNullException("uri");
            if (uri.IndexOf(TOKEN_ACTION) < 0)
            {
                uri = uri + (uri.EndsWith("/") ? "" : "/") + TOKEN_SERVICE + "/" + TOKEN_ACTION;
            }
            this.uri = uri;
        }
        readonly string uri;
        bool disposed;

        /// <summary>
        /// Releases any resources associated with the transport.
        /// </summary>
        public void Dispose() {
            Dispose(true);
        }
        /// <summary>
        /// Releases any resources associated with the transport.
        /// </summary>
        protected virtual void Dispose(bool disposing) {
            disposed = true;
        }
        /// <summary>
        /// Raises an exception if the instance has been disposed.
        /// </summary>
        protected void CheckDisposed()
        {
            if (disposed) throw new ObjectDisposedException(ToString());
        }
        void ITransport.SendRequestAsync(ServiceRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            Uri requestUri = new Uri(uri.Replace(TOKEN_ACTION, request.Action).Replace(TOKEN_SERVICE, request.Service));
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(requestUri);
            httpRequest.Method = "POST";
            httpRequest.Accept = httpRequest.ContentType = RpcUtils.HTTP_RPC_MIME_TYPE;
#if !SILVERLIGHT && !CF2
            httpRequest.AutomaticDecompression = DecompressionMethods.None;
#endif
#if !SILVERLIGHT
            httpRequest.UserAgent = "protobuf-net";
            httpRequest.Headers.Add(RpcUtils.HTTP_RPC_VERSION_HEADER, "0.1");
#endif
            Action<Exception> handler = delegate(Exception ex) {
                request.OnException(ex);
            };
            Action<WebResponse> onResponse = delegate(WebResponse webResponse)
            {
                try
                {
                    using (Stream stream = webResponse.GetResponseStream())
                    {
                        object result = RpcUtils.UnpackArgs(stream, request.Method, request.Args, RpcUtils.IsResponseArgument);
                        request.OnResponse(result);
                    }
                }
                catch (Exception ex) {handler(ex); }
            };
            Action<Stream> onGetRequest = delegate(Stream stream)
            {
                try
                {
                    RpcUtils.PackArgs(stream, request.Method, null, request.Args, RpcUtils.IsRequestArgument);
                    stream.Close();
                    AsyncUtility.RunAsync(httpRequest.BeginGetResponse, httpRequest.EndGetResponse, onResponse, handler);
                }
                catch (Exception ex) {
                    //Trace.WriteLine(ex, GetType() + ":" + request.Method.DeclaringType.Name);
                    handler(ex);
                }
            };
            AsyncUtility.RunAsync(httpRequest.BeginGetRequestStream, httpRequest.EndGetRequestStream, onGetRequest, handler);
        }

        private static bool HasFlag(ParameterInfo property, ParameterAttributes flag)
        {
            return property == null || ((property.Attributes & flag) != flag);
        }
    }
}
