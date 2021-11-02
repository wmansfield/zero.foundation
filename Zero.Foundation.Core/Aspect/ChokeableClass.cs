using System;
using System.Diagnostics;
using System.Threading;
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

        public static AsyncLocal<string> AsyncLocalState = new AsyncLocal<string>();
        public static ThreadLocal<string> ThreadLocalState = new ThreadLocal<string>();

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

    }
}
