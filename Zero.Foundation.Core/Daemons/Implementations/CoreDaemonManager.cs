using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Zero.Foundation.Aspect;
using Unity;
using System.Linq;
using Zero.Foundation.System;

namespace Zero.Foundation.Daemons.Implementations
{
    public class CoreDaemonManager : ChokeableClass, IDaemonManager, IDisposable
    {
        #region Constructor

        public CoreDaemonManager(IFoundation iFoundation)
            : base(iFoundation)
        {
            this.InnerDaemonRegistrations = new Dictionary<string, DaemonRegistration>(StringComparer.OrdinalIgnoreCase);
            this.InnerDaemons = new Dictionary<string, CoreDaemon>(StringComparer.OrdinalIgnoreCase);

            this.SharedItems = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        ~CoreDaemonManager()
        {
            this.Dispose(false);
        }
        #endregion

        #region Private/Protected Properties

        protected virtual Dictionary<string, DaemonRegistration> InnerDaemonRegistrations { get; set; }
        protected virtual Dictionary<string, CoreDaemon> InnerDaemons { get; set; }

        #endregion

        #region Public Properties

        public virtual IDictionary<string, object> SharedItems { get; set; }
        public virtual ICollection<IDaemonTask> Tasks
        {
            get
            {
                List<IDaemonTask> tasks = new List<IDaemonTask>();
                foreach (var item in this.InnerDaemonRegistrations.Values)
                {
                    tasks.Add(item.IDaemonTask);
                }
                return new ReadOnlyCollection<IDaemonTask>(tasks);
            }
        }
        public virtual ICollection<IDaemon> LoadedDaemons
        {
            get
            {
                return new ReadOnlyCollection<IDaemon>(new List<IDaemon>(this.InnerDaemons.Values));
            }
        }

        public virtual bool BootStrapComplete { get; set; }

        public virtual IDaemonSynchronizationHandler SynchronizationHandler { get; set; }
        public virtual IDaemonHost DaemonHost { get; set; }

        #endregion

        #region Public Methods

        public virtual List<DaemonExecutionEstimate> GetAllTimerDetails()
        {
            return base.ExecuteFunction<List<DaemonExecutionEstimate>>("GetAllTimerDetails", delegate ()
            {
                List<DaemonExecutionEstimate> result = new List<DaemonExecutionEstimate>();
                List<string> daemons = this.InnerDaemonRegistrations.Keys.ToList();
                foreach (string item in daemons)
                {
                    DaemonExecutionEstimate estimate = this.GetTimerDetail(item);
                    if (estimate != null)
                    {
                        result.Add(estimate);
                    }
                }
                return result;
            });
        }
        public virtual DaemonExecutionEstimate GetTimerDetail(string instanceName)
        {
            return base.ExecuteFunction<DaemonExecutionEstimate>("GetTimerDetail", delegate ()
            {
                if (InnerDaemons.ContainsKey(instanceName))
                {
                    CoreDaemon daemon = this.InnerDaemons[instanceName];
                    if (daemon != null)
                    {
                        DaemonExecutionEstimate estimate = new DaemonExecutionEstimate()
                        {
                            Name = daemon.Config.InstanceName,
                            IsScheduled = !daemon.IsOnDemand,
                            IsRunning = daemon.IsExecuting,
                            LastExecutedEnd = daemon.LastExecuteEndTime,
                            LastExecutedStart = daemon.LastExecuteStartTime,
                        };
                        if (!daemon.IsOnDemand)
                        {
                            estimate.NextScheduledStart = DateTime.Now; // safety

                         if (estimate.IsRunning && estimate.LastExecutedStart.HasValue)
                            {
                                estimate.NextScheduledStart = estimate.LastExecutedStart.Value.AddMilliseconds(daemon.IntervalMilliSeconds);
                            }
                            else if (estimate.LastExecutedEnd.HasValue)
                            {
                                estimate.NextScheduledStart = estimate.LastExecutedEnd.Value.AddMilliseconds(daemon.IntervalMilliSeconds);
                            }
                            else if (daemon.TimerCreated.HasValue)
                            {
                                estimate.NextScheduledStart = daemon.TimerCreated.Value.AddMilliseconds(daemon.DelayStartMilliSeconds);
                            }
                        }
                        else
                        {
                            if (daemon.IsInitial)
                            {
                                estimate.NextScheduledStart = DateTime.Now.AddMilliseconds(daemon.DelayStartMilliSeconds); // safety
                             if (daemon.TimerCreated.HasValue)
                                {
                                    estimate.NextScheduledStart = daemon.TimerCreated.Value.AddMilliseconds(daemon.DelayStartMilliSeconds);
                                }
                            }
                        }
                        return estimate;
                    }
                }
                else
                {
                    DaemonRegistration registration = this.InnerDaemonRegistrations[instanceName];
                    if (registration != null)
                    {
                        return new DaemonExecutionEstimate()
                        {
                            Name = registration.DaemonConfig.InstanceName,
                            IsScheduled = false,
                            IsRunning = false
                        };
                    }
                }
                return null;
            });
        }

        public virtual bool RegisterDaemon(DaemonConfig config, IDaemonTask iDaemonTask, bool autoStart)
        {
            return base.ExecuteFunction<bool>("RegisterDaemon", delegate ()
            {
                if (this.InnerDaemonRegistrations.ContainsKey(config.InstanceName))
                {
                    throw new InvalidOperationException(string.Format("A DaemonTask with the Instance Name '{0}' has already been registered.", config.InstanceName));
                }

                base.Logger.Write(string.Format("Registering DaemonTask: {0}.", config.InstanceName), Category.Trace);
                this.InnerDaemonRegistrations[config.InstanceName] = new DaemonRegistration() 
                { 
                    IDaemonTask = iDaemonTask, 
                    DaemonConfig = config,
                    AutoStart = autoStart
                };

                if (this.BootStrapComplete)
                {
                    if (autoStart)
                    {
                        this.StartDaemon(config.InstanceName);
                    }
                    else
                    {
                        this.EnsureInnerDeamon(config.InstanceName);
                    }
                }
                return true;
            });
        }
        public virtual bool UnRegisterDaemon(string instanceName)
        {
            return base.ExecuteFunction<bool>("UnRegisterDaemon", delegate ()
            {
                base.Logger.Write(string.Format("UnRegistering DaemonTask: {0}.", instanceName), Category.Trace);
                
                this.StopDaemon(instanceName);
                
                if (this.InnerDaemonRegistrations.ContainsKey(instanceName))
                {
                    this.InnerDaemonRegistrations.Remove(instanceName);
                }
                return true;
            });
        }
        public virtual bool IsDaemonRegistered(string instanceName)
        {
            return base.ExecuteFunction<bool>("IsDaemonRegistered", delegate ()
            {
                return this.InnerDaemonRegistrations.ContainsKey(instanceName);
            });
        }
        public virtual IDaemonTask GetRegisteredDaemonTask(string instanceName)
        {
            return base.ExecuteFunction<IDaemonTask>("GetRegisteredDaemonTask", delegate ()
            {
                if (this.InnerDaemonRegistrations.ContainsKey(instanceName))
                {
                    return this.InnerDaemonRegistrations[instanceName].IDaemonTask;
                }
                return null;
            });
        }

        public virtual IDaemonTask[] FindRegisteredDaemonTasks(Func<string, bool> predicate)
        {
            return base.ExecuteFunction("FindRegisteredDaemonTasks", delegate ()
            {
                List<IDaemonTask> result = new List<IDaemonTask>();
                string[] keys = this.InnerDaemonRegistrations.Keys.ToArray();
                foreach (string key in keys)
                {
                    if (this.InnerDaemonRegistrations.TryGetValue(key, out DaemonRegistration instance))
                    {
                        if (predicate(key))
                        {
                            result.Add(instance.IDaemonTask);
                        }
                    }
                }
                
                return result.ToArray();
            });
        }

        public virtual void UnRegisterAllDaemons()
        {
            base.ExecuteMethod("UnRegisterAllDaemons", delegate ()
            {
                List<string> keys = this.InnerDaemons.Keys.ToList();
                foreach (string key in keys)
                {
                    try
                    {
                        this.UnRegisterDaemon(key);
                    }
                    catch (Exception ex)
                    {
                        base.Logger.Write(string.Format("Error Unregistering DaemonTask: {0}. Error: {1}", key, ex.Message), Category.Warning);
                    }
                }
            });
        }

        public virtual void StartDaemons(bool includeManualStart)
        {
            base.ExecuteMethod("StartDaemons", delegate ()
            {
                if (!this.BootStrapComplete)
                {
                    base.Logger.Write("Call to StartDaemons before BootStrap Complete", Category.Warning);
                    return;
                }

                List<string> instanceNames = this.InnerDaemonRegistrations.Keys.ToList();
                foreach (string instanceName in instanceNames)
                {
                    try
                    {
                        DaemonRegistration registration = this.InnerDaemonRegistrations[instanceName];
                        if(includeManualStart || registration.AutoStart)
                        {
                            this.StartDaemon(instanceName);
                        }
                        else
                        {
                            this.EnsureInnerDeamon(instanceName);
                        }
                    }
                    catch (Exception ex)
                    {
                        base.Logger.Write(string.Format("Error Starting DaemonTask: {0}. Error: {1}", instanceName, ex.Message), Category.Warning);
                    }
                }
            });
        }
        public virtual void StopDaemons()
        {
            base.ExecuteMethod("StopDaemons", delegate ()
            {
                if (!this.BootStrapComplete)
                {
                    this.Logger.Write("Call to StopDaemon before BootStrap Complete", Category.Warning);
                    return;
                }
                List<string> keys = this.InnerDaemons.Keys.ToList();
                foreach (string key in keys)
                {
                    try
                    {
                        this.StopDaemon(key);
                    }
                    catch (Exception ex)
                    {
                        base.Logger.Write(string.Format("Error Stopping DaemonTask: {0}. Error: {1}", key, ex.Message), Category.Warning);
                    }
                }
            });
        }

        public virtual void StartDaemon(string instanceName)
        {
            base.ExecuteMethod("StartDaemon", delegate ()
            {
                if (!this.BootStrapComplete)
                {
                    base.Logger.Write("Call to StartDaemon before BootStrap Complete", Category.Warning);
                    return;
                }
                base.Logger.Write(string.Format("Starting DaemonTask: {0}.", instanceName), Category.Trace);

                this.EnsureInnerDeamon(instanceName);

                if (this.InnerDaemons.ContainsKey(instanceName))
                {
                    this.InnerDaemons[instanceName].Start();
                }
            });
        }
        public virtual void StopDaemon(string instanceName)
        {
            base.ExecuteMethod("StopDaemon", delegate ()
            {
                if (!this.BootStrapComplete)
                {
                    base.Logger.Write("Call to StopDaemon before BootStrap Complete", Category.Warning);
                    return;
                }
                base.Logger.Write(string.Format("Stopping DaemonTask: {0}.", instanceName), Category.Trace);
                // if its an IDaemon, stop it, dispose it, remove it
                if (this.InnerDaemons.ContainsKey(instanceName))
                {
                    CoreDaemon daemon = this.InnerDaemons[instanceName];
                    if (daemon != null)
                    {
                        daemon.Dispose();
                    }
                    this.InnerDaemons.Remove(instanceName);
                }
            });
        }

        public virtual void OnAfterBootStrapComplete()
        {
            base.ExecuteMethod("OnAfterBootStrapComplete", delegate ()
            {
                this.SynchronizationHandler = this.IFoundation.Container.Resolve<IDaemonSynchronizationHandler>();
                this.DaemonHost = this.IFoundation.Container.Resolve<IDaemonHost>();

                this.BootStrapComplete = true;

                this.StartDaemons(false);
            });
        }

        public virtual bool TryBeginDaemonTask(IDaemonTask task)
        {
            return base.ExecuteFunction<bool>("TryBeginDaemonTask", delegate ()
            {
                return this.SynchronizationHandler.TryBeginDaemonTask(this.DaemonHost, task);
            });
        }
        public virtual bool EndDaemonTask(IDaemonTask task)
        {
            return base.ExecuteFunction<bool>("EndDaemonTask", delegate ()
            {
                return this.SynchronizationHandler.EndDaemonTask(this.DaemonHost, task);
            });
        }


        protected virtual bool EnsureInnerDeamon(string instanceName)
        {
            return base.ExecuteFunction("EnsureInnerDeamon", delegate ()
            {
                if (!this.InnerDaemons.ContainsKey(instanceName))
                {
                    if (this.InnerDaemonRegistrations.ContainsKey(instanceName))
                    {
                        DaemonRegistration registration = this.InnerDaemonRegistrations[instanceName];
                        if ((registration != null) && (registration.IDaemonTask != null) && (registration.DaemonConfig != null))
                        {
                            IHandleExceptionProvider exceptionHandlerProvider = base.IFoundation.SafeResolve<IHandleExceptionProvider>(FoundationAssumptions.SWALLOWED_EXCEPTION_HANDLER);
                            if (exceptionHandlerProvider == null)
                            {
                                exceptionHandlerProvider = base.IFoundation.Container.Resolve<IHandleExceptionProvider>();
                            }
                            this.InnerDaemons[instanceName] = new CoreDaemon(base.IFoundation, exceptionHandlerProvider, instanceName, registration.DaemonConfig, registration.IDaemonTask, this);
                            return true;
                        }
                        else
                        {
                            base.Logger.Write(string.Format("DaemonTask '{0}' was null.", instanceName), Category.Warning);
                            return false;
                        }
                    }
                    else
                    {
                        base.Logger.Write(string.Format("Unable to find DaemonTask: {0}.", instanceName), Category.Warning);
                        return false;
                    }
                }
                else
                {
                    base.Logger.Write(string.Format("DaemonTask '{0}' was already present.", instanceName), Category.Trace);
                    return true;
                }
            });
        }

        #endregion

        #region IDisposable Members


        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                try
                {
                    this.UnRegisterAllDaemons();
                }
                catch { }
                try
                {
                    if (this.SynchronizationHandler != null)
                    {
                        this.SynchronizationHandler.ClearAllDaemonTasks(this.DaemonHost);
                    }
                    else
                    {
                        this.DaemonHost = null;
                        this.SynchronizationHandler = null;
                    }
                }
                catch
                {
                }
            }
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }

}