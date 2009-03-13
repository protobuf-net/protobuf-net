using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace ProtoBuf.ServiceModel.Client
{
    /// <summary>
    /// Represents an in-progress request (and response mechanism)
    /// for a basic RPC stack.
    /// </summary>
    public class ServiceRequest
    {
        
        private readonly object userState, requestObject;
        /// <summary>Caller-defined state for this operation.</summary>
        public object UserState { get { return userState; } }
        private readonly Action<ServiceRequest> callback;
        /// <summary>The object graph representing the query request object.</summary>
        public object RequestObject { get { return requestObject; } }
        private object responseObject;
        /// <summary>The object graph representing the server's response.</summary>
        public object ResponseObject {
            get { return responseObject; }
            private set { responseObject = value; }
        }
        private Exception exception;
        /// <summary> Descripbes any exception raised by the transport.</summary>
        public Exception Exception {
            get { return exception; }
            private set { exception = value; }
        }
        /// <summary>Called by transports; signals that the operation failed.</summary>
        /// <param name="exception">The details of the failure.</param>
        public void OnException(Exception exception) { RaiseCallback(null, exception); }
        /// <summary>Called by transports; signals that the operation succeeded.</summary>
        /// <param name="responseObject">The server's response the the request.</param>
        public void OnResponse(object responseObject) { RaiseCallback(responseObject, null); }
        private void RaiseCallback(object responseObject, Exception exception)
        {
            Exception = exception;
            ResponseObject = responseObject;
            callback(this);
        }
        private readonly string action;
        /// <summary>The contract-based name of the operation to perform.</summary>
        public string Action { get { return action; } }
        private readonly MethodInfo method;
        /// <summary>Provides reflection access to the contract member representing the operation.</summary>
        public MethodInfo Method { get { return method; } }
        //public int RequestNumber { get; private set; }
        /// <summary>Create a new service request.</summary>
        /// <param name="action">The contract-based name of the operation to perform.</param>
        /// <param name="method">Provides reflection access to the contract member representing the operation.</param>
        /// <param name="requestObject">The object graph representing the query request object.</param>
        /// <param name="userState">Caller-defined state for this operation.</param>
        /// <param name="callback">The operation to perform when this request has completed.</param>
        public ServiceRequest(
            string action, MethodInfo method,
            object requestObject, object userState,
            Action<ServiceRequest> callback)
        {
            this.userState = userState;
            this.requestObject = requestObject;
            this.action = action;
            this.method = method;
            this.callback = callback;
        }

    }      

}
