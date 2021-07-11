using System;
using Zero.Foundation.Aspect;

namespace Zero.Foundation.Plugins
{
   public interface IFoundationPlugin : IPlugin
   {
      void Dissever();

      bool FoundationInitialize(IFoundation foundation);

      void OnGenericEvent(object sender, string name, object state);

      void OnFoundationPluginAdded(IFoundationPlugin plugin);
      void OnFoundationPluginRemoved(IFoundationPlugin plugin);
      void OnAfterFoundationPluginsLoaded();

      void OnBeforeSelfRegistration();
      void OnAfterSelfRegistration();

      void OnAfterBootStrapComplete();
      void OnBootStrapComplete();

      bool InterceptsChokePoints { get; }
      int DesiredChokePriority { get; }

      /// <summary>
      /// Return true if item should be choked
      /// </summary>
      bool ProcessChokeExit<TReturn>(object invoker, EventArgs args, ChokePointResult<TReturn> previousResult, out ChokePointResult<TReturn> newResult);
      /// <summary>
      /// Return true if item should be choked
      /// </summary>
      bool ProcessChokeEnter<TReturn>(object invoker, EventArgs args, ChokePointResult<TReturn> previousResult, out ChokePointResult<TReturn> newResult);

   }
}