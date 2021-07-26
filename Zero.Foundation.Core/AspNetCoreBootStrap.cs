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
       

        public override void OnAfterBootStrapComplete(IFoundation foundation)
        {
            base.OnAfterBootStrapComplete(foundation);
        }

        protected virtual void BindToAspNetCore(IUnityContainer container,IWebHostEnvironment webHostEnvironment, IServiceCollection services, IConfiguration configuration)
        {
            container.RegisterInstance<IConfiguration>(configuration);
            container.RegisterInstance<IServiceCollection>(services);
            container.RegisterInstance<IWebHostEnvironment>(webHostEnvironment);

            services.AddMvcCore();
        }
    }
}
