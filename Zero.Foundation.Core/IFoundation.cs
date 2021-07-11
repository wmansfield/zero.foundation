using Zero.Foundation.Aspect;
using Zero.Foundation.Daemons;
using Zero.Foundation.System;
using Zero.Foundation.Plugins;
using Unity;

namespace Zero.Foundation
{
    public interface IFoundation
    {
        void Start(IUnityContainer container, IBootStrap bootStrap);
        void Stop();
        IUnityContainer Container { get; }
        ILogger GetLogger();
        ITracer GetTracer();
        IAspectCoordinator GetAspectCoordinator();
        IPluginManager GetPluginManager();
        IDaemonManager GetDaemonManager();

        T SafeResolve<T>();
        T SafeResolve<T>(string name);
    }
}
