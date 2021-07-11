using System;
using System.Collections.Concurrent;
using System.Threading;
using Unity.Lifetime;
using Zero.Foundation.System;

namespace Zero.Foundation.Unity
{
   public class StaticLifetimeManager : LifetimeManager
   {
      public StaticLifetimeManager(string globalKey)
      {
         this.GlobalKey = "StaticLifetimeManager" + globalKey;
      }
      ~StaticLifetimeManager()
      {
         this.Dispose(false);
      }

      
      public object Scope { get; set; }

      protected string GlobalKey { get; set; }
      protected static object _creationRoot = new object();

      protected static ReaderWriterLockSlim _accessLock = new ReaderWriterLockSlim();

      protected static TimeSpan _LockTimeout = TimeSpan.FromMilliseconds(300);
      protected static TimeSpan _LockRemoveTimeout = TimeSpan.FromMilliseconds(50);

      protected override LifetimeManager OnCreateLifetimeManager()
      {
         return new StaticLifetimeManager(this.GlobalKey);
      }
      protected ConcurrentDictionary<string, StaticValue> StaticItems
      {
         get
         {
            if (Single<ConcurrentDictionary<string, StaticValue>>.Instance == null)
            {
               lock (_creationRoot)
               {
                  if (Single<ConcurrentDictionary<string, StaticValue>>.Instance == null)
                  {
                     Single<ConcurrentDictionary<string, StaticValue>>.Instance = new ConcurrentDictionary<string, StaticValue>(StringComparer.OrdinalIgnoreCase);
                  }
               }
            }
            return Single<ConcurrentDictionary<string, StaticValue>>.Instance;
         }
      }

      public override object GetValue(ILifetimeContainer container = null)
      {
         object result = null;

         if (_accessLock.TryEnterReadLock(_LockTimeout))
         {
            try
            {
               if (this.StaticItems.TryGetValue(this.GlobalKey, out StaticValue value))
               {
                  if (value != null)
                  {
                     result = value.Value;
                  }
               }
            }
            finally
            {
               _accessLock.ExitReadLock();
            }
         }

         return result;
      }
      public override void SetValue(object newValue, ILifetimeContainer container = null)
      {
         StaticValue found = null;

         if (_accessLock.TryEnterWriteLock(_LockRemoveTimeout))
         {
            try
            {
               this.StaticItems.TryRemove(this.GlobalKey, out found);
            }
            finally
            {
               _accessLock.ExitWriteLock();
            }
         }

         if (found != null)
         {
            try
            {
               found.Dispose();
            }
            catch { } // gulp
         }
      }

      public void Dispose()
      {
         this.Dispose(true);
         GC.SuppressFinalize(this);
      }
      protected void Dispose(bool disposing)
      {

      }


      public class StaticValue : IDisposable
      {
         public StaticValue(object value)
         {
            this.Value = value;
         }
         ~StaticValue()
         {
            this.Dispose(false);
         }
         public object Value;

         public void Dispose()
         {
            this.Dispose(true);
            GC.SuppressFinalize(this);
         }
         protected void Dispose(bool disposing)
         {
            try
            {
               if (disposing)
               {
                  object value = this.Value;
                  this.Value = null;
                  if (value != null)
                  {
                     IDisposable disposable = value as IDisposable;
                     if (disposable != null)
                     {
                        disposable.Dispose();
                     }
                  }
               }
            }
            catch { }
         }
      }


   }
}
