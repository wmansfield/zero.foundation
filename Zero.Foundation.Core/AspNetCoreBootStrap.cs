using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Unity;
using Zero.Foundation.Plugins;

namespace Zero.Foundation
{
    public class AspNetCoreBootStrap : CoreBootStrap
    {
        public AspNetCoreBootStrap(IUnityContainer container, IWebHostEnvironment webHostEnvironment, IServiceCollection services, IConfiguration configuration)
        {
            this.BindToAspNetCore(container, webHostEnvironment, services, configuration);
        }

        
        public override void OnAfterPluginsLoaded(IFoundation foundation)
        {
            base.OnAfterPluginsLoaded(foundation);

            this.OnBeforeLoadWebPlugins(foundation);
            this.LoadWebPlugins(foundation);
            this.OnAfterLoadWebPlugins(foundation);
        }

        protected virtual void BindToAspNetCore(IUnityContainer container, IWebHostEnvironment webHostEnvironment, IServiceCollection services, IConfiguration configuration)
        {
            container.RegisterInstance<IConfiguration>(configuration);
            container.RegisterInstance<IServiceCollection>(services);
            container.RegisterInstance<IWebHostEnvironment>(webHostEnvironment);

            services.AddMvcCore();
        }

        protected virtual void LoadWebPlugins(IFoundation foundation)
        {
            IWebPluginLoader webPluginLoader = foundation.Resolve<IWebPluginLoader>();
            webPluginLoader.LoadPlugins();
        }
        protected virtual void OnBeforeLoadWebPlugins(IFoundation foundation)
        {
            // lifecycle
        }
        protected virtual void OnAfterLoadWebPlugins(IFoundation foundation)
        {
            // lifecycle
        }
        public virtual void InitializeWebPlugins(IFoundation foundation)
        {
            IWebPluginLoader webPluginLoader = foundation.Resolve<IWebPluginLoader>();
            webPluginLoader.InitializePlugins();
        }
       
    }
}
