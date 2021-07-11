using System;
using System.Threading;

namespace Zero.Foundation.Daemons
{
   public interface IDaemonTask : IDisposable
   {
      void Execute(IFoundation iFoundation, CancellationToken token);
      DaemonSynchronizationPolicy SynchronizationPolicy { get; }
      string DaemonName { get; }
   }
}
