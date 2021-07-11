using Unity;
using Zero.Foundation.System;

namespace Zero.Foundation
{

   /// <summary>
   /// Typically, you should derive from ChokeableClass insead
   /// </summary>
   public abstract class BaseClass
   {
      public BaseClass(IFoundation iFoundation)
      {
         IFoundation = iFoundation;
      }

      protected virtual IFoundation IFoundation { get; set; }
      protected virtual ILogger Logger
      {
         get
         {
            return IFoundation.Container.Resolve<ILogger>();
         }
      }
      protected virtual ITracer Tracer
      {
         get
         {
            return IFoundation.Container.Resolve<ITracer>();
         }
      }

   }
}
