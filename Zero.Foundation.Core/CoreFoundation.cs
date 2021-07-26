using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity;
using Unity.Lifetime;
using Zero.Foundation.Aspect;
using Zero.Foundation.Aspect.Implementations;
using Zero.Foundation.Daemons;
using Zero.Foundation.Daemons.Implementations;
using Zero.Foundation.Plugins;
using Zero.Foundation.Plugins.Implementations;
using Zero.Foundation.System;
using Zero.Foundation.System.Implementations;


namespace Zero.Foundation
{
   public class CoreFoundation : IFoundation
   {
      #region Statics

      /// <summary>
      /// Initializes the CoreFoundation. The preferred entry point into the API.
      /// </summary>
      /// <param name="forceRecreate"></param>
      /// <returns></returns>
      [MethodImpl(MethodImplOptions.Synchronized)]
      public static IFoundation Initialize(IUnityContainer container, IBootStrap bootStrap, bool forceRecreate)
      {
         if (forceRecreate || (Single<IFoundation>.Instance == null))
         {
            CoreFoundation foundation = new CoreFoundation(bootStrap);
            Single<IFoundation>.Instance = foundation;
            foundation.Start(container, foundation.BootStrap);
         }
         return Single<IFoundation>.Instance;
      }

      /// <summary>
      /// Gets the currently running foundation.
      /// </summary>
      public static IFoundation Current
      {
         get
         {
            if (Single<IFoundation>.Instance == null)
            {
               CoreFoundation.Initialize(null, null, false);
            }
            return Single<IFoundation>.Instance;
         }
      }

      /// <summary>
      /// Destroys the CoreFoundation. The preferred exit point from the API.
      /// </summary>
      public static void Destroy()
      {
         IFoundation foundation = Single<IFoundation>.Instance;
         if (foundation != null)
         {
            foundation.Stop();
         }
         Single<IFoundation>.Instance = null;
      }

      #endregion

      #region Constructors

      protected CoreFoundation(IBootStrap bootStrap)
      {
         this.BootStrap = bootStrap;
      }

      #endregion

      #region Properties

      protected IBootStrap BootStrap { get; set; }

      public virtual IUnityContainer Container { get; protected set; }

      #endregion


      #region IFoundation Members

