using System;
using System.Threading.Tasks;
using Zero.Foundation.System;

namespace Zero.Foundation.Aspect
{
   public interface IAspectCoordinator
   {
      void WrapMethodCall(object invoker, string methodName, object[] parameters, bool forceThrow, IHandleExceptionProvider exceptionProvider, Action action);
      Task WrapMethodCallAsync(object invoker, string methodName, object[] parameters, bool forceThrow, IHandleExceptionProvider exceptionProvider, Func<Task> action);
      T WrapFunctionCall<T>(object invoker, string methodName, object[] parameters, bool forceThrow, IHandleExceptionProvider exceptionProvider, Func<T> function);
      Task<T> WrapFunctionCallAsync<T>(object invoker, string methodName, object[] parameters, bool forceThrow, IHandleExceptionProvider exceptionProvider, Func<Task<T>> function);

      ChokePointResult EnterChokePoint(object invoker, EventArgs args);
      ChokePointResult<TReturn> EnterChokePoint<TReturn>(object invoker, EventArgs args);
      ChokePointResult ExitChokePoint(object invoker, EventArgs args);
      ChokePointResult<TReturn> ExitChokePoint<TReturn>(object invoker, EventArgs args);

   }
}
