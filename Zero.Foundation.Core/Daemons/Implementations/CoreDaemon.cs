using System;
using System.Threading;
using Zero.Foundation.Aspect;
using Zero.Foundation.System;

namespace Zero.Foundation.Daemons.Implementations
{
    public class CoreDaemon : ChokeableClass, IDaemon, IDisposable
    {
        #region Constructor & Finalizer

        public CoreDaemon(IFoundation iFoundation, IHandleExceptionProvider iHandleExceptionProvider, string instanceName, DaemonConfig config, IDaemonTask iDaemonTask, IDaemonManager daemonManager)
            : base(iFoundation, iHandleExceptionProvider)
        {
            this.AgitateIntervalMilliSeconds = 250;

            this.CancellationToken = new CancellationTokenSource();
            this.InstanceName = instanceName;
            this.IDaemonTask = iDaemonTask;
            this.DaemonManager = daemonManager;
            this.Config = config;
            this.ApplyConfiguration();
        }

        ~CoreDaemon()
        {
            this.Dispose(false);
        }

        #endregion

        #region Public Properties

        [ThreadStatic]
        public static CoreDaemon Current;

        public virtual string InstanceName { get; protected set; }
        public virtual DaemonConfig Config { get; protected set; }
        public virtual int IntervalMilliSeconds { get; set; }
        public virtual int AgitateIntervalMilliSeconds { get; protected set; }

        public virtual int DelayStartMilliSeconds { get; protected set; }

        public virtual bool IsExecuting { get; protected set; }
        public virtual DateTime? LastExecuteStartTime { get; protected set; }
        public virtual DateTime? LastExecuteEndTime { get; protected set; }
        public virtual DateTime? TimerCreated { get; set; }
        public virtual bool IsInitial { get; set; }

        public virtual IDaemonTask IDaemonTask { get; protected set; }
        public virtual IDaemonManager DaemonManager { get; protected set; }

        public virtual CancellationTokenSource CancellationToken { get; protected set; }

        public virtual bool IsOnDemand
        {
            get
            {
                return (IntervalMilliSeconds == -1);
            }
        }

        #endregion

        #region Protected Properties

        private object _agitateRoot = new object();
        private object _tickRoot = new object();
        private bool _isTicking = false;

        protected virtual bool WasAgitated { get; set; }
        protected virtual bool IsDisposed { get; set; }

        protected virtual Timer Timer { get; set; }
        protected virtual Thread WorkerThread { get; set; }

        #endregion

        #region Protected Methods

        protected virtual void ApplyConfiguration()
        {
            base.ExecuteMethod("ApplyConfiguration", delegate ()
            {
                this.IntervalMilliSeconds = this.Config.IntervalMilliSeconds;
                if (IntervalMilliSeconds <= 0)
                {
                    this.IntervalMilliSeconds = -1;
                }
                this.DelayStartMilliSeconds = this.Config.StartDelayMilliSeconds;
                if (this.DelayStartMilliSeconds < 0)
                {
                    this.DelayStartMilliSeconds = 0;
                }
            });
        }
        protected virtual void ExecuteAction()
        {
            try
            {
                lock (_agitateRoot)
                {
                    this.WasAgitated = false;
                }
                if ((this.IDaemonTask.SynchronizationPolicy == DaemonSynchronizationPolicy.None) || this.DaemonManager.TryBeginDaemonTask(this.IDaemonTask))
                {
                    base.Logger.Write(string.Format("{0}:: Executing", this.Config.InstanceName), Category.Trace);
                    this.LastExecuteStartTime = DateTime.Now;
                    this.IsExecuting = true;
                    try
                    {
                        CoreDaemon.Current = this;
                        this.IDaemonTask.Execute(this.IFoundation, this.CancellationToken.Token);
                    }
                    catch (ThreadAbortException)
                    {
                        // gulp
                    }
                    catch (ThreadInterruptedException)
                    {
                        // gulp
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        this.LastExecuteEndTime = DateTime.Now;
                        if (this.IDaemonTask.SynchronizationPolicy == DaemonSynchronizationPolicy.SingleAppDomain)
                        {
                            this.DaemonManager.EndDaemonTask(this.IDaemonTask);
                        }
                    }
                }
                else
                {
                    base.Logger.Write(string.Format("{0}:: Was Not Executed, synchronization disabled this instance from running.", this.Config.InstanceName), Category.Trace);
                }
            }
            catch (Exception ex)
            {
                this.IFoundation.LogError(ex, "CoreDaemon.ExecuteAction");
            }
            finally
            {
                this.IsExecuting = false;
            }
        }

        #endregion

        #region Public Methods

