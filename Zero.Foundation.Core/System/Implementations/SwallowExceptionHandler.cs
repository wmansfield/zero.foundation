using System;
using Zero.Foundation.System;

namespace Zero.Foundation.System.Implementations
{
   public class SwallowExceptionHandler : IHandleException
   {
      public SwallowExceptionHandler(ILogger iLogger)
      {
         this.ILogger = iLogger;
      }

      public virtual string PolicyName { get; set; }
      protected virtual ILogger ILogger { get; set; }

      public virtual bool HandleException(Exception ex, out bool rethrowCurrent, out Exception replacedException)
      {
         this.LogException(ex);

         rethrowCurrent = false;
         replacedException = null;
         return true;
      }

      protected virtual void LogException(Exception ex)
      {
         string message = FormatException(ex);
         this.ILogger.Write(message, Category.Error);
      }
      protected virtual string FormatException(Exception ex)
      {
         return FoundationUtility.FormatException(ex, "Swallowed");
      }

   }
}