      public virtual void Start(IUnityContainer container, IBootStrap bootStrap)
        {
            Trace.Write(FoundationAssumptions.LOG_PREFIX + "CoreFoundation.Start Begin", Category.Trace);
            this.Container = container;
            if(this.Container == null)
            {
               this.Container = new UnityContainer();
            }

            // dependencies
            this.Container.RegisterType<ILogger, DebugLogger>();
            this.Container.RegisterType<ITracer, StandardTracer>();
            this.Container.RegisterType<ITracer, StandardTracer>(ChokeLocation.Unknown);
            this.Container.RegisterType<ITracer, WrappedMethodTracer>(ChokeLocation.Method);
            
            this.Container.RegisterType<IHandleException, StandardThrowExceptionHandler>();
            this.Container.RegisterType<IHandleExceptionProvider, StandardThrowExceptionHandlerProvider>();
            this.Container.RegisterType<IAspectCoordinator, CoreAspectCoordinator>();
            this.Container.RegisterType<IFindClassTypes, AssemblyClassFinder>();

            this.Container.RegisterType<IHandleException, SwallowExceptionHandler>(FoundationAssumptions.SWALLOWED_EXCEPTION_HANDLER);
            this.Container.RegisterType<IHandleExceptionProvider, SwallowExceptionHandlerProvider>(FoundationAssumptions.SWALLOWED_EXCEPTION_HANDLER);
            

            // Self
            this.Container.RegisterInstance<IFoundation>(this);
            
            // Memory cache
            this.Container.RegisterInstance<AspectCache>(new AspectCache(AspectCache.FOUNDATION_KEY, this), new ContainerControlledLifetimeManager());

            // Daemons
            this.Container.RegisterInstance<IDaemonHost>(new ServerDaemonHost(this));
            this.Container.RegisterInstance<IDaemonSynchronizationHandler>(new EmptyDaemonSynchronizationHandler(this));
            this.Container.RegisterInstance<IDaemonManager>(new CoreDaemonManager(this));

            // bootstrap
            if (bootStrap != null) 
            {
                this.Container.RegisterInstance<IBootStrap>(bootStrap);
                bootStrap.OnFoundationCreated(this); 
            }

            // Plugins
            this.Container.RegisterInstance<IPluginManager>(new CorePluginManager(this));
            this.Container.RegisterType<IWebPluginLoader, WebPluginLoader>();

            IPluginManager manager = this.Container.Resolve<IPluginManager>();
            manager.LoadAllPlugins();

            if (bootStrap != null) { bootStrap.OnAfterPluginsLoaded(this); };

            manager.OnBeforeSelfRegisters();
            if (bootStrap != null) { bootStrap.OnBeforeSelfRegisters(this); }

            // Self IoC Registers
            using (this.Container.Resolve<ITracer>().StartTrace("IDynamicallySelfRegister"))
            {
                IFindClassTypes iFindClassTypes = this.Container.Resolve<IFindClassTypes>();
                IEnumerable<Type> selfRegisters = iFindClassTypes.FindClassesOfType<IDynamicallySelfRegister>(true);
                foreach (Type selfRegister in selfRegisters)
                {
                    try
                    {
                        object[] dynamicLoadAttributes = selfRegister.GetCustomAttributes(typeof(PreventDynamicLoadAttribute), true);
                        if (dynamicLoadAttributes != null && (dynamicLoadAttributes.Length > 0))
                        {
                            bool allowed = true;
                            foreach (object attribute in dynamicLoadAttributes)
                            {
                                if (((PreventDynamicLoadAttribute)attribute).PreventDynamicLoading)
                                {
                                    allowed = false;
                                    this.Container.Resolve<ILogger>().Write(selfRegister.FullName + " was not auto loaded, it was prevented by PreventDynamicLoadAttribute", Category.Trace);
                                    break;
                                }
                            }
                            if (!allowed)
                            {
                                continue;
                            }
                        }
                        IDynamicallySelfRegister iDynamicallySelfRegister = (IDynamicallySelfRegister)Activator.CreateInstance(selfRegister);
                        if (iDynamicallySelfRegister != null)
                        {
                            if (bootStrap != null)
                            {
                                SelfRegisteringArgs args = new SelfRegisteringArgs(iDynamicallySelfRegister);
                                bootStrap.OnSelfRegister(this, args);
                                if(args.Cancel)
                                {
                                    continue;
                                }
                            }
                            this.Container.Resolve<ILogger>().Write(selfRegister.FullName + " - Invoking Start", Category.Warning);
                            iDynamicallySelfRegister.SelfRegister(this.Container);
                            this.Container.Resolve<ILogger>().Write(selfRegister.FullName + " - Invoking Complete", Category.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Container.Resolve<ILogger>().Write(string.Format("Error while calling SelfRegister on {0}: {1}", selfRegister.FullName, ex.Message), Category.Error);
                    }
                }
            }

            manager.OnAfterSelfRegisters();

            if (bootStrap != null) { bootStrap.OnAfterSelfRegisters(this); }

            // anything needed here?
            manager.OnBootStrapComplete();

            if (bootStrap != null) { bootStrap.OnBootStrapComplete(this); }

            // turn off logging [verbose only while boot strapping]
            this.Container.RegisterType<ILogger, WarningErrorLogger>();
            this.Container.RegisterType<ITracer, EmptyTracer>();
            this.Container.RegisterType<ITracer, EmptyTracer>(ChokeLocation.Unknown);
            this.Container.RegisterType<ITracer, EmptyTracer>(ChokeLocation.Method);

            manager.OnAfterBootStrapComplete();
            if (bootStrap != null) { bootStrap.OnAfterBootStrapComplete(this); }

            IDaemonManager daemonManager = this.Container.Resolve<IDaemonManager>();
            daemonManager.OnAfterBootStrapComplete();
            
        }

      public virtual void Stop()
      {
         IUnityContainer containerToDestroy = this.Container;
         if (containerToDestroy != null)
         {
            this.Container = null;
            containerToDestroy.Dispose();
         }
      }

      public T SafeResolve<T>()
      {
         try
         {
            return this.Container.Resolve<T>();
         }
         catch (Exception ex)
         {
            this.GetLogger().Write(string.Format("Unable to resolve type '{0}': {1}", typeof(T).ToString(), ex.Message), Category.Warning);
            return default(T);
         }
      }
      public T SafeResolve<T>(string name)
      {
         try
         {
            return this.Container.Resolve<T>(name);
         }
         catch (Exception ex)
         {
            this.GetLogger().Write(string.Format("Unable to resolve named type ({2}): '{0}': {1}", typeof(T).ToString(), ex.Message, name), Category.Warning);
            return default(T);
         }
      }


      [DebuggerStepThrough]
      public virtual ILogger GetLogger()
      {
         return this.Container.Resolve<ILogger>();
      }
      [DebuggerStepThrough]
      public virtual ITracer GetTracer()
      {
         return this.Container.Resolve<ITracer>();
      }

      [DebuggerStepThrough]
      public virtual IAspectCoordinator GetAspectCoordinator()
      {
         return this.Container.Resolve<IAspectCoordinator>();
      }
      [DebuggerStepThrough]
      public virtual IPluginManager GetPluginManager()
      {
         return this.Container.Resolve<IPluginManager>();
      }
      [DebuggerStepThrough]
      public virtual IDaemonManager GetDaemonManager()
      {
         return this.Container.Resolve<IDaemonManager>();
      }

      #endregion


   }
}
