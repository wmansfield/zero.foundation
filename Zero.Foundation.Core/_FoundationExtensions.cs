using System;
using Unity.Resolution;
using Zero.Foundation.System;
using Unity;
using Zero.Foundation.Aspect;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Zero.Foundation
{
    public static class _FoundationExtensions
    {
        public static T GetLastImplementation<T>(this IServiceCollection serviceCollection)
        {
            return (T)serviceCollection
                            .LastOrDefault(d => d.ServiceType == typeof(T))
                            ?.ImplementationInstance;
        }
        public static T Resolve<T>(this IFoundation iFoundation, params ResolverOverride[] overrides)
        {
            return iFoundation.Container.Resolve<T>(overrides);
        }
        public static T Resolve<T>(this IFoundation iFoundation, string name, params ResolverOverride[] overrides)
        {
            return iFoundation.Container.Resolve<T>(name, overrides);
        }
        public static void LogError(this IFoundation iFoundation, Exception ex, string tag = "")
        {
            iFoundation.GetLogger().Write(FoundationUtility.FormatException(ex, tag), Category.Error);
        }
        public static void LogError(this IFoundation iFoundation, string message, string tag = "")
        {
            iFoundation.GetLogger().Write(message, Category.Error);
        }
        public static void LogWarning(this IFoundation iFoundation, string message)
        {
            iFoundation.GetLogger().Write(message, Category.Warning);
        }
        public static void LogTrace(this IFoundation iFoundation, string message)
        {
            iFoundation.GetLogger().Write(message, Category.Trace);
        }

        public static T CachePerFoundation<T>(this IFoundation iFoundation, string callerName, Func<T> retrieveMethod)
        {
            // works because we know internals here, should create instance, and use it, but.. performance is better this way
            AspectCache cache = iFoundation.Resolve<AspectCache>();
            return cache.PerFoundation(callerName, retrieveMethod);
        }
    }
}
