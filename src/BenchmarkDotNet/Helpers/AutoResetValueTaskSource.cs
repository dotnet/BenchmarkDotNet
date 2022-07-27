using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace BenchmarkDotNet.Helpers
{
    /// <summary>
    /// Implementation for <see cref="IValueTaskSource{TResult}"/> that will reset itself when awaited so that it can be re-used.
    /// </summary>
    public class AutoResetValueTaskSource<TResult> : IValueTaskSource<TResult>, IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<TResult> _sourceCore;

        /// <summary>Completes with a successful result.</summary>
        /// <param name="result">The result.</param>
        public void SetResult(TResult result) => _sourceCore.SetResult(result);

        /// <summary>Completes with an error.</summary>
        /// <param name="error">The exception.</param>
        public void SetException(Exception error) => _sourceCore.SetException(error);

        /// <summary>Gets the operation version.</summary>
        public short Version => _sourceCore.Version;

        private TResult GetResult(short token)
        {
            // We don't want to reset this if the token is invalid.
            if (token != Version)
            {
                throw new InvalidOperationException();
            }
            try
            {
                return _sourceCore.GetResult(token);
            }
            finally
            {
                _sourceCore.Reset();
            }
        }

        void IValueTaskSource.GetResult(short token) => GetResult(token);
        TResult IValueTaskSource<TResult>.GetResult(short token) => GetResult(token);

        ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _sourceCore.GetStatus(token);
        ValueTaskSourceStatus IValueTaskSource<TResult>.GetStatus(short token) => _sourceCore.GetStatus(token);

        // Don't pass the flags, we don't want to schedule the continuation on the current SynchronizationContext or TaskScheduler if the user runs this in-process, as that may cause a deadlock when this is waited on synchronously.
        // And we don't want to capture the ExecutionContext (we don't use it, and it causes allocations in the full framework).
        void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _sourceCore.OnCompleted(continuation, state, token, ValueTaskSourceOnCompletedFlags.None);
        void IValueTaskSource<TResult>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _sourceCore.OnCompleted(continuation, state, token, ValueTaskSourceOnCompletedFlags.None);
    }
}