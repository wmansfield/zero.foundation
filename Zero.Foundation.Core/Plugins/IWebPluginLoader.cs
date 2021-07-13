using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Zero.Foundation.Plugins
{
   public interface IWebPluginLoader
   {

      List<IWebPlugin> LoadPlugins();
      List<IWebPlugin> InitializePlugins();

      List<IWebPlugin> GetRegisteredPlugins();
   }
}