        public virtual void Start()
        {
            base.ExecuteMethod("Start", delegate ()
            {
                if (this.Timer == null)
                {
                    this.ApplyConfiguration();
                    base.Logger.Write(string.Format("Starting Timer for {0}", this.InstanceName), Category.Trace);
                    this.TimerCreated = DateTime.Now;
                    this.IsInitial = true;
                    this.Timer = new Timer(new TimerCallback(this.Timer_Tick), null, this.DelayStartMilliSeconds, this.IntervalMilliSeconds);
                }
                else
                {
                    base.Logger.Write(string.Format("Timer already started for {0}. Will attempt to agitate", this.InstanceName), Category.Trace);
                    this.Agitate();
                }
            });
        }
        public virtual void Agitate()
        {
            base.ExecuteMethod("Agitate", delegate ()
            {
                base.Logger.Write(string.Format("{0}:: Agitating", this.Config.InstanceName), Category.Trace);
                if (this.IsOnDemand)
                {
                    lock (this._agitateRoot)
                    {
                        this.WasAgitated = true;
                    }
                    if (!this.IsExecuting)
                    {
                        this.Timer.Change(0, this.AgitateIntervalMilliSeconds);
                    }
                }
                else
                {
                    lock (this._agitateRoot)
                    {
                        this.WasAgitated = true;
                    }
                    if (!this.IsExecuting)
                    {
                        this.Timer.Change(0, this.AgitateIntervalMilliSeconds);
                    }
                }
            });

        }

        public virtual void ConvertToOnDemand()
        {
            base.ExecuteMethod("ConvertToOnDemand", delegate ()
            {
                base.Logger.Write(string.Format("{0}:: ConvertToOnDemand", this.Config.InstanceName), Category.Trace);
                this.IntervalMilliSeconds = 0;
            });
        }

        #endregion

        #region Event Handlers

        protected virtual void Timer_Tick(object state)
        {
            base.ExecuteMethod("Timer_Tick", delegate ()
            {
             // prevent race conditions due to slow cpu performance
             bool shouldContinue = false;
                lock (_tickRoot)
                {
                    if (!_isTicking)
                    {
                        shouldContinue = true;
                        _isTicking = true;
                    }
                }
                if (!shouldContinue)
                {
                    base.Logger.Write(string.Format("{0}:: Timer Tick Race Condition Detected", this.Config.InstanceName), Category.Trace);
                 // let the next timer tick or allow it to be aborted as part of execution process
                 return;
                }
                try
                {
                    base.Logger.Write(string.Format("{0}:: Timer Tick", this.Config.InstanceName), Category.Trace);
                    this.IsInitial = false;
                    this.Timer.Change(-1, -1);
                    ManualResetEvent threadStarted = new ManualResetEvent(false);
                    WorkerThread = new Thread(delegate ()
                    {
                        threadStarted.Set();
                        this.ExecuteAction();
                    });
                    WorkerThread.Start();
                    threadStarted.WaitOne();
                    threadStarted.Dispose();
                    WorkerThread.Join();
                }
                finally
                {
                    lock (_tickRoot)
                    {
                        _isTicking = false;
                    }
                    if (!this.IsDisposed && this.Timer != null)
                    {
                        this.Timer.Change(this.IntervalMilliSeconds, this.IntervalMilliSeconds);
                    }
                }

                if (this.IsDisposed) { return; }
                if (this.IsOnDemand)
                {
                    lock (_agitateRoot)
                    {
                        if (this.WasAgitated)
                        {
                            base.Logger.Write(string.Format("{0}:: Was Agitated: {1}", this.Config.InstanceName, this.WasAgitated), Category.Trace);
                            if (this.Timer != null)
                            {
                                this.Timer.Change(0, this.AgitateIntervalMilliSeconds);
                            }
                        }
                    }
                }
                else
                {
                    if (this.WasAgitated)
                    {
                        base.Logger.Write(string.Format("{0}:: Was Agitated: {1}", this.Config.InstanceName, this.WasAgitated), Category.Trace);
                        if (this.Timer != null)
                        {
                            this.Timer.Change(0, this.AgitateIntervalMilliSeconds);
                        }
                    }
                }

            });
        }

        #endregion

        #region IDisposable Members

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                this.IsDisposed = true;

                this.CancellationToken.Cancel();

                // prevent timer
                try
                {
                    if (this.Timer != null)
                    {
                        base.Logger.Write(string.Format("Disposing Daemon: {0}", this.InstanceName), Category.Trace);
                        this.Timer.Dispose();
                        this.Timer = null;
                    }
                }
                catch { }

                if (this.IDaemonTask != null)
                {
                    this.IDaemonTask.Dispose();
                }
                this.IDaemonTask = null;

                if (this.WorkerThread != null)
                {
                    this.WorkerThread.Interrupt();
                    this.WorkerThread = null;
                }

                this.IsExecuting = false; //JIC
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