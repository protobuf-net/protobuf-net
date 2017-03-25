using System;
using System.Threading;
using Nuxleus.Performance;
using System.Net;

namespace SilverlightExtended {

    public struct ProtoBufOperationAsyncResult : IAsyncResult {

        HttpWebRequest m_state;
        Boolean m_isCompleted;
        AsyncCallback m_cb;

        public ProtoBufOperationAsyncResult(AsyncCallback cb, HttpWebRequest state) {
            this.m_cb = cb;
            m_isCompleted = false;
            m_state = state;
        }


        public object AsyncState {
            get {
                return m_state;
            }
        }

        public bool CompletedSynchronously {
            get {
                return false;
            }
        }

        public WaitHandle AsyncWaitHandle {
            get {
                throw new InvalidOperationException(
                          "Silverlight should never use this property");
            }
        }

        public bool IsCompleted {
            get {
                return m_isCompleted;
            }
        }

        public void CompleteCall () {
            m_isCompleted = true;
            if (m_cb != null) {
                m_cb(this);
            }
        }

    }
}
