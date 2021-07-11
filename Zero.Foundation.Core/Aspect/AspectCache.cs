using System;
using System.Collections.Concurrent;
using System.Threading;
using Unity;
using Unity.Lifetime;

namespace Zero.Foundation.Aspect
{
   //TODO: More efficient locks (one for each type)
   /// <summary>
   /// Enables caching across instance boundaries
   /// </summary>
   public class AspectCache : ChokeableClass
   {
      public AspectCache(string ownerToken)
          : base(CoreFoundation.Current)
      {
         this.OwnerToken = ownerToken;
         this.Lifetime = new ContainerControlledLifetimeManager();
         this.InstanceCache = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
      }
      public AspectCache(string ownerToken, LifetimeManager lifeTime)
          : base(CoreFoundation.Current)
      {
         this.OwnerToken = ownerToken;
         this.Lifetime = lifeTime;
         this.InstanceCache = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
      }
      public AspectCache(string ownerToken, IFoundation iFoundation)
          : base(iFoundation)
      {
         this.OwnerToken = ownerToken;
         this.InstanceCache = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
         this.Lifetime = new ContainerControlledLifetimeManager();
      }
      public AspectCache(string ownerToken, IFoundation iFoundation, LifetimeManager lifeTime)
          : base(iFoundation)
      {
         this.OwnerToken = ownerToken;
         this.InstanceCache = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
         this.Lifetime = lifeTime;
      }

      internal const string FOUNDATION_KEY = "foundation";

      private ReaderWriterLockSlim _accessLock = new ReaderWriterLockSlim();

      private LifetimeManager _lifetime;
      public virtual LifetimeManager Lifetime
      {
         get
         {
            return _lifetime;
         }
         protected set
         {
            _lifetime = value;
         }
      }

      public virtual string OwnerToken { get; protected set; }
      public virtual ConcurrentDictionary<string, object> InstanceCache { get; protected set; }

      /// <summary>
      /// Gets the value from cache if it exists, otherwise executes the retrievemethod then caches the result.
      /// </summary>
      public virtual T PerInstance<T>(string callerName, Func<T> retrieveMethod)
      {
         return base.ExecuteFunction<T>("PerInstance", delegate ()
         {
            T result = default(T);
            bool found = false;
            _accessLock.EnterReadLock();
            try
            {
               found = this.InstanceCache.ContainsKey(callerName);
               if (found)
               {
                  result = (T)this.InstanceCache[callerName];
               }
            }
            finally
            {
               _accessLock.ExitReadLock();
            }
            if (!found)
            {
               result = retrieveMethod();
               _accessLock.EnterWriteLock(); // race condition here is ok, last one should win
                  try
               {
                  this.InstanceCache[callerName] = result;
               }
               finally
               {
                  _accessLock.ExitWriteLock();
               }
            }
            return result;
         });
      }
      /// <summary>
      /// Gets the value from cache if it exists, otherwise executes the retrievemethod then caches the result.
      /// </summary>
      public virtual T PerFoundation<T>(string callerName, Func<T> retrieveMethod)
      {
         return base.ExecuteFunction<T>("PerFoundation", delegate ()
         {
            AspectCache cache = base.IFoundation.Container.Resolve<AspectCache>();
            return cache.PerInstance<T>(string.Format("{0}::{1}", this.OwnerToken, callerName), retrieveMethod);
         });
      }
      /// <summary>
      /// Gets the value from cache if it exists, otherwise executes the retrievemethod then caches the result.
      /// </summary>
      public virtual T PerLifetime<T>(string callerName, Func<T> retrieveMethod)
      {
         return base.ExecuteFunction<T>("PerLifetime", delegate ()
         {
            AspectCache cache = null;

            _accessLock.EnterReadLock();
            try
            {
               cache = this.Lifetime.GetValue() as AspectCache;
            }
            finally
            {
               _accessLock.ExitReadLock();
            }

            if (cache == null)
            {
               _accessLock.EnterWriteLock(); // race condition here is ok, last one should win
                  try
               {
                  cache = new AspectCache(this.OwnerToken, base.IFoundation, this.Lifetime);
                  this.Lifetime.SetValue(cache);
               }
               finally
               {
                  _accessLock.ExitWriteLock();
               }
            }
            return cache.PerInstance(callerName, retrieveMethod);
         });
      }

