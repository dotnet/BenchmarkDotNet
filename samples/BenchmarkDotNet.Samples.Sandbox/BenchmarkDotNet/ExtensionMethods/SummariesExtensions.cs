using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet;

public static class SummariesExtensions
{
    public static bool HasError(this Summary[] summaries)
    {
        if (summaries.Length == 0)
        {
            var hashSet = new HashSet<string>(["--help", "--list", "--info", "--version"]);

            var args = Environment.GetCommandLineArgs();
            return !args.Any(hashSet.Contains);
        }

        if (summaries.Any(x => x.HasCriticalValidationErrors))
            return true;

        return summaries.Any(x => x.Reports.Any(r => !r.Success));
    }
}

