using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public abstract class ExecutionValidatorBase : IValidator
    {
        protected ExecutionValidatorBase(bool failOnError)
        {
            TreatsWarningsAsErrors = failOnError;
        }

        public bool TreatsWarningsAsErrors { get; }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            var errors = new List<ValidationError>();

            foreach (var typeGroup in validationParameters.Benchmarks.GroupBy(benchmark => benchmark.Descriptor.Type))
            {
                if (!TryCreateBenchmarkTypeInstance(typeGroup.Key, errors, out var benchmarkTypeInstance))
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

                TryToCallGlobalCleanup(benchmarkTypeInstance, errors);
            }

            return errors;
        }

        private bool TryCreateBenchmarkTypeInstance(Type type, List<ValidationError> errors, out object? instance)
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
                    $"Unable to create instance of {type.Name}, exception was: {GetDisplayExceptionMessage(ex)}"));

                instance = null;
                return false;
            }
        }

        private bool TryToCallGlobalSetup(object benchmarkTypeInstance, List<ValidationError> errors)
        {
            return TryToCallGlobalMethod<GlobalSetupAttribute>(benchmarkTypeInstance, errors);
        }

        private void TryToCallGlobalCleanup(object benchmarkTypeInstance, List<ValidationError> errors)
        {
            TryToCallGlobalMethod<GlobalCleanupAttribute>(benchmarkTypeInstance, errors);
        }

        private bool TryToCallGlobalMethod<T>(object benchmarkTypeInstance, List<ValidationError> errors)
        {
            var methods = benchmarkTypeInstance
                .GetType()
                .GetAllMethods()
                .Where(methodInfo => methodInfo.GetCustomAttributes(false).OfType<T>().Any())
                .ToArray();

            if (!methods.Any())
            {
                return true;
            }

            if (methods.Count(methodInfo => !methodInfo.IsVirtual) > 1)
            {
                errors.Add(new ValidationError(
                    TreatsWarningsAsErrors,
                    $"Only single [{GetAttributeName(typeof(T))}] method is allowed per type, type {benchmarkTypeInstance.GetType().Name} has few"));

                return false;
            }

            try
            {
                var result = methods.First().Invoke(benchmarkTypeInstance, null);

                TryToGetTaskResult(result);
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError(
                    TreatsWarningsAsErrors,
                    $"Failed to execute [{GetAttributeName(typeof(T))}] for {benchmarkTypeInstance.GetType().Name}, exception was {GetDisplayExceptionMessage(ex)}"));

                return false;
            }

            return true;
        }

        private string GetAttributeName(Type type) => type.Name.Replace("Attribute", string.Empty);

        private void TryToGetTaskResult(object result)
        {
            if (result == null)
            {
                return;
            }

            var returnType = result.GetType();
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                var asTaskMethod = result.GetType().GetMethod("AsTask");
                result = asTaskMethod.Invoke(result, null);
            }

            if (result is Task task)
            {
                task.GetAwaiter().GetResult();
            }
            else if (result is ValueTask valueTask)
            {
                valueTask.GetAwaiter().GetResult();
            }
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
                        $"Failed to set {paramField.Name} of {benchmarkTypeInstance.GetType().Name} to {values.First()}, exception was: {GetDisplayExceptionMessage(ex)}"));

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
                        $"Failed to set {paramProperty.Name} of {benchmarkTypeInstance.GetType().Name} to {values.First()}, exception was: {GetDisplayExceptionMessage(ex)}"));

                    return false;
                }
            }

            return true;
        }

        protected static string GetDisplayExceptionMessage(Exception ex)
        {
            if (ex is TargetInvocationException targetInvocationException)
                ex = targetInvocationException.InnerException;

            return ex?.Message ?? "Unknown error";
        }

        protected abstract void ExecuteBenchmarks(object benchmarkTypeInstance, IEnumerable<BenchmarkCase> benchmarks, List<ValidationError> errors);
    }
}