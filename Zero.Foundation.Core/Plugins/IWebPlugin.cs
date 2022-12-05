using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Zero.Foundation.Plugins
{
    public interface IWebPlugin : IPlugin
    {

        bool Construct(IFoundation foundation);

        void Initialize();

        int DesiredInitializationPriority { get; }

        void OnWebPluginInitialized(IWebPlugin plugin);

        void OnAllWebPluginsInitialized(IEnumerable<IWebPlugin> allWebPlugins);
    }
}
