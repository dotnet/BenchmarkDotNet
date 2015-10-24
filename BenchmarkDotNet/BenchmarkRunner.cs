using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using BenchmarkDotNet.Export;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Logging;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tasks;
using Microsoft.Build.Execution;
using Microsoft.CSharp;

namespace BenchmarkDotNet
{
    public class BenchmarkRunner
    {
        public BenchmarkRunner(IEnumerable<IBenchmarkLogger> loggers)
        {
            Logger = new BenchmarkCompositeLogger(loggers.ToArray());
            ReportExporter = MarkdownReportExporter.Default;
        }

        public BenchmarkRunner() : this(new[] { new BenchmarkConsoleLogger() })
        {
        }

        public IBenchmarkLogger Logger { get; }
        public IReportExporter ReportExporter { get; }

        private readonly BenchmarkProjectGenerator benchmarkProjectGenerator = new BenchmarkProjectGenerator();

        public IEnumerable<BenchmarkReport> RunCompetition(object benchmarkCompetition, BenchmarkSettings defaultSettings = null)
        {
            return RunCompetition(CompetitionToBenchmarks(benchmarkCompetition, defaultSettings).ToList());
        }

        public IEnumerable<BenchmarkReport> RunUrl(string url, BenchmarkSettings defaultSettings = null)
        {
            return RunCompetition(UrlToBenchmarks(url, defaultSettings).ToList());
        }

        public IEnumerable<BenchmarkReport> RunSource(string source, BenchmarkSettings defaultSettings = null)
        {
            return RunCompetition(SourceToBenchmarks(source, defaultSettings).ToList());
        }

        private IEnumerable<BenchmarkReport> RunCompetition(List<Benchmark> benchmarks)
        {
            benchmarks.Sort((a, b) => string.Compare((a.Task.Configuration.Caption + a.Target.Caption), b.Task.Configuration.Caption + b.Target.Caption, StringComparison.Ordinal));
            Logger.WriteLineHeader("// ***** Competition: Start   *****");
            Logger.WriteLineInfo("// Found benchmarks:");
            foreach (var benchmark in benchmarks)
                Logger.WriteLineInfo($"//   {benchmark.Caption} {benchmark.Task.Settings.ToArgs()}");
            Logger.NewLine();

            var importantPropertyNames = benchmarks.Select(b => b.Properties).GetImportantNames();

            var reports = new List<BenchmarkReport>();
            foreach (var benchmark in benchmarks)
            {
                if (benchmark.Task.Params == null)
                {
                    var report = Run(benchmark, importantPropertyNames);
                    reports.Add(report);
                    if (report.Runs.Count > 0)
                    {
                        var stat = new BenchmarkRunReportsStatistic("Target", report.Runs);
                        Logger.WriteLineResult($"AverageTime (ns/op): {stat.AverageTime}");
                        Logger.WriteLineResult($"OperationsPerSecond: {stat.OperationsPerSeconds}");
                    }
                }
                else
                {
                    var @params = benchmark.Task.Params;
                    foreach (int param in @params.Values)
                    {
                        var report = Run(benchmark, importantPropertyNames, param);
                        reports.Add(report);
                        if (report.Runs.Count > 0)
                        {
                            var stat = new BenchmarkRunReportsStatistic("Target", report.Runs);
                            Logger.WriteLineResult($"AverageTime (ns/op): {stat.AverageTime}");
                            Logger.WriteLineResult($"OperationsPerSecond: {stat.OperationsPerSeconds}");
                        }
                    }
                }
                Logger.NewLine();
            }
            Logger.WriteLineHeader("// ***** Competition: Finish  *****");
            Logger.NewLine();

            ReportExporter.Export(reports, Logger);

            Logger.NewLine();
            Logger.WriteLineHeader("// ***** Competition: End *****");
            return reports;
        }

