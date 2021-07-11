using System;

namespace Zero.Foundation.Plugins
{
   [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
   public sealed class PreventDynamicLoadAttribute : Attribute
   {
      readonly bool _preventDynamicLoading;

      public PreventDynamicLoadAttribute(bool preventDynamicLoading)
      {
         _preventDynamicLoading = preventDynamicLoading;
      }

      public bool PreventDynamicLoading
      {
         get { return _preventDynamicLoading; }
      }
   }
}
