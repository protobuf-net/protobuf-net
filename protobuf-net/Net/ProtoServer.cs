#if !SILVERLIGHT
using System;
using System.Net;

// just a quick noddy mock-up!!!

namespace ProtoBuf.Net
{
    /// <summary>
    /// Simple server for implement RPC using proto serialization.
    /// </summary>
    public class ProtoServer<TService> : IDisposable
        where TService : class
    {
        private TService service;
        HttpListener server;

        /// <summary>
        /// Stop the server and release any resources
        /// </summary>
        public void Dispose()
        {
            if (server != null)
            {
                try { server.Stop(); }
                catch { }
                server = null;
            }
            service = null;
        }
        /// <summary>
        /// Create a new server for the given base-address,
        /// using the supplied service implementation
        /// </summary>
        /// <param name="baseAddress">Base address for the service.</param>
        /// <param name="service">The service implementation.</param>
        public ProtoServer(string baseAddress, TService service)
        {
            if (string.IsNullOrEmpty(baseAddress)) throw new ArgumentNullException("baseAddress");
            if (service == null) throw new ArgumentNullException("service");
            server = new HttpListener();
            server.Prefixes.Add(baseAddress);
            server.Start();
            this.service = service;

        }
    }
}
#endif