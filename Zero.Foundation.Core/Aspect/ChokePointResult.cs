using System;
using System.Diagnostics;

namespace Zero.Foundation.Aspect
{
   [DebuggerStepThrough]
   public partial class ChokePointResult
   {
      public ChokePointResult()
      {
         this.DisplayReason = string.Empty;
      }

      public virtual bool Choke { get; set; }
      public virtual bool ForceResult { get; set; }
      public virtual object NewResult { get; set; }
      public virtual bool PreventChokePropogation { get; set; }

      public object State { get; set; }

      public string DisplayReason { get; set; }
   }

   [DebuggerStepThrough]
   public partial class ChokePointResult<TReturn> : ChokePointResult
   {
      public ChokePointResult()
          : base()
      {
      }

      public new TReturn NewResult { get; set; }
   }
}
