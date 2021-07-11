using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Zero.Foundation.Plugins
{
   public interface IWebPlugin : IPlugin
   {
      bool WebInitialize(IFoundation foundation, IDictionary<string, string> pluginConfig);
     
      void Register();

      int DesiredRegistrationPriority { get; }

      void OnWebPluginRegistered(IWebPlugin plugin);
      void OnWebPluginUnRegistered(IWebPlugin iWebPlugin);

      void OnAfterWebPluginsRegistered(IEnumerable<IWebPlugin> allWebPlugins);
      void OnAfterWebPluginsUnRegistered(IWebPlugin[] iWebPlugin);
   }
}
