using System;

namespace Zero.Foundation.System.Implementations
{
   public class SwallowExceptionHandlerProvider : IHandleExceptionProvider
   {
      public SwallowExceptionHandlerProvider(ILogger iLogger)
      {
         this.ILogger = iLogger;
      }
      protected virtual ILogger ILogger { get; set; }

      #region IHandleExceptionProvider Members

      public virtual string PolicyName { get; set; }

      public virtual IHandleException CreateHandler()
      {
         return new SwallowExceptionHandler(this.ILogger);
      }

      #endregion
   }
}
