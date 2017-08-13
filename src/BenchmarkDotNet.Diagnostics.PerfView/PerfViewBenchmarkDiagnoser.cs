using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnostics.PerfView
{
    public class PerfViewBenchmarkDiagnoser : IDiagnoser
    {
        private readonly string tempPath;
        private readonly (string key, string displayName)[] methodColumns;
        private readonly int maxParallelism;
        private readonly bool leaveTraces;

        public PerfViewBenchmarkDiagnoser(string tempPath = null, (string key, string displayName)[] methodColumns = null, int? maxParallelism = null, bool leaveTraces = false)
        {
            this.tempPath = tempPath ?? Path.GetTempPath();
            this.methodColumns = methodColumns ?? new (string key, string displayName)[0];
            this.maxParallelism = maxParallelism ?? Environment.ProcessorCount;
            this.leaveTraces = leaveTraces;
        }

        private Dictionary<Benchmark, string> logFile = new Dictionary<Benchmark, string>();
        private ConcurrentDictionary<Benchmark, float[]> methodTimes = new ConcurrentDictionary<Benchmark, float[]>();
        private ConcurrentQueue<Action> actionQueue = new ConcurrentQueue<Action>();

        private PerfViewHandler.CollectionHandler commandProcessor = null;
        private Benchmark currentBenchmark;

        public IEnumerable<string> Ids => new[] { nameof(PerfViewBenchmarkDiagnoser) };
        
        public void AfterSetup(DiagnoserActionParameters parameters)
        {
        }

        public void BeforeAnythingElse(DiagnoserActionParameters parameters)
        {
        }

        private void ProcessTrace(Dictionary<string, ETWHelper.CallTreeItem> callTree, Benchmark benchmark)
        {
            var times = ETWHelper.ComputeTimeFractions(callTree, methodColumns.Select(t => t.key).ToArray()).ToArray();
            //var serializedTree = callTree.OrderByDescending(k => k.Value.IncSamples).Select(t => $"\"{t.Key}\",{t.Value.IncSamples},{t.Value.Samples}");
            //File.WriteAllLines(logFile[benchmark] + ".methods.csv", serializedTree);
            methodTimes.TryAdd(benchmark, times);
        }

        public void BeforeCleanup()
        {
            var finishDelegate = commandProcessor.StopAndLazyMerge(leaveTraces);
            var benchmark = currentBenchmark;
            if (actionQueue.Count > 8) FlushQueue();
            actionQueue.Enqueue(() => {
                var stacks = finishDelegate();
                ProcessTrace(stacks, benchmark);
            });
            commandProcessor = null;
        }

        void FlushQueue()
        {
            var list = new List<Action>();
            while (actionQueue.TryDequeue(out var a)) list.Add(a);
            Parallel.ForEach(list, a => a());
        }

        public void BeforeMainRun(DiagnoserActionParameters parameters)
        {
            if (commandProcessor != null) throw new Exception("Collection is already running.");

            // workaround: too long file paths
            var folderInfo = parameters.Benchmark.Parameters?.FolderInfo;
            if (string.IsNullOrEmpty(folderInfo)) folderInfo = parameters.Benchmark.FolderInfo;
            folderInfo = new string(folderInfo.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            string path = Path.Combine(tempPath, "benchmarkLogs", (parameters.Benchmark.Parameters?.FolderInfo ?? parameters.Benchmark.FolderInfo).Replace("/", "_") + "_" + Guid.NewGuid().ToString().Replace("-", "_") + ".etl.zip");

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            logFile.Add(parameters.Benchmark, path);
            try
            {
                commandProcessor = PerfViewHandler.StartCollection(path, parameters.Process);
                currentBenchmark = parameters.Benchmark;
            }
            catch(Exception ex)
            {
                logFile.Remove(parameters.Benchmark);
                new CompositeLogger(parameters.Config.GetLoggers().ToArray()).WriteLineError("Could not start ETW trace: " + ex);
            }
        }

        public void DisplayResults(ILogger logger)
        {
        }

        public IColumnProvider GetColumnProvider()
        {
            FlushQueue();
            return new SimpleColumnProvider(
                methodColumns.Select((m, i) => (IColumn)new MethodTimeFractionColumn(m.displayName, methodTimes, i))
                .Concat(new[] { new FileNameColumn(logFile) }.Take(leaveTraces ? 1 : 0))
                .ToArray()
            );
        }

        public void ProcessResults(Benchmark benchmark, BenchmarkReport report)
        {
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            yield break;
        }

        public void AfterGlobalSetup(DiagnoserActionParameters parameters)
        {
        }

        public void BeforeGlobalCleanup()
        {
        }
    }
}
