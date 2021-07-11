using System;

namespace Zero.Foundation.Daemons
{
   [Serializable]
   public class DaemonConfig
   {
      public string InstanceName { get; set; }
      public bool ContinueOnError { get; set; }
      public int StartDelayMilliSeconds { get; set; }
      /// <summary>
      /// Below 1 for on demand
      /// </summary>
      public int IntervalMilliSeconds { get; set; }

      public string TaskConfiguration { get; set; }
   }
}
