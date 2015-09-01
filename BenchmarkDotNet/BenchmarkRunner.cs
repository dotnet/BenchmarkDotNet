using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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
        }

        public BenchmarkRunner() : this(new[] { new BenchmarkConsoleLogger() })
        {
        }

        public IBenchmarkLogger Logger { get; }

        private readonly BenchmarkProjectGenerator benchmarkProjectGenerator = new BenchmarkProjectGenerator();

        public IEnumerable<BenchmarkReport> RunCompetition(object benchmarkCompetition, BenchmarkSettings defaultSettings = null)
        {
            return RunCompetition(CompetitionToBenchmarks(benchmarkCompetition, defaultSettings).ToList());
        }

        public IEnumerable<BenchmarkReport> RunUrl(string url, BenchmarkSettings defaultSettings = null)
        {
            return RunCompetition(UrlToBenchmarks(url, defaultSettings).ToList());
        }

        public IEnumerable<BenchmarkReport> RunCompetition(List<Benchmark> benchmarks)
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
                var report = Run(benchmark, importantPropertyNames);
                reports.Add(report);
                if (report.Runs.Count > 0)
                {
                    var stat = new BenchmarkRunReportsStatistic("Target", report.Runs);
                    Logger.WriteLineResult($"AverageTime (ns/op): {stat.AverageTime}");
                    Logger.WriteLineResult($"OperationsPerSecond: {stat.OperationsPerSeconds}");
                }
                Logger.NewLine();
            }
            Logger.WriteLineHeader("// ***** Competition: Finish  *****");
            Logger.NewLine();
            Logger.WriteLineInfo("```ini");
            Logger.WriteLineInfo(EnvironmentHelper.GetFullEnvironmentInfo("Host", false));
            var reportStats = reports.Where(r => r.Runs.Count > 0).Select(
                r => new
                {
                    r.Benchmark,
                    Stat = new BenchmarkRunReportsStatistic("Target", r.Runs)
                }).ToList();

            // Ensure uniform number formats and use of time units via these helpers.
            var averageTimeStats = reportStats.Select(reportStat => reportStat.Stat);
            var timeToStringFunc = GetTimeMeasurementFormattingFunc(averageTimeStats);
            var opsPerSecToStringFunc = GetOpsPerSecFormattingFunc();

            var table = new List<string[]> { new[] { "Type", "Method", "Mode", "Platform", "Jit", ".NET", "AvrTime", "StdDev", "op/s" } };
            foreach (var reportStat in reportStats)
            {
                var b = reportStat.Benchmark;

                string[] row = {
                    b.Target.Type.Name,
                    b.Target.Method.Name,
                    b.Task.Configuration.Mode.ToString(),
                    b.Task.Configuration.Platform.ToString(),
                    b.Task.Configuration.JitVersion.ToString(),
                    b.Task.Configuration.Framework.ToString(),
                    timeToStringFunc(reportStat.Stat.AverageTime.Median),
                    timeToStringFunc(reportStat.Stat.AverageTime.StandardDeviation),
                    opsPerSecToStringFunc(reportStat.Stat.OperationsPerSeconds.Median)
                };
                table.Add(row);
            }
            PrintTable(table);
            var benchmarksWithTroubles = reports.Where(r => r.Runs.Count == 0).Select(r => r.Benchmark).ToList();
            if (benchmarksWithTroubles.Count > 0)
            {
                Logger.NewLine();
                Logger.WriteLineError("Benchmarks with troubles:");
                foreach (var benchmarkWithTroubles in benchmarksWithTroubles)
                    Logger.WriteLineError("  " + benchmarkWithTroubles.Caption);
            }
            Logger.NewLine();
            Logger.WriteLineHeader("// ***** Competition: End *****");
            return reports;
        }

        private void PrintTable(List<string[]> table)
        {
            int rowCount = table.Count, colCount = table[0].Length;
            int[] widths = new int[colCount];
            bool[] areSame = new bool[colCount];
            for (int colIndex = 0; colIndex < colCount; colIndex++)
            {
                areSame[colIndex] = rowCount > 2 && colIndex < colCount - 3;
                for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    widths[colIndex] = Math.Max(widths[colIndex], table[rowIndex][colIndex].Length + 1);
                    if (rowIndex > 1 && table[rowIndex][colIndex] != table[1][colIndex])
                        areSame[colIndex] = false;
                }
            }
            if (areSame.Any(s => s))
            {
                for (int colIndex = 0; colIndex < colCount; colIndex++)
                    if (areSame[colIndex])
                        Logger.WriteInfo($"{table[0][colIndex]}={table[1][colIndex]}  ");
                Logger.NewLine();
                Logger.WriteLineInfo("```");
                Logger.NewLine();
            }

            table.Insert(1, widths.Select(w => new string('-', w)).ToArray());
            foreach (var row in table)
            {
                for (int colIndex = 0; colIndex < colCount; colIndex++)
                    if (!areSame[colIndex])
                        Logger.WriteStatistic(row[colIndex].PadLeft(widths[colIndex], ' ') + " |");
                Logger.NewLine();
            }
        }

        public BenchmarkReport Run(Benchmark benchmark, IList<string> importantPropertyNames)
        {
            Logger.WriteLineHeader("// **************************");
            Logger.WriteLineHeader("// Benchmark: " + benchmark.Description);
            var directoryPath = benchmarkProjectGenerator.GenerateProject(benchmark);
            Logger.WriteLineInfo("// Generated project: " + directoryPath);
            Logger.NewLine();
            Logger.WriteLineInfo("// Build:");
            var buildResult = benchmarkProjectGenerator.BuildProject(directoryPath);
            if (buildResult.OverallResult == BuildResultCode.Success)
                Logger.WriteLineInfo("// OverallResult = Success");
            else
            {
                Logger.WriteLineError("// OverallResult = Failure");
                return new BenchmarkReport(benchmark, new BenchmarkRunReport[0]);
            }
            Logger.NewLine();
            var processCount = Math.Max(1, benchmark.Task.ProcessCount);
            var runReports = new List<BenchmarkRunReport>();
            var exeFileName = Path.Combine(directoryPath, "Program.exe");
            for (int processNumber = 0; processNumber < processCount; processNumber++)
            {
                Logger.WriteLineInfo($"// Run, Process: {processNumber + 1} / {processCount}");
                if (importantPropertyNames.Any())
                {
                    Logger.WriteInfo("// ");
                    foreach (var name in importantPropertyNames)
                        Logger.WriteInfo($"{name}={benchmark.Properties.GetValue(name)} ");
                    Logger.NewLine();
                }

                var executor = new BenchmarkExecutor(Logger);
                if (File.Exists(exeFileName))
                {
                    var lines = executor.Exec(exeFileName, benchmark.Task.Settings.ToArgs());
                    var iterRunReports = lines.Select(line => BenchmarkRunReport.Parse(Logger, line)).Where(r => r != null).ToList();
                    runReports.AddRange(iterRunReports);
                }
            }
            Logger.NewLine();
            return new BenchmarkReport(benchmark, runReports);
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
                        yield return new Benchmark(target, task);
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
                    yield break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
                yield break;
            }
            var cSharpCodeProvider = new CSharpCodeProvider();
            var compilerParameters = new CompilerParameters(new[] { "mscorlib.dll", "System.Core.dll" }) { CompilerOptions = "/unsafe" };
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

        /// <summary>
        /// Given a list of benchmark statistics creates a function to convert 
        /// raw <see cref="AverageTime"/> measurements to string format so that they
        /// are shown using uniform time units and align nicely.
        /// The <see cref="AverageTime"/> measurements are assumed to contain time lengths in nanoseconds.
        /// </summary>
        /// <param name="statistics">The list of time-based <see cref="BenchmarkRunReportsStatistic"/>.</param>
        /// <returns>A function which should be used to convert all <see cref="AverageTime"/> measurements to string.</returns>
        /// <remarks>
        /// The measurements are formatted in such a way that they use the same time unit 
        /// the number of decimals so that they are easily comparable and align nicely.
        /// 
        /// Example:
        /// Consider we have the following raw input where numbers are durations in nanoseconds: 
        ///     Median=597855, StdErr=485;
        ///     Median=7643, StdErr=87;
        /// 
        /// When using the formatting function, the output will be like this:
        ///     597.8550 us, 0.0485 us;
        ///       7.6430 us, 0.0087 us;
        /// </remarks>
        public static Func<double, string> GetTimeMeasurementFormattingFunc(IEnumerable<BenchmarkRunReportsStatistic> statistics)
        {
            // Find the smallest measurement in the primary statistics, which is the Median.
            // This will determine the time unit we will use for all measurements.
            var minRecordedMedian = statistics.Min(stat => stat.AverageTime.Median);

            // Use the largest unit to display the smallest recorded measurement without loss of precision.
            // TODO: This is duplicated in BenchmarkMeasurementStatistic.ToString(), figure out how to refactor this later?
            Func<double, string> measurementToString;
            if (minRecordedMedian < 1000)
            {
                measurementToString = (value) => string.Format(EnvironmentHelper.MainCultureInfo, "{0:N4} ns", value);
            }
            else if ((minRecordedMedian / 1000) < 1000)
            {
                measurementToString = (value) => string.Format(EnvironmentHelper.MainCultureInfo, "{0:N4} us", value / 1000);
            }
            else if ((minRecordedMedian / 1000 / 1000) < 1000)
            {
                measurementToString = (value) => string.Format(EnvironmentHelper.MainCultureInfo, "{0:N4} ms", value / 1000 / 1000);
            }
            else
            {
                measurementToString = (value) => string.Format(EnvironmentHelper.MainCultureInfo, "{0:N4}  s", value / 1000 / 1000 / 1000);
            }

            return measurementToString;
        }

        ///  <summary>
        /// Given a list of benchmark statistics creates a function to convert 
        /// raw <see cref="OperationsPerSeconds"/> measurements to string format so that they align nicely.
        ///  </summary>
        /// <returns>A function which should be used to convert all <see cref="OperationsPerSeconds"/> measurements to string.</returns>
        /// <remarks>
        ///  Ops/sec number formatting:
        ///       - Thousand separators: we generally expect large numbers so these 
        ///         would make it easier to view.
        ///       - Decimals: Do we really need these? Perhaps we do but only if we have
        ///         really small values to deal with.
        ///  In any case, we would like to have all numbers to be aligned, ideally by decimal point
        ///  but I'm too lazy to do that now, so maybe a compomise of fixed two decimals would do at the mo.
        /// 
        ///  Hence the choice of {N2} formatting string.
        ///  </remarks>
        public static Func<double, string> GetOpsPerSecFormattingFunc()
        {
            return (opsPerSec) => string.Format(EnvironmentHelper.MainCultureInfo, "{0:N2}", opsPerSec);
        }
    }
}