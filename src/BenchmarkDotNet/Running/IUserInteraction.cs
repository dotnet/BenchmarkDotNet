using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Running
{
    internal interface IUserInteraction
    {
        void PrintNoBenchmarksError(ILogger logger);

        ValueTask PrintWrongFilterInfoAsync(IReadOnlyList<Type> allTypes, ILogger logger, string[] userFilters, CancellationToken cancellationToken);

        IReadOnlyList<Type> AskUser(IReadOnlyList<Type> allTypes, ILogger logger);
    }
}