        private BenchmarkReport Run(Benchmark benchmark, IList<string> importantPropertyNames, int? benchmarkParam = null)
        {
            Logger.WriteLineHeader("// **************************");
            Logger.WriteLineHeader("// Benchmark: " + benchmark.Description);
            var directoryPath = benchmarkProjectGenerator.GenerateProject(benchmark);
            Logger.WriteLineInfo("// Generated project: " + directoryPath);
            Logger.NewLine();
            Logger.WriteLineInfo("// Build:");
            var buildResult = benchmarkProjectGenerator.BuildProject(directoryPath, Logger);
            if (buildResult.OverallResult == BuildResultCode.Success)
            {
                Logger.WriteLineInfo("// OverallResult = Success");
            }
            else
            {
                Logger.WriteLineError("// OverallResult = Failure");
                if (buildResult.Exception != null)
                    Logger.WriteLineError(buildResult.Exception.Message);
                return new BenchmarkReport(benchmark, new BenchmarkRunReport[0]);
            }
            Logger.NewLine();
            var processCount = Math.Max(1, benchmark.Task.ProcessCount);
            var runReports = new List<BenchmarkRunReport>();
            var exeFileName = Path.Combine(directoryPath, "Program.exe");
            for (int processNumber = 0; processNumber < processCount; processNumber++)
            {
                Logger.WriteLineInfo($"// Run, Process: {processNumber + 1} / {processCount}");
                if (benchmarkParam != null)
                    Logger.WriteLineInfo($"// {BenchmarkParams.ParamTitle}={benchmarkParam}");
                if (importantPropertyNames.Any())
                {
                    Logger.WriteInfo("// ");
                    foreach (var name in importantPropertyNames)
                        Logger.WriteInfo($"{name}={benchmark.Properties.GetValue(name)} ");
                    Logger.NewLine();
                }

                var executor = new BenchmarkExecutor(Logger, benchmark.Task.Settings.Runtime);
                if (File.Exists(exeFileName))
                {
                    var args = benchmark.Task.Settings.ToArgs();
                    if (benchmarkParam != null)
                        args += (" " + BenchmarkParams.ParamToArgs(benchmarkParam.Value));
                    var lines = executor.Exec(exeFileName, args);
                    var iterRunReports = lines.Select(line => BenchmarkRunReport.Parse(Logger, line)).Where(r => r != null).ToList();
                    runReports.AddRange(iterRunReports);
                }
            }
            Logger.NewLine();
            return new BenchmarkReport(benchmark, runReports, benchmarkParam);
        }

        private static IEnumerable<Benchmark> CompetitionToBenchmarks(object competition, BenchmarkSettings defaultSettings)
        {
            if (defaultSettings == null)
                defaultSettings = BenchmarkSettings.CreateDefault();
            var targetType = competition.GetType();
            var methods = targetType.GetMethods();
            var setupMethod = methods.FirstOrDefault(m => m.ResolveAttribute<SetupAttribute>() != null);
            if (setupMethod != null)
            {
                // setupMethod is optional, but if it's there it must have the correct signature, accessibility, etc
                AssertMethodHasCorrectSignature("Setup", setupMethod);
                AssertMethodIsAccessible("Setup", setupMethod);
                AssertMethodIsNotDeclaredInGeneric("Setup", setupMethod);
                AssertMethodIsNotGeneric("Setup", setupMethod);
            }

            // If there is one, get the single Field or Property that has the [Params(..)] attribute
            var fields = targetType.GetFields().Select(f => new
                {
                    f.Name,
                    Attribute = f.ResolveAttribute<ParamsAttribute>(),
                    IsStatic = f.IsStatic,
                });
            var properties = targetType.GetProperties().Select(f => new
                {
                    f.Name,
                    Attribute = f.ResolveAttribute<ParamsAttribute>(),
                    IsStatic = f.GetSetMethod().IsStatic
                });
            var fieldOrProperty = fields.Concat(properties).FirstOrDefault(i => i.Attribute != null);

            for (int i = 0; i < methods.Length; i++)
            {
                var methodInfo = methods[i];
                var benchmarkAttribute = methodInfo.ResolveAttribute<BenchmarkAttribute>();
                if (benchmarkAttribute != null)
                {
                    var target = new BenchmarkTarget(targetType, methodInfo, setupMethod, benchmarkAttribute.Description);
                    AssertMethodHasCorrectSignature("Benchmark", methodInfo);
                    AssertMethodIsAccessible("Benchmark", methodInfo);
                    AssertMethodIsNotDeclaredInGeneric("Benchmark", methodInfo);
                    AssertMethodIsNotGeneric("Benchmark", methodInfo);
                    foreach (var task in BenchmarkTask.Resolve(methodInfo, defaultSettings))
                    {
                        if (fieldOrProperty == null)
                        {
                            yield return new Benchmark(target, task);
                        }
                        else
                        {
                            var @params = new BenchmarkParams(fieldOrProperty.Name, fieldOrProperty.IsStatic, fieldOrProperty.Attribute.Args);
                            // All the properties of BenchmarkTask and it's children are immutable, so cloning a BenchmarkTask like this should be safe
                            var newTask = new BenchmarkTask(task.ProcessCount, task.Configuration, task.Settings, @params);
                            yield return new Benchmark(target, newTask);
                        }
                    }
                }
            }
        }

