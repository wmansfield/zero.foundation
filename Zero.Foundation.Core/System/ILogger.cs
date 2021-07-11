using System;
using System.Diagnostics;

namespace Zero.Foundation.System
{
   public interface ILogger
   {
      void Write(object message);
      void Write(object message, string category);
      void Write(object message, string category, int eventId);
      void Write(object message, string category, int eventId, int priority);
      void Write(object message, string category, int eventId, int priority, TraceEventType severity);
   }
}
