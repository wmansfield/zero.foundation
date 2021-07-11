using System;
using System.Diagnostics;

namespace Zero.Foundation.System.Implementations
{
   [DebuggerStepThrough]
   public class EmptyTrace : IDisposable
   {
      public EmptyTrace()
      {

      }

      public virtual void Dispose()
      {

      }
   }
}