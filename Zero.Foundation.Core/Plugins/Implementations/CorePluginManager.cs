using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Zero.Foundation.Aspect;
using Zero.Foundation.System;
using Unity;
using System.Linq;

namespace Zero.Foundation.Plugins.Implementations
{
   public class CorePluginManager : ChokeableClass, IPluginManager, IDisposable
   {
      #region Constructor

      public CorePluginManager(IFoundation iFoundation)
          : base(iFoundation)
      {
         _innerList = new List<IFoundationPlugin>();
         _innerChokingPlugins = new List<IFoundationPlugin>();

         this.SharedItems = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
         this.FoundationPlugins = new ReadOnlyCollection<IFoundationPlugin>(_innerList);
         this.ChokingPlugins = new ReadOnlyCollection<IFoundationPlugin>(_innerChokingPlugins);
      }

      ~CorePluginManager()
      {
         this.Dispose(false);
      }
      #endregion

      #region Private Properties

      private object _changeLockRoot = new object();
      private List<IFoundationPlugin> _innerList { get; set; }
      private List<IFoundationPlugin> _innerChokingPlugins { get; set; }

      #endregion

      #region Public Properties

      public virtual IDictionary<string, object> SharedItems { get; set; }
      public virtual ICollection<IFoundationPlugin> FoundationPlugins
      {
         get;
         protected set;
      }
      public virtual ICollection<IFoundationPlugin> ChokingPlugins
      {
         get;
         protected set;
      }

      #endregion

      #region Public Methods

