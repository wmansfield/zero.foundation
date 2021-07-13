using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Unity;
using Zero.Foundation.Plugins;

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
            webPluginLoader.LoadPlugins();
            webPluginLoader.InitializePlugins();
        }

        public override void OnAfterSelfRegisters(IFoundation foundation)
        {
            base.OnAfterSelfRegisters(foundation);

            this.RegisterWebServices(foundation);
        }

        protected virtual void RegisterWebServices(IFoundation foundation)
        {
            // designed for overriding
        }

        
    }
}
