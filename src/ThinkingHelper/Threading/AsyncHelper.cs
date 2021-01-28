using Nito.AsyncEx;
using System;
using System.Threading.Tasks;

namespace ThinkingHelper.Threading
{
    public static class AsyncHelper
    {
        /// <summary>
        /// Runs a async method synchronously.
        /// </summary>
        /// <param name="func">A function that returns a result</param>
        /// <typeparam name="TResult">Result type</typeparam>
        /// <returns>Result of the async operation</returns>
        public static TResult RunSync<TResult>(Func<Task<TResult>> func) => AsyncContext.Run(func);

        /// <summary>
        /// Runs a async method synchronously.
        /// </summary>
        /// <param name="action">An async action</param>
        public static void RunSync(Func<Task> action) => AsyncContext.Run(action);
    }
}