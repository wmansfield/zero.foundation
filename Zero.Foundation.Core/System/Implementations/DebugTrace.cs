using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Zero.Foundation.System.Implementations
{
   /// <summary>
   /// Writes trace information.
   /// </summary>
   [DebuggerStepThrough]
   public class DebugTrace : IDisposable
   {
      private static MethodBase GetCallingMethod(int totalFramesToSkip)
      {
         return new StackFrame(totalFramesToSkip, false).GetMethod();
      }
      private static Type GetCallingType(int totalFramesToSkip)
      {
         return new StackFrame(totalFramesToSkip, false).GetMethod().DeclaringType;
      }

      private string _category;
      private string _operation;
      private string _identifier;
      private int _stackWalk = 2;
      private ITracer _tracer;


      public DebugTrace(ITracer tracer, int extraStackWalk, string category)
          : this(tracer, extraStackWalk++, category, string.Empty, string.Empty)
      {

      }
      public DebugTrace(ITracer tracer, int extraStackWalk, string category, string operation)
          : this(tracer, extraStackWalk++, category, operation, string.Empty)
      {
      }
      public DebugTrace(ITracer tracer, int extraStackWalk, string category, string operation, string identifier)
      {
         _stackWalk = extraStackWalk + 2; // 2 = static is zero, we are one, caller is two
         _category = category;
         _operation = operation;
         _identifier = identifier;
         _tracer = tracer;


         Trace.WriteLine(string.Format(GenerateFormatString(), "<" + GetCallingType(_stackWalk + 2).ToString(), GetCallingMethod(_stackWalk + 2).Name, ">"), _category);
      }


      public virtual void Dispose()
      {
         Trace.WriteLine(string.Format(GenerateFormatString(), "</" + GetCallingType(_stackWalk).ToString(), GetCallingMethod(_stackWalk).Name, ">"), _category);
         if (_tracer != null)
         {
            _tracer.CurrentDepth--;
         }
      }
      protected string GenerateFormatString()
      {
         string result = FoundationAssumptions.LOG_PREFIX;
         if (_tracer != null)
         {
            result += string.Concat(Enumerable.Repeat(" ", _tracer.CurrentDepth));
         }
         result += "{0}.{1}";

         if (!string.IsNullOrEmpty(_operation))
         {
            if (!string.IsNullOrEmpty(_identifier))
            {
               result += string.Format("({0}:{1})", _identifier, _operation);
            }
            else
            {
               result += string.Format("({0})", _operation);
            }
         }
         else
         {
            if (!string.IsNullOrEmpty(_identifier))
            {
               result += string.Format("({0})", _identifier);
            }
         }
         return result + "{2}";
      }

   }
}