using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Zero.Foundation.System
{
   [DebuggerStepThrough]
   public class Single
   {
      static Single()
      {
         _GlobalStore = new ConcurrentDictionary<Type, object>();
      }

      private static readonly ConcurrentDictionary<Type, object> _GlobalStore;

      public static ConcurrentDictionary<Type, object> GlobalStore
      {
         get
         {
            return _GlobalStore;
         }
      }
      public static T GetDefault<T>()
          where T : new()
      {
         T result = Single<T>.Instance;
         if (result == null)
         {
            Single<T>.Instance = new T();
            result = Single<T>.Instance;
         }
         return result;
      }
   }
   [DebuggerStepThrough]
   public class Single<T> : Single
   {
      private static T _Instance;


      public static T Instance
      {
         get
         {
            return _Instance;
         }
         set
         {
            _Instance = value;
            GlobalStore[typeof(T)] = value;
         }
      }

   }
}
