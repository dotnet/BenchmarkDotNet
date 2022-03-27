using JetBrains.Annotations;
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace BenchmarkDotNet.Helpers
{
    /// <summary>
    /// Implementation for <see cref="IValueTaskSource{TResult}"/> that will reset itself when awaited so that it can be re-used.
    /// </summary>
    public class AutoResetValueTaskSource<TResult> : IValueTaskSource<TResult>, IValueTaskSource
    {
        [CanBeNull] private Action<object> _continuation;
        [CanBeNull] private object _continuationState;
        private bool _completed;
        [CanBeNull] private TResult _result;
        [CanBeNull] private ExceptionDispatchInfo _error;
        private short _version;

        private void Reset()
        {
            // Reset/update state for the next use/await of this instance.
            _version++;
            _completed = false;
            _result = default;
            _error = null;
            _continuation = null;
            _continuationState = null;
        }

        /// <summary>Completes with a successful result.</summary>
        /// <param name="result">The result.</param>
        public void SetResult(TResult result)
        {
            _result = result;
            SignalCompletion();
        }

        /// <summary>Completes with an error.</summary>
        /// <param name="error">The exception.</param>
        public void SetException(Exception error)
        {
            _error = ExceptionDispatchInfo.Capture(error);
            SignalCompletion();
        }

        /// <summary>Gets the operation version.</summary>
        public short Version => _version;

        /// <summary>Gets the status of the operation.</summary>
        /// <param name="token">Opaque value that was provided to the <see cref="ValueTask"/>'s constructor.</param>
        public ValueTaskSourceStatus GetStatus(short token)
        {
            ValidateToken(token);
            return
                _continuation == null || !_completed ? ValueTaskSourceStatus.Pending :
                _error == null ? ValueTaskSourceStatus.Succeeded :
                _error.SourceException is OperationCanceledException ? ValueTaskSourceStatus.Canceled :
                ValueTaskSourceStatus.Faulted;
        }

        /// <summary>Gets the result of the operation.</summary>
        /// <param name="token">Opaque value that was provided to the <see cref="ValueTask"/>'s constructor.</param>
        public TResult GetResult(short token)
        {
            ValidateToken(token);
            if (!_completed)
            {
                throw new InvalidOperationException();
            }

            var result = _result;
            var error = _error;
            Reset();
            error?.Throw();
            return result;
        }

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }
            ValidateToken(token);

            // We need to set the continuation state before we swap in the delegate, so that
            // if there's a race between this and SetResult/Exception and SetResult/Exception
            // sees the _continuation as non-null, it'll be able to invoke it with the state
            // stored here.  However, this also means that if this is used incorrectly (e.g.
            // awaited twice concurrently), _continuationState might get erroneously overwritten.
            // To minimize the chances of that, we check preemptively whether _continuation
            // is already set to something other than the completion sentinel.

            object oldContinuation = _continuation;
            if (oldContinuation == null)
            {
                _continuationState = state;
                oldContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
            }

            if (oldContinuation != null)
            {
                // Operation already completed, so we need to call the supplied callback.
                if (!ReferenceEquals(oldContinuation, AutoResetValueTaskSourceCoreShared.s_sentinel))
                {
                    throw new InvalidOperationException();
                }

                continuation(state);
            }
        }

        private void ValidateToken(short token)
        {
            if (token != _version)
            {
                throw new InvalidOperationException();
            }
        }

        private void SignalCompletion()
        {
            if (_completed)
            {
                throw new InvalidOperationException();
            }
            _completed = true;

            Interlocked.CompareExchange(ref _continuation, AutoResetValueTaskSourceCoreShared.s_sentinel, null)
                ?.Invoke(_continuationState);
        }

        void IValueTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    internal static class AutoResetValueTaskSourceCoreShared // separated out of generic to avoid unnecessary duplication
    {
        internal static readonly Action<object> s_sentinel = CompletionSentinel;
        private static void CompletionSentinel(object _) // named method to aid debugging
        {
            Debug.Fail("The sentinel delegate should never be invoked.");
            throw new InvalidOperationException();
        }
    }
}