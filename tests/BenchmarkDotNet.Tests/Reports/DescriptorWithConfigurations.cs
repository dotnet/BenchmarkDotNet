using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Tests.Reports
{
    public class DescriptorWithConfigurations
    {
        public class ExceptionsFrequencyMetricDescriptor : IMetricDescriptor
        {
            private readonly ExceptionDiagnoserConfig? _config;
            public ExceptionsFrequencyMetricDescriptor(ExceptionDiagnoserConfig config = null)
            {
                _config = config;
            }

            public string Id => "ExceptionFrequency";
            public string DisplayName => Column.Exceptions;
            public string Legend => "Exceptions thrown per single operation";
            public string NumberFormat => "#0.0000";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "Count";
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric)
            {
                if (_config == null)
                    return metric.Value > 0;
                else
                    return _config.DisplayExceptionsIfZeroValue || metric.Value > 0;
            }
        }

        public class CompletedWorkItemCountMetricDescriptor : IMetricDescriptor
        {
            private readonly ThreadingDiagnoserConfig? _config;
            public CompletedWorkItemCountMetricDescriptor(ThreadingDiagnoserConfig config = null)
            {
                _config = config;
            }
            public string Id => "CompletedWorkItemCount";
            public string DisplayName => Column.CompletedWorkItems;
            public string Legend => "The number of work items that have been processed in ThreadPool (per single operation)";
            public string NumberFormat => "#0.0000";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "Count";
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric)
            {
                if (_config == null)
                    return metric.Value > 0;
                else
                    return _config.DisplayCompletedWorkItemCountWhenZero || metric.Value > 0;
            }
        }

        public class LockContentionCountMetricDescriptor : IMetricDescriptor
        {
            private readonly ThreadingDiagnoserConfig? _config;
            public LockContentionCountMetricDescriptor(ThreadingDiagnoserConfig config = null)
            {
                _config = config;
            }

            public string Id => "LockContentionCount";
            public string DisplayName => Column.LockContentions;
            public string Legend => "The number of times there was contention upon trying to take a Monitor's lock (per single operation)";
            public string NumberFormat => "#0.0000";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "Count";
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric)
            {
                if (_config == null)
                    return metric.Value > 0;
                else
                    return _config.DisplayLockContentionWhenZero || metric.Value > 0;
            }
        }
    }
}
