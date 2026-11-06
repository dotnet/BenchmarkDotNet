using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.IntegrationTests;

/// <summary>
/// Collects the warnings a benchmark run logs, so that a test can fail on them.
/// </summary>
/// <remarks>
/// Analyser conclusions are skipped: they describe the measurements and the machine (a tiny Dry run, an antivirus,
/// a virtual machine) rather than a problem with the run itself, and the summary keeps them anyway.
/// </remarks>
internal sealed class WarningCollectingLogger : ILogger
{
    // The header ConclusionHelper prints before its warning conclusions, which last until something else is written.
    private const string ConclusionsHeader = "// * Warnings *";

    private readonly List<string> warnings = [];
    private string currentLine = "";
    private bool inConclusions;

    public string Id => nameof(WarningCollectingLogger);
    public int Priority => 0;

    public IReadOnlyList<string> Warnings => warnings;

    public void Write(LogKind logKind, string text)
    {
        Track(logKind, text);
        if (logKind == LogKind.Warning)
            currentLine += text;
    }

    public void WriteLine() => EndLine();

    public void WriteLine(LogKind logKind, string text)
    {
        Write(logKind, text);
        EndLine();
    }

    public void Flush() { }

    private void Track(LogKind logKind, string text)
    {
        if (logKind != LogKind.Warning)
            inConclusions = false;
        else if (text == ConclusionsHeader)
            inConclusions = true;
    }

    private void EndLine()
    {
        // ConclusionHelper writes an empty warning line before its header.
        if (!inConclusions && currentLine.Length > 0)
            warnings.Add(currentLine);
        currentLine = "";
    }
}
