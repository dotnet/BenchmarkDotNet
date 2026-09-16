using System.Collections.Generic;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.Wasm;

public sealed record WasmSettings : DotNetCliSettings
{
    public static readonly WasmSettings Default = new();

    /// <summary>Full path to a JavaScript engine used to run the benchmarks.</summary>
    public string JavaScriptEngine { get; init; } = "v8";
    /// <summary>Arguments for the JavaScript engine.</summary>
    public string JavaScriptEngineArguments { get; init; } = "";
    /// <summary>Maximum time in minutes to wait for a single benchmark process to finish before force killing it. Default is 10 minutes.</summary>
    public int ProcessTimeoutMinutes { get; init; } = 10;
    /// <summary>
    /// Specifies the IPC mechanism to use. Default is <see cref="WasmIpcType.Auto"/> which automatically detects the JavaScript engine capabilities.
    /// </summary>
    public WasmIpcType IpcType { get; init; } = WasmIpcType.Auto;
    /// <summary>Allows to format or customize the arguments passed to the JavaScript engine.</summary>
    public Func<WasmSettings, ArtifactsPaths, string, string> JavaScriptEngineArgumentFormatter { get; init; } = DefaultArgumentFormatter;
    /// <summary>Optional custom template for the generated main.mjs file.</summary>
    public FileInfo? MainJsTemplate { get; init; }

    public WasmSettings() { }

    internal WasmSettings(CommandLineOptions options) : base(options)
    {
        JavaScriptEngine = options.WasmJavaScriptEngine ?? JavaScriptEngine;
        JavaScriptEngineArguments = options.WasmJavaScriptEngineArguments ?? JavaScriptEngineArguments;
        ProcessTimeoutMinutes = options.WasmProcessTimeoutMinutes;
        MainJsTemplate = options.WasmMainJsTemplate;
    }

    /// <inheritdoc />
    // JavaScriptEngineArgumentFormatter (a delegate) is intentionally not surfaced.
    public override void FillSettings(IDictionary<string, object?> settings)
    {
        base.FillSettings(settings);
        settings[nameof(JavaScriptEngine)] = JavaScriptEngine;
        settings[nameof(JavaScriptEngineArguments)] = JavaScriptEngineArguments;
        settings[nameof(ProcessTimeoutMinutes)] = ProcessTimeoutMinutes;
        settings[nameof(IpcType)] = IpcType;
        settings[nameof(MainJsTemplate)] = MainJsTemplate?.FullName;
    }

    // FileInfo compares by reference; compare MainJsTemplate by value and chain into the base record. The Func compares
    // by delegate equality (the default DefaultArgumentFormatter is a static method, so two defaults are equal).
    public bool Equals(WasmSettings? other)
        => other is not null
            && base.Equals(other)
            && JavaScriptEngine == other.JavaScriptEngine
            && JavaScriptEngineArguments == other.JavaScriptEngineArguments
            && ProcessTimeoutMinutes == other.ProcessTimeoutMinutes
            && IpcType == other.IpcType
            && JavaScriptEngineArgumentFormatter == other.JavaScriptEngineArgumentFormatter
            && MainJsTemplate?.FullName == other.MainJsTemplate?.FullName;

    public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), JavaScriptEngine, JavaScriptEngineArguments, ProcessTimeoutMinutes, IpcType, JavaScriptEngineArgumentFormatter, MainJsTemplate?.FullName);

    private static string DefaultArgumentFormatter(WasmSettings settings, ArtifactsPaths artifactsPaths, string args)
    {
        return Path.GetFileNameWithoutExtension(settings.JavaScriptEngine).ToLower() switch
        {
            "node" or "bun" => $"{settings.JavaScriptEngineArguments} {artifactsPaths.ExecutablePath} -- --run {artifactsPaths.ProgramName}.dll {args}",
            _ => $"{settings.JavaScriptEngineArguments} --module {artifactsPaths.ExecutablePath} -- --run {artifactsPaths.ProgramName}.dll {args}",
        };
    }
}
