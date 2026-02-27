using System;
using System.Globalization;

namespace BenchmarkDotNet.Helpers;

internal static class CultureInfoHelper
{
    public static IDisposable CreateInvariantUICultureScope()
        => new InvariantUICultureScope();

    private class InvariantUICultureScope : IDisposable
    {
        private readonly int savedThreadId;
        private readonly CultureInfo savedCultureInfo;

        public InvariantUICultureScope()
        {
            savedThreadId = Environment.CurrentManagedThreadId;
            savedCultureInfo = CultureInfo.CurrentUICulture;

            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        public void Dispose()
        {
            if (Environment.CurrentManagedThreadId == savedThreadId)
            {
                CultureInfo.CurrentUICulture = savedCultureInfo;
            }
        }
    }
}