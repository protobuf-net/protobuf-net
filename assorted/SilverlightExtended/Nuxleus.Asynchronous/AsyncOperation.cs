using System;
using System.ComponentModel;
using System.Threading;
using Nuxleus.Performance;


namespace SilverlightExtended {

    class ProgressData {

        internal long TotalTasksToComplete = 0L;
        internal long TotalTasksCompleted = 0L;

        // Methods
        internal void Reset() {
            this.TotalTasksToComplete = 0L;
            this.TotalTasksCompleted = 0L;
        }
    }

    public class AsyncProtoBufOperation {
        SendOrPostCallback reportOperationProgressChanged;

        public event OperationProgressChangedEventHandler OperationProgressChanged;

        ProgressData m_progress;

        public AsyncProtoBufOperation(int totalTasksToComplete) {
            m_progress = new ProgressData { TotalTasksToComplete = totalTasksToComplete };
            reportOperationProgressChanged = new SendOrPostCallback(ReportOperationProgressChanged);
        }

        //public IAsyncResult BeginOperation(SerializerPerformanceTestAgent agent, AsyncOperation asyncOp, AsyncCallback callback, PerformanceLog perfLog) {
            
        //    //IAsyncResult result = new ProtoBufOperationAsyncResult(callback, perfLog);

        //    //RunSerializationTest(m_progress.TotalTasksCompleted, agent);
            
        //    m_progress.TotalTasksCompleted += 1;

            
        //    //if (OperationProgressChanged != null) {
        //    //    OperationProgressChanged(this, args);
        //    //}
        //    if (callback != null) {
        //        callback(result);
        //    }

        //    return result;
        //}

        private void ReportOperationProgressChanged(object arg) {
            OnOperationProgressChanged((OperationProgressChangedEventArgs)arg);
        }


        private void PostProgressChanged(AsyncOperation asyncOp, PerformanceLog perfLog) {
            long percentageComplete = (m_progress.TotalTasksCompleted / m_progress.TotalTasksToComplete) * 100;
            asyncOp.Post(reportOperationProgressChanged, new OperationProgressChangedEventArgs((int)percentageComplete, perfLog, m_progress.TotalTasksCompleted, m_progress.TotalTasksToComplete));
        }

        protected virtual void OnOperationProgressChanged(OperationProgressChangedEventArgs e) {
            if (OperationProgressChanged != null) {
                OperationProgressChanged(this, e);
            }
        }

    }
}
