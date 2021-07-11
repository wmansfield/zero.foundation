using System;
using System.Collections.Generic;

namespace Zero.Foundation.System
{
   public class SingleDictionary<TKey, TValue> : Single<IDictionary<TKey, TValue>>
   {
      static SingleDictionary()
      {
         Single<Dictionary<TKey, TValue>>.Instance = new Dictionary<TKey, TValue>();
      }

      public new static IDictionary<TKey, TValue> Instance
      {
         get { return Single<Dictionary<TKey, TValue>>.Instance; }
      }
   }
}
