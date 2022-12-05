using System;
using System.Collections.Generic;

namespace Zero.Foundation.Daemons
{
   public interface IDaemonManager
   {
      bool RegisterDaemon(DaemonConfig config, IDaemonTask iDaemonTask, bool autoStart);
      bool UnRegisterDaemon(string instanceName);
      void UnRegisterAllDaemons();
      bool IsDaemonRegistered(string instanceName);
      IDaemonTask GetRegisteredDaemonTask(string instanceName);
      IDaemonTask[] FindRegisteredDaemonTasks(Func<string, bool> predicate);

      ICollection<IDaemonTask> Tasks { get; }
      ICollection<IDaemon> LoadedDaemons { get; }
      IDictionary<string, object> SharedItems { get; set; }

      void StartDaemons(bool includeManualStart);
      void StopDaemons();

      /// <summary>
      /// Immediately starts a daemon, ignoring any synchronization mechanisms
      /// </summary>
      void StartDaemon(string instanceName);
      void StopDaemon(string instanceName);

      void OnAfterBootStrapComplete();

      /// <summary>
      /// Uses synchronization to start daemons
      /// </summary>
      /// <param name="task"></param>
      /// <returns></returns>
      bool TryBeginDaemonTask(IDaemonTask task);
      bool EndDaemonTask(IDaemonTask task);

      List<DaemonExecutionEstimate> GetAllTimerDetails();
      DaemonExecutionEstimate GetTimerDetail(string instanceName);
   }
}
