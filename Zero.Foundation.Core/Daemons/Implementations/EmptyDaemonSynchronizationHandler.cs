using System;

namespace Zero.Foundation.Daemons.Implementations
{
   public class EmptyDaemonSynchronizationHandler : IDaemonSynchronizationHandler
   {
      public EmptyDaemonSynchronizationHandler(IFoundation iFoundation)
      {

      }

      #region IDaemonSynchronizationHandler Members

      public bool TryBeginDaemonTask(IDaemonHost host, IDaemonTask task)
      {
         return true;
      }
      public void ClearAllDaemonTasks(IDaemonHost host)
      {
      }
      public bool EndDaemonTask(IDaemonHost host, IDaemonTask task)
      {
         return true;
      }

      #endregion
   }
}
