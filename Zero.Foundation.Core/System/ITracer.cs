using System;

namespace Zero.Foundation.System
{
   public interface ITracer
   {
      IDisposable StartTrace(string operation);
      IDisposable StartTrace(string operation, string identifier);

      int CurrentDepth { get; set; }
   }
}
