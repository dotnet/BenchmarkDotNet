using JetBrains.Annotations;
using System.ComponentModel;
using System.Reflection;

namespace BenchmarkDotNet.Helpers
{
    [UsedImplicitly]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ExceptionHelper
    {
        public static bool IsProperCancellation(Exception ex, CancellationToken cancellationToken)
            => cancellationToken.IsCancellationRequested && IsCancellation(ex);

        // On .NET Framework an Exception thrown from a reflection-invoked method is wrapped in a TargetInvocationException.
        internal static bool IsCancellation(Exception ex) =>
            ex is OperationCanceledException ||
            (ex is TargetInvocationException && ex.InnerException is OperationCanceledException);

        public static bool IsOom(Exception ex) =>
            ex is OutOfMemoryException ||
            (ex is TargetInvocationException && ex.InnerException is OutOfMemoryException);
    }
}
