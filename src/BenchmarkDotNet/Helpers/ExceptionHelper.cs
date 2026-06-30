using JetBrains.Annotations;
using System.Reflection;

namespace BenchmarkDotNet.Helpers
{
    [UsedImplicitly]
    public static class ExceptionHelper
    {
        public static bool IsProperCancelation(Exception ex, CancellationToken cancellationToken)
            => cancellationToken.IsCancellationRequested && IsCancelation(ex);

        // On .NET Framework an Exception thrown from a reflection-invoked method is wrapped in a TargetInvocationException.
        internal static bool IsCancelation(Exception ex) =>
            ex is OperationCanceledException ||
            (ex is TargetInvocationException && ex.InnerException is OperationCanceledException);

        public static bool IsOom(Exception ex) =>
            ex is OutOfMemoryException ||
            (ex is TargetInvocationException && ex.InnerException is OutOfMemoryException);
    }
}
