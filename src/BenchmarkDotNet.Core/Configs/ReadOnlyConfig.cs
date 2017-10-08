﻿using System;
using System.Collections.Generic;

using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Validators;

using JetBrains.Annotations;

namespace BenchmarkDotNet.Configs
{
    [PublicAPI]
    public class ReadOnlyConfig : IConfig
    {
        #region Fields & .ctor
        private readonly IConfig config;

        public ReadOnlyConfig([NotNull] IConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            this.config = config;
        }
        #endregion

        #region IConfig implementation
        public IEnumerable<IColumnProvider> GetColumnProviders() => config.GetColumnProviders();
        public IEnumerable<IExporter> GetExporters() => config.GetExporters();
        public IEnumerable<ILogger> GetLoggers() => config.GetLoggers();
        public IEnumerable<IDiagnoser> GetDiagnosers() => config.GetDiagnosers();
        public IEnumerable<IAnalyser> GetAnalysers() => config.GetAnalysers();
        public IEnumerable<Job> GetJobs() => config.GetJobs();
        public IEnumerable<IValidator> GetValidators() => config.GetValidators();
        public IEnumerable<HardwareCounter> GetHardwareCounters() => config.GetHardwareCounters();
        public IEnumerable<IFilter> GetFilters() => config.GetFilters();

        public IOrderProvider GetOrderProvider() => config.GetOrderProvider();
        public ISummaryStyle GetSummaryStyle() => config.GetSummaryStyle();

        public ConfigUnionRule UnionRule => config.UnionRule;

        public bool KeepBenchmarkFiles => config.KeepBenchmarkFiles;

        #endregion
    }
}