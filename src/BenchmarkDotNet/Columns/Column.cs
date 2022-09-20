using JetBrains.Annotations;

namespace BenchmarkDotNet.Columns
{
    // ReSharper disable once InconsistentNaming
    [PublicAPI] // this type is public, so the users can do things like [HideColumns(Column.$)] and get suggestions from IDE
    public static class Column
    {
        public const string Namespace = "Namespace";
        public const string Type = "Type";
        public const string Method = "Method";

        public const string Job = "Job";

        public const string Mean = "Mean";
        public const string StdErr = "StdErr";
        public const string StdDev = "StdDev";
        public const string Error = "Error";
        public const string OperationPerSecond = "Op/s";
        public const string Min = "Min";
        public const string Q1 = "Q1";
        public const string Median = "Median";
        public const string Q3 = "Q3";
        public const string Max = "Max";
        public const string Skewness = "Skewness";
        public const string Kurtosis = "Kurtosis";
        public const string MValue = "MValue";
        public const string Iterations = "Iterations";

        public const string P0 = "P0";
        public const string P25 = "P25";
        public const string P50 = "P50";
        public const string P67 = "P67";
        public const string P80 = "P80";
        public const string P85 = "P85";
        public const string P90 = "P90";
        public const string P95 = "P95";
        public const string P100 = "P100";

        public const string Categories = "Categories";
        public const string LogicalGroup = "LogicalGroup";
        public const string Rank = "Rank";

        public const string Ratio = "Ratio";
        public const string RatioSD = "RatioSD";
        public const string AllocRatio = "Alloc Ratio";

        public const string Allocated = "Allocated";
        public const string Gen0 = "Gen0";
        public const string Gen1 = "Gen1";
        public const string Gen2 = "Gen2";

        public const string AllocatedNativeMemory = "Allocated native memory";
        public const string NativeMemoryLeak = "Native memory leak";
        public const string CompletedWorkItems = "Completed Work Items";
        public const string LockContentions = "Lock Contentions";
        public const string CodeSize = "Code Size";

        //Characteristics:
        public const string Id = "Id";

        public const string MaxRelativeError = "MaxRelativeError";
        public const string MaxAbsoluteError = "MaxAbsoluteError";
        public const string MinIterationTime = "MinIterationTime";
        public const string MinInvokeCount = "MinInvokeCount";
        public const string EvaluateOverhead = "EvaluateOverhead";
        public const string OutlierMode = "OutlierMode";
        public const string AnalyzeLaunchVariance = "AnalyzeLaunchVariance";

        public const string Platform = "Platform";
        public const string Jit = "Jit";
        public const string Runtime = "Runtime";
        public const string Affinity = "Affinity";
        public const string Gc = "Gc";
        public const string EnvironmentVariables = "EnvironmentVariables";
        public const string PowerPlanMode = "PowerPlanMode";

        public const string Server = "Server";
        public const string Concurrent = "Concurrent";
        public const string CpuGroups = "CpuGroups";
        public const string Force = "Force";
        public const string AllowVeryLargeObjects = "AllowVeryLargeObjects";
        public const string RetainVm = "RetainVm";
        public const string NoAffinitize = "NoAffinitize";
        public const string HeapAffinitizeMask = "HeapAffinitizeMask";
        public const string HeapCount = "HeapCount";

        public const string Toolchain = "Toolchain";
        public const string Clock = "Clock";
        public const string EngineFactory = "EngineFactory";
        public const string BuildConfiguration = "BuildConfiguration";
        public const string Arguments = "Arguments";
        public const string NuGetReferences = "NuGetReferences";

        public const string Environment = "Environment";
        public const string Run = "Run";
        public const string Infrastructure = "Infrastructure";
        public const string Accuracy = "Accuracy";
        public const string Meta = "Meta";

        public const string Baseline = "Baseline";
        public const string IsMutator = "IsMutator";
        public const string IsDefault = "IsDefault";

        public const string RunStrategy = "RunStrategy";
        public const string LaunchCount = "LaunchCount";
        public const string InvocationCount = "InvocationCount";
        public const string UnrollFactor = "UnrollFactor";
        public const string IterationCount = "IterationCount";
        public const string MinIterationCount = "MinIterationCount";
        public const string MaxIterationCount = "MaxIterationCount";
        public const string IterationTime = "IterationTime";
        public const string WarmupCount = "WarmupCount";
        public const string MinWarmupIterationCount = "MinWarmupIterationCount";
        public const string MaxWarmupIterationCount = "MaxWarmupIterationCount";
        public const string MemoryRandomization = "MemoryRandomization";
    }
}