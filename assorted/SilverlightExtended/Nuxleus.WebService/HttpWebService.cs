using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Serialization;
using Nuxleus.Asynchronous;
using System.Windows.Threading;

namespace Nuxleus.WebService {

    public struct HttpRequestSettings {
        public WebServiceType WebServiceType { get; set; }
        public int Timeout { get; set; }
        public bool KeepAlive { get; set; }
        public bool Pipelined { get; set; }
        public string Method { get; set; }
        public string ContentType { get; set; }
    }

    public struct HttpWebServiceRequest<TRequestType> {

        
        static readonly string WEBSERVICE_URI_ENDPOINT = "not-set";
        static XmlSerializer m_xSerializer = new XmlSerializer(typeof(TRequestType));
        static Encoding m_encoding = new UTF8Encoding();

        public static IEnumerable<IAsync> CallWebServiceAsync(ITask task) {
            
            HttpWebRequest request = null;
            try {
                request = (HttpWebRequest)WebRequest.Create(task.Request.RequestUri);
                request.Method = "GET";
                //request.ContentType = settings.ContentType;
            } catch (UriFormatException ufe) {
                //Log.LogInfo<HttpWebServiceRequest<TRequestType>>("Caught UriFormatException on WebRequest.Create: {0}", ufe.Message);
            }
            IRequest webServiceRequest = task.Request;
            byte[] buffer = null;

            int contentLength = buffer.Length;

            foreach (KeyValuePair<string, string> header in webServiceRequest.Headers) {
                //TODO: Figure out how the hell to add new headers to the WebRequest.Headers collection
            }

            Async<Stream> webStream = null;

            try {
                webStream = request.GetRequestStreamAsync();
            } catch (WebException we) {
                //Log.LogInfo<HttpWebServiceRequest<TRequestType>>("Caught WebException on GetResponseAsync: {0}", we.Message);
            }
            if (webStream != null) {
                using (Stream wStream = webStream.Result) {
                    wStream.Write(buffer, 0, contentLength);
                    //Log.LogInfo<HttpWebServiceRequest<TRequestType>>("Sending request for task {0} on thread: {1}", task.TaskID, Thread.CurrentThread.ManagedThreadId);

                    Async<WebResponse> response = null;
                    try {
                        response = request.GetResponseAsync();
                        //Log.LogInfo<HttpWebServiceRequest<TRequestType>>("Received response for task {0} on thread: {1}", task.TaskID, Thread.CurrentThread.ManagedThreadId);
                    } catch (WebException we) {
                        //Log.LogDebug<HttpWebServiceRequest<TRequestType>>("The call to GetResponseAsync for {0} failed with the error: {1}.", task.TaskID, we.Message);
                        //TODO: Add the failed task to a retry queue.
                    }
                    if (response != null) {
                        yield return response;
                        Stream stream = null;
                        try {
                            stream = response.Result.GetResponseStream();
                        } catch (NotSupportedException nse) {
                            //Log.LogDebug<HttpWebServiceRequest<TRequestType>>("Caught NotSupportedException on Result.GetResponseStream(): {0}", nse.Message);
                        } catch (WebException we) {
                            //Log.LogDebug<HttpWebServiceRequest<TRequestType>>("Caught WebException on Result.GetResponseStream(): {0}", we.Message);
                        } catch (Exception e) {
                            //Log.LogDebug<HttpWebServiceRequest<TRequestType>>("Caught Exception on Result.GetResponseStream(): {0}", e.Message);
                        }
                        if (stream != null) {
                            Async<MemoryStream> responseObject = null;
                            try {
                                responseObject = stream.ReadToEndAsync().ExecuteAsync<MemoryStream>();
                            } catch (Exception e) {
                                //TODO: Add the failed task to a retry queue.
                                //Log.LogDebug<HttpWebServiceRequest<TRequestType>>("The call to stream.ReadToEndAsync<String>().ExecuteAsync<String>() for {0} failed with the error: {1}.", task.TaskID, e.Message);
                            }

                            MemoryStream result = null;

                            if (responseObject != null) {
                                yield return responseObject;
                                result = responseObject.Result;
                            }

                            task.Response.Response = result;
                        }
                    } else {
                        //Log.LogDebug<HttpWebServiceRequest<TRequestType>>("Task {0} has failed. Need to add to to new queue to be reprocessed.", task.TaskID);
                        //TODO: Add the failed task to a retry queue.
                    }
                }
            } else {
                //Log.LogDebug<HttpWebServiceRequest<TRequestType>>("Task {0} has failed. Need to add to to new queue to be reprocessed.", task.TaskID);
                //TODO: Add the failed task to a retry queue.
            }
        }
    }
}
