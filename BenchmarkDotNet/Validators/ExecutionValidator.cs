using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class ExecutionValidator : IValidator
    {
        public static readonly ExecutionValidator DontFailOnError = new ExecutionValidator(false);
        public static readonly ExecutionValidator FailOnError = new ExecutionValidator(true);

        private readonly bool failOnError;

        private ExecutionValidator(bool failOnError)
        {
            this.failOnError = failOnError;
        }

        public IEnumerable<IValidationError> Validate(IList<Benchmark> benchmarks)
        {
            var errors = new List<IValidationError>();

            foreach (var typeGroup in benchmarks.GroupBy(benchmark => benchmark.Target.Type))
            {
                object benchmarkTypeInstance = null;
                if (!TryCreateBenchmarkTypeInstance(typeGroup.Key, typeGroup, errors, out benchmarkTypeInstance))
                {
                    continue;
                }

                if (!TryToSetParamsFields(benchmarkTypeInstance, typeGroup, errors))
                {
                    continue;
                }

                if (!TryToSetParamsProperties(benchmarkTypeInstance, typeGroup, errors))
                {
                    continue;
                }


                if (!TryToCallSetup(benchmarkTypeInstance, typeGroup, errors))
                {
                    continue;
                }

                ExecuteBenchmarks(benchmarkTypeInstance, typeGroup, errors);
            }

            return errors;
        }

        private bool TryCreateBenchmarkTypeInstance(Type type, IEnumerable<Benchmark> benchmarks, List<IValidationError> errors, out object instance)
        {
            try
            {
                instance = Activator.CreateInstance(type);

                return true;
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError(
                    failOnError,
                    $"Unable to create instance of {type.Name}, exception was: {ex.Message}"));

                instance = null;
                return false;
            }
        }

        private bool TryToCallSetup(object benchmarkTypeInstance, IEnumerable<Benchmark> benchmarks, List<IValidationError> errors)
        {
            var setupMethods = benchmarkTypeInstance
                .GetType()
                .GetAllMethods()
                .Where(methodInfo => methodInfo.GetCustomAttributes<SetupAttribute>(true).Any())
                .ToArray();

            if (!setupMethods.Any())
            {
                return true;
            }

            if (setupMethods.Length > 1)
            {
                errors.Add(new ValidationError(
                    failOnError,
                    $"Only single [Setup] method is allowed per type, type {benchmarkTypeInstance.GetType().Name} has few"));

                return false;
            }

            try
            {
                setupMethods.Single().Invoke(benchmarkTypeInstance, null);
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError(
                    failOnError,
                    $"Failed to execute [Setup] for {benchmarkTypeInstance.GetType().Name}, exception was {ex.Message}"));

                return false;
            }

            return true;
        }

        private bool TryToSetParamsFields(object benchmarkTypeInstance, IEnumerable<Benchmark> benchmarks, List<IValidationError> errors)
        {
            var paramFields = benchmarkTypeInstance
                .GetType()
                .GetAllFields()
                .Where(fieldInfo => fieldInfo.GetCustomAttributes<ParamsAttribute>(false).Any())
                .ToArray();

            if (!paramFields.Any())
            {
                return true;
            }

            foreach (var paramField in paramFields)
            {
                if (!paramField.IsPublic)
                {
                    errors.Add(new ValidationError(
                        failOnError,
                        $"Fields marked with [Params] must be public, {paramField.Name} of {benchmarkTypeInstance.GetType().Name} is not"));

                    return false;
                }

                var values = paramField.GetCustomAttributes<ParamsAttribute>(false).Single().Values;
                if (!values.Any())
                {
                    errors.Add(new ValidationError(
                        failOnError,
                        $"Fields marked with [Params] must have some values defined, {paramField.Name} of {benchmarkTypeInstance.GetType().Name} has none"));

                    return false;
                }

                try
                {
                    paramField.SetValue(benchmarkTypeInstance, values.First());
                }
                catch (Exception ex)
                {
                    errors.Add(new ValidationError(
                            failOnError,
                            $"Failed to set {paramField.Name} of {benchmarkTypeInstance.GetType().Name} to {values.First()}, exception was: {ex.Message}"));

                    return false;
                }
            }

            return true;
        }

        private bool TryToSetParamsProperties(object benchmarkTypeInstance, IEnumerable<Benchmark> benchmarks, List<IValidationError> errors)
        {
            var paramProperties = benchmarkTypeInstance
                .GetType()
                .GetAllProperties()
                .Where(propertyInfo => propertyInfo.GetCustomAttributes<ParamsAttribute>(false).Any())
                .ToArray();

            if (!paramProperties.Any())
            {
                return true;
            }

            foreach (var paramProperty in paramProperties)
            {
                var setter = paramProperty.GetSetter();
                if (setter == null || !setter.IsPublic)
                {
                    errors.Add(new ValidationError(
                        failOnError,
                        $"Properties marked with [Params] must have public setter, {paramProperty.Name} of {benchmarkTypeInstance.GetType().Name} has not"));

                    return false;
                }

                var values = paramProperty.GetCustomAttributes<ParamsAttribute>(false).Single().Values;
                if (!values.Any())
                {
                    errors.Add(new ValidationError(
                        failOnError,
                        $"Properties marked with [Params] must have some values defined, {paramProperty.Name} of {benchmarkTypeInstance.GetType().Name} has not"));

                    return false;
                }

                try
                {
                    setter.Invoke(benchmarkTypeInstance, new[] { values.First() });
                }
                catch (Exception ex)
                {
                    errors.Add(new ValidationError(
                        failOnError,
                        $"Failed to set {paramProperty.Name} of {benchmarkTypeInstance.GetType().Name} to {values.First()}, exception was: {ex.Message}"));

                    return false;
                }
            }

            return true;
        }

        private void ExecuteBenchmarks(object benchmarkTypeInstance, IEnumerable<Benchmark> benchmarks, List<IValidationError> errors)
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
                        failOnError,
                        $"Failed to execute benchmark {benchmark.ShortInfo}, exception was: {ex.Message}",
                        benchmark));
                }
            }
        }
    }
}