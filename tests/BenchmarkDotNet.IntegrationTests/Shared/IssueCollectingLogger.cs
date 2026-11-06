using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.IntegrationTests;

/// <summary>
/// Collects the warnings and errors a benchmark run logs, so that a test can fail on them.
/// </summary>
/// <remarks>
/// Warning analyser conclusions are skipped: they describe the measurements and the machine (a tiny Dry run, an antivirus,
/// a virtual machine) rather than a problem with the run itself, and the summary keeps them anyway.
/// </remarks>
internal sealed class IssueCollectingLogger : ILogger
{
    // The header ConclusionHelper prints before its warning conclusions, which last until something else is written.
    private const string WarningConclusionsHeader = "// * Warnings *";

    private readonly List<string> warnings = [];
    private readonly List<string> errors = [];
    private string currentLine = "";
    private bool currentLineHasError;
    private bool inWarningConclusions;

    public string Id => nameof(IssueCollectingLogger);
    public int Priority => 0;

    public IReadOnlyList<string> Warnings => warnings;
    public IReadOnlyList<string> Errors => errors;

    public void Write(LogKind logKind, string text)
    {
        if (logKind != LogKind.Warning)
            inWarningConclusions = false;
        else if (text == WarningConclusionsHeader)
            inWarningConclusions = true;

        if (logKind is LogKind.Warning or LogKind.Error)
            currentLine += text;
        if (logKind == LogKind.Error)
            currentLineHasError = true;
    }

    public void WriteLine() => EndLine();

    public void WriteLine(LogKind logKind, string text)
    {
        Write(logKind, text);
        EndLine();
    }

    public void Flush() { }

    private void EndLine()
    {
        // ConclusionHelper writes an empty line before each of its headers.
        if (currentLine.Length > 0)
        {
            if (currentLineHasError)
                errors.Add(currentLine);
            else if (!inWarningConclusions)
                warnings.Add(currentLine);
        }
        currentLine = "";
        currentLineHasError = false;
    }
}
