using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using Nuxleus.MetaData;

namespace Nuxleus.Performance {

    [DefaultValue(UnitPrecision.NANOSECONDS)]
    public enum UnitPrecision {
        [Label("seconds")]
        SECONDS = 1,
        [Label("milliseconds")]
        MILLISECONDS = 1000,
        [Label("microseconds")]
        MICROSECONDS = 1000000,
        [Label("nanoseconds")]
        NANOSECONDS = 1000000000
    }

    /// <summary>
    /// Wraps a Stopwatch object with methods and properties that automate the process of creating, 
    /// starting, monitoring, and stopping the Stopwatch and logging related data to a PerformanceLog.
    /// </summary>
    public struct PerformanceTimer : IDisposable {

        static object obj = new object();
        public delegate void CodeBlock();
        static long m_startTicks;
        //static readonly Stopwatch m_stopwatch = new Stopwatch();
        static readonly DateTime m_dateTime = new DateTime();

        [DefaultValue(UnitPrecision.NANOSECONDS)]
        public static UnitPrecision UnitPrecision { get; set; }

        static Stack<long> m_setMarkerStack = new Stack<long>();
        static Stack<long> m_releaseMarkerStack = new Stack<long>();
        //static long m_freq = Stopwatch.Frequency;

        static PerformanceTimer() {
            m_startTicks = m_dateTime.Ticks;
            if (UnitPrecision == 0) {
                //TODO: Read the DefaultValue attribute of the UnitPrecision property and initialize it with that value.
                UnitPrecision = UnitPrecision.NANOSECONDS;
            }
        }

        /// <summary>
        /// Provides a way to inject code into a timed container using a lambda expression.  
        /// When the invocation of the lambda expression has completed, use 
        /// [name of perf timer object].Duration to get the total duration of the scoped code block. 
        /// 
        /// Scope CodeBlock's can be nested one with another at an infinite depth.
        /// </summary>
        public CodeBlock Scope {
            set {
                // Not 100% sure this lock is necessary (at least from a serial perspective), 
                // but better safe than gain inaccurate readings when Duration is read.
                lock (obj) {
                    this.SetMarker();
                    value.Invoke();
                    this.ReleaseMarker();
                }
            }
        }

        /// <summary>
        /// Provides an automated wrapper for timing an operation and logging a message to a given PerformanceLog
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="log">The <typeparamref name="PerformanceLog"/> the message should be written to.</param>
        /// <param name="code">The code to be invoked and timed.</param>
        /// <returns><typeparamref name="PerformanceLog"/> which is the same <typeparamref name="PerformanceLog"/> passed into the method.</returns>
        public PerformanceLog LogScope(String message, PerformanceLog log, CodeBlock code) {
            lock (obj) {
                this.SetMarker();
                code.Invoke();
                this.ReleaseMarker();
                log.LogData(message, this.Duration);
            }
            return log;
        }

        /// <summary>
        /// Resets the timer
        /// </summary>
        //void Reset() {
        //    m_stopwatch.Reset();
        //}

        /// <summary>
        /// Set a performance marker
        /// </summary>
        void SetMarker() {
            // TODO: I've debated whether I should check to see if the Stopwatch is running, and start it if it's not,
            // in the static constructor or in the first call to SetMarker and it seems to me that using SetMarker
            // provides a greater level of accuracy at the Scope level given the marker is set just before the 
            // CodeBlock expression is invoked and therefore immediately before any processing related to the CodeBlock
            // begins.  However, this should be thoroughly tested before making a final determination.
            m_setMarkerStack.Push(m_dateTime.Ticks);
        }

        /// <summary>
        /// Release a performance marker
        /// </summary>
        void ReleaseMarker() {
            m_releaseMarkerStack.Push(m_dateTime.Ticks);
        }

        /// <summary>
        /// Returns the duration of time between the last SetMarker and ReleaseMarker added to the stack
        /// </summary>         
        public double Duration {
            get {
                return GetDuration(m_setMarkerStack.Pop(), m_releaseMarkerStack.Pop());
            }
        }

        /// <summary>
        /// Returns the total elapsed time
        /// </summary>         
        public double Elapsed {
            get {
                return GetDuration(m_dateTime.Ticks, m_startTicks);
            }
        }

        double GetDuration(long startTime, long stopTime) {
            return ((double)(stopTime - startTime) * (Double)UnitPrecision);
        }

        #region IDisposable Members

        public void Dispose() {

        }

        #endregion
    }
}