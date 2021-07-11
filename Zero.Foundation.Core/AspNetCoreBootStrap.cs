using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Unity;
using Unity.Lifetime;
using Zero.Foundation.Plugins;
using Zero.Foundation.Plugins.Implementations;

namespace Zero.Foundation
{
   public class AspNetCoreBootStrap : CoreBootStrap
   {
      public AspNetCoreBootStrap()
      {
      }

      public override void OnAfterPluginsLoaded(IFoundation foundation)
      {
         base.OnAfterPluginsLoaded(foundation);

         IWebPluginLoader webPluginLoader = foundation.Resolve<IWebPluginLoader>();
         webPluginLoader.AcquireAndLoadPermittedPlugins();
         List<IWebPlugin> plugins = webPluginLoader.RegisterWebPlugins();

         ApplicationPartManager manager = this.GetServiceFromCollection<ApplicationPartManager>(foundation);

         foreach (var item in plugins)
         {
            Assembly assembly = item.GetType().Assembly;
            var partFactory = ApplicationPartFactory.GetApplicationPartFactory(assembly);
            foreach (var applicationPart in partFactory.GetApplicationParts(assembly))
            {
               manager.ApplicationParts.Add(applicationPart);
            }
         }

      }

      protected void AddToApplicationParts(Assembly assembly)
      {


      }

      public override void OnAfterBootStrapComplete(IFoundation foundation)
      {
         base.OnAfterBootStrapComplete(foundation);
      }

      public override void OnAfterSelfRegisters(IFoundation foundation)
      {
         base.OnAfterSelfRegisters(foundation);

         this.RegisterWebServices(foundation);
      }

      protected virtual void RegisterWebServices(IFoundation foundation)
      {
      }

      protected T GetServiceFromCollection<T>(IFoundation foundation)
      {
         return (T)foundation.Resolve<IServiceCollection>()
             .LastOrDefault(d => d.ServiceType == typeof(T))
             ?.ImplementationInstance;
      }
   }
}
