using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

// The benchmarks are only found through BenchmarkSwitcher.FromAssembly: nothing else references them, so this also checks
// that the generated registration keeps them from being trimmed and registers their result types.
var artifacts = Path.Combine(AppContext.BaseDirectory, "BenchmarkDotNet.Artifacts");
var config = DefaultConfig.Instance
    .AddJob(Job.Dry)
    .AddExporter(JsonExporter.Full)
    .WithArtifactsPath(artifacts)
    .WithOptions(ConfigOptions.JoinSummary);

var summaries = BenchmarkSwitcher.FromAssembly(typeof(ReturnValues).Assembly).Run(args.Length > 0 ? args : ["--filter", "*"], config).ToArray();

string[] expectedTypes =
[
    "ReturnValues", "CustomStructs", "AsyncResults", "Parameters", "SetupAndCleanup", "Derived", "Memory",
    "Generic<Int32>", "Generic<String>", "Generic<MyStruct>",
];

var failures = new List<string>();
var reports = summaries.SelectMany(summary => summary.Reports).ToArray();

foreach (var error in summaries.SelectMany(summary => summary.ValidationErrors))
    failures.Add($"Validation error: {error.Message}");
foreach (var report in reports.Where(report => !report.Success))
    failures.Add($"Failed: {report.BenchmarkCase.DisplayInfo}");

var ranTypes = reports.Select(report => GetName(report.BenchmarkCase.Descriptor.Type)).ToHashSet();
foreach (var type in expectedTypes.Where(type => !ranTypes.Contains(type)))
    failures.Add($"Not run: {type}");

if (!Directory.Exists(Path.Combine(artifacts, "results")) || !Directory.EnumerateFiles(Path.Combine(artifacts, "results"), "*-full.json").Any())
    failures.Add("The JSON report was not written.");

Console.WriteLine();
Console.WriteLine($"Native AOT host: {reports.Count(report => report.Success)} of {reports.Length} benchmarks ran in {ranTypes.Count} types.");
foreach (var failure in failures)
    Console.WriteLine(failure);

return failures.Count == 0 ? 0 : 1;

static string GetName(Type type)
    => type.IsGenericType
        ? $"{type.Name[..type.Name.IndexOf('`')]}<{string.Join(", ", type.GenericTypeArguments.Select(GetName))}>"
        : type.Name;