        private static IEnumerable<Benchmark> UrlToBenchmarks(string url, BenchmarkSettings defaultSettings)
        {
            string benchmarkContent = String.Empty;
            try
            {
                var webRequest = WebRequest.Create(url);
                using (var response = webRequest.GetResponse())
                using (var content = response.GetResponseStream())
                using (var reader = new StreamReader(content))
                    benchmarkContent = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(benchmarkContent))
                {
                    Console.WriteLine($"content of '{url}' is empty.");
                    return new Benchmark[0];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
                return new Benchmark[0];
            }
            return SourceToBenchmarks(benchmarkContent, defaultSettings);
        }

        private static IEnumerable<Benchmark> SourceToBenchmarks(string source, BenchmarkSettings defaultSettings)
        {
            string benchmarkContent = source;
            var cSharpCodeProvider = new CSharpCodeProvider();
            var compilerParameters = new CompilerParameters(new[] { "mscorlib.dll", "System.dll", "System.Core.dll" }) { CompilerOptions = "/unsafe" };
            compilerParameters.ReferencedAssemblies.Add(typeof(BenchmarkRunner).Assembly.Location);
            var compilerResults = cSharpCodeProvider.CompileAssemblyFromSource(compilerParameters, benchmarkContent);
            if (compilerResults.Errors.HasErrors)
            {
                compilerResults.Errors.Cast<CompilerError>().ToList().ForEach(error => Console.WriteLine(error.ErrorText));
                yield break;
            }
            foreach (var type in compilerResults.CompiledAssembly.GetTypes())
            {
                var instance = Activator.CreateInstance(type);
                foreach (var benchmark in CompetitionToBenchmarks(instance, defaultSettings))
                {
                    yield return new Benchmark(new BenchmarkTarget(benchmark.Target.Type,
                                                                   benchmark.Target.Method,
                                                                   benchmark.Target.SetupMethod,
                                                                   benchmark.Target.Description,
                                                                   benchmarkContent),
                                               benchmark.Task);
                }
            }
        }

        private static void AssertMethodHasCorrectSignature(string methodType, MethodInfo methodInfo)
        {
            if (methodInfo.GetParameters().Any())
                throw new InvalidOperationException($"{methodType} method {methodInfo.Name} has incorrect signature.\nMethod shouldn't have any arguments.");
        }

        private static void AssertMethodIsAccessible(string methodType, MethodInfo methodInfo)
        {
            if (!methodInfo.IsPublic)
                throw new InvalidOperationException($"{methodType} method {methodInfo.Name} has incorrect access modifiers.\nMethod must be public.");

            var declaringType = methodInfo.DeclaringType;

            while (declaringType != null)
            {
                if (!declaringType.IsPublic && !declaringType.IsNestedPublic)
                    throw new InvalidOperationException($"{methodType} method {methodInfo.Name} defined within type {declaringType.FullName} has incorrect access modifiers.\nDeclaring type must be public.");

                declaringType = declaringType.DeclaringType;
            }
        }

        private static void AssertMethodIsNotDeclaredInGeneric(string methodType, MethodInfo methodInfo)
        {
            var declaringType = methodInfo.DeclaringType;

            while (declaringType != null)
            {
                if (declaringType.IsGenericType)
                    throw new InvalidOperationException($"{methodType} method {methodInfo.Name} defined within generic type {declaringType.FullName}.\n{methodType} methods in generic types are not supported.");

                declaringType = declaringType.DeclaringType;
            }
        }

        private static void AssertMethodIsNotGeneric(string methodType, MethodInfo methodInfo)
        {
            if (methodInfo.IsGenericMethod)
                throw new InvalidOperationException($"{methodType} method {methodInfo.Name} is generic.\nGeneric {methodType} methods are not supported.");
        }
    }
}