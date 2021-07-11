using System;
using System.Diagnostics;

namespace Zero.Foundation.System.Implementations
{
   /// <summary>
   /// Provides tracing for standard method calls (caller is being traced)
   /// </summary>
   [DebuggerStepThrough]
   public class StandardTracer : ITracer
   {
      public static int DEPTH;

      #region ITracer Members

      public int CurrentDepth
      {
         get
         {
            return DEPTH;
         }
         set
         {
            DEPTH = value;
         }
      }

      public virtual IDisposable StartTrace(string operation)
      {
         this.CurrentDepth++;
         return new DebugTrace(this, 0, Category.Trace, operation);
      }
      public virtual IDisposable StartTrace(string operation, string identifier)
      {
         this.CurrentDepth++;
         return new DebugTrace(this, 0, Category.Trace, operation, identifier);
      }

      #endregion

   }
}
