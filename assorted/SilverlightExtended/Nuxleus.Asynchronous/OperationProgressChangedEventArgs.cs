using System.ComponentModel;
using Nuxleus.Performance;

namespace SilverlightExtended {

    public class OperationProgressChangedEventArgs : ProgressChangedEventArgs {

        long m_totalTasksCompleted;
        long m_totalTasksToComplete;
        public PerformanceLog PerformanceLog { get; set; }


        public OperationProgressChangedEventArgs(int progressPercentage, object userState, long totalTasksCompleted, long totalTasksToComplete)
            : base(progressPercentage, userState) {
            m_totalTasksCompleted = totalTasksCompleted;
            m_totalTasksToComplete = totalTasksToComplete;
            PerformanceLog = (PerformanceLog)userState;
        }


        public long TotalTasksToComplete {
            get {
                return m_totalTasksToComplete;
            }
        }

        public long TotalTasksCompleted {
            get {
                return m_totalTasksCompleted;
            }
        }
    }
}

