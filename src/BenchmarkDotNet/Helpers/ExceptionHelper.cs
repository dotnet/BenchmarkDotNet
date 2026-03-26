using JetBrains.Annotations;
using System.Reflection;

namespace BenchmarkDotNet.Helpers
{
    [UsedImplicitly]
    public static class ExceptionHelper
    {
        public static bool IsProperCancelation(Exception ex, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            return ex is OperationCanceledException ||
                (ex is TargetInvocationException && ex.InnerException is OperationCanceledException);
        }

        public static bool IsOom(Exception ex) =>
            ex is OutOfMemoryException ||
            (ex is TargetInvocationException && ex.InnerException is OutOfMemoryException);
    }
}
