using System;
using System.Collections.Generic;
using Nuxleus.Asynchronous;
using SilverlightExtended;

namespace Nuxleus.WebService {

    public struct PutObject : ITask {

        Guid m_taskID;
        IRequest m_request;
        IResponse m_response;
        public int Sequence { get; set; }
        public SerializerPerformanceTestAgent Agent { get; set; }

        #region ITask Members

        public IRequest Request {
            get {
                return m_request;
            }
        }

        public IResponse Response {
            get {
                return m_response;
            }
        }

        public Guid TaskID {
            get { return m_taskID; }
        }

        public IEnumerable<IAsync> InvokeAsync() {
            Init();
            Request.RequestUri = new Uri(String.Format("Person_{0}.xml", Sequence), UriKind.Relative);
            return HttpWebServiceRequest<PutObject>.CallWebServiceAsync(this);
        }

        public IResponse Invoke(ITask task) {
            Init();
            throw new NotImplementedException();
        }

        void Init() {
            m_request = new PutObjectRequest();
            m_response = new PutObjectResponse();
            m_taskID = System.Guid.NewGuid();
        }

        #endregion
    }
}
