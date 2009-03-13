using System;

namespace ProtoBuf
{
    internal static class AsyncUtility
    {
        /// <summary>Simplified calling convention for asynchronous Begin/End operations.</summary>
        /// <typeparam name="T">The type of data returned by the async operation.</typeparam>
        /// <param name="begin">The start (Begin*) of the async operation.</param>
        /// <param name="end">The end (End*) of the async operation.</param>
        /// <param name="callback">The operation to perform once the operation has completed and a value received.</param>
        /// <param name="exceptionHandler">Callback to invoke when an excetption is thrown during the async operation.</param>
        public static void RunAsync<T>(
            AsyncBegin<T> begin,
            AsyncEnd<T> end,
            Action<T> callback,
            Action<Exception> exceptionHandler)
        {
            begin(delegate (IAsyncResult ar)
            {
                T result;
                try
                {
                    result = end(ar);
                }
                catch (Exception ex)
                {
                    if (exceptionHandler != null)
                    {
                        exceptionHandler(ex);
                    }
                    return;
                }
                callback(result);
            }, null);
        }
    }
    /// <summary>Defines the start of a Begin/End async operation pair.</summary>
    /// <typeparam name="T">The type of value returned by the async operation.</typeparam>
    /// <param name="operation">The operation to be performed.</param>
    /// <param name="state">User-state to be passed to the operation.</param>
    /// <returns>A token to the async operation.</returns>
    internal delegate IAsyncResult AsyncBegin<T>(AsyncCallback operation, object state);
    /// <summary>Defines the completion callback of a Begin/End async operation pair.</summary>
    /// <typeparam name="T">The type of value returned by the async operation.</typeparam>
    /// <param name="operation">The async operation token.</param>
    /// <returns>The final value of the async operation.</returns>
    internal delegate T AsyncEnd<T>(IAsyncResult operation);
}
