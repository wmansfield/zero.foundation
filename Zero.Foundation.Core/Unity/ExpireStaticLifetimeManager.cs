using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Unity.Lifetime;
using Zero.Foundation.System;

namespace Zero.Foundation.Unity
{
   public class ExpireStaticLifetimeManager : LifetimeManager
   {
      public ExpireStaticLifetimeManager(string globalKey, TimeSpan lifeSpan, bool renewOnAccess = false)
      {
         this.GlobalKey = "ExpireStaticLifetimeManager" + globalKey;
         this.NestedKey = "ExpireStaticLifetimeManager.Nested" + globalKey;

         this.LifeSpan = lifeSpan;
         this.RenewOnAccess = renewOnAccess;
         ExpireStaticLifetimeDaemon.EnsureDaemon();
      }
      ~ExpireStaticLifetimeManager()
      {
         this.Dispose(false);
      }

      protected string GlobalKey { get; set; }
      protected string NestedKey { get; set; }
      protected TimeSpan LifeSpan { get; set; }
      protected bool RenewOnAccess { get; set; }

      protected static object _creationLock = new object();

      private static ReaderWriterLockSlim _accessLock = new ReaderWriterLockSlim();

      protected static TimeSpan _LockTimeout = TimeSpan.FromMilliseconds(300);
      protected static TimeSpan _LockRemoveTimeout = TimeSpan.FromMilliseconds(50);



      public static void CleanExpiredValues()
      {
         try
         {
            ConcurrentDictionary<string, ExpireStaticValue> staticItems = null;
            KeyValuePair<string, ExpireStaticValue>[] values = null;

            if (_accessLock.TryEnterReadLock(_LockTimeout))
            {
               try
               {
                  staticItems = Single<ConcurrentDictionary<string, ExpireStaticValue>>.Instance;
                  if (staticItems != null)
                  {
                     values = staticItems.ToArray();
                  }
               }
               finally
               {
                  _accessLock.ExitReadLock();
               }
            }

            if (staticItems != null && values != null)
            {
               foreach (KeyValuePair<string, ExpireStaticValue> item in values)
               {
                  if (item.Value != null && !item.Value.AllowAccess(false))
                  {
                     ExpireStaticValue expired = null;

                     if (_accessLock.TryEnterWriteLock(_LockRemoveTimeout))
                     {
                        try
                        {
                           staticItems.TryRemove(item.Key, out expired);
                        }
                        finally
                        {
                           _accessLock.ExitWriteLock();
                        }
                     }
                     if (expired != null)
                     {
                        expired.Dispose();
                     }
                  }
               }
            }
         }
         catch
         {
            // gulp
         }
      }


      protected override LifetimeManager OnCreateLifetimeManager()
      {
         return new StaticLifetimeManager(this.GlobalKey);
      }
      protected ConcurrentDictionary<string, ExpireStaticValue> StaticItems
      {
         get
         {
            if (Single<ConcurrentDictionary<string, ExpireStaticValue>>.Instance == null)
            {
               lock (_creationLock)
               {
                  if (Single<ConcurrentDictionary<string, ExpireStaticValue>>.Instance == null)
                  {
                     Single<ConcurrentDictionary<string, ExpireStaticValue>>.Instance = new ConcurrentDictionary<string, ExpireStaticValue>(StringComparer.OrdinalIgnoreCase);
                  }
               }
            }
            return Single<ConcurrentDictionary<string, ExpireStaticValue>>.Instance;
         }
      }


      public bool HasExpired()
      {
         ExpireStaticValue value = null;

         if (_accessLock.TryEnterReadLock(_LockTimeout))
         {

            try
            {
               this.StaticItems.TryGetValue(this.GlobalKey, out value);
            }
            finally
            {
               _accessLock.ExitReadLock();
            }
         }
         if (value != null)
         {
            return !value.AllowAccess(false);
         }
         return true;
      }
      public override object GetValue(ILifetimeContainer container = null)
      {
         object result = null;
         ExpireStaticValue value = null;

         if (_accessLock.TryEnterReadLock(_LockTimeout))
         {
            try
            {
               this.StaticItems.TryGetValue(this.GlobalKey, out value);
            }
            finally
            {
               _accessLock.ExitReadLock();
            }
         }
         if (value != null)
         {
            if (value.AllowAccess(true))
            {
               result = value.Value;
            }
            else
            {
               this.RemoveValue();
            }
         }
         return result;
      }
      public override void RemoveValue(ILifetimeContainer container = null)
      {
         ExpireStaticValue found = null;

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

      public override void SetValue(object newValue, ILifetimeContainer container = null)
      {
         ExpireStaticValue old = null;

         if (_accessLock.TryEnterWriteLock(_LockTimeout))
         {
            try
            {
               this.StaticItems.TryRemove(this.GlobalKey, out old);
               this.StaticItems[this.GlobalKey] = new ExpireStaticValue(this.RenewOnAccess, this.LifeSpan, newValue);
            }
            finally
            {
               _accessLock.ExitWriteLock();
            }
         }
         if (old != null)
         {
            try
            {
               old.Dispose();
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


      public class ExpireStaticValue : IDisposable
      {
         public ExpireStaticValue(bool renewOnAccess, TimeSpan lifeSpan, object value)
         {
            this.LifeSpan = lifeSpan;
            this.RenewOnAccess = renewOnAccess;
            this.Value = value;

            this.RenewLease();
         }
         ~ExpireStaticValue()
         {
            this.Dispose(false);
         }

         public bool RenewOnAccess { get; set; }
         public TimeSpan LifeSpan { get; set; }
         public object Value { get; protected set; }
         public DateTime UtcExpire { get; set; }

         public void RenewLease()
         {
            this.UtcExpire = DateTime.UtcNow.Add(this.LifeSpan);
         }
         public bool AllowAccess(bool renewIfAllowed)
         {
            if (this.UtcExpire < DateTime.UtcNow)
            {
               return false;
            }
            if (renewIfAllowed && this.RenewOnAccess)
            {
               this.RenewLease();
            }
            return true;
         }

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
