using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Tests.XUnit
{
    public static class SmartAssert
    {
        public static void Equal<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual)
        {
            Exception CreateException(string comment)
            {
                var builder = new StringBuilder();
                builder.AppendLine($"Failure ({comment})");
                builder.AppendLine("*** Expected ***");
                for (int i = 0; i < expected.Count; i++)
                    builder.AppendLine($"[{i}]: {expected[i]}");
                builder.AppendLine("*** Actual ***");
                for (int i = 0; i < actual.Count; i++)
                    builder.AppendLine($"[{i}]: {actual[i]}");
                return new Exception(builder.ToString());
            }

            if (expected.Count != actual.Count)
                throw CreateException("Length mismatch");
            for (int i = 0; i < expected.Count; i++)
                if (!expected[i].Equals(actual[i]))
                    throw CreateException($"Element mismatch (index={i})");
        }
    }
}