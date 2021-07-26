using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zero.Foundation
{
    public static class AsyncHelper
    {

        public static TResult SyncResult<TResult>(this Task<TResult> task)
        {
            return task.GetAwaiter().GetResult();
        }
        private static readonly TaskFactory _taskFactory = new TaskFactory(CancellationToken.None,
                                                                           TaskCreationOptions.None,
                                                                           TaskContinuationOptions.None,
                                                                           TaskScheduler.Default);

        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            return _taskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        public static void RunSync(Func<Task> func)
        {
            _taskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }
    }
}
