using System;

namespace ProtoBuf.ServiceModel.Client
{
    /// <summary>
    /// Provides the underlying transport for a family of RPC operations.
    /// </summary>
    public interface ITransport : IDisposable
    {
        /// <summary>
        /// Begins an async operation over the transport.
        /// </summary>
        /// <param name="request">The operation to perform (includes the facility
        /// to provide a response for the operation).</param>
        void SendRequestAsync(ServiceRequest request);
    }
}
