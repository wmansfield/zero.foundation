using System;

namespace Zero.Foundation.Daemons
{
   public class DaemonExecutionEstimate
   {
      public string Name { get; set; }
      public bool IsScheduled { get; set; }
      public DateTime? NextScheduledStart { get; set; }
      public DateTime? LastExecutedStart { get; set; }
      public DateTime? LastExecutedEnd { get; set; }

      public bool IsRunning { get; set; }
   }
}
