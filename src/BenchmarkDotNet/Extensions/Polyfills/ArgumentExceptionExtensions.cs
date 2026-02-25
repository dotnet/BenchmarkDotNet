#if NET6_0 || NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

internal static partial class ArgumentExceptionExtensions
{
    extension(ArgumentException)
    {
        public static void ThrowIfNullOrEmpty(
            string? argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
                throw new ArgumentNullException(paramName);

            if (argument.Length == 0)
                throw new ArgumentException("String cannot be empty.", paramName);
        }

        public static void ThrowIfNullOrWhiteSpace([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentException("String cannot be empty or whitespace.", paramName);
            }
        }
    }
}
#endif
