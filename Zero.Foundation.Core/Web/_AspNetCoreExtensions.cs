using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Unity;

namespace Zero.Foundation
{
    public static class _AspNetCoreExtensions
    {
        public static IFoundation AddZeroFoundation<TBootStrap>(this IServiceCollection serviceCollection, IUnityContainer unityContainer, TBootStrap bootStrap)
            where TBootStrap : AspNetCoreBootStrap
        {
            return CoreFoundation.Initialize(unityContainer, bootStrap, true);
        }
        public static void UseZeroFoundation(this IApplicationBuilder app, IFoundation foundation)
        {
            if(foundation == null)
            {
                foundation = CoreFoundation.Current;
            }
            foundation.Container.RegisterInstance<IServiceProvider>(app.ApplicationServices);

            AspNetCoreBootStrap aspnetCoreBootstrap = foundation.BootStrap as AspNetCoreBootStrap;
            if(aspnetCoreBootstrap != null)
            {
                aspnetCoreBootstrap.InitializeWebPlugins(foundation);
            }
        }
    }
}
