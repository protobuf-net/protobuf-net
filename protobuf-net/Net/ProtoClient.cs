#if !SILVERLIGHT
using System;
using System.IO;
using System.Net;

// just a quick noddy mock-up!!!

namespace ProtoBuf.Net
{

    /// <summary>
    /// Simple client for invoking RPC calls using proto serialization.
    /// </summary>
    public class ProtoClient : IDisposable
    {
        private WebClient client;
        /// <summary>
        /// Create a new client for the given base-address.
        /// </summary>
        /// <param name="baseAddress">The base-address of the remote service.</param>
        public ProtoClient(string baseAddress)
        {
            client = new WebClient();
            client.BaseAddress = baseAddress;
        }
        /// <summary>
        /// The remote address for the service.
        /// </summary>
        public Uri BaseAddress { get; private set; }

        /// <summary>
        /// Releases any resources.
        /// </summary>
        public void Dispose()
        {
            if (client != null)
            {
                client.Dispose();
                client = null;
            }
        }

        /// <summary>
        /// Verify that the instance is still available for use.
        /// </summary>
        /// <exception cref="ObjectDisposedException">If disposed.</exception>
        protected void CheckDisposed()
        {
            if (client == null) throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Invokes the named operation on the remote server
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="operation">The operation to invoke.</param>
        /// <param name="request">The request object (the values for the query).</param>
        /// <returns>The response object returned from the remote server.</returns>
        public TResponse Send<TRequest, TResponse>(string operation, TRequest request)
            where TRequest : class, new()
            where TResponse : class, new()
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(operation)) throw new ArgumentNullException("operation");
            if (request == null) throw new ArgumentNullException("request");

            //TODO: cleanse operation

            if (!Serializer.IsEntityType(typeof(TRequest))
                || !Serializer.IsEntityType(typeof(TResponse)))
            {
                throw new InvalidOperationException("Both the request and response must be proto-serializable.");
            }
            byte[] buffer;
            using(MemoryStream ms = new MemoryStream()) {
                Serializer.Serialize(ms, request);
                buffer = ms.ToArray();
            }
            buffer = client.UploadData(operation, buffer);
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                return Serializer.Deserialize<TResponse>(ms);
            }
        }
    }
}
#endif