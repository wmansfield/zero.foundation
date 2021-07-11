using System;

namespace Zero.Foundation.System
{
   public class SelfRegisteringArgs : EventArgs
   {
      public SelfRegisteringArgs(IDynamicallySelfRegister selfRegisterer)
      {
         this.SelfRegisterer = selfRegisterer;
      }

      public IDynamicallySelfRegister SelfRegisterer { get; private set; }
      public bool Cancel { get; set; }
   }
}
