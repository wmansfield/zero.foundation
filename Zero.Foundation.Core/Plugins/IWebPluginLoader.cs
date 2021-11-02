using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Zero.Foundation.Plugins
{
   public interface IWebPluginLoader
   {

      void LoadPlugins();
      List<IWebPlugin> InitializePlugins();

      List<IWebPlugin> GetRegisteredPlugins();
   }
}
