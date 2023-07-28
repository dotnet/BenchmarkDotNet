using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace BenchmarkDotNet.Engines
{
    internal class ExceptionsStats
    {
        internal const string ResultsLinePrefix = "// Exceptions: ";

        private long exceptionsCount;

        internal long ExceptionsCount { get => exceptionsCount; }

        public void StartListening()
        {
            AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
        }

        public void Stop()
        {
            AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
        }

        private void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            Interlocked.Increment(ref exceptionsCount);
        }

        public static string ToOutputLine(double exceptionCount) => $"{ResultsLinePrefix} {exceptionCount}";

        public static double Parse(string line)
        {
            if (!line.StartsWith(ResultsLinePrefix))
                throw new NotSupportedException($"Line must start with {ResultsLinePrefix}");

            var measurement = line.Remove(0, ResultsLinePrefix.Length);
            if (!double.TryParse(measurement, out var exceptionsNumber))
            {
                throw new NotSupportedException("Invalid string");
            }

            return exceptionsNumber;
        }
    }
}
