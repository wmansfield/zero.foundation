using System;

namespace Zero.Foundation.Daemons
{
   public interface IDaemonSynchronizationHandler
   {
      bool TryBeginDaemonTask(IDaemonHost host, IDaemonTask task);
      void ClearAllDaemonTasks(IDaemonHost host);
      bool EndDaemonTask(IDaemonHost host, IDaemonTask task);
   }
}
