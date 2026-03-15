using JetBrains.Annotations;
using System;
using System.Reflection;

namespace BenchmarkDotNet.Helpers
{
    [UsedImplicitly]
    public static class ExceptionHelper
    {
        public static bool IsCancelation(Exception ex) =>
            ex is OperationCanceledException ||
            (ex.InnerException is TargetInvocationException tie && tie.InnerException is OperationCanceledException);

        public static bool IsOom(Exception ex) =>
            ex is OutOfMemoryException ||
            (ex.InnerException is TargetInvocationException tie && tie.InnerException is OutOfMemoryException);
    }
}
