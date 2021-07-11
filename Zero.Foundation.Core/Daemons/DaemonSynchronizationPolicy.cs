using System;

namespace Zero.Foundation.Daemons
{
   public enum DaemonSynchronizationPolicy
   {
      /// <summary>
      /// The Task has no synchronization concerns. Can run in multiple App Domains on multiple servers.
      /// NOTE: Timing is per instance, not global
      /// </summary>
      None,
      /// <summary>
      /// The task should only run in one App Domain. Synchronization handled by current IDaemonSynchronizationHandler
      /// WARNING: Timing is per instance, not global. [setting a schedule for every 30 minutes with two hosts, will cause the task to run up to two times every 30 minutes]
      /// </summary>
      SingleAppDomain
   }
}
