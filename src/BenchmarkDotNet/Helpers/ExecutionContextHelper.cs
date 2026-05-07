namespace BenchmarkDotNet.Helpers;

internal static class ExecutionContextHelper
{
#if NETCOREAPP
    internal static AsyncFlowControl SuppressFlow()
        => ExecutionContext.SuppressFlow();
#else
    // .Net Framework throws if ExecutionContext.SuppressFlow() is called when it's already suppressed.
    internal static WrappedAsyncFlowControl SuppressFlow()
        => ExecutionContext.IsFlowSuppressed() ? default : new WrappedAsyncFlowControl(ExecutionContext.SuppressFlow());

    internal readonly struct WrappedAsyncFlowControl(AsyncFlowControl asyncFlowControl) : IDisposable
    {
        public void Dispose()
        {
            if (asyncFlowControl != default)
            {
                asyncFlowControl.Dispose();
            }
        }
    }
#endif
}
