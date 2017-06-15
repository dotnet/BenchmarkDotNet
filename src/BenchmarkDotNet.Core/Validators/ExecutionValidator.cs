using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class ExecutionValidator : IValidator
    {
        public static readonly ExecutionValidator DontFailOnError = new ExecutionValidator(false);
        public static readonly ExecutionValidator FailOnError = new ExecutionValidator(true);

        private ExecutionValidator(bool failOnError)
        {
            TreatsWarningsAsErrors = failOnError;
        }

        public bool TreatsWarningsAsErrors { get; }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            var errors = new List<ValidationError>();

            foreach (var typeGroup in validationParameters.Benchmarks.GroupBy(benchmark => benchmark.Target.Type))
            {
                object benchmarkTypeInstance = null;
                if (!TryCreateBenchmarkTypeInstance(typeGroup.Key, errors, out benchmarkTypeInstance))
                {
                    continue;
                }

                if (!TryToSetParamsFields(benchmarkTypeInstance, errors))
                {
                    continue;
                }

                if (!TryToSetParamsProperties(benchmarkTypeInstance, errors))
                {
                    continue;
                }


                if (!TryToCallGlobalSetup(benchmarkTypeInstance, errors))
                {
                    continue;
                }

                ExecuteBenchmarks(benchmarkTypeInstance, typeGroup, errors);
            }

            return errors;
        }

        private bool TryCreateBenchmarkTypeInstance(Type type, List<ValidationError> errors, out object instance)
        {
            try
            {
                instance = Activator.CreateInstance(type);

                return true;
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError(
                    TreatsWarningsAsErrors,
                    $"Unable to create instance of {type.Name}, exception was: {ex.Message}"));

                instance = null;
                return false;
            }
        }

        private bool TryToCallGlobalSetup(object benchmarkTypeInstance, List<ValidationError> errors)
        {
            var globalSetupMethods = benchmarkTypeInstance
                .GetType()
                .GetAllMethods()
                .Where(methodInfo => methodInfo.GetCustomAttributes(false).OfType<GlobalSetupAttribute>().Any())
                .ToArray();

            if (!globalSetupMethods.Any())
            {
                return true;
            }

            if (globalSetupMethods.Count(methodInfo => !methodInfo.IsVirtual) > 1)
            {
                errors.Add(new ValidationError(
                    TreatsWarningsAsErrors,
                    $"Only single [GlobalSetup] method is allowed per type, type {benchmarkTypeInstance.GetType().Name} has few"));

                return false;
            }

            try
            {
                globalSetupMethods.First().Invoke(benchmarkTypeInstance, null);
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError(
                    TreatsWarningsAsErrors,
                    $"Failed to execute [GlobalSetup] for {benchmarkTypeInstance.GetType().Name}, exception was {ex.Message}"));

                return false;
            }

            return true;
        }

        private bool TryToSetParamsFields(object benchmarkTypeInstance, List<ValidationError> errors)
        {
            var paramFields = benchmarkTypeInstance
                .GetType()
                .GetAllFields()
                .Where(fieldInfo => fieldInfo.GetCustomAttributes(false).OfType<ParamsAttribute>().Any())
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
                        TreatsWarningsAsErrors,
                        $"Fields marked with [Params] must be public, {paramField.Name} of {benchmarkTypeInstance.GetType().Name} is not"));

                    return false;
                }

                var values = paramField.GetCustomAttributes(false).OfType<ParamsAttribute>().Single().Values;
                if (!values.Any())
                {
                    errors.Add(new ValidationError(
                        TreatsWarningsAsErrors,
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
                            TreatsWarningsAsErrors,
                            $"Failed to set {paramField.Name} of {benchmarkTypeInstance.GetType().Name} to {values.First()}, exception was: {ex.Message}"));

                    return false;
                }
            }

            return true;
        }

        private bool TryToSetParamsProperties(object benchmarkTypeInstance, List<ValidationError> errors)
        {
            var paramProperties = benchmarkTypeInstance
                .GetType()
                .GetAllProperties()
                .Where(propertyInfo => propertyInfo.GetCustomAttributes(false).OfType<ParamsAttribute>().Any())
                .ToArray();

            if (!paramProperties.Any())
            {
                return true;
            }

            foreach (var paramProperty in paramProperties)
            {
                var setter = paramProperty.SetMethod;
                if (setter == null || !setter.IsPublic)
                {
                    errors.Add(new ValidationError(
                        TreatsWarningsAsErrors,
                        $"Properties marked with [Params] must have public setter, {paramProperty.Name} of {benchmarkTypeInstance.GetType().Name} has not"));

                    return false;
                }

                var values = paramProperty.GetCustomAttributes(false).OfType<ParamsAttribute>().Single().Values;
                if (!values.Any())
                {
                    errors.Add(new ValidationError(
                        TreatsWarningsAsErrors,
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
                        TreatsWarningsAsErrors,
                        $"Failed to set {paramProperty.Name} of {benchmarkTypeInstance.GetType().Name} to {values.First()}, exception was: {ex.Message}"));

                    return false;
                }
            }

            return true;
        }

        private void ExecuteBenchmarks(object benchmarkTypeInstance, IEnumerable<Benchmark> benchmarks, List<ValidationError> errors)
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
                        $"Failed to execute benchmark '{benchmark.DisplayInfo}', exception was: '{ex.Message}'",
                        benchmark));
                }
            }
        }
    }
}