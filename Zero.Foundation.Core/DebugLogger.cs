using System.Diagnostics;
using Zero.Foundation.System;

namespace Zero.Foundation
{
   public class DebugLogger : ILogger
   {

      #region ILogger Members
      [DebuggerNonUserCode]
      public virtual void Write(object message)
      {
         Trace.WriteLine(FoundationAssumptions.LOG_PREFIX + message);
      }
      [DebuggerNonUserCode]
      public virtual void Write(object message, string category)
      {
         Trace.WriteLine(FoundationAssumptions.LOG_PREFIX + message, category);
      }
      [DebuggerNonUserCode]
      public virtual void Write(object message, string category, int eventId)
      {
         Trace.WriteLine(string.Format(FoundationAssumptions.LOG_PREFIX + "EventID: {0}. Message: {1}", eventId, message), category);
      }
      [DebuggerNonUserCode]
      public virtual void Write(object message, string category, int eventId, int priority)
      {
         Trace.WriteLine(string.Format(FoundationAssumptions.LOG_PREFIX + "EventID: {0}. Priority: {1}. Message: {2}", eventId, priority, message), category);
      }
      [DebuggerNonUserCode]
      public virtual void Write(object message, string category, int eventId, int priority, global::System.Diagnostics.TraceEventType severity)
      {
         Trace.WriteLine(string.Format(FoundationAssumptions.LOG_PREFIX + "Severity: {3}. EventID: {0}. Priority: {1}. Message: {2}", eventId, priority, message, severity.ToString()), category);
      }

      #endregion
   }
}