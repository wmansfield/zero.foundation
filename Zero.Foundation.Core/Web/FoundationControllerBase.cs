using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using Unity;
using Zero.Foundation.System;

namespace Zero.Foundation.Web
{
    [DebuggerStepThrough]
    public class FoundationControllerBase : ControllerBase
    {
        [Obsolete("Prefer IoC", true)]
        public FoundationControllerBase()
            : this(CoreFoundation.Current)
        {
        }
        public FoundationControllerBase(IFoundation iFoundation)
            : this(iFoundation, null)
        {
        }
        public FoundationControllerBase(IFoundation iFoundation, IHandleExceptionProvider iHandleExceptionProvider)
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
        protected virtual T ExecuteFunction<T>(string methodName, bool forceThrow, Func<T> function, params object[] parameters)
        {
            return IFoundation.GetAspectCoordinator().WrapFunctionCall<T>(this, methodName, parameters, forceThrow, this.IHandleExceptionProvider, function);
        }
        protected virtual void ExecuteMethod(string methodName, Action action, params object[] parameters)
        {
            this.IFoundation.GetAspectCoordinator().WrapMethodCall(this, methodName, parameters, false, this.IHandleExceptionProvider, action);
        }
        protected virtual void ExecuteMethod(string methodName, bool forceThrow, Action action, params object[] parameters)
        {
            this.IFoundation.GetAspectCoordinator().WrapMethodCall(this, methodName, parameters, forceThrow, this.IHandleExceptionProvider, action);
        }

    }
}
