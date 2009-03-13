using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoBuf.ServiceModel
{
    /// <summary>
    /// Represents an exception raised through an event.
    /// </summary>
    public sealed class ExceptionEventArgs : EventArgs
    {
        private readonly Exception exception;
        /// <summary>
        /// The exception represented by the event.
        /// </summary>
        public Exception Exception { get { return exception; } }
        /// <summary>
        /// Creates a new instance of ExceptionEventArgs for the gievn exception.
        /// </summary>
        /// <param name="exception"></param>
        public ExceptionEventArgs(Exception exception)
        {
            this.exception = exception;
        }
    }
}
