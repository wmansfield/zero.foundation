using System;
using System.Collections.Generic;
using Zero.Foundation.System;

namespace Zero.Foundation
{
   public class CoreBootStrap : IBootStrap
   {
      public CoreBootStrap()
      {
         this.BootStrapChain = new List<IBootStrap>();
      }

      public virtual List<IBootStrap> BootStrapChain { get; set; }

      public virtual void OnFoundationCreated(IFoundation foundation)
      {
         foreach (IBootStrap item in BootStrapChain)
         {
            item.OnFoundationCreated(foundation);
         }
      }
      public virtual void OnAfterPluginsLoaded(IFoundation foundation)
      {
         foreach (IBootStrap item in BootStrapChain)
         {
            item.OnAfterPluginsLoaded(foundation);
         }
      }
      public virtual void OnBeforeSelfRegisters(IFoundation foundation)
      {
         foreach (IBootStrap item in BootStrapChain)
         {
            item.OnBeforeSelfRegisters(foundation);
         }
      }
      public virtual void OnAfterSelfRegisters(IFoundation foundation)
      {
         foreach (IBootStrap item in BootStrapChain)
         {
            item.OnAfterSelfRegisters(foundation);
         }
      }
      public virtual void OnSelfRegister(IFoundation foundation, SelfRegisteringArgs args)
      {
         foreach (IBootStrap item in BootStrapChain)
         {
            item.OnSelfRegister(foundation, args);
            if (args.Cancel)
            {
               break;
            }
         }
      }
      public virtual void OnBootStrapComplete(IFoundation foundation)
      {
         foreach (IBootStrap item in BootStrapChain)
         {
            item.OnBootStrapComplete(foundation);
         }
      }
      public virtual void OnAfterBootStrapComplete(IFoundation foundation)
      {
         foreach (IBootStrap item in BootStrapChain)
         {
            item.OnAfterBootStrapComplete(foundation);
         }
      }

   }
}
