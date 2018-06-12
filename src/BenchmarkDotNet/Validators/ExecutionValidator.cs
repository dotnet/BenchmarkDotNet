using System;
using System.Collections.Generic;
using System.Reflection;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class ExecutionValidator : ExecutionValidatorBase
    {
        public static readonly ExecutionValidator DontFailOnError = new ExecutionValidator(false);
        public static readonly ExecutionValidator FailOnError = new ExecutionValidator(true);

        private ExecutionValidator(bool failOnError)
            : base(failOnError) { }

        protected override void ExecuteBenchmarks(object benchmarkTypeInstance, IEnumerable<Benchmark> benchmarks, List<ValidationError> errors)
        {
            foreach (var benchmark in benchmarks)
            {
                try
                {
                    benchmark.Target.Method.Invoke(benchmarkTypeInstance, null);
                }
                catch (Exception ex)
                {
                    errors.Add(new ValidationError(
                        TreatsWarningsAsErrors,
                        $"Failed to execute benchmark '{benchmark.DisplayInfo}', exception was: '{GetDisplayExceptionMessage(ex)}'",
                        benchmark));
                }
            }
        }
    }
}