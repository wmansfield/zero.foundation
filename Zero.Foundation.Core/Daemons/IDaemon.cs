using System;

namespace Zero.Foundation.Daemons
{
   public interface IDaemon
   {
      string InstanceName { get; }
      DaemonConfig Config { get; }
      bool IsExecuting { get; }
      bool IsOnDemand { get; }
      int IntervalMilliSeconds { get; set; }
      DateTime? LastExecuteStartTime { get; }
      DateTime? LastExecuteEndTime { get; }

   }
}
