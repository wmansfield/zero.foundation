using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity;
using Zero.Foundation.System;

namespace Zero.Foundation.Web
{
    /// <summary>
    /// A base class for an MVC controller with view support.
    /// </summary>
    [DebuggerStepThrough]
    public class FoundationController : Controller
    {
        [Obsolete("Prefer IoC", true)]
        public FoundationController()
            : this(CoreFoundation.Current)
        {
        }
        public FoundationController(IFoundation iFoundation)
            : this(iFoundation, null)
        {
        }
        public FoundationController(IFoundation iFoundation, IHandleExceptionProvider iHandleExceptionProvider)
        {
            this.IFoundation = iFoundation;
            this.IHandleExceptionProvider = iHandleExceptionProvider;
            if (this.IHandleExceptionProvider == null)
            {
                this.IHandleExceptionProvider = this.IFoundation.Container.Resolve<IHandleExceptionProvider>();
                this.IHandleExceptionProvider.PolicyName = this.GetType().ToString();
            }

        }

        protected virtual IFoundation IFoundation { get; set; }
        protected virtual IHandleExceptionProvider IHandleExceptionProvider { get; set; }

        protected virtual T ExecuteFunction<T>(string methodName, Func<T> function, params object[] parameters)
        {
            return IFoundation.GetAspectCoordinator().WrapFunctionCall<T>(this, methodName, parameters, false, this.IHandleExceptionProvider, function);
        }
        protected virtual Task<T> ExecuteFunctionAsync<T>(string methodName, Func<Task<T>> function, params object[] parameters)
        {
            return IFoundation.GetAspectCoordinator().WrapFunctionCallAsync<T>(this, methodName, parameters, false, this.IHandleExceptionProvider, function);
        }
        protected virtual T ExecuteFunctionThrowing<T>(string methodName, Func<T> function, params object[] parameters)
        {
            return IFoundation.GetAspectCoordinator().WrapFunctionCall<T>(this, methodName, parameters, true, this.IHandleExceptionProvider, function);
        }
        protected virtual Task<T> ExecuteFunctionThrowingAsync<T>(string methodName, Func<Task<T>> function, params object[] parameters)
        {
            return IFoundation.GetAspectCoordinator().WrapFunctionCallAsync<T>(this, methodName, parameters, true, this.IHandleExceptionProvider, function);
        }
        protected virtual void ExecuteMethod(string methodName, Action action, params object[] parameters)
        {
            this.IFoundation.GetAspectCoordinator().WrapMethodCall(this, methodName, parameters, false, this.IHandleExceptionProvider, action);
        }
        protected virtual Task ExecuteMethodAsync(string methodName, Func<Task> action, params object[] parameters)
        {
            return this.IFoundation.GetAspectCoordinator().WrapMethodCallAsync(this, methodName, parameters, false, this.IHandleExceptionProvider, action);
        }
        protected virtual void ExecuteMethodThrowing(string methodName, Action action, params object[] parameters)
        {
            this.IFoundation.GetAspectCoordinator().WrapMethodCall(this, methodName, parameters, true, this.IHandleExceptionProvider, action);
        }
        protected virtual Task ExecuteMethodThrowingAsync(string methodName, Func<Task> action, params object[] parameters)
        {
            return this.IFoundation.GetAspectCoordinator().WrapMethodCallAsync(this, methodName, parameters, true, this.IHandleExceptionProvider, action);
        }
        [Obsolete("Incorrect api call, use the Async Version of this method", true)]
        protected virtual void ExecuteMethod(string methodName, Func<Task> action, params object[] parameters)
        {
            this.IFoundation.GetAspectCoordinator().WrapMethodCall(this, methodName, parameters, false, this.IHandleExceptionProvider, () => action());
        }

    }
}
