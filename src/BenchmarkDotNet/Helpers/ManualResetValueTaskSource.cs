// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using JetBrains.Annotations;
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace BenchmarkDotNet.Helpers
{
    /// <summary>Provides the core logic for implementing a manual-reset <see cref="IValueTaskSource"/> or <see cref="IValueTaskSource{TResult}"/>.</summary>
    /// <typeparam name="TResult"></typeparam>
    [StructLayout(LayoutKind.Auto)]
    public class ManualResetValueTaskSource<TResult> : IValueTaskSource<TResult>, IValueTaskSource
    {
        /// <summary>
        /// The callback to invoke when the operation completes if <see cref="OnCompleted"/> was called before the operation completed,
        /// or <see cref="ManualResetValueTaskSourceCoreShared.s_sentinel"/> if the operation completed before a callback was supplied,
        /// or null if a callback hasn't yet been provided and the operation hasn't yet completed.
        /// </summary>
        [CanBeNull] private Action<object> _continuation;
        /// <summary>State to pass to <see cref="_continuation"/>.</summary>
        [CanBeNull] private object _continuationState;
        /// <summary>Whether the current operation has completed.</summary>
        private bool _completed;
        /// <summary>The result with which the operation succeeded, or the default value if it hasn't yet completed or failed.</summary>
        [CanBeNull] private TResult _result;
        /// <summary>The exception with which the operation failed, or null if it hasn't yet completed or completed successfully.</summary>
        [CanBeNull] private ExceptionDispatchInfo _error;
        /// <summary>The current version of this value, used to help prevent misuse.</summary>
        private short _version;

        /// <summary>Resets to prepare for the next operation.</summary>
        public void Reset()
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

            _error?.Throw();
            return _result;
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
                if (!ReferenceEquals(oldContinuation, ManualResetValueTaskSourceCoreShared.s_sentinel))
                {
                    throw new InvalidOperationException();
                }

                continuation(state);
            }
        }

        /// <summary>Ensures that the specified token matches the current version.</summary>
        /// <param name="token">The token supplied by <see cref="ValueTask"/>.</param>
        private void ValidateToken(short token)
        {
            if (token != _version)
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>Signals that the operation has completed.  Invoked after the result or error has been set.</summary>
        private void SignalCompletion()
        {
            if (_completed)
            {
                throw new InvalidOperationException();
            }
            _completed = true;

            if (_continuation != null || Interlocked.CompareExchange(ref _continuation, ManualResetValueTaskSourceCoreShared.s_sentinel, null) != null)
            {
                InvokeContinuation();
            }
        }

        /// <summary>
        /// Invokes the continuation with the appropriate captured context / scheduler.
        /// This assumes that if <see cref="_executionContext"/> is not null we're already
        /// running within that <see cref="ExecutionContext"/>.
        /// </summary>
        private void InvokeContinuation()
        {
            Debug.Assert(_continuation != null);
            _continuation(_continuationState);
        }

        void IValueTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    internal static class ManualResetValueTaskSourceCoreShared // separated out of generic to avoid unnecessary duplication
    {
        internal static readonly Action<object> s_sentinel = CompletionSentinel;
        private static void CompletionSentinel(object _) // named method to aid debugging
        {
            Debug.Fail("The sentinel delegate should never be invoked.");
            throw new InvalidOperationException();
        }
    }
}