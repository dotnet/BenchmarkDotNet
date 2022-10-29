using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JsonConvert = SimpleJson.SimpleJson;

namespace BenchmarkDotNet.Toolchains.Snapshot.Stores
{
    /// <summary>
    ///
    /// </summary>
    public class JsonSnapshotStore : ISnapshotStore
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static ISnapshotStore From(string filename)
        {
            return new JsonSnapshotStore(filename);
        }

        private record BenchmarkStoreInfo
        {
            public string? Id { get; init; }
            public string? FullName { get; init; }
            public string? DisplayInfo { get; init; }
            public string? Namespace { get; init; }
            public string? Type { get; init; }
            public string? Method { get; init; }
            public string? MethodTitle { get; init; }
            public string? Parameters { get; init; }
            public BenchmakrStoreExecutionInfo[]? ExecuteResults { get; init; }
        }

        private record BenchmakrStoreExecutionInfo
        {
            public bool FoundExecutable { get; init; }
            public int? ExitCode { get; init; }
            public string[]? Results { get; init; }
            public string[]? StandardOutput { get; init; }
            public string[]? PrefixedLines { get; init; }
            public int LunchIndex { get; init; }
        }

        private record BenchmarkSummaryStoreInfo
        {
            public string? Title { get; init; }
            public BenchmarkStoreInfo[]? Benchmarks { get; init; }
        }

        private BenchmarkSummaryStoreInfo? storeInfo = default;
        private string? artifactsPath;

        internal JsonSnapshotStore(string filepath)
        {
            Filename = filepath;
        }

        /// <summary>
        ///
        /// </summary>
        public string Name { get; }
            = nameof(JsonSnapshotStore);

        /// <summary>
        ///
        /// </summary>
        public string Filename { get; }


        void ISnapshotStore.ExportBegin(ILogger logger)
        {
            artifactsPath = default;
        }

        void ISnapshotStore.Export(Summary summary, ILogger logger)
        {
            var benchmarks = summary.Reports.Select(report =>
               {
                   var list = new List<BenchmakrStoreExecutionInfo>();
                   foreach (var result in report.ExecuteResults.Where(e => e.IsSuccess))
                   {
                       var launchIndex = result.Measurements[0].LaunchIndex;
                       var x = new BenchmakrStoreExecutionInfo()
                       {
                           LunchIndex = launchIndex,
                           Results = result.Results.ToArray(),
                           ExitCode = result.ExitCode,
                           FoundExecutable = result.FoundExecutable,
                           PrefixedLines = result.PrefixedLines.ToArray(),
                       };
                       list.Add(x);
                   }
                   var info = new BenchmarkStoreInfo()
                   {
                       Id = report.BenchmarkCase.Descriptor.ToHash(),
                       FullName = FullNameProvider.GetBenchmarkName(report.BenchmarkCase),
                       DisplayInfo = report.BenchmarkCase.DisplayInfo,
                       Namespace = report.BenchmarkCase.Descriptor.Type.Namespace,
                       Type = FullNameProvider.GetTypeName(report.BenchmarkCase.Descriptor.Type),
                       Method = report.BenchmarkCase.Descriptor.WorkloadMethod.Name,
                       MethodTitle = report.BenchmarkCase.Descriptor.WorkloadMethodDisplayInfo,
                       Parameters = report.BenchmarkCase.Parameters.PrintInfo,
                       ExecuteResults = list.ToArray()
                   };

                   return info;
               });
            storeInfo = new BenchmarkSummaryStoreInfo()
            {
                Title = summary.Title,
                Benchmarks = benchmarks.ToArray(),
            };
            artifactsPath = summary.ResultsDirectoryPath;
        }

        void ISnapshotStore.ExportEnd(ILogger logger)
        {
            var filePath = string.Empty;
            try
            {
                filePath = Path.Combine(artifactsPath ?? string.Empty, Filename);
                File.WriteAllText(filePath, JsonConvert.SerializeObject(storeInfo));
            }
            catch (Exception)
            {
                logger.WriteError($"{nameof(JsonSnapshotStore)} error in generating the file {filePath}");
            }
        }

        ExecuteResult? ISnapshotStore.GetResult(ExecuteParameters executeParameters)
        {
            var id = executeParameters.BenchmarkCase.Descriptor.ToHash();
            if (storeInfo?.Benchmarks?.FirstOrDefault(b => b.Id == id) is BenchmarkStoreInfo benchmarkResult)
            {
                var ai = benchmarkResult.ExecuteResults?.FirstOrDefault(e => e.LunchIndex == executeParameters.LaunchIndex);
                if (ai is { })
                {
                    return new ExecuteResult(ai.FoundExecutable, ai.ExitCode, default, ai.Results, ai.PrefixedLines, ai.StandardOutput, ai.LunchIndex);
                }
            }
            executeParameters.Logger.WriteError($"Cannot find Snapshot for BenchmarkCase {executeParameters.BenchmarkId}");
            return default;
        }

        bool ISnapshotStore.IsSupported(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            if (storeInfo is null)
            {
                var filepath = Path.Combine(benchmarkCase.Config.ArtifactsPath, Filename);
                if (System.IO.File.Exists(filepath))
                {
                    storeInfo = JsonConvert.DeserializeObject<BenchmarkSummaryStoreInfo>(System.IO.File.ReadAllText(Filename));
                }
                else
                {
                    storeInfo = new BenchmarkSummaryStoreInfo();
                }
            }
            var id = benchmarkCase.Descriptor.ToHash();
            var support = storeInfo?.Benchmarks?.FirstOrDefault(b => b.Id == id) is { };
            return support;
        }
    }
}
