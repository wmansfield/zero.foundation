using System;

namespace Zero.Foundation.Daemons.Implementations
{
   public class ServerDaemonHost : IDaemonHost
   {
      public ServerDaemonHost(IFoundation iFoundation)
      {
      }

      #region IDaemonHost Members

      public string DaemonHostName
      {
         get
         {
            return Environment.MachineName;
         }
      }

      #endregion
   }
}
