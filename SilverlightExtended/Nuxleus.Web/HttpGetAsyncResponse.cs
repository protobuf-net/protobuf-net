using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Nuxleus.Asynchronous;
using Nuxleus.Performance;

namespace Nuxleus.Web {

    public struct HttpGetAsyncResponse {

        static Dictionary<int, MemoryStream> m_responseStreamDictionary = new Dictionary<int, MemoryStream>();
        TextWriter m_logWriter;
        string[] m_httpRequestArray;
        int m_httpRequestArrayLength;
        bool m_DEBUG;
        Stopwatch m_stopwatch;
        List<long> m_elapsedTimeList;
        bool m_runSynchronously;
        bool m_pipelineRequests;

        public HttpGetAsyncResponse ( params string[] httpRequestArray )
            : this(Console.Out, false, true, false, httpRequestArray) { }

        public HttpGetAsyncResponse ( TextWriter logWriter, params string[] httpRequestArray )
            : this(logWriter, false, true, false, httpRequestArray) { }

        public HttpGetAsyncResponse ( TextWriter logWriter, bool debug, params string[] httpRequestArray )
            : this(logWriter, debug, true, false, httpRequestArray) { }

        public HttpGetAsyncResponse ( TextWriter logWriter, bool debug, bool pipelineRequests, params string[] httpRequestArray )
            : this(logWriter, debug, pipelineRequests, false, httpRequestArray) { }

        public HttpGetAsyncResponse ( TextWriter logWriter, bool debug, bool pipelineRequests, bool runSynchronously, params string[] httpRequestArray ) {
            m_logWriter = logWriter;
            m_httpRequestArray = httpRequestArray;
            m_httpRequestArrayLength = httpRequestArray.Length;
            m_DEBUG = debug;
            m_stopwatch = new Stopwatch();
            m_elapsedTimeList = new List<long>();
            m_pipelineRequests = pipelineRequests;
            m_runSynchronously = runSynchronously;
        }

        public IAsyncResult BeginProcessRequests ( AsyncCallback callback, object extraData ) {
            if (DEBUG) {
                m_logWriter.WriteLine("Beginning async HTTP request process...");
            }
            NuxleusAsyncResult nuxleusAsyncResult = new NuxleusAsyncResult(callback, extraData);
            ProcessRequests(nuxleusAsyncResult);
            return nuxleusAsyncResult;
        }

        public Dictionary<int, MemoryStream> ResponseStreamDictionary { get { return m_responseStreamDictionary; } set { m_responseStreamDictionary = value; } }
        public bool DEBUG { get { return m_DEBUG; } set { m_DEBUG = value; } }
        public Stopwatch Stopwatch { get { return m_stopwatch; } }
        public List<long> ElapsedTimeList { get { return m_elapsedTimeList; } }

        private void ProcessRequests ( NuxleusAsyncResult asyncResult ) {

            int queryArrayLength = m_httpRequestArrayLength;
            TextWriter logWriter = m_logWriter;
            bool DEBUG = m_DEBUG;
            List<long> elaspedTimeList = m_elapsedTimeList;

            Encoding encoding = Encoding.UTF8;

            foreach (string r in m_httpRequestArray) {
                Stopwatch stopwatch = new Stopwatch();
                if (DEBUG) {
                    stopwatch.Start();
                }
                string requestString = r;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(requestString));

                    new AsyncHttpRequest(request, logWriter, DEBUG, stopwatch,
                            delegate( Stream stream, Stopwatch myStopwatch ) {
                                //logWriter.WriteLine("The stopwatch objects are the same: {0}", stopwatch.Equals(myStopwatch));
                                myStopwatch.Stop();
                                long elapsedTime = stopwatch.ElapsedMilliseconds;
                                elaspedTimeList.Add(elapsedTime);
                                myStopwatch.Reset();
                                if (DEBUG) {
                                    logWriter.WriteLine("Current thread id: {0} for current request: {1}", Thread.CurrentThread.ManagedThreadId, requestString);
                                }

                                try {
                                    using (stream) {
                                        StreamReader reader = new StreamReader(stream);
                                        m_responseStreamDictionary.Add(requestString.GetHashCode(), new MemoryStream(encoding.GetBytes(reader.ReadToEnd())));
                                    }
                                } catch (Exception e) {
                                    Console.WriteLine("Exception: {0}", e.Message);
                                }

                                if (m_responseStreamDictionary.Count == queryArrayLength) {
                                    if (DEBUG) {
                                        logWriter.WriteLine("Elapsed time of this request:\t {0}ms", elapsedTime);
                                        logWriter.WriteLine("Completing call.");
                                    }
                                    asyncResult.CompleteCall();
                                } else {
                                    if (DEBUG) {
                                        logWriter.WriteLine("Elapsed time of this request:\t {0}ms", elapsedTime);
                                        logWriter.WriteLine("Continuing process...");
                                    }
                                }

                            });
               
            }
        }
    }

    public delegate void HttpResponseStream ( Stream responseStream, Stopwatch stopwatch );

    public struct AsyncHttpRequest {

        HttpWebRequest m_request;
        HttpResponseStream m_responseStream;
        TextWriter m_logWriter;
        bool m_DEBUG;
        Stopwatch m_stopwatch;

        public bool DEBUG { get { return m_DEBUG; } set { m_DEBUG = value; } }

        public AsyncHttpRequest ( HttpWebRequest request, TextWriter logWriter, bool debug, Stopwatch stopwatch, HttpResponseStream responseStreamCallback ) {
            m_request = request;
            m_responseStream = responseStreamCallback;
            m_logWriter = logWriter;
            m_DEBUG = debug;
            m_stopwatch = stopwatch;
            if (DEBUG) {
                m_stopwatch.Start();
            }
            if (DEBUG) {
                m_logWriter.WriteLine("Beginning call to {0} on thread: {1}", request.RequestUri, Thread.CurrentThread.ManagedThreadId);
            }
            request.BeginGetResponse(RequestIsComplete, request);
        }

        private void RequestIsComplete ( IAsyncResult asyncResult ) {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
            HttpWebResponse response = (HttpWebResponse)m_request.EndGetResponse(asyncResult);

            m_responseStream(response.GetResponseStream(), m_stopwatch);
            if (DEBUG) {
                m_logWriter.WriteLine("Ending call to {0}", request.RequestUri);
            }
        }
    }
}