      public virtual void LoadAllPlugins()
      {
         base.ExecuteMethod("LoadAllPlugins", delegate ()
         {
            IFindClassTypes finder = this.IFoundation.Container.Resolve<IFindClassTypes>();
            IEnumerable<Type> foundationPlugins = finder.FindClassesOfType<IFoundationPlugin>(true);
            foreach (Type item in foundationPlugins)
            {
               try
               {
                  object[] dynamicLoadAttributes = item.GetCustomAttributes(typeof(PreventDynamicLoadAttribute), false);
                  if (dynamicLoadAttributes != null && (dynamicLoadAttributes.Length > 0))
                  {
                     bool allowed = true;
                     foreach (object attribute in dynamicLoadAttributes)
                     {
                        if (((PreventDynamicLoadAttribute)attribute).PreventDynamicLoading)
                        {
                           allowed = false;
                           base.Logger.Write(item.FullName + " was not auto loaded, it was prevented by PreventDynamicLoadAttribute", Category.Trace);
                           break;
                        }
                     }
                     if (!allowed)
                     {
                        continue;
                     }
                  }
                  IFoundationPlugin iFoundationPlugin = (IFoundationPlugin)Activator.CreateInstance(item);
                  if (iFoundationPlugin != null)
                  {
                     RegisterFoundationPlugin(iFoundationPlugin);
                  }
               }
               catch (Exception ex)
               {
                  base.Logger.Write(string.Format("Error while loading IFoundationPlugin plugin from {0}: {1}", item.FullName, ex.Message), Category.Error);
               }
            }

            List<IFoundationPlugin> plugins = GetCurrentFoundationPluginListSynchronized();
            foreach (IFoundationPlugin item in plugins)
            {
               try
               {
                  item.OnAfterFoundationPluginsLoaded();
               }
               catch (Exception ex)
               {
                  base.Logger.Write(string.Format("Error raising AfterFoundationPluginsLoaded for '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Warning);
               }
            }
         });
      }

      public virtual bool RegisterFoundationPlugin(IFoundationPlugin iFoundationPlugin)
      {
         return base.ExecuteFunction<bool>("RegisterFoundationPlugin", delegate ()
         {
            if (iFoundationPlugin != null)
            {
               try
               {
                  base.Logger.Write(string.Format("Attempting to Register Plugin: '{0}-{1}'", iFoundationPlugin.DisplayName, iFoundationPlugin.DisplayVersion), Category.Trace);
                  if (iFoundationPlugin.FoundationInitialize(base.IFoundation))
                  {
                     lock (_changeLockRoot)
                     {
                        this._innerList.Add(iFoundationPlugin);
                        if (iFoundationPlugin.InterceptsChokePoints)
                        {
                           _innerChokingPlugins.Add(iFoundationPlugin);
                           _innerChokingPlugins.Sort(delegate (IFoundationPlugin l, IFoundationPlugin r) { return l.DesiredChokePriority.CompareTo(r.DesiredChokePriority); });
                        }
                     }
                     RaiseOnFoundationPluginAdded(iFoundationPlugin);
                     return true;
                  }
               }
               catch (Exception ex)
               {
                  base.Logger.Write(string.Format("Error while loading plugin '{0}-{1}': {2}", iFoundationPlugin.DisplayName, iFoundationPlugin.DisplayVersion, ex.Message), Category.Warning);
               }
            }
            return false;
         });
      }

      public virtual bool UnRegisterFoundationPlugin(string displayName)
      {
         return base.ExecuteFunction<bool>("UnRegisterFoundationPlugin", delegate ()
         {
            IFoundationPlugin iPlugin = FoundationPlugins.FirstOrDefault(p => p.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase));
            return UnRegisterFoundationPlugin(iPlugin);
         });
      }
      public virtual bool UnRegisterFoundationPlugin(IFoundationPlugin iFoundationPlugin)
      {
         return UnRegisterFoundationPlugin(iFoundationPlugin, false);
      }
      public virtual bool UnRegisterFoundationPlugin(IFoundationPlugin iFoundationPlugin, bool isDisposing)
      {
         return base.ExecuteFunction<bool>("UnRegisterFoundationPlugin", delegate ()
         {
            if (iFoundationPlugin != null)
            {
               base.Logger.Write(string.Format("Attempting to UnRegister Plugin: '{0}-{1}'", iFoundationPlugin.DisplayName, iFoundationPlugin.DisplayVersion), Category.Trace);
               bool result = false;
               lock (_changeLockRoot)
               {
                  result = _innerList.Remove(iFoundationPlugin);
                  _innerChokingPlugins.Remove(iFoundationPlugin);
               }
               if (result)
               {
                  try
                  {
                     iFoundationPlugin.Dissever();
                  }
                  catch (Exception ex)
                  {
                     base.Logger.Write(string.Format("Error calling Dissever for '{0}-{1}': {2}", iFoundationPlugin.DisplayName, iFoundationPlugin.DisplayVersion, ex.Message), Category.Warning);
                  }
                  if (!isDisposing)
                  {
                     RaiseOnFoundationPluginRemoved(iFoundationPlugin);
                  }
               }
               return result;
            }
            return false;
         });
      }

      public virtual void RaiseGenericEvent(object sender, string name, object state)
      {
         base.ExecuteMethod("RaiseGenericEvent", delegate ()
         {
            List<IFoundationPlugin> plugins = GetCurrentFoundationPluginListSynchronized();
            foreach (IFoundationPlugin item in plugins)
            {
               try
               {
                  item.OnGenericEvent(sender, name, state);
               }
               catch (Exception ex)
               {
                  base.Logger.Write(string.Format("Error raising RaiseGenericEvent for '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Warning);
               }
            }
         });
      }

      public virtual void UnloadAllPlugins()
      {
         UnloadAllPlugins(false);
      }
      public virtual void UnloadAllPlugins(bool disposing)
      {
         base.ExecuteMethod("UnloadAllPlugins", delegate ()
         {
            List<IFoundationPlugin> foundationPlugins = GetCurrentFoundationPluginListSynchronized();
            foreach (IFoundationPlugin item in foundationPlugins)
            {
               try
               {
                  UnRegisterFoundationPlugin(item, disposing);
               }
               catch (Exception ex)
               {
                  base.Logger.Write(string.Format("Error unloading '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Warning);
               }
            }
         });
      }

      #endregion

      #region Protected Methods

      protected virtual void RaiseOnFoundationPluginAdded(IFoundationPlugin iFoundationPlugin)
      {
         base.ExecuteMethod("RaiseOnFoundationPluginAdded", delegate ()
         {
            List<IFoundationPlugin> plugins = GetCurrentFoundationPluginListSynchronized();
            foreach (IFoundationPlugin item in plugins)
            {
               try
               {
                  item.OnFoundationPluginAdded(iFoundationPlugin);
               }
               catch (Exception ex)
               {
                  base.Logger.Write(string.Format("Error raising OnFoundationPluginAdded for '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Warning);
               }
            }
         });
      }
      protected virtual void RaiseOnFoundationPluginRemoved(IFoundationPlugin iFoundationPlugin)
      {
         base.ExecuteMethod("RaiseOnFoundationPluginRemoved", delegate ()
         {
            List<IFoundationPlugin> plugins = GetCurrentFoundationPluginListSynchronized();
            foreach (IFoundationPlugin item in plugins)
            {
               try
               {
                  item.OnFoundationPluginRemoved(iFoundationPlugin);
               }
               catch (Exception ex)
               {
                  base.Logger.Write(string.Format("Error raising RaiseOnFoundationPluginRemoved for '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Warning);
               }
            }
         });
      }

      protected virtual List<IFoundationPlugin> GetCurrentFoundationPluginListSynchronized()
      {
         List<IFoundationPlugin> plugins = null;
         lock (_changeLockRoot)
         {
            plugins = new List<IFoundationPlugin>(this._innerList);
         }
         return plugins;
      }

      #endregion

      #region IDisposable Members

      protected virtual void Dispose(bool isDisposing)
      {
         if (isDisposing)
         {
            try
            {
               UnloadAllPlugins();
            }
            catch { }
         }
      }
      public void Dispose()
      {
         this.Dispose(true);
         GC.SuppressFinalize(this);
      }

      #endregion

      #region IPluginManager Members

      public void OnBeforeSelfRegisters()
      {
         base.ExecuteMethod("OnBeforeSelfRegisters", delegate ()
         {
            List<IFoundationPlugin> plugins = GetCurrentFoundationPluginListSynchronized();
            foreach (IFoundationPlugin item in plugins)
            {
               try
               {
                  item.OnBeforeSelfRegistration();
               }
               catch (Exception ex)
               {
                  base.Logger.Write(string.Format("Error raising OnBeforeSelfRegisters for '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Warning);
               }
            }
         });
      }
      public void OnAfterSelfRegisters()
      {
         base.ExecuteMethod("OnAfterSelfRegisters", delegate ()
         {
            List<IFoundationPlugin> plugins = GetCurrentFoundationPluginListSynchronized();
            foreach (IFoundationPlugin item in plugins)
            {
               try
               {
                  item.OnAfterSelfRegistration();
               }
               catch (Exception ex)
               {
                  base.Logger.Write(string.Format("Error raising OnAfterSelfRegisters for '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Warning);
               }
            }
         });
      }

      public void OnBootStrapComplete()
      {
         base.ExecuteMethod("OnBootStrapComplete", delegate ()
         {
            List<IFoundationPlugin> plugins = GetCurrentFoundationPluginListSynchronized();
            foreach (IFoundationPlugin item in plugins)
            {
               try
               {
                  item.OnBootStrapComplete();
               }
               catch (Exception ex)
               {
                  base.Logger.Write(string.Format("Error raising OnBootStrapComplete for '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Warning);
               }
            }
         });
      }
      public void OnAfterBootStrapComplete()
      {
         base.ExecuteMethod("OnAfterBootStrapComplete", delegate ()
         {
            List<IFoundationPlugin> plugins = GetCurrentFoundationPluginListSynchronized();
            foreach (IFoundationPlugin item in plugins)
            {
               try
               {
                  item.OnAfterBootStrapComplete();
               }
               catch (Exception ex)
               {
                  base.Logger.Write(string.Format("Error raising OnAfterBootStrapComplete for '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Warning);
               }
            }
         });
      }
      #endregion
   }
}
