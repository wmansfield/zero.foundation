using System;

namespace Zero.Foundation.System
{
    public interface IHandleExceptionProvider
    {
        string PolicyName { get; set; }
        IHandleException CreateHandler();
    }
}
