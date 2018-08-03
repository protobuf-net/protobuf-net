using Microsoft.Build.Framework;
using System.Collections.Generic;

namespace ProtoBuf.Build
{
    class TestLogger : ILogger
    {
        public TestLogger()
        {
            this.Errors = new List<BuildErrorEventArgs>();
            this.Warnings = new List<BuildWarningEventArgs>();
        }

        public LoggerVerbosity Verbosity { get; set; }
        public string Parameters { get; set; }

        public void Initialize(IEventSource eventSource)
        {
            eventSource.ErrorRaised += EventSource_ErrorRaised;
            eventSource.WarningRaised += EventSource_WarningRaised;
        }

        public List<BuildErrorEventArgs> Errors { get; }
        public List<BuildWarningEventArgs> Warnings { get; }

        private void EventSource_WarningRaised(object sender, BuildWarningEventArgs e)
        {
            this.Warnings.Add(e);
        }

        private void EventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            this.Errors.Add(e);
        }

        public void Shutdown()
        {
        
        }
    }
}
