using System;

namespace Zero.Foundation.System
{
   public interface IHandleException
    {
        string PolicyName { get; set; }
        /// <summary>
        /// Sends an exception through an exception handling policy.
        /// </summary>
        /// <param name="ex">The Exception to process</param>
        /// <param name="replacedException">If set, this should be thrown immediately.</param>
        /// <param name="rethrowCurrent">If true, the error should be rethrown</param>
        /// <returns><c>true</c> if the sub-system successfully processed the request.</returns>
        bool HandleException(Exception ex, out bool rethrowCurrent, out Exception replacedException);
    }
}
