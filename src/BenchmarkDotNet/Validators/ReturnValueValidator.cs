using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess;

namespace BenchmarkDotNet.Validators
{
    public class ReturnValueValidator : ExecutionValidatorBase
    {
        public static ReturnValueValidator DontFailOnError { get; } = new ReturnValueValidator(false);
        public static ReturnValueValidator FailOnError { get; } = new ReturnValueValidator(true);

        private ReturnValueValidator(bool failOnError)
            : base(failOnError) { }

        protected override void ExecuteBenchmarks(object benchmarkTypeInstance, IEnumerable<Benchmark> benchmarks, List<ValidationError> errors)
        {
            foreach (var parameterGroup in benchmarks.GroupBy(i => i.Parameters, ParameterInstancesEqualityComparer.Instance))
            {
                try
                {
                    InProcessRunner.FillMembers(benchmarkTypeInstance, parameterGroup.First());
                }
                catch (Exception ex)
                {
                    errors.Add(new ValidationError(
                        TreatsWarningsAsErrors,
                        $"Failed to set benchmark parameters: '{parameterGroup.First().Parameters.DisplayInfo}', exception was: '{ex.Message}'"));
                }

                var results = new List<(Benchmark benchmark, object returnValue)>();
                bool hasErrorsInGroup = false;

                foreach (var benchmark in parameterGroup)
                {
                    try
                    {
                        var result = benchmark.Target.Method.Invoke(benchmarkTypeInstance, null);

                        if (benchmark.Target.Method.ReturnType != typeof(void))
                            results.Add((benchmark, result));
                    }
                    catch (Exception ex)
                    {
                        hasErrorsInGroup = true;

                        errors.Add(new ValidationError(
                            TreatsWarningsAsErrors,
                            $"Failed to execute benchmark '{benchmark.DisplayInfo}', exception was: '{ex.Message}'",
                            benchmark));
                    }
                }

                if (hasErrorsInGroup || results.Count == 0)
                    continue;

                if (results.Any(result => !Equals(result.returnValue, results[0].returnValue)))
                {
                    errors.Add(new ValidationError(
                        TreatsWarningsAsErrors,
                        $"Inconsistent benchmark return values in {results[0].benchmark.Target.TypeInfo}: {string.Join(", ", results.Select(r => $"{r.benchmark.Target.MethodDisplayInfo}: {r.returnValue}"))} {parameterGroup.Key.DisplayInfo}"));
                }
            }
        }

        private class ParameterInstancesEqualityComparer : IEqualityComparer<ParameterInstances>
        {
            public static ParameterInstancesEqualityComparer Instance { get; } = new ParameterInstancesEqualityComparer();

            public bool Equals(ParameterInstances x, ParameterInstances y)
            {
                if (ReferenceEquals(x, y))
                    return true;

                if (x == null || y == null)
                    return false;

                if (x.Count != y.Count)
                    return false;

                return x.Items.OrderBy(i => i.Name).Zip(y.Items.OrderBy(i => i.Name), (a, b) => a.Name == b.Name && Equals(a.Value, b.Value)).All(i => i);
            }

            public int GetHashCode(ParameterInstances obj)
            {
                if (obj.Count == 0)
                    return 0;

                unchecked
                {
                    int result = 0;

                    foreach (var paramInstance in obj.Items.OrderBy(i => i.Name))
                    {
                        result = (result * 397) ^ paramInstance.Name.GetHashCode();
                        result = (result * 397) ^ (paramInstance.Value?.GetHashCode() ?? 0);
                    }

                    return result;
                }
            }
        }
    }
}