using System.Collections.Generic;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers
{
    public interface IDiagnoser
    {
        IColumnProvider GetColumnProvider();

        /// <summary>
        /// before jitting, warmup
        /// </summary>
        void BeforeAnythingElse(DiagnoserActionParameters parameters);

        /// <summary>
        /// after setup, before run
        /// </summary>
        void AfterSetup(DiagnoserActionParameters parameters);

        /// <summary>
        /// after setup, warmup and pilot but before the main run
        /// </summary>
        void BeforeMainRun(DiagnoserActionParameters parameters);

        /// <summary>
        /// after run, before cleanup
        /// </summary>
        void BeforeCleanup();

        void ProcessResults(Benchmark benchmark, BenchmarkReport report);

        void DisplayResults(ILogger logger);

        IEnumerable<ValidationError> Validate(ValidationParameters validationParameters);
    }
}
