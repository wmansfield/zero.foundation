using System;
using System.Diagnostics;

namespace Zero.Foundation.System.Implementations
{
    /// <summary>
    /// Provides tracing for standard method calls (caller is being traced)
    /// </summary>
    [DebuggerStepThrough]
    public class EmptyTracer : ITracer
    {
        private EmptyTrace _emptyTrace = new EmptyTrace();

        #region ITracer Members

        public int CurrentDepth { get; set; }

        public virtual IDisposable StartTrace(string operation)
        {
            return _emptyTrace;
        }
        public virtual IDisposable StartTrace(string operation, string identifier)
        {
            return _emptyTrace;
        }

        #endregion

    }
}
