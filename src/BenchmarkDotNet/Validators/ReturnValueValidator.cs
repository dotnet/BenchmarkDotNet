using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace BenchmarkDotNet.Validators
{
    public class ReturnValueValidator : ExecutionValidatorBase
    {
        public static ReturnValueValidator DontFailOnError { get; } = new ReturnValueValidator(false);
        public static ReturnValueValidator FailOnError { get; } = new ReturnValueValidator(true);

        private ReturnValueValidator(bool failOnError)
            : base(failOnError) { }

        protected override void ExecuteBenchmarks(object benchmarkTypeInstance, IEnumerable<BenchmarkCase> benchmarks, List<ValidationError> errors)
        {
            foreach (var parameterGroup in benchmarks.GroupBy(i => i.Parameters, ParameterInstancesEqualityComparer.Instance))
            {
                var results = new List<(BenchmarkCase benchmark, object returnValue)>();
                bool hasErrorsInGroup = false;

                foreach (var benchmark in parameterGroup.DistinctBy(i => i.Descriptor.WorkloadMethod))
                {
                    try
                    {
                        InProcessNoEmitRunner.FillMembers(benchmarkTypeInstance, benchmark);
                        var result = benchmark.Descriptor.WorkloadMethod.Invoke(benchmarkTypeInstance, null);

                        if (benchmark.Descriptor.WorkloadMethod.ReturnType != typeof(void))
                            results.Add((benchmark, result));
                    }
                    catch (Exception ex)
                    {
                        hasErrorsInGroup = true;

                        errors.Add(new ValidationError(
                            TreatsWarningsAsErrors,
                            $"Failed to execute benchmark '{benchmark.DisplayInfo}', exception was: '{GetDisplayExceptionMessage(ex)}'",
                            benchmark));
                    }
                }

                if (hasErrorsInGroup || results.Count == 0)
                    continue;

                if (results.Any(result => !InDepthEqualityComparer.Instance.Equals(result.returnValue, results[0].returnValue)))
                {
                    errors.Add(new ValidationError(
                        TreatsWarningsAsErrors,
                        $"Inconsistent benchmark return values in {results[0].benchmark.Descriptor.TypeInfo}: {string.Join(", ", results.Select(r => $"{r.benchmark.Descriptor.WorkloadMethodDisplayInfo}: {r.returnValue}"))} {parameterGroup.Key.DisplayInfo}"));
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

                var hashCode = new HashCode();
                foreach (var instance in obj.Items.OrderBy(i => i.Name))
                {
                    hashCode.Add(instance.Name);
                    hashCode.Add(instance.Value);
                }
                return hashCode.ToHashCode();
            }
        }

        private class InDepthEqualityComparer : IEqualityComparer
        {
            public static InDepthEqualityComparer Instance { get; } = new InDepthEqualityComparer();

            [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
            public new bool Equals(object x, object y)
            {
                if (ReferenceEquals(x, y) || object.Equals(x, y))
                    return true;

                if (x == null || y == null)
                    return false;

                return CompareEquatable(x, y) || CompareEquatable(y, x) || CompareStructural(x, y) || CompareStructural(y, x);
            }

            private static bool CompareEquatable(object x, object y)
            {
                var yType = y.GetType();

                var equatableInterface = x.GetType().GetInterfaces().FirstOrDefault(i => i.IsGenericType
                                                                                         && i.GetGenericTypeDefinition() == typeof(IEquatable<>)
                                                                                         && i.GetGenericArguments().Single().IsAssignableFrom(yType));

                if (equatableInterface == null)
                    return false;

                var method = equatableInterface.GetMethod(nameof(IEquatable<object>.Equals), BindingFlags.Public | BindingFlags.Instance);
                return (bool?)method?.Invoke(x, new[] { y }) ?? false;
            }

            private bool CompareStructural(object x, object y)
            {
                if (x is IStructuralEquatable xStructuralEquatable)
                    return xStructuralEquatable.Equals(y, this);

                var xArray = ToStructuralEquatable(x);
                var yArray = ToStructuralEquatable(y);

                if (xArray != null && yArray != null)
                    return Equals(xArray, yArray);

                return false;

                Array ToStructuralEquatable(object obj)
                {
                    switch (obj)
                    {
                        case Array array:
                            return array;

                        case IDictionary dict:
                            return dict.Keys.Cast<object>().OrderBy(k => k).Select(k => (k, dict[k])).ToArray();

                        case IEnumerable enumerable:
                            return enumerable.Cast<object>().ToArray();

                        default:
                            return null;
                    }
                }
            }

            public int GetHashCode(object obj) => StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
        }
    }
}