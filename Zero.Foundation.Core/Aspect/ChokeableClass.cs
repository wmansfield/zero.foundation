using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Unity;
using Zero.Foundation.System;

namespace Zero.Foundation.Aspect
{
    [DebuggerStepThrough]
    public class ChokeableClass : BaseClass
    {
        /// <summary>
        /// Dependencies will attempt to be resolved by using IFoundation.Current.Container
        /// </summary>
        public ChokeableClass(IFoundation iFoundation)
            : base(iFoundation)
        {
            if (base.IFoundation != null)
            {
                this.IHandleExceptionProvider = base.IFoundation.Container.Resolve<IHandleExceptionProvider>();
            }
            this.IHandleExceptionProvider.PolicyName = this.GetType().ToString();
        }
        public ChokeableClass(IFoundation iFoundation, IHandleExceptionProvider iHandleExceptionProvider)
            : base(iFoundation)
        {
            this.IHandleExceptionProvider = iHandleExceptionProvider;
            this.IHandleExceptionProvider.PolicyName = this.GetType().ToString();
        }

        public static AsyncLocal<Dictionary<string, object>> AsyncLocalState = new AsyncLocal<Dictionary<string, object>>();
        public static ThreadLocal<Dictionary<string, object>> ThreadLocalState = new ThreadLocal<Dictionary<string, object>>();

        protected IHandleExceptionProvider IHandleExceptionProvider { get; set; }

        protected virtual T ExecuteFunction<T>(string methodName, Func<T> function, params object[] parameters)
        {
            return IFoundation.GetAspectCoordinator().WrapFunctionCall<T>(this, methodName, parameters, false, this.IHandleExceptionProvider, function);
        }
        protected virtual T ExecuteFunction<T>(string methodName, bool forceThrow, Func<T> function, params object[] parameters)
        {
            return IFoundation.GetAspectCoordinator().WrapFunctionCall<T>(this, methodName, parameters, forceThrow, this.IHandleExceptionProvider, function);
        }
        protected virtual void ExecuteMethod(string methodName, Action action, params object[] parameters)
        {
            base.IFoundation.GetAspectCoordinator().WrapMethodCall(this, methodName, parameters, false, this.IHandleExceptionProvider, action);
        }
        protected virtual void ExecuteMethod(string methodName, bool forceThrow, Action action, params object[] parameters)
        {
            base.IFoundation.GetAspectCoordinator().WrapMethodCall(this, methodName, parameters, forceThrow, this.IHandleExceptionProvider, action);
        }

        protected virtual Task ExecuteMethodAsync(string methodName, Func<Task> action, params object[] parameters)
        {
            return this.IFoundation.GetAspectCoordinator().WrapMethodCallAsync(this, methodName, parameters, false, this.IHandleExceptionProvider, action);
        }
        protected virtual Task ExecuteMethodThrowingAsync(string methodName, Func<Task> action, params object[] parameters)
        {
            return this.IFoundation.GetAspectCoordinator().WrapMethodCallAsync(this, methodName, parameters, true, this.IHandleExceptionProvider, action);
        }
        protected virtual Task<T> ExecuteFunctionAsync<T>(string methodName, Func<Task<T>> function, params object[] parameters)
        {
            return IFoundation.GetAspectCoordinator().WrapFunctionCallAsync<T>(this, methodName, parameters, false, this.IHandleExceptionProvider, function);
        }
        protected virtual Task<T> ExecuteFunctionThrowingAsync<T>(string methodName, Func<Task<T>> function, params object[] parameters)
        {
            return IFoundation.GetAspectCoordinator().WrapFunctionCallAsync<T>(this, methodName, parameters, true, this.IHandleExceptionProvider, function);
        }
        [Obsolete("Incorrect api call, use the Async Version of this method", true)]
        protected virtual void ExecuteMethod(string methodName, Func<Task> action, params object[] parameters)
        {
            this.IFoundation.GetAspectCoordinator().WrapMethodCall(this, methodName, parameters, false, this.IHandleExceptionProvider, () => action());
        }
        [Obsolete("Incorrect api call, use the Async Version of this method", true)]
        protected K ExecuteFunction<K>(string methodName, Func<Task<K>> function, params object[] parameters)
        {
            return IFoundation.GetAspectCoordinator().WrapFunctionCallAsync<K>(this, methodName, parameters, true, this.IHandleExceptionProvider, function).Result;
        }
    }
}
