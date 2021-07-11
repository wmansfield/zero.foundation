using System;
using System.Diagnostics;

namespace Zero.Foundation.System.Implementations
{
   /// <summary>
   /// Provides tracing for any caller that is invoked with a method wrapper. Caller or Caller is being traced.
   /// </summary>
   [DebuggerStepThrough]
   public partial class WrappedMethodTracer : ITracer
   {
      #region ITracer Members

      public int CurrentDepth { get; set; }

      public virtual IDisposable StartTrace(string operation)
      {
         return new DebugTrace(this, 2, Category.Trace, operation);
      }
      public virtual IDisposable StartTrace(string operation, string identifier)
      {
         return new DebugTrace(this, 2, Category.Trace, operation, identifier);
      }


      #endregion

   }
}
