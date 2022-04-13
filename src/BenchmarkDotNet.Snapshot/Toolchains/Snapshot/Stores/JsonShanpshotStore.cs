using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;
using Newtonsoft.Json;
using Perfolizer.Horology;
using System;
using System.Collections.Generic;
using System.Linq;


namespace BenchmarkDotNet.Toolchains.Snapshot.Stores
{
    public class JsonShanpshotStore : ISnapshotStore
    {
        private static readonly ExecuteResult failedExecuting =
            new ExecuteResult(false, -1, default, Array.Empty<string>(), Array.Empty<string>(), 0);

        private static readonly ExecuteResult succesExecuting =
            new ExecuteResult(true, 0, default, Array.Empty<string>(), Array.Empty<string>(), 0);

        readonly static private System.Reflection.FieldInfo _dataField =
            typeof(ExecuteResult).GetField("data", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        public static ISnapshotStore From(string filename)
        {
            return new JsonShanpshotStore(filename);
        }


        private class BenchmarkStoreInfo
        {
            public string Id { get; set; }
            public string FullName { get; set; }
            public string DisplayInfo { get; set; }
            public string Namespace { get; set; }
            public string Type { get; set; }
            public string Method { get; set; }
            public string MethodTitle { get; set; }
            public string Parameters { get; set; }
            public BenchmakrStoreExecutionInfo[] ExecuteResults { get; set; }
        }

        private class BenchmakrStoreExecutionInfo
        {
            public bool FoundExecutable { get; set; }
            public int? ExitCode { get; set; }
            public string[] Data { get; set; }
            public string[] Errors { get; set; }
            public string[] ExtraOutput { get; set; }
            public int LunchIndex { get; set; }
        }

        private class BenchmarkSummaryStoreInfo
        {
            public string Title { get; set; }
            public BenchmarkStoreInfo[] Benchmarks { get; set; }
        }

        private BenchmarkSummaryStoreInfo? storeInfo;
        private string ResultsDirectoryPath;

        internal JsonShanpshotStore(string filepath)
        {
            Filename = filepath;
        }


        public string Name { get; }
            = nameof(JsonShanpshotStore);

        public string Filename { get; }

        void ISnapshotStore.ExportBegin(ILogger logger)
        {
            storeInfo = new BenchmarkSummaryStoreInfo();
        }

        void ISnapshotStore.Export(Summary summary, ILogger logger)
        {
            if (storeInfo is { })
            {
                ResultsDirectoryPath = summary.ResultsDirectoryPath;
                storeInfo.Title = summary.Title;
                storeInfo.Benchmarks = summary.Reports.Select(report =>
                    {
                        var list = new List<BenchmakrStoreExecutionInfo>();
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
                        };

                        foreach (var result in report.ExecuteResults.Where(e => e.IsSuccess))
                        {
                            var launchIndex = result.Measurements[0].LaunchIndex;
                            var x = new BenchmakrStoreExecutionInfo()
                            {
                                LunchIndex = launchIndex,
                                Errors = result.Errors.ToArray(),
                                ExtraOutput = result.ExtraOutput.ToArray(),
                                ExitCode = result.ExitCode,
                                FoundExecutable = result.FoundExecutable,
                                Data = ((IReadOnlyList<string>)_dataField.GetValue(result)).ToArray(),
                            };
                            list.Add(x);
                        }
                        info.ExecuteResults = list.ToArray();

                        return info;
                    }).ToArray();

            }
        }

        void ISnapshotStore.ExportEnd(ILogger logger)
        {
            System.Diagnostics.Debug.WriteLine(Environment.CurrentDirectory);
            System.IO.File.WriteAllText(Filename, JsonConvert.SerializeObject(storeInfo));
        }

        ExecuteResult? ISnapshotStore.GetResult(ExecuteParameters executeParameters)
        {
            var id = executeParameters.BenchmarkCase.Descriptor.ToHash();
            var benchmarkResult = storeInfo?.Benchmarks?.FirstOrDefault(b => b.Id == id);
            if (benchmarkResult is { })
            {
                var ai = benchmarkResult.ExecuteResults.FirstOrDefault(e => e.LunchIndex == executeParameters.LaunchIndex);
                if (ai is { })
                {
                    return new ExecuteResult(ai.FoundExecutable, ai.ExitCode, default, ai.Data, ai.ExtraOutput, ai.LunchIndex);
                }
            }
            executeParameters.Logger.WriteError($"Cannot find Snapshot for BenchmarkCase {executeParameters.BenchmarkId}");
            return default;
        }

        bool ISnapshotStore.IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver)
        {
            if (storeInfo is null)
            {
                if (System.IO.File.Exists(Filename))
                {
                    storeInfo = JsonConvert.DeserializeObject<BenchmarkSummaryStoreInfo>(System.IO.File.ReadAllText(Filename));
                }
                else
                {
                    storeInfo = new BenchmarkSummaryStoreInfo();
                }
            }
            var id = benchmarkCase.Descriptor.ToHash();
            var suuport = storeInfo?.Benchmarks?.FirstOrDefault(b => b.Id == id) is { };
            return suuport;
        }
    }
}