      /// <summary>
      /// Forcibly updates the cache with the supplied value
      /// </summary>
      public virtual T SetPerInstance<T>(string callerName, T value)
      {
         return base.ExecuteFunction<T>("SetPerInstance", delegate ()
         {
            _accessLock.EnterWriteLock();
            try
            {
               this.InstanceCache[callerName] = value;
            }
            finally
            {
               _accessLock.ExitWriteLock();
            }
            return value;
         });
      }
      /// <summary>
      /// Forcibly updates the cache with the supplied value
      /// </summary>
      public virtual T SetPerFoundation<T>(string callerName, T value)
      {
         return base.ExecuteFunction<T>("SetPerFoundation", delegate ()
         {
            AspectCache cache = base.IFoundation.Container.Resolve<AspectCache>();
            return cache.SetPerInstance<T>(string.Format("{0}::{1}", this.OwnerToken, callerName), value);
         });
      }
      /// <summary>
      /// Forcibly updates the cache with the supplied value
      /// </summary>
      public virtual T SetPerLifetime<T>(string callerName, T value)
      {
         return base.ExecuteFunction<T>("SetPerLifetime", delegate ()
         {
            AspectCache cache = null;
            _accessLock.EnterReadLock();
            try
            {
               cache = Lifetime.GetValue() as AspectCache;
            }
            finally
            {
               _accessLock.ExitReadLock();
            }

            if (cache == null)
            {
               cache = new AspectCache(this.OwnerToken, base.IFoundation, this.Lifetime);
               Lifetime.SetValue(cache);
            }
            return cache.SetPerInstance<T>(callerName, value);
         });
      }

      /// <summary>
      /// Forcibly removes the cache with the supplied value
      /// </summary>
      public virtual void ClearPerInstance(string callerName)
      {
         base.ExecuteMethod("ClearPerInstance", delegate ()
         {
            _accessLock.EnterWriteLock();
            try
            {
               object ignore = null;
               this.InstanceCache.TryRemove(callerName, out ignore);
            }
            finally
            {
               _accessLock.ExitWriteLock();
            }
         });
      }
      /// <summary>
      /// Forcibly removes the cache with the supplied value
      /// </summary>
      public virtual void ClearPerFoundation<T>(string callerName)
      {
         base.ExecuteMethod("ClearPerFoundation", delegate ()
         {
            AspectCache cache = base.IFoundation.Container.Resolve<AspectCache>();
            cache.ClearPerInstance(string.Format("{0}::{1}", this.OwnerToken, callerName));
         });
      }
      /// <summary>
      /// Forcibly removes the cache with the supplied callerName
      /// </summary>
      public virtual void ClearPerLifetime<T>(string callerName)
      {
         base.ExecuteMethod("ClearPerLifetime", delegate ()
         {
            AspectCache cache = null;
            _accessLock.EnterReadLock();
            try
            {
               cache = Lifetime.GetValue() as AspectCache;
            }
            finally
            {
               _accessLock.ExitReadLock();
            }
            if (cache != null)
            {
               cache.ClearPerInstance(callerName);
            }
         });
      }

      public virtual void ClearInstanceCache()
      {
         base.ExecuteMethod("ClearInstanceCache", delegate ()
         {
            _accessLock.EnterWriteLock();
            try
            {
               this.InstanceCache.Clear();
            }
            finally
            {
               _accessLock.ExitWriteLock();
            }
         });
      }
      public virtual void ClearFoundationCache()
      {
         base.ExecuteMethod("ClearFoundationCache", delegate ()
         {
            AspectCache cache = base.IFoundation.Container.Resolve<AspectCache>();
            cache.ClearInstanceCache();
         });
      }
      public virtual void ClearLifetimeCache()
      {
         base.ExecuteMethod("ClearLifetimeCache", delegate ()
         {
            AspectCache cache = Lifetime.GetValue() as AspectCache;
            if (cache != null)
            {
               cache.ClearInstanceCache();
            }
         });
      }
      public virtual void ClearAll()
      {
         base.ExecuteMethod("ClearAll", delegate ()
         {
            this.ClearInstanceCache();
            this.ClearLifetimeCache();
            this.ClearFoundationCache();
         });
      }

   }
}
