using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Zero.Foundation.Plugins
{
   public interface IWebPluginLoader
   {
      List<IWebPlugin> RegisterWebPlugins();

      List<IWebPlugin> AcquireAndLoadPermittedPlugins();

      List<IWebPlugin> GetRegisteredPlugins();
   }
}
