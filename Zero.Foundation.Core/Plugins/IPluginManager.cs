using System;
using System.Collections.Generic;

namespace Zero.Foundation.Plugins
{
   public interface IPluginManager
   {
      void LoadAllPlugins();
      void UnloadAllPlugins();

      bool RegisterFoundationPlugin(IFoundationPlugin iFoundationPlugin);
      bool UnRegisterFoundationPlugin(string displayName);
      bool UnRegisterFoundationPlugin(IFoundationPlugin iFoundationPlugin);

      void RaiseGenericEvent(object sender, string name, object state);

      ICollection<IFoundationPlugin> FoundationPlugins { get; }
      ICollection<IFoundationPlugin> ChokingPlugins { get; }
      IDictionary<string, object> SharedItems { get; set; }

      void OnBeforeSelfRegisters();
      void OnAfterSelfRegisters();
      void OnBootStrapComplete();
      void OnAfterBootStrapComplete();
   }
}
